using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Security.Claims;
using TMS.Data;
using TMS.Models;
using TMS.Services;

public static class SqlChecks
{
    public static async Task RunAsync()
    {
        // Never accept the application's connection string. Only this disposable database is touched.
        var database = "DizgeAccessTests_" + Guid.NewGuid().ToString("N");
        var connection = $"Server=(localdb)\\MSSQLLocalDB;Database={database};Integrated Security=True;Encrypt=False";
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);
        var count = 0;
        void Check(bool condition, string name) { if (!condition) throw new Exception("SQL FAILED: " + name); count++; Console.WriteLine("SQL PASS: " + name); }
        async Task Denied(Func<Task> action, string name)
        {
            try { await action(); } catch (UnauthorizedAccessException) { Check(true, name); return; }
            throw new Exception("SQL FAILED (allowed): " + name);
        }
        async Task Rejected(Func<Task> action, string name)
        {
            try { await action(); } catch (InvalidOperationException) { Check(true, name); return; }
            throw new Exception("SQL FAILED (accepted): " + name);
        }
        AccessService Access(string id)
        {
            db.ChangeTracker.Clear();
            return new(db, new HttpContextAccessor { HttpContext = new DefaultHttpContext {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id)], "Test")) } });
        }
        ProjectService Projects(AccessService a) => new(db, a, new(db));
        TaskService Tasks(AccessService a) => new(db, a, new(db), new(db, a));
        PipelineService Pipelines(AccessService a) => new(db, a, new(db));
        TicketService Tickets(AccessService a) => new(db, a, Tasks(a), new(db, a));
        TicketIntakeService Intakes(AccessService a) => new(db, a);
        CommentService Comments(AccessService a) => new(db, a, new(db), Tasks(a));
        TaskTimeService Time(AccessService a) => new(db, a);
        try
        {
            await db.Database.MigrateAsync();
            var one = new Department { Name = "One", IsTicketSupport = true }; var two = new Department { Name = "Two" };
            db.Departments.AddRange(one, two); await db.SaveChangesAsync();
            foreach (var role in new[] { "Admin", "Manager", "Member" }) db.Roles.Add(new IdentityRole { Id = role, Name = role, NormalizedName = role.ToUpperInvariant() });
            foreach (var (id, role, dept) in new[] { ("admin", "Admin", (int?)null), ("m1", "Manager", (int?)one.Id), ("m2", "Manager", (int?)two.Id), ("member", "Member", (int?)one.Id), ("other", "Member", (int?)two.Id) })
            {
                var user = new ApplicationUser { Id = id, UserName = id + "@example.test", Email = id + "@example.test",
                    NormalizedUserName = (id + "@example.test").ToUpperInvariant(), NormalizedEmail = (id + "@example.test").ToUpperInvariant(),
                    FullName = id, DepartmentId = dept, SecurityStamp = Guid.NewGuid().ToString() };
                user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, "TestPassword123");
                db.Users.Add(user);
                db.UserRoles.Add(new IdentityUserRole<string> { UserId = id, RoleId = role });
            }
            await db.SaveChangesAsync();
            db.ManagerDepartments.AddRange(
                new ManagerDepartment { DepartmentId = one.Id, UserId = "m1", IsDefault = true },
                new ManagerDepartment { DepartmentId = two.Id, UserId = "m2", IsDefault = true });
            await db.SaveChangesAsync();
            Check(await db.ManagerDepartments.CountAsync() == 2, "Manager departments are configured");
            Check(await db.ManagerDepartments.CountAsync(x => x.IsDefault) == 2, "Each department has a default Manager");
            Check(!db.Database.HasPendingModelChanges(), "Migration snapshot matches model");

            var a = Access("m1");
            var p = new Project { Name = "Project", DepartmentId = one.Id, ManagerUserId = "m1" };
            await Projects(a).CreateProjectAsync(p, memberIds: ["member"]);
            var relationProject = new Project { Name = "Relation target", DepartmentId = one.Id, ManagerUserId = "m1" };
            var relationProject2 = new Project { Name = "Relation target 2", DepartmentId = one.Id, ManagerUserId = "m1" };
            await Projects(a).CreateProjectAsync(relationProject, memberIds: ["member"]);
            await Projects(a).CreateProjectAsync(relationProject2, memberIds: ["member"]);
            var hiddenProject = new Project { Name = "Hidden relation target", DepartmentId = two.Id, ManagerUserId = "m2" };
            await Projects(Access("m2")).CreateProjectAsync(hiddenProject);
            Check((await a.ProjectAsync(p.Id)).Edit, "Department owner can edit");
            Check(!(await Access("m2").ProjectAsync(p.Id)).View, "Unrelated Manager cannot view project");
            a = Access("m1");
            var task = new TaskItem { Title = "External task", ProjectId = p.Id, AssignedToUserId = "m2" };
            await Tasks(a).CreateTaskAsync(task);
            var initialNotification = await db.Notifications.SingleAsync(x => x.UserId == "m2" && x.Url == $"/Tasks/Details/{task.Id}");
            Check(initialNotification.ReadAt == null, "Task assignment creates an unread notification for the assignee");
            a = Access("m2");
            var notificationService = new NotificationService(db, a);
            Check((await notificationService.LatestAsync()).Any(x => x.Id == initialNotification.Id),
                "User sees only their notification stream");
            await notificationService.MarkReadAsync(initialNotification.Id);
            Check((await db.Notifications.FindAsync(initialNotification.Id))!.ReadAt.HasValue,
                "User can mark their notification as read");
            Check(await (await a.TasksAsync()).AnyAsync(x => x.Id == task.Id), "External assigned task is in scoped SQL query");
            Check(!await (await a.ProjectsAsync()).AnyAsync(x => x.Id == p.Id), "External task does not disclose full project");
            await Denied(() => Tasks(a).DeleteTaskAsync(task.Id), "External assignee cannot delete");
            var edit = await Tasks(a).GetTaskByIdAsync(task.Id) ?? throw new Exception("Missing task");
            edit.AssignedToUserId = "other";
            await Tasks(a).UpdateTaskAsync(edit);
            a = Access("m2");
            Check(await a.TaskAsync(task.Id) == new TaskAccess(true, false, false, false), "Devolution persists read-only access");
            Check(await db.TaskAssignments.CountAsync(x => x.TaskItemId == task.Id) == 2, "Initial and subsequent assignment events recorded");
            await Denied(() => Tasks(a).UpdateTaskStatusAsync(task.Id, TMS.Models.TaskStatus.Done), "Former assignee cannot change status");
            var grant = await db.TaskHistoryAccesses.SingleAsync(x => x.TaskItemId == task.Id && x.UserId == "m2");
            grant.RevokedAt = DateTime.UtcNow; grant.RevokedByUserId = "admin"; await db.SaveChangesAsync();
            Check(!(await Access("m2").TaskAsync(task.Id)).View, "History revocation removes read access in SQL");

            a = Access("member");
            Check((await a.ProjectAsync(p.Id)).CreateTask, "Project Member can create task");
            await Denied(() => Projects(a).CreateProjectAsync(new Project { Name = "Forbidden", DepartmentId = one.Id, ManagerUserId = "m1" }), "Member cannot create project");
            await Denied(() => Tasks(a).CreateTaskAsync(new TaskItem { Title = "Forbidden assignment", ProjectId = p.Id, AssignedToUserId = "other" }), "Member cannot assign outside project team");
            var memberTask = new TaskItem { Title = "Member task", ProjectId = p.Id, AssignedToUserId = "member" };
            await Tasks(a).CreateTaskAsync(memberTask);
            Check((await a.TaskAsync(memberTask.Id)).Contribute && !(await a.TaskAsync(memberTask.Id)).Edit, "Assigned Member has contribution rights only");
            await Denied(() => Tasks(a).DeleteTaskAsync(memberTask.Id), "Member creator cannot delete");

            var memberPipeline = await Pipelines(a).GetProjectAsync(p.Id);
            Check(memberPipeline is { CanManage: false }, "Member can view project pipeline read only");
            await Denied(() => Pipelines(a).AddStageAsync(p.Id, "Forbidden", null), "Member cannot configure pipeline");
            await Denied(() => Pipelines(a).AddProjectRelationAsync(p.Id, relationProject.Id, ProjectRelationType.Related),
                "Member cannot add project relation");
            await Denied(() => Pipelines(a).UpdateTaskScheduleAsync(p.Id, memberTask.Id,
                new DateTime(2026, 9, 10), new DateTime(2026, 9, 12)), "Member cannot change timeline schedule");
            a = Access("m1");
            await Denied(() => Pipelines(a).AddProjectRelationAsync(p.Id, hiddenProject.Id, ProjectRelationType.Related),
                "Manager cannot relate an inaccessible project");
            await Pipelines(a).AddProjectRelationAsync(p.Id, relationProject.Id, ProjectRelationType.Dependency);
            await Pipelines(a).AddProjectRelationAsync(relationProject.Id, relationProject2.Id, ProjectRelationType.Dependency);
            Check(await db.ProjectRelations.CountAsync(x => x.Type == ProjectRelationType.Dependency) == 2,
                "Manager creates directional project dependencies");
            await Rejected(() => Pipelines(a).AddProjectRelationAsync(relationProject2.Id, p.Id, ProjectRelationType.Dependency),
                "Project dependency cycle is rejected");
            Check(!(await Access("m2").ProjectAsync(p.Id)).View, "Project relation does not grant access");
            await Pipelines(a).AddProjectRelationAsync(p.Id, relationProject2.Id, ProjectRelationType.Related);
            var removableRelationId = await db.ProjectRelations.Where(x => x.Type == ProjectRelationType.Related
                && (x.SourceProjectId == p.Id || x.TargetProjectId == p.Id)).Select(x => x.Id).SingleAsync();
            await Pipelines(a).RemoveProjectRelationAsync(p.Id, removableRelationId);
            Check(!await db.ProjectRelations.AnyAsync(x => x.Id == removableRelationId), "Manager removes project relation");
            await Pipelines(a).AddStageAsync(p.Id, "Analysis", "Discovery work");
            await Pipelines(a).AddStageAsync(p.Id, "Temporary stage", null);
            var stage = await db.PipelineStages.SingleAsync(x => x.ProjectId == p.Id && x.Name == "Analysis");
            var temporaryStage = await db.PipelineStages.SingleAsync(x => x.ProjectId == p.Id && x.Name == "Temporary stage");
            await Pipelines(a).MoveStageAsync(p.Id, temporaryStage.Id, -1);
            Check(await db.PipelineStages.AnyAsync(x => x.Id == temporaryStage.Id && x.SortOrder == 1), "Manager reorders pipeline stages");
            await Pipelines(a).UpdateStageAsync(p.Id, stage.Id, "Analysis updated", "Updated discovery work");
            Check(await db.PipelineStages.AnyAsync(x => x.Id == stage.Id && x.Name == "Analysis updated"), "Manager edits pipeline stage");
            await Denied(() => Pipelines(Access("member")).UpdateStageAsync(p.Id, stage.Id, "Forbidden", null), "Member cannot edit pipeline stage");
            await Pipelines(a).AssignTaskAsync(p.Id, memberTask.Id, temporaryStage.Id, null);
            await Pipelines(a).DeleteStageAsync(p.Id, temporaryStage.Id);
            Check(await db.TaskItems.AnyAsync(x => x.Id == memberTask.Id && x.PipelineStageId == null && x.PipelineCheckpointId == null)
                && await db.PipelineStages.AnyAsync(x => x.Id == stage.Id && x.SortOrder == 1),
                "Deleting stage returns tasks to waiting area and normalizes order");
            Check(await db.AuditLogs.AnyAsync(x => x.Action == "PipelineStageDeleted" && x.EntityId == temporaryStage.Id
                && x.ProjectId == p.Id), "Deleted pipeline stage keeps permanent project-scoped history");
            await Pipelines(a).AddCheckpointAsync(p.Id, stage.Id, "Analysis approval", true);
            await Pipelines(a).AddCheckpointAsync(p.Id, stage.Id, "Temporary checkpoint", false, "Temporary description");
            var checkpoint = await db.PipelineCheckpoints.SingleAsync(x => x.PipelineStageId == stage.Id && x.Name == "Analysis approval");
            var temporaryCheckpoint = await db.PipelineCheckpoints.SingleAsync(x => x.PipelineStageId == stage.Id && x.Name == "Temporary checkpoint");
            await Pipelines(a).MoveCheckpointAsync(p.Id, temporaryCheckpoint.Id, -1);
            Check(await db.PipelineCheckpoints.AnyAsync(x => x.Id == temporaryCheckpoint.Id && x.SortOrder == 1), "Manager reorders checkpoints");
            await Pipelines(a).UpdateCheckpointAsync(p.Id, temporaryCheckpoint.Id, "Temporary checkpoint updated", "Updated", true);
            Check(await db.PipelineCheckpoints.AnyAsync(x => x.Id == temporaryCheckpoint.Id && x.Name == "Temporary checkpoint updated" && x.RequiresApproval),
                "Manager edits checkpoint");
            await Denied(() => Pipelines(Access("member")).DeleteCheckpointAsync(p.Id, temporaryCheckpoint.Id), "Member cannot delete checkpoint");
            await Pipelines(a).AssignTaskAsync(p.Id, memberTask.Id, stage.Id, temporaryCheckpoint.Id);
            await Pipelines(a).DeleteCheckpointAsync(p.Id, temporaryCheckpoint.Id);
            Check(await db.TaskItems.AnyAsync(x => x.Id == memberTask.Id && x.PipelineStageId == stage.Id && x.PipelineCheckpointId == null)
                && await db.PipelineCheckpoints.AnyAsync(x => x.Id == checkpoint.Id && x.SortOrder == 1),
                "Deleting checkpoint keeps task in stage and normalizes order");
            var dependencyRelationId = await db.ProjectRelations.Where(x => x.SourceProjectId == p.Id
                && x.TargetProjectId == relationProject.Id && x.Type == ProjectRelationType.Dependency).Select(x => x.Id).SingleAsync();
            await Pipelines(a).UpdateProjectRelationCheckpointAsync(p.Id, dependencyRelationId, checkpoint.Id);
            Check(await db.ProjectRelations.AnyAsync(x => x.Id == dependencyRelationId && x.BlockingCheckpointId == checkpoint.Id),
                "Manager binds project dependency to checkpoint");
            await Denied(() => Pipelines(Access("member")).UpdateProjectRelationCheckpointAsync(p.Id, dependencyRelationId, null),
                "Member cannot change checkpoint dependency");
            await Pipelines(a).AssignTaskAsync(p.Id, memberTask.Id, stage.Id, checkpoint.Id);
            await Pipelines(a).UpdateTaskScheduleAsync(p.Id, memberTask.Id,
                new DateTime(2026, 9, 10), new DateTime(2026, 9, 12));
            Check(await db.TaskItems.AnyAsync(x => x.Id == memberTask.Id && x.PipelineStageId == stage.Id
                && x.PipelineCheckpointId == checkpoint.Id && x.PlannedStartDate == new DateTime(2026, 9, 10)
                && x.DueDate == new DateTime(2026, 9, 12)), "Manager assigns and schedules project task in pipeline");
            await Denied(() => Pipelines(Access("member")).AddTaskDependencyAsync(p.Id, memberTask.Id, task.Id),
                "Member cannot configure task dependencies");
            await Pipelines(Access("m1")).AddTaskDependencyAsync(p.Id, memberTask.Id, task.Id);
            Check(await db.TaskDependencies.AnyAsync(x => x.DependentTaskId == memberTask.Id && x.PrerequisiteTaskId == task.Id),
                "Manager creates same-project task dependency");
            Check(await db.AuditLogs.AnyAsync(x => x.Action == "TaskDependencyCreated" && x.ProjectId == p.Id),
                "Task dependency creation keeps project-scoped history");
            await Rejected(() => Pipelines(Access("m1")).AddTaskDependencyAsync(p.Id, memberTask.Id, task.Id),
                "Duplicate task dependency is rejected");
            await Rejected(() => Pipelines(Access("m1")).AddTaskDependencyAsync(p.Id, task.Id, memberTask.Id),
                "Task dependency cycle is rejected");
            await Rejected(() => Tasks(Access("member")).UpdateTaskStatusAsync(memberTask.Id, TMS.Models.TaskStatus.Done),
                "Incomplete prerequisite blocks task completion");
            var blockedEdit = await Tasks(Access("m1")).GetTaskByIdAsync(memberTask.Id) ?? throw new Exception("Missing dependent task");
            blockedEdit.Status = TMS.Models.TaskStatus.Done;
            await Rejected(() => Tasks(Access("m1")).UpdateTaskAsync(blockedEdit),
                "Task edit cannot bypass incomplete prerequisite");
            var directAssigneeView = await Tasks(Access("other")).GetTaskByIdAsync(task.Id) ?? throw new Exception("Missing assigned task");
            Check(directAssigneeView.DependentDependencies.Count == 0,
                "Task-only access does not reveal another task through dependencies");
            await Tasks(Access("m1")).UpdateTaskStatusAsync(task.Id, TMS.Models.TaskStatus.Done);
            await Tasks(Access("member")).UpdateTaskStatusAsync(memberTask.Id, TMS.Models.TaskStatus.Done);
            Check(await db.TaskItems.AnyAsync(x => x.Id == memberTask.Id && x.ActualStartedAt != null && x.CompletedAt != null),
                "Task completion records actual timeline dates");
            await Rejected(() => Pipelines(Access("m1")).ApproveCheckpointAsync(p.Id, checkpoint.Id),
                "Incomplete project dependency blocks checkpoint approval");
            await db.Projects.Where(x => x.Id == relationProject.Id)
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.Status, ProjectStatus.Completed));
            await Pipelines(Access("m1")).ApproveCheckpointAsync(p.Id, checkpoint.Id);
            db.ChangeTracker.Clear();
            checkpoint = await db.PipelineCheckpoints.Include(x => x.Tasks)
                .Include(x => x.BlockingProjectRelations).ThenInclude(x => x.TargetProject).SingleAsync(x => x.Id == checkpoint.Id);
            Check(PipelineService.IsCheckpointComplete(checkpoint), "Completed dependency, tasks and approval complete checkpoint");
            await Tasks(Access("member")).UpdateTaskStatusAsync(memberTask.Id, TMS.Models.TaskStatus.InProgress);
            Check(!await db.PipelineCheckpoints.AnyAsync(x => x.Id == checkpoint.Id && x.ApprovedAt != null),
                "Reopened task clears checkpoint approval");
            Check(await db.TaskItems.AnyAsync(x => x.Id == memberTask.Id && x.ActualStartedAt != null && x.CompletedAt == null),
                "Reopened task keeps start and clears completion date");
            var memberHistory = await Pipelines(Access("member")).GetHistoryAsync(p.Id);
            Check(memberHistory is { Count: > 0 } && memberHistory.Any(x => x.Log.Action == "StatusChanged"),
                "Project Member can view pipeline and task status history");
            var structureHistory = await Pipelines(Access("member")).GetHistoryAsync(p.Id, "structure");
            Check(structureHistory is { Count: > 0 } && structureHistory.All(x => x.Category == "structure"),
                "Pipeline history category filter returns only matching events");
            Check(await Pipelines(Access("m2")).GetHistoryAsync(p.Id) is null,
                "Task-only assignment does not reveal project pipeline history");
            var taskDependencyId = await db.TaskDependencies.Where(x => x.DependentTaskId == memberTask.Id
                && x.PrerequisiteTaskId == task.Id).Select(x => x.Id).SingleAsync();
            await Pipelines(Access("m1")).RemoveTaskDependencyAsync(p.Id, taskDependencyId);
            Check(!await db.TaskDependencies.AnyAsync(x => x.Id == taskDependencyId)
                && await db.AuditLogs.AnyAsync(x => x.Action == "TaskDependencyRemoved" && x.ProjectId == p.Id),
                "Manager removes task dependency and keeps project-scoped history");

            a = Access("m1");
            var independentlyDeleted = new TaskItem { Title = "Deleted earlier", ProjectId = p.Id };
            await Tasks(a).CreateTaskAsync(independentlyDeleted);
            await Tasks(a).DeleteTaskAsync(independentlyDeleted.Id);
            await Projects(a).DeleteProjectAsync(p.Id);
            Check(!await (await Access("other").TasksAsync()).AnyAsync(x => x.Id == task.Id), "Project deletion hides assigned task");
            a = Access("admin");
            await Projects(a).RestoreProjectAsync(p.Id);
            Check(await db.TaskItems.AnyAsync(x => x.Id == task.Id), "Project restoration restores its deleted tasks");
            Check(!await db.TaskItems.AnyAsync(x => x.Id == independentlyDeleted.Id), "Independent deleted task is not restored");

            a = Access("m1");
            await Denied(() => Projects(a).ChangeStatusAsync(p.Id, ProjectStatus.Completed, true), "Manager cannot mutate Kanban");
            await Projects(a).ChangeStatusAsync(p.Id, ProjectStatus.InProgress);
            Check(await db.Projects.AnyAsync(x => x.Id == p.Id && x.Status == ProjectStatus.InProgress), "Participant Manager changes detail status");

            a = Access("admin");
            var projectEdit = await Projects(a).GetProjectByIdAsync(p.Id) ?? throw new Exception("Missing project");
            projectEdit.DepartmentId = two.Id;
            await Projects(a).UpdateProjectAsync(projectEdit, memberIds: ["member"]);
            Check(projectEdit.ManagerUserId == "m2", "Department move selects default Manager");
            Check(await db.ProjectMembers.AnyAsync(x => x.ProjectId == p.Id && x.UserId == "member"), "Department move preserves team");
            Check(await db.TaskItems.AnyAsync(x => x.Id == task.Id && x.AssignedToUserId == "other"), "Department move preserves task assignment");

            a = Access("m2");
            var departments = (await a.ActorAsync()).DepartmentIds;
            Check(departments.SequenceEqual([two.Id]), "Only managed departments loaded");
            Check(!(await a.AssignableUsersAsync()).Any(x => x.Id == "admin"), "Admin excluded from assignments");
            a = Access("m1");
            var limitedProject = new Project { Name = "Limited project", DepartmentId = one.Id, ManagerUserId = "m1" };
            await Projects(a).CreateProjectAsync(limitedProject, memberIds: ["member"]);
            var limitedTask = new TaskItem { Title = "Visible task", ProjectId = limitedProject.Id, AssignedToUserId = "m2" };
            await Tasks(a).CreateTaskAsync(limitedTask);
            await Time(Access("m2")).AddManualAsync(limitedTask.Id, 90, DateTime.Today, "Analiz");
            Check(await db.TaskTimeEntries.AnyAsync(x => x.TaskItemId == limitedTask.Id && x.UserId == "m2"
                && x.DurationMinutes == 90 && x.IsManual), "Task contributor records manual work time");
            await Denied(() => Time(Access("member")).AddManualAsync(limitedTask.Id, 30, DateTime.Today, null),
                "Read-only project member cannot record task time");
            await Time(Access("m2")).StartAsync(limitedTask.Id, "Uygulama");
            await Rejected(() => Time(Access("m2")).StartAsync(limitedTask.Id, null),
                "User cannot run two task timers simultaneously");
            await Time(Access("m2")).StopAsync(limitedTask.Id);
            Check(await db.TaskTimeEntries.AnyAsync(x => x.TaskItemId == limitedTask.Id && x.UserId == "m2"
                && !x.IsManual && x.EndedAt != null && x.DurationMinutes >= 1), "Task timer stops with a measured duration");
            await Tasks(a).CreateTaskAsync(new TaskItem { Title = "SECRET OTHER TASK", ProjectId = limitedProject.Id, AssignedToUserId = "member" });
            var tickets = Tickets(Access("m1"));
            var ticket = await tickets.CreateAsync("CRM erişim sorunu", "other", one.Id, "İlk talep mesajı");
            db.ChangeTracker.Clear();
            var createdTicket = await db.Tickets.AsNoTracking().SingleAsync(x => x.Id == ticket.Id);
            Check(createdTicket.Priority == TicketPriority.Normal && createdTicket.SlaDueAt.HasValue
                && Math.Abs((createdTicket.SlaDueAt.Value - createdTicket.CreatedAt).TotalHours - 72) < .01,
                "Ticket receives normal priority SLA target by default");
            await Denied(() => Tickets(Access("other")).UpdateSlaAsync(ticket.Id, TicketPriority.Critical, null),
                "Requester cannot change ticket SLA");
            await Tickets(Access("m1")).UpdateSlaAsync(ticket.Id, TicketPriority.Critical, null);
            db.ChangeTracker.Clear();
            Check(await db.Tickets.AnyAsync(x => x.Id == ticket.Id && x.Priority == TicketPriority.Critical
                    && x.SlaDueAt == x.CreatedAt.AddHours(8))
                && await db.TicketEvents.AnyAsync(x => x.TicketId == ticket.Id && x.Type == "SlaChanged"),
                "Support changes priority and recalculates SLA with an audit event");
            var departmentsService = new DepartmentService(db);
            await departmentsService.SetTicketAutoAssignmentAsync(one.Id, true);
            var autoAssignedTicket = await Tickets(Access("m1")).CreateAsync("Otomatik atama denemesi", "other",
                one.Id, "İş yüküne göre atanmalı");
            db.ChangeTracker.Clear();
            Check(await db.Tickets.AnyAsync(x => x.Id == autoAssignedTicket.Id && x.AssignedToUserId != null
                    && x.AssignedToUser!.DepartmentId == one.Id)
                && await db.TicketEvents.AnyAsync(x => x.TicketId == autoAssignedTicket.Id && x.Type == "AutoAssigned"),
                "Enabled ticket auto-assignment chooses an active support user and records history");
            await departmentsService.SetTicketAutoAssignmentAsync(one.Id, false);
            Check(await (await Tickets(Access("other")).VisibleAsync()).AnyAsync(x => x.Id == ticket.Id),
                "Ticket requester sees own ticket across departments");
            Check(await (await Tickets(Access("member")).VisibleAsync()).AnyAsync(x => x.Id == ticket.Id),
                "Support department sees ticket pool");
            Check(!await (await Tickets(Access("m2")).VisibleAsync()).AnyAsync(x => x.Id == ticket.Id),
                "Unrelated department cannot see requester ticket");
            await Denied(() => Tickets(Access("other")).AssignAsync(ticket.Id, "member"),
                "Requester cannot assign ticket");
            await Rejected(() => Tickets(Access("m1")).AssignAsync(ticket.Id, "m2"),
                "Ticket cannot be assigned outside support department");
            await Tickets(Access("m1")).AssignAsync(ticket.Id, "member");
            db.ChangeTracker.Clear();
            Check(await db.Tickets.AnyAsync(x => x.Id == ticket.Id && x.AssignedToUserId == "member"),
                "Support assigns primary ticket owner");
            await Tickets(Access("other")).SetViewerAsync(ticket.Id, "m2", true);
            db.ChangeTracker.Clear();
            Check(await (await Tickets(Access("m2")).VisibleAsync()).AnyAsync(x => x.Id == ticket.Id)
                && !(await Tickets(Access("m2")).RightsAsync(ticket.Id)).Manage,
                "Requester adds read-only viewer");
            await Denied(() => Tickets(Access("m2")).AddMessageAsync(ticket.Id, "gizli", true),
                "Viewer cannot add internal note");
            await Tickets(Access("m1")).AddMessageAsync(ticket.Id, "İç ekip notu", true);
            Check(!(await Tickets(Access("other")).MessagesAsync(ticket.Id)).Any(x => x.IsInternal)
                && (await Tickets(Access("member")).MessagesAsync(ticket.Id)).Any(x => x.IsInternal),
                "Internal notes are hidden from requester but visible to support");
            await Tickets(Access("other")).SetViewerAsync(ticket.Id, "m2", false);
            db.ChangeTracker.Clear();
            Check(!await (await Tickets(Access("m2")).VisibleAsync()).AnyAsync(x => x.Id == ticket.Id),
                "Removing extra viewer revokes ticket access");
            await Tickets(Access("member")).AssignAsync(ticket.Id, null);
            Check(await db.Tickets.AnyAsync(x => x.Id == ticket.Id && x.AssignedToUserId == null && x.Status == TicketStatus.Open),
                "Unassigned open ticket returns to pool");
            await Tickets(Access("m1")).SetFollowingAsync(ticket.Id, true);
            Check(await db.TicketViewers.AnyAsync(x => x.TicketId == ticket.Id && x.UserId == "m1" && x.IsFollowing),
                "Support user follows ticket personally");
            await Tickets(Access("m1")).SetFollowingAsync(ticket.Id, false);
            Check(await db.TicketViewers.AnyAsync(x => x.TicketId == ticket.Id && x.UserId == "m1" && !x.IsFollowing)
                && (await Tickets(Access("m1")).RightsAsync(ticket.Id)).View,
                "Leaving personal follow list keeps support access");
            await Tickets(Access("m1")).ChangeStatusAsync(ticket.Id, TicketStatus.Resolved);
            await Tickets(Access("other")).AddMessageAsync(ticket.Id, "Yeni bilgi ekliyorum", false);
            db.ChangeTracker.Clear();
            Check(await db.Tickets.AnyAsync(x => x.Id == ticket.Id && x.Status == TicketStatus.Resolved && x.HasNewReply),
                "Requester reply flags resolved ticket for review without reopening it");
            await Denied(() => Tickets(Access("other")).MarkReviewedAsync(ticket.Id),
                "Requester cannot clear support review flag");
            await Tickets(Access("m1")).MarkReviewedAsync(ticket.Id);
            Check(await db.Tickets.AnyAsync(x => x.Id == ticket.Id && x.Status == TicketStatus.Resolved && !x.HasNewReply),
                "Support review clears flag without changing status");
            await Tickets(Access("m1")).ChangeStatusAsync(ticket.Id, TicketStatus.Open);
            var linkedTaskId = await Tickets(Access("m1")).CreateLinkedTaskAsync(ticket.Id, limitedProject.Id,
                "Ticket development task", "Restricted ticket work");
            Check(await db.Tickets.AnyAsync(x => x.Id == ticket.Id && x.LinkedTaskId == linkedTaskId
                && x.Status == TicketStatus.InProgress), "Ticket conversion links a new task and starts work");
            Check((await Access("other").TaskAsync(linkedTaskId)).View
                && !(await Access("other").TaskAsync(linkedTaskId)).Edit,
                "Requester sees linked task read-only without project access");
            Check(!(await Access("m2").TaskAsync(linkedTaskId)).View
                && !await (await Access("m2").TasksAsync()).AnyAsync(x => x.Id == linkedTaskId),
                "Unrelated manager cannot see linked task");
            await Tickets(Access("other")).SetViewerAsync(ticket.Id, "m2", true);
            Check((await Access("m2").TaskAsync(linkedTaskId)).View
                && !(await Access("m2").TaskAsync(linkedTaskId)).Contribute,
                "Explicit viewer sees linked task read-only");
            await Tickets(Access("other")).SetViewerAsync(ticket.Id, "m2", false);
            await Denied(() => Tasks(Access("other")).UpdateTaskStatusAsync(linkedTaskId, TMS.Models.TaskStatus.Done),
                "Requester cannot complete linked task");
            await Tasks(Access("m1")).UpdateTaskStatusAsync(linkedTaskId, TMS.Models.TaskStatus.Done);
            Check(await db.Tickets.AnyAsync(x => x.Id == ticket.Id && x.Status == TicketStatus.Resolved
                && x.ResolvedByLinkedTask), "Completing linked task resolves ticket");
            await Tasks(Access("m1")).UpdateTaskStatusAsync(linkedTaskId, TMS.Models.TaskStatus.InProgress);
            Check(await db.Tickets.AnyAsync(x => x.Id == ticket.Id && x.Status == TicketStatus.InProgress),
                "Reopening linked task reopens automatically resolved ticket");
            await Rejected(() => Tickets(Access("m1")).CreateLinkedTaskAsync(ticket.Id, limitedProject.Id,
                "Duplicate", null), "Ticket cannot acquire a second active task");
            var editTasks = Tasks(Access("m1"));
            var editableLinkedTask = await editTasks.GetTaskByIdAsync(linkedTaskId)
                ?? throw new Exception("Missing linked task");
            editableLinkedTask.Status = TMS.Models.TaskStatus.Done;
            await editTasks.UpdateTaskAsync(editableLinkedTask);
            Check(await db.Tickets.AnyAsync(x => x.Id == ticket.Id && x.Status == TicketStatus.Resolved),
                "Task edit form completion resolves linked ticket");
            await Comments(Access("m1")).CreateTaskCommentAsync(linkedTaskId, "m1", "Work continues",
                TMS.Models.TaskStatus.InProgress);
            Check(await db.Tickets.AnyAsync(x => x.Id == ticket.Id && x.Status == TicketStatus.InProgress),
                "Task comment status change reopens automatically resolved ticket");
            var mailIngestion = new TicketMailIngestionService(db);
            var firstMail = new GraphMailMessage
            {
                Id = "graph-mail-1",
                InternetMessageId = "<mail-1@example.test>",
                ConversationId = "conversation-1",
                Subject = "Mail integration request",
                Body = new GraphMailBody { ContentType = "text", Content = "First message" },
                From = new GraphMailRecipient { EmailAddress = new GraphMailAddress { Address = "other@example.test" } },
                ReceivedDateTime = DateTimeOffset.UtcNow.AddMinutes(-2)
            };
            Check(await mailIngestion.ProcessAsync(firstMail, "test.mailbox@example.test")
                    == TicketMailIngestionResult.TicketCreated,
                "Known mailbox sender creates one ticket");
            Check(await mailIngestion.ProcessAsync(new GraphMailMessage
                {
                    Id = "graph-mail-filtered",
                    InternetMessageId = "<mail-filtered@example.test>",
                    Subject = "Ordinary personal message",
                    Body = new GraphMailBody { Content = "Not a ticket" },
                    From = new GraphMailRecipient { EmailAddress = new GraphMailAddress { Address = "other@example.test" } }
                }, "test.mailbox@example.test", "[DIZGE TEST]") == TicketMailIngestionResult.Ignored,
                "Personal mailbox subject prefix excludes ordinary mail");
            Check(await mailIngestion.ProcessAsync(firstMail, "test.mailbox@example.test")
                    == TicketMailIngestionResult.Duplicate,
                "Repeated mailbox delivery is idempotent");
            var mailTicket = await db.Tickets.SingleAsync(x => x.ExternalConversationId == "conversation-1");
            mailTicket.Status = TicketStatus.Resolved;
            await db.SaveChangesAsync();
            var replyMail = new GraphMailMessage
            {
                Id = "graph-mail-2",
                InternetMessageId = "<mail-2@example.test>",
                ConversationId = "conversation-1",
                Subject = "RE: Mail integration request",
                Body = new GraphMailBody { ContentType = "text", Content = "More information" },
                From = new GraphMailRecipient { EmailAddress = new GraphMailAddress { Address = "other@example.test" } },
                ReceivedDateTime = DateTimeOffset.UtcNow
            };
            Check(await mailIngestion.ProcessAsync(replyMail, "test.mailbox@example.test")
                    == TicketMailIngestionResult.ReplyAdded
                && await db.Tickets.CountAsync(x => x.ExternalConversationId == "conversation-1") == 1
                && await db.TicketMessages.CountAsync(x => x.TicketId == mailTicket.Id) == 2
                && await db.Tickets.AnyAsync(x => x.Id == mailTicket.Id && x.Status == TicketStatus.Resolved
                    && x.HasNewReply),
                "Mailbox reply stays in the conversation and flags review without reopening");
            var unknownMail = new GraphMailMessage
            {
                Id = "graph-mail-3",
                InternetMessageId = "<mail-3@example.test>",
                ConversationId = "conversation-unknown",
                Subject = "Unknown sender",
                Body = new GraphMailBody { ContentType = "html", Content = "<p>Review <strong>me</strong></p>" },
                From = new GraphMailRecipient { EmailAddress = new GraphMailAddress { Address = "mail.unknown@example.test" } }
            };
            Check(await mailIngestion.ProcessAsync(unknownMail, "test.mailbox@example.test")
                    == TicketMailIngestionResult.StagedForReview
                && await db.TicketIntakes.AnyAsync(x => x.ExternalMessageId == "<mail-3@example.test>"
                    && x.SourceMailboxAddress == "test.mailbox@example.test"),
                "Unknown mailbox sender is staged for review");
            var unknown = await Intakes(Access("m1")).StageAsync("new.sender@example.test", "New sender request",
                "Original request", one.Id, externalMessageId: "test-message-1");
            Check(await (await Intakes(Access("m1")).PendingAsync()).AnyAsync(x => x.Id == unknown.Id)
                && !await (await Intakes(Access("m2")).PendingAsync()).AnyAsync(x => x.Id == unknown.Id),
                "Unknown sender waits only in support department review pool");
            await Rejected(() => Intakes(Access("m1")).StageAsync("new.sender@example.test", "Duplicate",
                "Body", one.Id, externalMessageId: "test-message-1"),
                "External message ID cannot be staged twice");
            var newlyRegistered = new ApplicationUser { Id = "new-sender", UserName = "new.sender@example.test",
                Email = "new.sender@example.test", NormalizedUserName = "NEW.SENDER@EXAMPLE.TEST",
                NormalizedEmail = "NEW.SENDER@EXAMPLE.TEST", FullName = "New Sender", DepartmentId = two.Id,
                SecurityStamp = Guid.NewGuid().ToString() };
            db.Users.Add(newlyRegistered);
            await db.SaveChangesAsync();
            await Rejected(() => Intakes(Access("m1")).MatchAsync(unknown.Id, "other"),
                "Review cannot be assigned to a different email address");
            var matchedTicketId = await Intakes(Access("m1")).MatchAsync(unknown.Id, newlyRegistered.Id);
            Check(await db.Tickets.AnyAsync(x => x.Id == matchedTicketId && x.RequesterUserId == newlyRegistered.Id)
                && await db.TicketMessages.AnyAsync(x => x.TicketId == matchedTicketId
                    && x.ExternalMessageId == "test-message-1")
                && !await (await Intakes(Access("m1")).PendingAsync()).AnyAsync(x => x.Id == unknown.Id),
                "Registering and matching sender converts original message into one ticket");
            await Tasks(Access("m1")).UpdateTaskStatusAsync(linkedTaskId, TMS.Models.TaskStatus.Done);
            await Tasks(Access("m1")).DeleteTaskAsync(linkedTaskId);
            Check(await db.Tickets.AnyAsync(x => x.Id == ticket.Id && x.LinkedTaskId == linkedTaskId
                && x.Status == TicketStatus.InProgress), "Deleted linked task retains ticket history and active work status");
            await Rejected(() => Tickets(Access("m1")).CreateLinkedTaskAsync(ticket.Id, limitedProject.Id,
                "Replacement", null), "Deleted linked task must be restored before another conversion");
            Check(await db.Departments.CountAsync(x => x.IsTicketSupport) == 1,
                "Exactly one support department is designated");
            await Rejected(() => Tickets(Access("admin")).CreateAsync("Wrong support group", "other", two.Id, "Body"),
                "Admin cannot route new Ticket to an unrelated department");
            var inactive = await db.Users.SingleAsync(x => x.Id == "m2");
            inactive.IsActive = false;
            await db.SaveChangesAsync();
            await Denied(async () => { await Access("m2").ActorAsync(); },
                "Inactive account cannot use access services");
            Check(!(await Access("admin").AssignableUsersAsync()).Any(x => x.Id == "m2"),
                "Inactive account is absent from assignment choices");
            inactive = await db.Users.SingleAsync(x => x.Id == "m2");
            inactive.IsActive = true;
            await db.SaveChangesAsync();
            await HttpChecks.RunAsync(connection, limitedProject.Id, limitedTask.Id, one.Id);
            Console.WriteLine($"{count} SQL scenarios passed using isolated database {database}.");
        }
        finally
        {
            // Name is generated above and cannot reference application data.
            await db.Database.EnsureDeletedAsync();
        }
    }
}
