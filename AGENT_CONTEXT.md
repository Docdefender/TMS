# Project Management System - Agent Mode Context

## Current Status
- **Phase**: Development
- **Framework**: ASP.NET Core Razor Pages (.NET 8)
- **Database**: SQL Server
- **Last Updated**: Current session

## Completed Tasks
1. ✅ Entity models created (Project.cs, TaskItem.cs)
2. ✅ DbContext created (ApplicationDbContext.cs)
3. ✅ Program.cs configured with DbContext registration
4. ✅ appsettings.json template provided
5. ✅ Service Layer (ProjectService, TaskService)
6. ✅ Razor Pages (Projects/Index, Projects/Create, Projects/Details)
7. ✅ EF Core NuGet packages installed

## In Progress
- [ ] Database connection and migrations

## Architecture Overview
- **Models**: Project, TaskItem with enums (ProjectStatus, TaskStatus)
- **Data**: ApplicationDbContext with EF Core
- **Services**: Business logic layer
- **Pages**: Razor Pages for UI
- **No Controllers**: Pure Razor Pages approach

## Key Files Location
- `/Models/Project.cs` - Project entity
- `/Models/TaskItem.cs` - TaskItem entity
- `/Data/ApplicationDbContext.cs` - DbContext
- `/Services/ProjectService.cs` - Project business logic (to create)
- `/Services/TaskService.cs` - Task business logic (to create)
- `/Pages/Projects/` - Project pages (to create)

## Important Notes
- All database operations use async/await
- Dependency injection for all services
- Database connection will be configured last
- Keep code simple and production-ready

## Next Steps
1. Create Services (ProjectService, TaskService)
2. Create Razor Pages (Index, Create, Details)
3. Add database connection
4. Test the application

## Issues & Decisions
- None yet