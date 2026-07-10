# TMS — Complete Code Explanation Guide

## 🎯 Purpose
This guide explains **every line of code** in the TMS (Task Management System) application, from database connections to UI rendering. Perfect for understanding how ASP.NET Core Razor Pages applications work.

---

## 📚 Table of Contents
1. [Project Structure Overview](#project-structure)
2. [Database Layer (Models & DbContext)](#database-layer)
3. [Connection Strings & Configuration](#configuration)
4. [Service Layer (Business Logic)](#service-layer)
5. [Razor Pages (UI Layer)](#razor-pages)
6. [Authentication & Authorization](#authentication)
7. [Dependency Injection](#dependency-injection)
8. [Request Lifecycle](#request-lifecycle)

---

## 📁 Project Structure Overview
TMS/ ├── Models/                      # Data structures (classes that represent database tables) │   ├── Project.cs               # Project entity │   ├── TaskItem.cs              # Task entity │   ├── ApplicationUser.cs       # User entity (extends Identity) │   ├── Comment.cs               # Comment entity │   ├── AuditLog.cs              # Audit log entity │   ├── Department.cs            # Department entity │   ├── Category.cs              # Category entity │   ├── ProjectStatus.cs         # Enum for project statuses │   └── TaskStatus.cs            # Enum for task statuses │ ├── Data/                        # Database context (the bridge to SQL Server) │   └── ApplicationDbContext.cs  # EF Core DbContext (database connection manager) │ ├── Services/                    # Business logic (rules, calculations, data operations) │   ├── ProjectService.cs        # Project CRUD operations │   ├── TaskService.cs           # Task CRUD operations │   ├── CommentService.cs        # Comment operations │   ├── DashboardService.cs      # Dashboard data calculations │   ├── DepartmentService.cs     # Department operations │   ├── CategoryService.cs       # Category operations │   └── AuditLogService.cs       # Logging operations │ ├── Pages/                       # Razor Pages (UI + code-behind) │   ├── Index.cshtml             # Dashboard HTML │   ├── Index.cshtml.cs          # Dashboard C# logic (PageModel) │   ├── _ViewStart.cshtml        # Sets default layout │   ├── _ViewImports.cshtml      # Global using statements │   │ │   ├── Shared/ │   │   └── _Layout.cshtml       # Master layout (sidebar, navigation) │   │ │   ├── Projects/ │   │   ├── Index.cshtml         # Project list page (HTML) │   │   ├── Index.cshtml.cs      # Project list logic │   │   ├── Create.cshtml        # Create project form │   │   ├── Create.cshtml.cs     # Create project logic │   │   ├── Details.cshtml       # Project details + tasks │   │   └── Details.cshtml.cs    # Details logic │   │ │   ├── Tasks/ │   │   ├── Index.cshtml         # Task list page │   │   └── Index.cshtml.cs      # Task list logic │   │ │   ├── Kanban/ │   │   ├── Index.cshtml         # Kanban board │   │   └── Index.cshtml.cs      # Kanban logic │   │ │   ├── Account/ │   │   ├── Login.cshtml         # Login form │   │   ├── Login.cshtml.cs      # Login logic │   │   ├── Register.cshtml      # Registration form │   │   └── Register.cshtml.cs   # Registration logic │   │ │   └── Admin/ │       ├── Departments.cshtml   # Admin department management │       ├── Categories.cshtml    # Admin category management │       └── AuditLogs.cshtml     # Audit log viewer │ ├── wwwroot/                     # Static files (CSS, JS, images) │   ├── css/ │   │   └── site.css             # Custom styles │   ├── js/ │   │   └── site.js              # Custom JavaScript │   └── lib/                     # Third-party libraries (Bootstrap, jQuery) │ ├── appsettings.json             # Configuration (connection strings, logging) ├── Program.cs                   # Application startup (configures everything) └── TMS.csproj                   # Project file (dependencies, build config)
---

## 🗄️ Database Layer (Models & DbContext)

### What is a Model?
A **model** is a C# class that represents a database table. Each property is a column.

### Example: Project.cs
using System.ComponentModel.DataAnnotations;
namespace TMS.Models;
public class Project { // PRIMARY KEY - uniquely identifies each project public int Id { get; set; }
// REQUIRED field - cannot be null/empty
// Maximum 200 characters
[Required]
[StringLength(200)]
public string Name { get; set; } = string.Empty;

// OPTIONAL field - can be null
// Maximum 1000 characters
[StringLength(1000)]
public string? Description { get; set; }

// Date fields - when project starts and ends
public DateTime StartDate { get; set; } = DateTime.Today;
public DateTime? EndDate { get; set; }

// ENUM - project status (NotStarted, InProgress, etc.)
// Stored as integer in database
public ProjectStatus Status { get; set; } = ProjectStatus.NotStarted;

// FOREIGN KEYS - references to other tables
// "?" means nullable (optional relationship)
public string? CreatedByUserId { get; set; }
public string? AssignedToUserId { get; set; }
public int? DepartmentId { get; set; }
public int? CategoryId { get; set; }

// NAVIGATION PROPERTIES - EF Core uses these to load related data
// "?" means it might be null when loaded from database
public ApplicationUser? CreatedByUser { get; set; }
public ApplicationUser? AssignedToUser { get; set; }
public Department? Department { get; set; }
public Category? Category { get; set; }

// COLLECTIONS - one project has many tasks/comments
// Initialized to empty list to avoid null reference exceptions
public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}

**What happens in the database:**
CREATE TABLE Projects ( Id INT PRIMARY KEY IDENTITY(1,1),     -- Auto-incrementing ID Name NVARCHAR(200) NOT NULL,          -- Required string Description NVARCHAR(1000) NULL,      -- Optional string StartDate DATETIME2 NOT NULL, EndDate DATETIME2 NULL, Status INT NOT NULL,                   -- Enum stored as integer CreatedByUserId NVARCHAR(450) NULL,   -- Foreign key to AspNetUsers AssignedToUserId NVARCHAR(450) NULL, DepartmentId INT NULL,                 -- Foreign key to Departments CategoryId INT NULL,                   -- Foreign key to Categories
FOREIGN KEY (CreatedByUserId) REFERENCES AspNetUsers(Id),
FOREIGN KEY (AssignedToUserId) REFERENCES AspNetUsers(Id),
FOREIGN KEY (DepartmentId) REFERENCES Departments(Id),
FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
);


---

### ApplicationDbContext.cs — The Database Connection Manager

using Microsoft.AspNetCore.Identity.EntityFrameworkCore; using Microsoft.EntityFrameworkCore; using TMS.Models;
namespace TMS.Data;
// Inherits from IdentityDbContext to get ASP.NET Core Identity tables // (Users, Roles, Claims, etc.) public class ApplicationDbContext : IdentityDbContext<ApplicationUser> { // Constructor - receives configuration from dependency injection public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)  // Pass options to base class { }
// DbSet<T> properties - these become database tables
// Each property represents a table, generic type is the model
public DbSet<Project> Projects => Set<Project>();
public DbSet<TaskItem> TaskItems => Set<TaskItem>();
public DbSet<Comment> Comments => Set<Comment>();
public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
public DbSet<Department> Departments => Set<Department>();
public DbSet<Category> Categories => Set<Category>();

// OnModelCreating - configure database relationships and constraints
// Called when EF Core builds the database model
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Call base class to configure Identity tables
    base.OnModelCreating(modelBuilder);

    // Configure Task -> Project relationship
    modelBuilder.Entity<TaskItem>()
        .HasOne(t => t.Project)              // Each task has one project
        .WithMany(p => p.Tasks)              // Each project has many tasks
        .HasForeignKey(t => t.ProjectId)     // Foreign key column
        .OnDelete(DeleteBehavior.Cascade);   // Delete tasks when project deleted

    // Configure Project -> CreatedByUser relationship
    modelBuilder.Entity<Project>()
        .HasOne(p => p.CreatedByUser)        // Project has one creator
        .WithMany()                          // User has many projects (not tracked)
        .HasForeignKey(p => p.CreatedByUserId)
        .OnDelete(DeleteBehavior.NoAction);  // Don't cascade delete

    // Configure Project -> AssignedToUser relationship
    modelBuilder.Entity<Project>()
        .HasOne(p => p.AssignedToUser)
        .WithMany()
        .HasForeignKey(p => p.AssignedToUserId)
        .OnDelete(DeleteBehavior.NoAction);

    // Similar configurations for TaskItem, Comment, etc...
    // NoAction prevents cascade delete conflicts
    // (multiple paths to same table cause SQL Server errors)
}
}


**What this does:**
1. **DbContext** is the gateway to the database
2. **DbSet<T>** properties let you query tables: `_context.Projects.Where(...)`
3. **OnModelCreating** defines relationships (foreign keys, cascade rules)
4. EF Core translates C# queries to SQL automatically

---

## 🔌 Connection Strings & Configuration

### appsettings.json

{ "ConnectionStrings": { // This tells EF Core how to connect to SQL Server "DefaultConnection": "Server=localhost;Database=TMS;User Id=sa;Password=YourPassword;TrustServerCertificate=true;"
// Breakdown:
// - Server=localhost → SQL Server on this machine
// - Database=TMS → Database name
// - User Id=sa → SQL Server login username
// - Password=YourPassword → SQL Server password
// - TrustServerCertificate=true → Accept self-signed SSL cert
}, "Logging": { "LogLevel": { "Default": "Information",       // Log info and above "Microsoft.AspNetCore": "Warning"  // Only log warnings for framework } }, "AllowedHosts": "*"  // Allow requests from any host }


### How Connection String is Used in Program.cs

var builder = WebApplication.CreateBuilder(args);
// Read connection string from appsettings.json // builder.Configuration["ConnectionStrings:DefaultConnection"] // → "Server=localhost;Database=TMS;..."
// Register DbContext as a service (dependency injection) builder.Services.AddDbContext<ApplicationDbContext>(options => // Configure to use SQL Server with the connection string options.UseSqlServer( builder.Configuration.GetConnectionString("DefaultConnection") ) );


**What happens:**
1. ASP.NET Core reads `appsettings.json` at startup
2. `GetConnectionString("DefaultConnection")` retrieves the connection string
3. `UseSqlServer(...)` tells EF Core to use SQL Server with that connection
4. Now `ApplicationDbContext` can connect to the database

---

## 🛠️ Service Layer (Business Logic)

Services contain **business logic** — the rules and operations of your application.

### Example: ProjectService.cs

using Microsoft.EntityFrameworkCore; using TMS.Data; using TMS.Models;
namespace TMS.Services;
public class ProjectService { // Dependency - the database context private readonly ApplicationDbContext _context; private readonly AuditLogService _auditLogService;
// Constructor - receives dependencies via dependency injection
public ProjectService(ApplicationDbContext context, AuditLogService auditLogService)
{
    _context = context;
    _auditLogService = auditLogService;
}

// GET ALL PROJECTS
// async = runs asynchronously (doesn't block the thread)
// Task<List<Project>> = returns a Task that will complete with a List<Project>
public async Task<List<Project>> GetAllProjectsAsync()
{
    // _context.Projects → DbSet<Project> (represents Projects table)
    // .Include(p => p.Tasks) → SQL JOIN to load related tasks
    // .Include(p => p.Department) → JOIN to load department
    // .ToListAsync() → Execute query and return results
    return await _context.Projects
        .Include(p => p.Tasks)              // Eager loading (JOIN)
        .Include(p => p.Department)
        .Include(p => p.Category)
        .Include(p => p.CreatedByUser)
        .Include(p => p.AssignedToUser)
        .OrderByDescending(p => p.StartDate)  // Sort by date descending
        .ToListAsync();                       // Execute query
}

// GET PROJECT BY ID
// Returns null if not found (Project?)
public async Task<Project?> GetProjectByIdAsync(int id)
{
    // FirstOrDefaultAsync → returns first match or null
    return await _context.Projects
        .Include(p => p.Tasks)                // Load all related data
            .ThenInclude(t => t.AssignedToUser)  // Load task assignees too
        .Include(p => p.Comments)
            .ThenInclude(c => c.User)
        .Include(p => p.Department)
        .Include(p => p.Category)
        .Include(p => p.CreatedByUser)
        .Include(p => p.AssignedToUser)
        .FirstOrDefaultAsync(p => p.Id == id);  // WHERE Id = @id
}

// CREATE PROJECT
public async Task<int> CreateProjectAsync(Project project, string userId)
{
    // Set the creator
    project.CreatedByUserId = userId;

    // Add to DbSet (marks as "to be inserted")
    _context.Projects.Add(project);

    // SaveChangesAsync → commits to database (runs INSERT SQL)
    await _context.SaveChangesAsync();

    // Log the action
    await _auditLogService.LogAsync(
        "Create", 
        "Project", 
        project.Id, 
        userId, 
        $"Project '{project.Name}' created"
    );

    // Return the auto-generated ID
    return project.Id;
}

// UPDATE PROJECT
public async Task UpdateProjectAsync(Project project)
{
    // Mark entity as modified
    _context.Projects.Update(project);

    // Save changes (runs UPDATE SQL)
    await _context.SaveChangesAsync();
}

// DELETE PROJECT
public async Task DeleteProjectAsync(int id)
{
    // Find the project by ID
    var project = await _context.Projects.FindAsync(id);
    
    // If found, remove it
    if (project is not null)
    {
        _context.Projects.Remove(project);  // Mark for deletion
        await _context.SaveChangesAsync();  // Execute DELETE SQL
    }
}

// GET PROJECTS BY STATUS
public async Task<List<Project>> GetProjectsByStatusAsync(ProjectStatus status)
{
    // LINQ query - translated to SQL WHERE clause
    return await _context.Projects
        .Include(p => p.Tasks)
        .Where(p => p.Status == status)  // WHERE Status = @status
        .ToListAsync();
}

// GET ALL USERS (for dropdowns)
public async Task<List<ApplicationUser>> GetAllUsersAsync()
{
    // Query the AspNetUsers table
    return await _context.Users
        .OrderBy(u => u.FullName)
        .ToListAsync();
}
}


**Key Concepts:**

1. **`async/await`**: Allows non-blocking database operations

// Synchronous (blocks thread) var projects = _context.Projects.ToList();
// Asynchronous (doesn't block) var projects = await _context.Projects.ToListAsync();


2. **`.Include()`**: Eager loading (loads related data in one query)

// Without Include - 2 queries (N+1 problem) var project = _context.Projects.First(); var tasks = project.Tasks;  // Separate query!
// With Include - 1 query with JOIN var project = _context.Projects.Include(p => p.Tasks).First(); var tasks = project.Tasks;  // Already loaded!


3. **`.SaveChangesAsync()`**: Commits all changes to database

var project = new Project { Name = "Test" }; _context.Projects.Add(project);      // Queued for insert await _context.SaveChangesAsync();   // INSERT executed now


---

## 📄 Razor Pages (UI Layer)

Razor Pages combine HTML (`.cshtml`) with C# logic (`.cshtml.cs`).

### Example: Projects/Index.cshtml.cs (Code-Behind)


using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.AspNetCore.Mvc.RazorPages; using TMS.Models; using TMS.Services;
namespace TMS.Pages.Projects;
// [Authorize] - only authenticated users can access this page [Authorize] public class IndexModel : PageModel  // Inherits from PageModel { // Dependencies injected via constructor private readonly ProjectService _projectService;
public IndexModel(ProjectService projectService)
{
    _projectService = projectService;
}

// PUBLIC PROPERTIES - available in the .cshtml view
public List<Project> Projects { get; set; } = new();
public string? Status { get; set; }
public string? Filter { get; set; }
public string? ActiveFilter { get; set; }

// OnGetAsync - runs when page is requested via GET
// Optional query parameters: ?status=InProgress&filter=overdue
public async Task OnGetAsync(string? status, string? filter)
{
    // Store query params in properties
    Status = status;
    Filter = filter;

    // Determine active filter for UI
    if (!string.IsNullOrEmpty(status))
    {
        ActiveFilter = status;
        
        // Parse enum from string
        if (Enum.TryParse<ProjectStatus>(status, out var projectStatus))
        {
            // Load filtered projects
            Projects = await _projectService.GetProjectsByStatusAsync(projectStatus);
        }
    }
    else if (filter == "overdue")
    {
        ActiveFilter = "overdue";
        // Load overdue projects (custom logic)
        var allProjects = await _projectService.GetAllProjectsAsync();
        Projects = allProjects
            .Where(p => p.EndDate.HasValue && p.EndDate.Value < DateTime.Today)
            .ToList();
    }
    else
    {
        // No filter - load all projects
        Projects = await _projectService.GetAllProjectsAsync();
    }
}
}


**Lifecycle:**
1. User navigates to `/Projects/Index`
2. ASP.NET Core creates `IndexModel` instance
3. Dependency injection provides `ProjectService`
4. `OnGetAsync()` method runs
5. `Projects` property is populated
6. `.cshtml` view renders using `Projects` data

---

### Example: Projects/Index.cshtml (View)


@page @model TMS.Pages.Projects.IndexModel @{ ViewData["Title"] = "Projects"; }
<!-- Razor syntax: @ switches to C# code --> <h1>Projects</h1>
<div class="mb-3"> <!-- asp-page creates a link to another Razor Page --> <a asp-page="Create" class="btn btn-primary">Create New Project</a>
<!-- C# if statement in Razor -->
@if (Model.ActiveFilter is not null)
{
    <!-- Link to clear filter (no query params) -->
    <a asp-page="Index" class="btn btn-outline-secondary">
        <i class="bi bi-x-circle"></i> Clear Filter
    </a>
    
    <!-- Display current filter -->
    <span class="badge bg-info">Filtered: @Model.Filter ?? @Model.Status</span>
}
</div>
<!-- Check if there are any projects --> @if (!Model.Projects.Any()) { <div class="alert alert-info">No projects found.</div> } else { <table class="table table-striped"> <thead> <tr> <th>Name</th> <th>Status</th> <th>Department</th> <th>Start Date</th> <th>Actions</th> </tr> </thead> <tbody> <!-- Loop through projects --> @foreach (var project in Model.Projects) { <tr> <td>@project.Name</td> <td> <!-- Call C# helper function --> <span class="badge @GetStatusBadgeClass(project.Status)"> @project.Status </span> </td> <td>@(project.Department?.Name ?? "—")</td> <td>@project.StartDate.ToString("yyyy-MM-dd")</td> <td> <!-- Link with route parameter --> <a asp-page="Details" asp-route-id="@project.Id" class="btn btn-sm btn-info"> Details </a> </td> </tr> } </tbody> </table> }
<!-- C# functions section (available to view only) --> @functions { // Helper function to get CSS class based on status string GetStatusBadgeClass(TMS.Models.ProjectStatus status) => status switch { TMS.Models.ProjectStatus.NotStarted => "bg-secondary", TMS.Models.ProjectStatus.InProgress => "bg-primary", TMS.Models.ProjectStatus.Completed => "bg-success", TMS.Models.ProjectStatus.OnHold => "bg-warning text-dark", TMS.Models.ProjectStatus.Cancelled => "bg-danger", _ => "bg-secondary" }; }



**Key Razor Syntax:**
- `@` - Switch to C# code
- `@{ }` - C# code block
- `@if`, `@foreach`, `@for` - Control structures
- `@Model` - Access the PageModel instance
- `asp-page` - Generate link to another page
- `asp-route-{param}` - Pass route parameters
- `@functions { }` - Define helper methods

---

## 🔐 Authentication & Authorization

### How Identity Works in TMS

1. **ApplicationUser** - Your custom user class

using Microsoft.AspNetCore.Identity;
namespace TMS.Models;
// Inherits from IdentityUser (built-in ASP.NET Core Identity) public class ApplicationUser : IdentityUser { // IdentityUser provides: Email, UserName, PasswordHash, etc.
// Custom properties
[Required]
[StringLength(100)]
public string FullName { get; set; } = string.Empty;

public DateTime CreatedAt { get; set; } = DateTime.Now;

public int? DepartmentId { get; set; }
public Department? Department { get; set; }
}


2. **Identity Configuration in Program.cs**

// Add Identity services builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => { // Password requirements options.Password.RequireDigit = true;           // Must have number options.Password.RequiredLength = 6;            // Min 6 characters options.Password.RequireNonAlphanumeric = false; // Special char optional options.Password.RequireUppercase = true;       // Must have uppercase options.Password.RequireLowercase = true;       // Must have lowercase options.SignIn.RequireConfirmedAccount = false; // No email confirmation }) .AddEntityFrameworkStores<ApplicationDbContext>()  // Store users in DB .AddDefaultTokenProviders();                        // For password reset, etc.
// Configure cookie authentication builder.Services.ConfigureApplicationCookie(options => { options.LoginPath = "/Account/Login";         // Redirect here if not logged in options.LogoutPath = "/Account/Logout";       // Logout endpoint options.AccessDeniedPath = "/Account/AccessDenied"; // Forbidden page options.ExpireTimeSpan = TimeSpan.FromDays(7); // Cookie expires after 7 days options.SlidingExpiration = true;              // Reset expiry on activity });


3. **Seeding Roles and Admin User**


using (var scope = app.Services.CreateScope()) { var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(); var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
// Create roles if they don't exist
string[] roles = ["Admin", "Manager", "Member"];
foreach (var role in roles)
{
    if (!await roleManager.RoleExistsAsync(role))
    {
        await roleManager.CreateAsync(new IdentityRole(role));
    }
}

// Create default admin user
var adminEmail = "admin@tms.com";
var adminUser = await userManager.FindByEmailAsync(adminEmail);
if (adminUser is null)
{
    adminUser = new ApplicationUser
    {
        UserName = adminEmail,
        Email = adminEmail,
        FullName = "System Admin",
        EmailConfirmed = true
    };
    
    // Create user with password
    await userManager.CreateAsync(adminUser, "Admin123");
    
    // Assign to Admin role
    await userManager.AddToRoleAsync(adminUser, "Admin");
}
}

4. **Login Logic**


public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page();
// Attempt to sign in
var result = await _signInManager.PasswordSignInAsync(
    Input.Email,              // Username (we use email)
    Input.Password,           // Password
    Input.RememberMe,         // Persistent cookie?
    lockoutOnFailure: false   // Lock account after failures?
);

if (result.Succeeded)
{
    // Success - redirect to return URL or home
    return LocalRedirect(returnUrl ?? "/");
}

if (result.RequiresTwoFactor)
{
    // 2FA required
    return RedirectToPage("./LoginWith2fa");
}

if (result.IsLockedOut)
{
    // Account locked
    return RedirectToPage("./Lockout");
}

// Failed login
ModelState.AddModelError(string.Empty, "Invalid login attempt.");
return Page();
}


5. **Authorization Attributes**

// Require any authenticated user [Authorize] public class IndexModel : PageModel { }
// Require specific role [Authorize(Roles = "Admin")] public class AdminOnlyModel : PageModel { }
// Require one of multiple roles [Authorize(Roles = "Admin,Manager")] public class ManagerOrAdminModel : PageModel { }
// Check in code if (User.IsInRole("Admin")) { // Admin-only code }
// Get current user ID var userId = _userManager.GetUserId(User);
// Get current user object var user = await _userManager.GetUserAsync(User);


---

## 💉 Dependency Injection

### What is Dependency Injection?

**Without DI (bad):**

public class ProjectController { private readonly ProjectService _service;
public ProjectController()
{
    // Tightly coupled - hard to test, inflexible
    var context = new ApplicationDbContext();
    _service = new ProjectService(context);
}
}


**With DI (good):**

public class IndexModel : PageModel { private readonly ProjectService _service;
// Dependencies provided by framework
public IndexModel(ProjectService service)
{
    _service = service;
}
}


### How to Register Services in Program.cs

// Singleton - one instance for entire application lifetime builder.Services.AddSingleton<IMyService, MyService>();
// Scoped - one instance per HTTP request (most common for database) builder.Services.AddScoped<ProjectService>(); builder.Services.AddScoped<TaskService>(); builder.Services.AddDbContext<ApplicationDbContext>(); // Scoped by default
// Transient - new instance every time it's requested builder.Services.AddTransient<IEmailService, EmailService>();



**Lifetimes:**
- **Singleton**: Created once, lives forever (e.g., configuration)
- **Scoped**: Created per request, disposed after request (e.g., DbContext)
- **Transient**: Created every time, short-lived (e.g., utilities)

---

## 🔄 Request Lifecycle

When a user visits `https://tms.com/Projects/Index?status=InProgress`:

1.	HTTP Request arrives ↓
2.	Middleware Pipeline (order matters!) ├── UseHttpsRedirection()     → Redirect HTTP to HTTPS ├── UseStaticFiles()          → Serve CSS, JS, images ├── UseRouting()              → Match URL to endpoint ├── UseAuthentication()       → Read auth cookie, set User ├── UseAuthorization()        → Check [Authorize] attributes └── MapRazorPages()           → Route to Razor Page ↓
3.	Razor Page Activation ├── Create IndexModel instance ├── Inject dependencies (ProjectService, etc.) └── Set properties from route/query (?status=...) ↓
4.	OnGetAsync() Execution ├── Read query parameters ├── Call ProjectService methods ├── Query database via EF Core ├── Populate Projects property └── Return Page() ↓
5.	View Rendering (Index.cshtml) ├── Access Model.Projects ├── Loop through data ├── Generate HTML └── Return HTML response ↓
6.	Browser receives HTML ├── Parse HTML ├── Load CSS/JS from wwwroot └── Display page


---

## 🔍 SQL Queries Generated by EF Core

### C# LINQ:
var projects = await _context.Projects
    .Include(p => p.Tasks)
    .Where(p => p.Status == ProjectStatus.InProgress)
    .ToListAsync();

    
### Generated SQL:
SELECT [p].[Id], [p].[Name], [p].[Description], [p].[StartDate], [p].[EndDate], [p].[Status], [p].[CreatedByUserId], [t].[Id], [t].[Title], [t].[ProjectId], [t].[Status] FROM [Projects] AS [p] LEFT JOIN [TaskItems] AS [t] ON [p].[Id] = [t].[ProjectId] WHERE [p].[Status] = 1 ORDER BY [p].[StartDate] DESC



---

## 📊 Common Patterns Explained

### 1. Async/Await Pattern

/ Synchronous (blocks thread while waiting for database) public List<Project> GetProjects() { return _context.Projects.ToList();  // Blocks here }
// Asynchronous (thread is free while waiting) public async Task<List<Project>> GetProjectsAsync() { return await _context.Projects.ToListAsync();  // Returns to caller }


**Why async?**
- Web server can handle more requests simultaneously
- Thread doesn't sit idle waiting for database
- Better performance under load

### 2. Null-Conditional Operator (`?.`)

// Without null check - throws exception if Department is null var name = project.Department.Name;
// With null-conditional - returns null if Department is null var name = project.Department?.Name;
// Null-coalescing - provide default if null var name = project.Department?.Name ?? "No Department";


### 3. String Interpolation
// Old way (concatenation) var message = "Project " + project.Name + " created by " + user.FullName;
// New way (interpolation) var message = $"Project {project.Name} created by {user.FullName}";


### 4. Collection Initializers
// Old way var statuses = new List<string>(); statuses.Add("InProgress"); statuses.Add("Completed");
// New way var statuses = new List<string> { "InProgress", "Completed" };
// Property initializer var project = new Project { Name = "Test Project", StartDate = DateTime.Today, Status = ProjectStatus.NotStarted };


### 5. LINQ Queries
// Get all completed projects var completed = await _context.Projects .Where(p => p.Status == ProjectStatus.Completed) .ToListAsync();
// Get projects with more than 5 tasks var busy = await _context.Projects .Include(p => p.Tasks) .Where(p => p.Tasks.Count > 5) .ToListAsync();
// Get project names only var names = await _context.Projects .Select(p => p.Name) .ToListAsync();
// Count projects by status var counts = await _context.Projects .GroupBy(p => p.Status) .Select(g => new { Status = g.Key, Count = g.Count() }) .ToListAsync();
