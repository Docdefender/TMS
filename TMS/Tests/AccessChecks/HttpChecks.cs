using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

public static class HttpChecks
{
    public static async Task RunAsync(string connection, int projectId, int taskId, int departmentId)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        var url = new Uri($"http://127.0.0.1:{port}");
        var filename = "access-check-" + Guid.NewGuid().ToString("N") + ".txt";
        var physicalFile = Path.Combine(root, "wwwroot", "uploads", filename);
        var uploadedTicketFiles = new List<string>();
        Directory.CreateDirectory(Path.GetDirectoryName(physicalFile)!);
        await File.WriteAllTextAsync(physicalFile, "private access test");
        int relationProjectId;
        int ticketId;
        await using (var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options))
        {
            db.Attachments.Add(new Attachment { FileName = filename, FilePath = "/uploads/" + filename, ProjectId = projectId, UploadedByUserId = "m1" });
            var relationProject = new Project { Name = "HTTP relation target", DepartmentId = departmentId, ManagerUserId = "m1", CreatedByUserId = "m1" };
            db.Projects.Add(relationProject);
            await db.SaveChangesAsync();
            relationProjectId = relationProject.Id;
            db.ProjectMembers.Add(new ProjectMember { ProjectId = relationProjectId, UserId = "member" });
            var ticket = new Ticket { Subject = "HTTP support ticket", RequesterUserId = "other", SupportDepartmentId = departmentId,
                SlaDueAt = DateTime.UtcNow.AddHours(72),
                Messages = [new TicketMessage { AuthorUserId = "other", Body = "Public ticket message", IsIncoming = true },
                    new TicketMessage { AuthorUserId = "m1", Body = "PRIVATE TICKET NOTE", IsInternal = true }] };
            db.Tickets.Add(ticket);
            var idle = new ApplicationUser { Id = "idle", UserName = "idle@example.test", Email = "idle@example.test",
                NormalizedUserName = "IDLE@EXAMPLE.TEST", NormalizedEmail = "IDLE@EXAMPLE.TEST",
                FullName = "Idle User", SecurityStamp = Guid.NewGuid().ToString() };
            idle.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(idle, "TestPassword123");
            db.Users.Add(idle);
            db.UserRoles.Add(new IdentityUserRole<string> { UserId = idle.Id, RoleId = "Member" });
            await db.SaveChangesAsync();
            ticketId = ticket.Id;
        }
        var info = new ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false,
            CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardOutput = true, RedirectStandardError = true };
#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif
        info.ArgumentList.Add(Path.Combine(root, "bin", configuration, "net8.0", "TMS.dll"));
        info.ArgumentList.Add("--urls"); info.ArgumentList.Add(url.ToString());
        info.Environment["ConnectionStrings__DefaultConnection"] = connection;
        info.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        info.Environment["Logging__LogLevel__Default"] = "Warning";
        info.Environment["TMS_BOOTSTRAP_ADMIN_PASSWORD"] = "DisposableAdmin123!";
        // The product default keeps mail intake closed. This isolated acceptance run enables
        // the retained review-pool feature without supplying Graph credentials.
        info.Environment["Ticketing__Microsoft365__Enabled"] = "true";
        using var process = new Process { StartInfo = info };
        var output = new StringBuilder();
        process.OutputDataReceived += (_, e) => { lock (output) { if (e.Data is not null) output.AppendLine(e.Data); } };
        process.ErrorDataReceived += (_, e) => { lock (output) { if (e.Data is not null) output.AppendLine(e.Data); } };
        process.Start(); process.BeginOutputReadLine(); process.BeginErrorReadLine();
        var count = 0;
        void Check(bool condition, string message) { if (!condition) throw new Exception("HTTP FAILED: " + message); count++; Console.WriteLine("HTTP PASS: " + message); }
        HttpClient Client() => new(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() }) { BaseAddress = url };
        string Token(string html)
        {
            var tag = Regex.Match(html, "<input[^>]*name=\"__RequestVerificationToken\"[^>]*>").Value;
            var token = Regex.Match(tag, "value=\"([^\"]+)\"").Groups[1].Value;
            if (token.Length == 0) throw new Exception("No antiforgery token");
            return WebUtility.HtmlDecode(token);
        }
        async Task<HttpResponseMessage> Post(HttpClient client, string path, string tokenPath, Dictionary<string, string> fields)
        {
            fields["__RequestVerificationToken"] = Token(await client.GetStringAsync(tokenPath));
            return await client.PostAsync(path, new FormUrlEncodedContent(fields));
        }
        bool Forbidden(HttpResponseMessage response) => response.StatusCode == HttpStatusCode.Forbidden
            || response.StatusCode == HttpStatusCode.Redirect && (response.Headers.Location?.ToString().Contains("AccessDenied") ?? false);
        async Task<HttpClient> Login(string id)
        {
            var client = Client();
            var result = await Post(client, "/Account/Login", "/Account/Login", new() { ["Input.Email"] = id + "@example.test", ["Input.Password"] = "TestPassword123" });
            Check(result.StatusCode == HttpStatusCode.Redirect, "Login " + id);
            return client;
        }
        try
        {
            using var anonymous = Client();
            var ready = false;
            for (var attempt = 0; attempt < 100; attempt++)
            {
                if (process.HasExited) throw new Exception("Test app exited: " + output);
                try { ready = (await anonymous.GetAsync("/Account/Login")).IsSuccessStatusCode; } catch (HttpRequestException) { }
                if (ready) break;
                await Task.Delay(200);
            }
            Check(ready, "Isolated application starts");
            Check((await anonymous.GetAsync("/uploads/projects/1/test.txt")).StatusCode == HttpStatusCode.Unauthorized, "Anonymous uploads denied");
            using var member = await Login("member");
            var authenticatedLogin = await member.GetAsync("/Account/Login");
            Check(authenticatedLogin.StatusCode == HttpStatusCode.Redirect && authenticatedLogin.Headers.Location?.ToString() == "/",
                "Authenticated login page redirects to dashboard");
            var profileSave = await Post(member, "/Account/Profile?handler=UpdateProfile", "/Account/Profile", new() { ["Input.FullName"] = "Profile Test Member" });
            Check(profileSave.StatusCode == HttpStatusCode.Redirect, "Profile saves without password fields");
            var invalidProfile = await Post(member, "/Account/Profile?handler=UpdateProfile", "/Account/Profile", new() { ["Input.FullName"] = "" });
            Check(invalidProfile.StatusCode == HttpStatusCode.OK && WebUtility.HtmlDecode(await invalidProfile.Content.ReadAsStringAsync()).Contains("Ad Soyad gereklidir"), "Empty profile name remains invalid");
            var passwordSave = await Post(member, "/Account/Profile?handler=ChangePassword", "/Account/Profile", new() {
                ["PasswordInput.CurrentPassword"] = "TestPassword123", ["PasswordInput.NewPassword"] = "ProfileCheck456!", ["PasswordInput.ConfirmPassword"] = "ProfileCheck456!" });
            Check(passwordSave.StatusCode == HttpStatusCode.Redirect, "Password saves without profile fields in disposable account");
            Check((await member.GetAsync("/uploads/" + filename)).IsSuccessStatusCode, "Project member can download project attachment");
            Check(!(await anonymous.GetAsync("/uploads%5C" + filename)).IsSuccessStatusCode, "Alternate separator cannot bypass private file provider");
            Check(Forbidden(await member.GetAsync("/Projects/Create")), "Member cannot open project creation");
            Check((await member.GetAsync("/Pipeline")).IsSuccessStatusCode, "Member can open general pipeline");
            var memberReport = WebUtility.HtmlDecode(await member.GetStringAsync("/Reports"));
            Check(memberReport.Contains("Erişebildiğiniz çalışmalar") && memberReport.Contains("Görev durumları"),
                "Member report is limited to accessible work");
            var memberPipeline = await member.GetStringAsync("/Pipeline/Details?id=" + projectId + "&viewMode=flow");
            Check(memberPipeline.Contains("Aşama bekleyen görevler") && !memberPipeline.Contains("Yeni aşama ekle"),
                "Member project pipeline is read only");
            Check(memberPipeline.Contains("İlişkiler ve bağımlılıklar") && !memberPipeline.Contains("Proje bağlantısı ekle"),
                "Member project relations are read only");
            var memberTimeline = await member.GetStringAsync("/Pipeline/Details?id=" + projectId + "&viewMode=timeline&scale=week");
            Check(memberTimeline.Contains("Zaman ölçeği") && !memberTimeline.Contains("Görev tarihlerini düzenle"),
                "Member timeline is read only");
            Check(!memberTimeline.Contains("data-pixels-per-day") && !memberTimeline.Contains("pipeline-timeline.js"),
                "Member timeline has no drag controls or drag script");
            var memberHistory = WebUtility.HtmlDecode(await member.GetStringAsync("/Pipeline/Details?id=" + projectId + "&viewMode=history"));
            Check(memberHistory.Contains("Pipeline geçmişi"), "Member can open pipeline history");
            Check(memberHistory.Contains("Aşamalar ve checkpoint"), "Pipeline history shows category filters");
            Check(!memberHistory.Contains("Yeni aşama ekle"), "Member pipeline history contains no management controls");
            var forbiddenStage = await Post(member, "/Pipeline/Details?id=" + projectId + "&handler=AddStage",
                "/Pipeline/Details?id=" + projectId, new() { ["StageName"] = "FORBIDDEN PIPELINE STAGE" });
            Check(Forbidden(forbiddenStage), "Member cannot POST pipeline stage");
            var forbiddenRelation = await Post(member, "/Pipeline/Details?id=" + projectId + "&handler=AddRelation",
                "/Pipeline/Details?id=" + projectId, new() {
                    ["RelationTargetProjectId"] = relationProjectId.ToString(), ["RelationType"] = "Related" });
            Check(Forbidden(forbiddenRelation), "Member cannot POST project relation");
            var forbiddenSchedule = await Post(member, "/Pipeline/Details?id=" + projectId + "&handler=UpdateSchedule&taskId=" + taskId,
                "/Pipeline/Details?id=" + projectId + "&viewMode=timeline", new() {
                    ["plannedStartDate"] = "2026-09-10", ["plannedEndDate"] = "2026-09-12", ["scale"] = "week" });
            Check(Forbidden(forbiddenSchedule), "Member cannot POST timeline schedule");
            var forbiddenCreate = await Post(member, "/Projects/Create", "/Tasks/Create", new() { ["Project.Name"] = "FORBIDDEN", ["Project.DepartmentId"] = departmentId.ToString(), ["Project.ManagerUserId"] = "m1" });
            Check(Forbidden(forbiddenCreate), "Member direct project POST denied");
            var newTask = await Post(member, "/Tasks/Create", "/Tasks/Create?projectId=" + projectId, new() {
                ["TaskItem.Title"] = "HTTP Member task", ["TaskItem.ProjectId"] = projectId.ToString(), ["TaskItem.AssignedToUserId"] = "member", ["TaskItem.Status"] = "ToDo" });
            Check(newTask.StatusCode == HttpStatusCode.Redirect && !Forbidden(newTask), "Member task form saves");
            int httpMemberTaskId;
            await using (var taskDb = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options))
                httpMemberTaskId = await taskDb.TaskItems.Where(x => x.ProjectId == projectId && x.Title == "HTTP Member task").Select(x => x.Id).SingleAsync();
            var forbiddenTaskDependency = await Post(member,
                "/Pipeline/Details?id=" + projectId + "&handler=AddTaskDependency&taskId=" + httpMemberTaskId,
                "/Pipeline/Details?id=" + projectId,
                new() { ["prerequisiteTaskId"] = taskId.ToString() });
            Check(Forbidden(forbiddenTaskDependency), "Member cannot POST task dependency");
            using var manager = await Login("m2");
            Check(WebUtility.HtmlDecode(await manager.GetStringAsync("/Reports")).Contains("Yönettiğiniz ve katıldığınız çalışmalar"),
                "Manager report uses managed and participating scope");
            Check((await manager.GetAsync("/uploads/" + filename)).StatusCode == HttpStatusCode.NotFound, "Task-only access does not grant project attachment access");
            var project = await manager.GetAsync("/Projects/Details/" + projectId);
            Check(project.StatusCode == HttpStatusCode.Redirect && (project.Headers.Location?.ToString().Contains("Basic") ?? false), "Task-only project access redirects to summary");
            var basic = await manager.GetStringAsync(project.Headers.Location);
            Check(basic.Contains("Limited project") && !basic.Contains("SECRET OTHER TASK"), "Project summary contains no other task data");
            Check((await manager.GetAsync("/Tasks/Details/" + taskId)).IsSuccessStatusCode, "External assigned task is viewable");
            var kanban = new HttpRequestMessage(HttpMethod.Post, "/Kanban/Projects?handler=UpdateStatus") {
                Content = new StringContent($"{{\"projectId\":{projectId},\"newStatus\":\"Completed\"}}", Encoding.UTF8, "application/json") };
            kanban.Headers.Add("RequestVerificationToken", Token(await manager.GetStringAsync("/Kanban/Projects")));
            var blocked = await manager.SendAsync(kanban);
            Check(Forbidden(blocked) || blocked.StatusCode == HttpStatusCode.NotFound, "Manager cannot POST Kanban changes");
            using var owner = await Login("m1");
            var managerTimeline = await owner.GetStringAsync("/Pipeline/Details?id=" + projectId + "&viewMode=timeline&scale=week");
            Check(managerTimeline.Contains("pipeline-timeline.js"),
                "Manager timeline loads drag interaction");
            var taskDependencySave = await Post(owner,
                "/Pipeline/Details?id=" + projectId + "&handler=AddTaskDependency&taskId=" + httpMemberTaskId,
                "/Pipeline/Details?id=" + projectId,
                new() { ["prerequisiteTaskId"] = taskId.ToString() });
            Check(taskDependencySave.StatusCode == HttpStatusCode.Redirect && !Forbidden(taskDependencySave),
                "Manager task dependency form saves");
            var dependencyFlow = WebUtility.HtmlDecode(await owner.GetStringAsync("/Pipeline/Details?id=" + projectId + "&viewMode=flow"));
            Check(dependencyFlow.Contains("ön koşul tamamlanmayı bekliyor") && dependencyFlow.Contains("Visible task"),
                "Incomplete task dependency is visible as blocked in pipeline");
            var memberDependencyFlow = WebUtility.HtmlDecode(await member.GetStringAsync("/Pipeline/Details?id=" + projectId + "&viewMode=flow"));
            Check(memberDependencyFlow.Contains("Visible task") && !memberDependencyFlow.Contains("Ön koşul ekle"),
                "Member sees task dependencies without management controls");
            var directTaskHtml = WebUtility.HtmlDecode(await manager.GetStringAsync("/Tasks/Details/" + taskId));
            Check(!directTaskHtml.Contains("HTTP Member task"),
                "Task-only detail does not reveal dependent task title");
            int taskDependencyId;
            await using (var taskDependencyDb = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options))
                taskDependencyId = await taskDependencyDb.TaskDependencies.Where(x => x.DependentTaskId == httpMemberTaskId
                    && x.PrerequisiteTaskId == taskId).Select(x => x.Id).SingleAsync();
            var pipelineStage = await Post(owner, "/Pipeline/Details?id=" + projectId + "&handler=AddStage",
                "/Pipeline/Details?id=" + projectId, new() { ["StageName"] = "HTTP Pipeline Stage", ["StageDescription"] = "HTTP check" });
            Check(pipelineStage.StatusCode == HttpStatusCode.Redirect && !Forbidden(pipelineStage), "Manager pipeline stage form saves");
            int httpStageId;
            await using (var stageDb = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options))
                httpStageId = await stageDb.PipelineStages.Where(x => x.ProjectId == projectId && x.Name == "HTTP Pipeline Stage").Select(x => x.Id).SingleAsync();
            var stageUpdate = await Post(owner, "/Pipeline/Details?id=" + projectId + "&handler=UpdateStage&stageId=" + httpStageId,
                "/Pipeline/Details?id=" + projectId, new() { ["stageName"] = "HTTP Pipeline Stage Updated", ["stageDescription"] = "Updated through HTTP" });
            Check(stageUpdate.StatusCode == HttpStatusCode.Redirect && !Forbidden(stageUpdate), "Manager edits pipeline stage through form");
            var checkpointAdd = await Post(owner, "/Pipeline/Details?id=" + projectId + "&handler=AddCheckpoint&stageId=" + httpStageId,
                "/Pipeline/Details?id=" + projectId, new() { ["CheckpointName"] = "HTTP checkpoint", ["CheckpointDescription"] = "HTTP description", ["RequiresApproval"] = "true" });
            Check(checkpointAdd.StatusCode == HttpStatusCode.Redirect && !Forbidden(checkpointAdd), "Manager checkpoint form saves description");
            int httpCheckpointId;
            await using (var checkpointDb = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options))
                httpCheckpointId = await checkpointDb.PipelineCheckpoints.Where(x => x.PipelineStageId == httpStageId && x.Name == "HTTP checkpoint").Select(x => x.Id).SingleAsync();
            var checkpointUpdate = await Post(owner, "/Pipeline/Details?id=" + projectId + "&handler=UpdateCheckpoint&checkpointId=" + httpCheckpointId,
                "/Pipeline/Details?id=" + projectId, new() { ["checkpointName"] = "HTTP checkpoint updated", ["checkpointDescription"] = "Updated", ["requiresApproval"] = "false" });
            Check(checkpointUpdate.StatusCode == HttpStatusCode.Redirect && !Forbidden(checkpointUpdate), "Manager edits checkpoint through form");
            var dependencySave = await Post(owner, "/Pipeline/Details?id=" + projectId + "&handler=AddRelation",
                "/Pipeline/Details?id=" + projectId, new() { ["RelationTargetProjectId"] = relationProjectId.ToString(),
                    ["RelationType"] = "Dependency", ["RelationCheckpointId"] = httpCheckpointId.ToString() });
            Check(dependencySave.StatusCode == HttpStatusCode.Redirect && !Forbidden(dependencySave),
                "Manager binds project dependency to checkpoint through form");
            int dependencyId;
            await using (var dependencyDb = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options))
                dependencyId = await dependencyDb.ProjectRelations.Where(x => x.SourceProjectId == projectId
                    && x.TargetProjectId == relationProjectId && x.Type == ProjectRelationType.Dependency).Select(x => x.Id).SingleAsync();
            Check((await owner.GetStringAsync("/Pipeline/Details?id=" + projectId)).Contains("HTTP checkpoint updated"),
                "Checkpoint dependency is visible in pipeline");
            var dependencyUpdate = await Post(owner, "/Pipeline/Details?id=" + projectId + "&handler=UpdateRelationCheckpoint&relationId=" + dependencyId,
                "/Pipeline/Details?id=" + projectId, new());
            Check(dependencyUpdate.StatusCode == HttpStatusCode.Redirect && !Forbidden(dependencyUpdate),
                "Manager changes dependency back to project level");
            var checkpointDelete = await Post(owner, "/Pipeline/Details?id=" + projectId + "&handler=DeleteCheckpoint&checkpointId=" + httpCheckpointId,
                "/Pipeline/Details?id=" + projectId, new());
            Check(checkpointDelete.StatusCode == HttpStatusCode.Redirect && !Forbidden(checkpointDelete), "Manager deletes checkpoint through form");
            var stageDelete = await Post(owner, "/Pipeline/Details?id=" + projectId + "&handler=DeleteStage&stageId=" + httpStageId,
                "/Pipeline/Details?id=" + projectId, new());
            Check(stageDelete.StatusCode == HttpStatusCode.Redirect && !Forbidden(stageDelete), "Manager deletes pipeline stage through form");
            var pipelineSchedule = await Post(owner, "/Pipeline/Details?id=" + projectId + "&handler=UpdateSchedule&taskId=" + taskId,
                "/Pipeline/Details?id=" + projectId + "&viewMode=timeline", new() {
                    ["plannedStartDate"] = "2026-09-10", ["plannedEndDate"] = "2026-09-12", ["scale"] = "week" });
            Check(pipelineSchedule.StatusCode == HttpStatusCode.Redirect && !Forbidden(pipelineSchedule),
                "Manager timeline schedule form saves");
            var relationSave = await Post(owner, "/Pipeline/Details?id=" + projectId + "&handler=AddRelation",
                "/Pipeline/Details?id=" + projectId, new() {
                    ["RelationTargetProjectId"] = relationProjectId.ToString(), ["RelationType"] = "Related" });
            Check(relationSave.StatusCode == HttpStatusCode.Redirect && !Forbidden(relationSave),
                "Manager project relation form saves");
            Check((await owner.GetStringAsync("/Pipeline/Details?id=" + projectId + "&viewMode=flow")).Contains("HTTP relation target"),
                "Project relation is visible in pipeline");
            int relationId;
            await using (var relationDb = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options))
                relationId = await relationDb.ProjectRelations.Where(x => x.SourceProjectId == Math.Min(projectId, relationProjectId)
                    && x.TargetProjectId == Math.Max(projectId, relationProjectId) && x.Type == ProjectRelationType.Related)
                    .Select(x => x.Id).SingleAsync();
            var relationRemove = await Post(owner, "/Pipeline/Details?id=" + projectId + "&handler=RemoveRelation&relationId=" + relationId,
                "/Pipeline/Details?id=" + projectId, new());
            Check(relationRemove.StatusCode == HttpStatusCode.Redirect && !Forbidden(relationRemove),
                "Manager removes project relation through pipeline");
            var taskDependencyRemove = await Post(owner,
                "/Pipeline/Details?id=" + projectId + "&handler=RemoveTaskDependency&dependencyId=" + taskDependencyId,
                "/Pipeline/Details?id=" + projectId, new());
            Check(taskDependencyRemove.StatusCode == HttpStatusCode.Redirect && !Forbidden(taskDependencyRemove),
                "Manager removes task dependency through pipeline");
            var ownerHistory = WebUtility.HtmlDecode(await owner.GetStringAsync("/Pipeline/Details?id=" + projectId + "&viewMode=history"));
            Check(ownerHistory.Contains("Aşama silindi") && ownerHistory.Contains("Proje bağlantısı kaldırıldı")
                && ownerHistory.Contains("Görev bağımlılığı eklendi") && ownerHistory.Contains("Görev bağımlılığı kaldırıldı"),
                "Manager sees durable structure, relation and task dependency events in pipeline history");
            var relationHistory = WebUtility.HtmlDecode(await owner.GetStringAsync("/Pipeline/Details?id=" + projectId + "&viewMode=history&historyFilter=relations"));
            Check(relationHistory.Contains("Proje bağlantısı kaldırıldı") && !relationHistory.Contains("Aşama silindi"),
                "Pipeline history filter narrows rendered events");
            Check((await owner.PostAsync("/Projects/Details/" + projectId + "?handler=Status", new FormUrlEncodedContent(new Dictionary<string, string> { ["status"] = "Completed" }))).StatusCode == HttpStatusCode.BadRequest, "Status POST requires antiforgery token");
            var invalidStatus = await Post(owner, "/Projects/Details/" + projectId + "?handler=Status", "/Projects/Details/" + projectId, new() { ["status"] = "invalid" });
            Check(invalidStatus.StatusCode == HttpStatusCode.BadRequest, "Invalid status is not coerced to default enum");
            var saved = await Post(owner, "/Projects/Create", "/Projects/Create", new() {
                ["Project.Name"] = "HTTP project", ["Project.DepartmentId"] = departmentId.ToString(), ["Project.ManagerUserId"] = "m1",
                ["Project.StartDate"] = "2026-09-09", ["Project.Status"] = "NotStarted", ["MemberUserIds"] = "member" });
            Check(saved.StatusCode == HttpStatusCode.Redirect && !Forbidden(saved), "Manager project form saves");
            using var admin = await Login("admin");
            Check(WebUtility.HtmlDecode(await admin.GetStringAsync("/Reports")).Contains("Tüm organizasyon"),
                "Admin report covers the organization");
            var invalidUser = await Post(admin, "/Admin/Users?handler=CreateUser", "/Admin/Users", new() {
                ["NewUser.FullName"] = new string('X', 101), ["NewUser.Email"] = "invalid-email", ["NewUser.Password"] = "ValidPassword123!", ["NewUser.Role"] = "Member" });
            var invalidUserHtml = WebUtility.HtmlDecode(await invalidUser.Content.ReadAsStringAsync());
            Check(invalidUser.StatusCode == HttpStatusCode.OK && invalidUserHtml.Contains("Ad Soyad en fazla 100") && invalidUserHtml.Contains("Geçerli bir e-posta"), "User form rejects oversized name and invalid email in Turkish");
            var validUser = await Post(admin, "/Admin/Users?handler=CreateUser", "/Admin/Users", new() {
                ["NewUser.FullName"] = "Created Member", ["NewUser.Email"] = "created-member@example.test", ["NewUser.Password"] = "ValidPassword123!", ["NewUser.Role"] = "Member" });
            Check(validUser.StatusCode == HttpStatusCode.Redirect, "Admin user creation succeeds");
            await using (var verifyDb = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options))
            {
                var createdId = await verifyDb.Users.Where(x => x.Email == "created-member@example.test").Select(x => x.Id).SingleAsync();
                Check(await (from ur in verifyDb.UserRoles join role in verifyDb.Roles on ur.RoleId equals role.Id where ur.UserId == createdId && role.Name == "Member" select ur).AnyAsync(), "Created user has requested role persisted");
            }
            Check((await admin.GetAsync("/Admin/Organization")).IsSuccessStatusCode, "Organization admin page renders");
            Check((await admin.GetAsync("/Admin/RecycleBin")).IsSuccessStatusCode, "Recycle bin renders user name lookup");
            using var requester = await Login("other");
            var requesterTicket = WebUtility.HtmlDecode(await requester.GetStringAsync("/Tickets/Details/" + ticketId));
            Check(requesterTicket.Contains("Public ticket message") && !requesterTicket.Contains("PRIVATE TICKET NOTE"),
                "Requester sees ticket conversation without internal notes");
            Check((await manager.GetAsync("/Tickets/Details/" + ticketId)).StatusCode == HttpStatusCode.NotFound,
                "Unrelated Manager cannot open ticket");
            var supportTicket = WebUtility.HtmlDecode(await owner.GetStringAsync("/Tickets/Details/" + ticketId));
            Check(supportTicket.Contains("PRIVATE TICKET NOTE") && supportTicket.Contains("Sorumlu ata"),
                "Support department manages ticket and sees internal notes");
            var assignedTicket = await Post(owner, "/Tickets/Details/" + ticketId + "?handler=Assign",
                "/Tickets/Details/" + ticketId, new() { ["userId"] = "member" });
            Check(assignedTicket.StatusCode == HttpStatusCode.Redirect,
                "Support assignment form saves");
            Check((await member.GetStringAsync("/Tickets/Index?filter=assigned")).Contains("HTTP support ticket"),
                "Assigned ticket appears in personal queue");
            Check((await owner.GetStringAsync("/Tickets/Index?filter=all")).Contains("HTTP support ticket")
                && !(await requester.GetStringAsync("/Tickets/Index?filter=all")).Contains("Tüm Ticket'lar"),
                "Support all-Ticket list is available only to support");
            Check((await owner.GetStringAsync("/Tickets/Index?filter=all&search=HTTP%20support")).Contains("HTTP support ticket"),
                "Support can search visible Ticket records");
            var followTicket = await Post(owner, "/Tickets/Details/" + ticketId + "?handler=Following",
                "/Tickets/Details/" + ticketId, new() { ["following"] = "true" });
            Check(followTicket.StatusCode == HttpStatusCode.Redirect
                && (await owner.GetStringAsync("/Tickets/Index?filter=mine")).Contains("HTTP support ticket"),
                "Support can follow ticket in personal list");
            var unfollowTicket = await Post(owner, "/Tickets/Details/" + ticketId + "?handler=Following",
                "/Tickets/Details/" + ticketId, new() { ["following"] = "false" });
            Check(unfollowTicket.StatusCode == HttpStatusCode.Redirect
                && !(await owner.GetStringAsync("/Tickets/Index?filter=mine")).Contains("HTTP support ticket"),
                "Support can leave personal list without losing ticket access");
            Check((await owner.GetStringAsync("/Tickets/Index?filter=all")).Contains("HTTP support ticket"),
                "Unfollowed Ticket remains in support all-Ticket list");
            var converted = await Post(owner, "/Tickets/Details/" + ticketId + "?handler=CreateTask",
                "/Tickets/Details/" + ticketId, new() { ["projectId"] = projectId.ToString(),
                    ["title"] = "HTTP restricted ticket task", ["description"] = "Private task work" });
            Check(converted.StatusCode == HttpStatusCode.Redirect && !Forbidden(converted),
                "Support converts ticket into a project task through form");
            int linkedTaskId;
            await using (var verifyDb = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options))
                linkedTaskId = (await verifyDb.Tickets.Where(x => x.Id == ticketId).Select(x => x.LinkedTaskId).SingleAsync())!.Value;
            var requesterTask = WebUtility.HtmlDecode(await requester.GetStringAsync("/Tasks/Details/" + linkedTaskId));
            Check(requesterTask.Contains("Kaynak Ticket") && requesterTask.Contains("HTTP restricted ticket task")
                && !requesterTask.Contains("Düzenle"), "Requester reads linked task without edit controls");
            Check((await manager.GetAsync("/Tasks/Details/" + linkedTaskId)).StatusCode == HttpStatusCode.NotFound,
                "Unrelated Manager cannot open linked task directly");
            var unknownPage = await owner.GetStringAsync("/Tickets/Unmatched");
            Check(unknownPage.Contains("Eşleşmeyen gönderenler"), "Support can open unmatched sender pool");
            var staged = await Post(owner, "/Tickets/Unmatched?handler=Stage", "/Tickets/Unmatched",
                new() { ["SenderEmail"] = "unknown.http@example.test", ["Subject"] = "Unmatched request",
                    ["Body"] = "Original unmatched message", ["SupportDepartmentId"] = departmentId.ToString() });
            Check(staged.StatusCode == HttpStatusCode.Redirect
                && (await owner.GetStringAsync("/Tickets/Unmatched")).Contains("unknown.http@example.test"),
                "Unknown sender is retained in review pool");
            Check(Forbidden(await requester.GetAsync("/Tickets/Unmatched")),
                "Requester cannot open unmatched sender pool");
            async Task<HttpResponseMessage> UploadTicketFile(bool internalFile, string name)
            {
                using var form = new MultipartFormDataContent();
                form.Add(new StringContent(Token(await owner.GetStringAsync("/Tickets/Details/" + ticketId))),
                    "__RequestVerificationToken");
                if (internalFile) form.Add(new StringContent("true"), "isInternal");
                form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("ticket file content")), "file", name);
                return await owner.PostAsync("/Tickets/Details/" + ticketId + "?handler=Upload", form);
            }
            Check((await UploadTicketFile(false, "public-ticket.txt")).StatusCode == HttpStatusCode.Redirect,
                "Support uploads a public ticket file");
            Check((await UploadTicketFile(true, "internal-ticket.txt")).StatusCode == HttpStatusCode.Redirect,
                "Support uploads an internal ticket file");
            List<Attachment> ticketFiles;
            await using (var verifyDb = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options))
                ticketFiles = await verifyDb.Attachments.Where(x => x.TicketId == ticketId).ToListAsync();
            uploadedTicketFiles.AddRange(ticketFiles.Select(x => Path.Combine(root, "wwwroot", x.FilePath.TrimStart('/'))));
            var publicFile = ticketFiles.Single(x => x.FileName == "public-ticket.txt");
            var internalFile = ticketFiles.Single(x => x.FileName == "internal-ticket.txt");
            Check((await requester.GetAsync(publicFile.FilePath)).StatusCode == HttpStatusCode.OK,
                "Requester downloads public ticket file");
            Check((await requester.GetAsync(internalFile.FilePath)).StatusCode == HttpStatusCode.NotFound
                && (await owner.GetAsync(internalFile.FilePath)).StatusCode == HttpStatusCode.OK,
                "Internal ticket file is limited to support team");
            Check((await manager.GetAsync(publicFile.FilePath)).StatusCode == HttpStatusCode.NotFound,
                "Unrelated user cannot download ticket file by direct URL");
            using var idleSession = await Login("idle");
            using var administrator = await Login("admin");
            var deactivated = await Post(administrator, "/Admin/Users?handler=DeactivateUser", "/Admin/Users",
                new() { ["userId"] = "idle" });
            Check(deactivated.StatusCode == HttpStatusCode.Redirect
                && (await idleSession.GetAsync("/Projects/Index")).StatusCode == HttpStatusCode.Redirect,
                "Deactivation revokes an existing account session");
            using var inactiveLogin = Client();
            var deniedLogin = await Post(inactiveLogin, "/Account/Login", "/Account/Login",
                new() { ["Input.Email"] = "idle@example.test", ["Input.Password"] = "TestPassword123" });
            Check(deniedLogin.StatusCode == HttpStatusCode.OK, "Inactive account cannot sign in");
            var reactivated = await Post(administrator, "/Admin/Users?handler=ActivateUser", "/Admin/Users",
                new() { ["userId"] = "idle" });
            Check(reactivated.StatusCode == HttpStatusCode.Redirect, "Admin can reactivate account");
            Console.WriteLine($"{count} HTTP scenarios passed.");
        }
        catch { Console.WriteLine(output.ToString()); throw; }
        finally
        {
            if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
            File.Delete(physicalFile);
            foreach (var uploaded in uploadedTicketFiles) File.Delete(uploaded);
        }
    }
}
