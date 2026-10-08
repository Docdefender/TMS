using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using TMS.Data;
using TMS.Models;
using TMS.Services;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
    builder.Configuration.AddEnvironmentVariables();
}

// Add Localization services
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Events.OnValidatePrincipal = async context =>
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(context.Principal!);
        if (user is null || !user.IsActive)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    };
});

builder.Services.AddScoped<AuditLogService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AccessService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<DepartmentService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddScoped<AttachmentService>();
builder.Services.AddScoped<PipelineService>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<TicketIntakeService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TaskTimeService>();
builder.Services.Configure<TicketMailOptions>(builder.Configuration.GetSection(TicketMailOptions.SectionName));
builder.Services.AddHttpClient("MicrosoftGraph", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Dizge-Ticket-Mail/1.0");
});
builder.Services.AddSingleton<MicrosoftGraphMailClient>();
builder.Services.AddScoped<TicketMailIngestionService>();
builder.Services.AddScoped<TicketMailSynchronizationService>();
if (builder.Configuration.GetValue<bool>($"{TicketMailOptions.SectionName}:Enabled"))
    builder.Services.AddHostedService<TicketMailSyncWorker>();

builder.Services.AddRazorPages().AddMvcOptions(options =>
{
    options.Filters.Add<AccessExceptionFilter>();
    options.Filters.Add<CurrentAdminFilter>();
})
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

var app = builder.Build();

// Seed roles and default admin user
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Roller — sadece yoksa ekle
    string[] roles = ["Admin", "Manager", "Member"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Admin kullanıcı — sadece yoksa ekle
    var adminEmail = "admin@tms.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser is null)
    {
        var bootstrapPassword = Environment.GetEnvironmentVariable("TMS_BOOTSTRAP_ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(bootstrapPassword))
            throw new InvalidOperationException("Initial admin password is required. Set TMS_BOOTSTRAP_ADMIN_PASSWORD before first startup.");

        await using var bootstrapTransaction = await context.Database.BeginTransactionAsync();
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Admin",
            EmailConfirmed = true
        };
        var created = await userManager.CreateAsync(adminUser, bootstrapPassword);
        if (!created.Succeeded)
            throw new InvalidOperationException("Initial admin account could not be created: " + string.Join("; ", created.Errors.Select(x => x.Description)));
        var assigned = await userManager.AddToRoleAsync(adminUser, "Admin");
        if (!assigned.Succeeded)
            throw new InvalidOperationException("Initial admin role could not be assigned: " + string.Join("; ", assigned.Errors.Select(x => x.Description)));
        await bootstrapTransaction.CommitAsync();
    }

    // Departmanlar — tablo tamamen boşsa varsayılanları ekle
    if (!context.Departments.IgnoreQueryFilters().Any())
    {
        context.Departments.AddRange(
            new Department { Name = "Engineering", Description = "Software development and engineering" },
            new Department { Name = "Marketing", Description = "Marketing and communications" },
            new Department { Name = "Human Resources", Description = "HR and people operations" },
            new Department { Name = "Finance", Description = "Finance and accounting" },
            new Department { Name = "Sistem Geliştirme", Description = "Ticket destek ekibi", IsTicketSupport = true }
        );
        await context.SaveChangesAsync();
    }

    if (!await context.Departments.AnyAsync(d => d.IsTicketSupport))
    {
        var ticketDepartment = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Sistem Geliştirme");
        if (ticketDepartment is null)
            context.Departments.Add(new Department { Name = "Sistem Geliştirme", Description = "Ticket destek ekibi", IsTicketSupport = true });
        else
            ticketDepartment.IsTicketSupport = true;
        await context.SaveChangesAsync();
    }

    if (app.Environment.IsDevelopment())
    {
        var demoProfiles = new[]
        {
            new { Email = "deniz.manager@demo.dizge.test", FullName = "Deniz Yılmaz", Role = "Manager", Department = "Marketing", Image = "/images/avatars/deniz.svg" },
            new { Email = "selin.manager@demo.dizge.test", FullName = "Selin Kaya", Role = "Manager", Department = "Finance", Image = "/images/avatars/selin.svg" },
            new { Email = "ece.member@demo.dizge.test", FullName = "Ece Demir", Role = "Member", Department = "Engineering", Image = "/images/avatars/ece.svg" },
            new { Email = "mert.member@demo.dizge.test", FullName = "Mert Arslan", Role = "Member", Department = "Engineering", Image = "/images/avatars/mert.svg" }
        };

        var demoDepartmentNames = demoProfiles.Select(x => x.Department).Distinct().ToList();
        var demoDepartments = await context.Departments
            .Where(x => demoDepartmentNames.Contains(x.Name))
            .ToDictionaryAsync(x => x.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var profile in demoProfiles)
        {
            if (!demoDepartments.TryGetValue(profile.Department, out var department)) continue;
            var demoUser = await userManager.FindByEmailAsync(profile.Email);
            if (demoUser is null)
            {
                demoUser = new ApplicationUser
                {
                    UserName = profile.Email,
                    Email = profile.Email,
                    EmailConfirmed = true,
                    FullName = profile.FullName,
                    DepartmentId = department.Id,
                    ProfileImagePath = profile.Image,
                    IsActive = true
                };
                var demoPassword = $"Dizge!{Guid.NewGuid():N}A1";
                var created = await userManager.CreateAsync(demoUser, demoPassword);
                if (!created.Succeeded)
                    throw new InvalidOperationException("Demo account could not be created: " + string.Join("; ", created.Errors.Select(x => x.Description)));
                var assigned = await userManager.AddToRoleAsync(demoUser, profile.Role);
                if (!assigned.Succeeded)
                    throw new InvalidOperationException("Demo role could not be assigned: " + string.Join("; ", assigned.Errors.Select(x => x.Description)));

                if (profile.Role == "Manager")
                {
                    var hasDefaultManager = await context.ManagerDepartments.AnyAsync(x => x.DepartmentId == department.Id && x.IsDefault);
                    context.ManagerDepartments.Add(new ManagerDepartment
                    {
                        DepartmentId = department.Id,
                        UserId = demoUser.Id,
                        IsDefault = !hasDefaultManager
                    });
                    await context.SaveChangesAsync();
                }
            }
        }

        var demoProfileImages = demoProfiles.ToDictionary(x => x.Email, x => x.Image, StringComparer.OrdinalIgnoreCase);
        var demoEmails = demoProfileImages.Keys.ToList();
        var demoUsers = await context.Users.Where(x => x.Email != null && demoEmails.Contains(x.Email)).ToListAsync();
        var profilesChanged = false;
        foreach (var demoUser in demoUsers)
        {
            var imagePath = demoProfileImages[demoUser.Email!];
            if (demoUser.ProfileImagePath == imagePath) continue;
            demoUser.ProfileImagePath = imagePath;
            profilesChanged = true;
        }
        if (profilesChanged) await context.SaveChangesAsync();
    }

    // Kategoriler — tablo tamamen boşsa varsayılanları ekle
    if (!context.Categories.IgnoreQueryFilters().Any())
    {
        context.Categories.AddRange(
            new Category { Name = "Bug Fix", Description = "Bug fixes and patches" },
            new Category { Name = "Feature", Description = "New feature development" },
            new Category { Name = "Improvement", Description = "Improvements to existing features" },
            new Category { Name = "Research", Description = "Research and investigation" }
        );
        await context.SaveChangesAsync();
    }

    // Görsel kabul turlarında durum, atama ve yazışma çeşitlerini gösterecek yerel örnekler.
    if (app.Environment.IsDevelopment()
        && !await context.Tickets.AnyAsync(x => x.Subject.StartsWith("[TEST] Görsel Ticket")))
    {
        var supportDepartment = await context.Departments.FirstAsync(x => x.IsTicketSupport);
        var activeUsers = await context.Users.Where(x => x.IsActive).OrderBy(x => x.CreatedAt).ToListAsync();
        var requesters = activeUsers.Where(x => x.DepartmentId != supportDepartment.Id).Take(4).ToList();
        if (requesters.Count == 0) requesters.Add(adminUser);
        var supportUser = activeUsers.FirstOrDefault(x => x.DepartmentId == supportDepartment.Id) ?? adminUser;
        var now = DateTime.UtcNow;

        Ticket DemoTicket(string subject, TicketStatus status, int daysAgo, int requesterIndex,
            string body, string? assigneeId = null, bool hasNewReply = false)
        {
            var requester = requesters[requesterIndex % requesters.Count];
            var createdAt = now.AddDays(-daysAgo);
            var priority = TicketPriority.Normal;
            var ticket = new Ticket
            {
                Subject = "[TEST] Görsel Ticket — " + subject,
                Status = status,
                Priority = priority,
                SlaDueAt = TicketSla.DefaultDueAt(createdAt, priority),
                RequesterUserId = requester.Id,
                RequesterDepartmentId = requester.DepartmentId,
                SupportDepartmentId = supportDepartment.Id,
                AssignedToUserId = assigneeId,
                HasNewReply = hasNewReply,
                CreatedAt = createdAt,
                UpdatedAt = hasNewReply ? now.AddHours(-2) : createdAt.AddHours(4)
            };
            ticket.Messages.Add(new TicketMessage
            {
                AuthorUserId = requester.Id,
                Body = body,
                IsIncoming = true,
                SentAt = createdAt
            });
            ticket.Events.Add(new TicketEvent
            {
                ActorUserId = requester.Id,
                Type = "Created",
                Details = "Yerel görsel kabul verisi oluşturuldu.",
                OccurredAt = createdAt
            });
            return ticket;
        }

        var demoTickets = new List<Ticket>
        {
            DemoTicket("CRM müşteri kartında kayıt hatası", TicketStatus.InProgress, 8, 0,
                "CRM üzerinde yeni müşteri kartı kaydederken doğrulama hatası alıyorum. Ekran yenilendiğinde girdiğim bilgiler kayboluyor.", supportUser.Id),
            DemoTicket("Yeni çalışan için rapor erişimi", TicketStatus.Open, 2, 1,
                "Ekibimize katılan çalışma arkadaşımız için satış raporlarına görüntüleme erişimi tanımlanmasını rica ederim."),
            DemoTicket("Mobil uygulamada oturum kapanması", TicketStatus.WaitingForInformation, 5, 2,
                "Saha uygulamasında işlem sırasında oturum kapanıyor. Özellikle zayıf bağlantıda daha sık tekrarlanıyor.", supportUser.Id, true),
            DemoTicket("Satış raporuna bölge filtresi", TicketStatus.Resolved, 14, 3,
                "Aylık satış raporuna bölge ve mağaza bazlı iki yeni filtre eklenmesini rica ederiz.", supportUser.Id),
            DemoTicket("Toplantı odası yazıcı kurulumu", TicketStatus.Closed, 21, 0,
                "Yeni toplantı odasındaki ağ yazıcısının bilgisayarlara tanımlanması konusunda destek rica ederim.", supportUser.Id)
        };

        demoTickets[0].Messages.Add(new TicketMessage
        {
            AuthorUserId = supportUser.Id,
            Body = "Sorun yeniden üretildi. Doğrulama servisi ve kayıt isteği inceleniyor.",
            SentAt = now.AddDays(-7).AddHours(3)
        });
        demoTickets[0].Messages.Add(new TicketMessage
        {
            AuthorUserId = supportUser.Id,
            Body = "Loglarda aynı zaman aralığında tekrar eden istekler görüldü; geliştirme maddesi olarak değerlendireceğiz.",
            IsInternal = true,
            SentAt = now.AddDays(-7).AddHours(4)
        });
        demoTickets[2].Messages.Add(new TicketMessage
        {
            AuthorUserId = supportUser.Id,
            Body = "Hangi cihaz ve uygulama sürümünde yaşandığını paylaşabilir misiniz?",
            SentAt = now.AddDays(-4)
        });
        demoTickets[2].Messages.Add(new TicketMessage
        {
            AuthorUserId = demoTickets[2].RequesterUserId,
            Body = "Android 15 yüklü saha cihazlarında 2.4.1 sürümünde tekrar ediyor.",
            IsIncoming = true,
            SentAt = now.AddHours(-2)
        });

        context.Tickets.AddRange(demoTickets);
        await context.SaveChangesAsync();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
// Existing upload URLs remain valid, but no upload is served without record authorization.
app.Use(async (http, next) =>
{
    if (!http.Request.Path.StartsWithSegments("/uploads", StringComparison.OrdinalIgnoreCase))
    { await next(); return; }
    if (http.User.Identity?.IsAuthenticated != true) { http.Response.StatusCode = 401; return; }
    var db = http.RequestServices.GetRequiredService<ApplicationDbContext>();
    var access = http.RequestServices.GetRequiredService<AccessService>();
    var tickets = http.RequestServices.GetRequiredService<TicketService>();
    var file = await db.Attachments.AsNoTracking().FirstOrDefaultAsync(x => x.FilePath == http.Request.Path.Value);
    var allowed = file is not null && (file.ProjectId.HasValue
        ? (await access.ProjectAsync(file.ProjectId.Value)).View
        : file.TaskItemId.HasValue ? (await access.TaskAsync(file.TaskItemId.Value)).View
        : file.TicketId.HasValue && (await tickets.RightsAsync(file.TicketId.Value)).View
            && (!file.IsInternal || (await tickets.RightsAsync(file.TicketId.Value)).Manage));
    if (!allowed || file is null) { http.Response.StatusCode = 404; return; }
    var root = Path.GetFullPath(Path.Combine(app.Environment.WebRootPath, "uploads")) + Path.DirectorySeparatorChar;
    var path = Path.GetFullPath(Path.Combine(app.Environment.WebRootPath, file.FilePath.TrimStart('/')));
    if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
    { http.Response.StatusCode = 404; return; }
    http.Response.ContentType = "application/octet-stream";
    http.Response.Headers["X-Content-Type-Options"] = "nosniff";
    http.Response.Headers.CacheControl = "private, no-store";
    http.Response.Headers.ContentDisposition = new System.Net.Mime.ContentDisposition
        { FileName = Uri.EscapeDataString(file.FileName), Inline = false }.ToString();
    await http.Response.SendFileAsync(path);
});
app.UseStaticFiles(new StaticFileOptions { FileProvider = new PublicFileProvider(app.Environment) });

app.UseRouting();

// Configure Turkish localization only
var supportedCultures = new[] { "tr-TR" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("tr-TR")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
app.UseRequestLocalization(localizationOptions);

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
