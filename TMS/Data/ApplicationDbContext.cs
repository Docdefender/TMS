using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TMS.Models;

namespace TMS.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<ManagerDepartment> ManagerDepartments => Set<ManagerDepartment>();
    public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();
    public DbSet<TaskHistoryAccess> TaskHistoryAccesses => Set<TaskHistoryAccess>();
    public DbSet<TaskLabel> TaskLabels => Set<TaskLabel>();
    public DbSet<TaskItemLabel> TaskItemLabels => Set<TaskItemLabel>();
    public DbSet<PipelineStage> PipelineStages => Set<PipelineStage>();
    public DbSet<PipelineCheckpoint> PipelineCheckpoints => Set<PipelineCheckpoint>();
    public DbSet<ProjectRelation> ProjectRelations => Set<ProjectRelation>();
    public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketViewer> TicketViewers => Set<TicketViewer>();
    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();
    public DbSet<TicketEvent> TicketEvents => Set<TicketEvent>();
    public DbSet<TicketIntake> TicketIntakes => Set<TicketIntake>();
    public DbSet<TicketMailboxSyncState> TicketMailboxSyncStates => Set<TicketMailboxSyncState>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<TaskTimeEntry> TaskTimeEntries => Set<TaskTimeEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Notification>().HasOne(x => x.User).WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Notification>().HasOne(x => x.ActorUser).WithMany()
            .HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Notification>().HasIndex(x => new { x.UserId, x.ReadAt, x.CreatedAt });

        modelBuilder.Entity<TaskTimeEntry>().HasOne(x => x.TaskItem).WithMany()
            .HasForeignKey(x => x.TaskItemId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TaskTimeEntry>().HasOne(x => x.User).WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<TaskTimeEntry>().HasIndex(x => new { x.TaskItemId, x.StartedAt });
        modelBuilder.Entity<TaskTimeEntry>().HasIndex(x => x.UserId).IsUnique()
            .HasFilter("[EndedAt] IS NULL");

        modelBuilder.Entity<Ticket>().HasOne(x => x.Requester).WithMany()
            .HasForeignKey(x => x.RequesterUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Ticket>().HasOne(x => x.SupportDepartment).WithMany()
            .HasForeignKey(x => x.SupportDepartmentId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Ticket>().HasOne(x => x.AssignedToUser).WithMany()
            .HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Ticket>().HasOne(x => x.LinkedTask).WithMany()
            .HasForeignKey(x => x.LinkedTaskId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Ticket>().HasIndex(x => x.LinkedTaskId).IsUnique().HasFilter("[LinkedTaskId] IS NOT NULL");
        modelBuilder.Entity<Ticket>().HasIndex(x => new { x.SupportDepartmentId, x.Status, x.AssignedToUserId });
        modelBuilder.Entity<Ticket>().HasIndex(x => new { x.SupportDepartmentId, x.SlaDueAt });
        modelBuilder.Entity<Ticket>().HasIndex(x => new { x.SourceMailboxAddress, x.ExternalConversationId });
        modelBuilder.Entity<TicketViewer>().HasKey(x => new { x.TicketId, x.UserId });
        modelBuilder.Entity<TicketViewer>().HasOne(x => x.Ticket).WithMany(x => x.Viewers)
            .HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TicketViewer>().HasOne(x => x.User).WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<TicketMessage>().HasOne(x => x.Ticket).WithMany(x => x.Messages)
            .HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TicketMessage>().HasOne(x => x.AuthorUser).WithMany()
            .HasForeignKey(x => x.AuthorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<TicketMessage>().HasIndex(x => x.ExternalMessageId).IsUnique()
            .HasFilter("[ExternalMessageId] IS NOT NULL");
        modelBuilder.Entity<TicketEvent>().HasOne(x => x.Ticket).WithMany(x => x.Events)
            .HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TicketEvent>().HasOne(x => x.ActorUser).WithMany()
            .HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<TicketIntake>().HasOne(x => x.SupportDepartment).WithMany()
            .HasForeignKey(x => x.SupportDepartmentId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<TicketIntake>().HasOne(x => x.MatchedTicket).WithMany()
            .HasForeignKey(x => x.MatchedTicketId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<TicketIntake>().HasIndex(x => x.ExternalMessageId).IsUnique()
            .HasFilter("[ExternalMessageId] IS NOT NULL");
        modelBuilder.Entity<TicketIntake>().HasIndex(x => new { x.SupportDepartmentId, x.MatchedTicketId });
        modelBuilder.Entity<TicketIntake>().HasQueryFilter(x => !x.SupportDepartment.IsDeleted);
        modelBuilder.Entity<TicketMailboxSyncState>().HasIndex(x => x.MailboxAddress).IsUnique();
        modelBuilder.Entity<Attachment>().HasOne(x => x.Ticket).WithMany()
            .HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ManagerDepartment>().HasKey(x => new { x.DepartmentId, x.UserId });
        modelBuilder.Entity<Department>().HasIndex(x => x.IsTicketSupport).IsUnique()
            .HasFilter("[IsTicketSupport] = 1");
        modelBuilder.Entity<ManagerDepartment>().HasOne(x => x.Department).WithMany(x => x.Managers)
            .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ManagerDepartment>().HasOne(x => x.User).WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ManagerDepartment>().HasIndex(x => x.DepartmentId).IsUnique().HasFilter("[IsDefault] = 1");
        modelBuilder.Entity<TaskAssignment>().HasOne(x => x.TaskItem).WithMany(x => x.AssignmentHistory)
            .HasForeignKey(x => x.TaskItemId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TaskAssignment>().Property(x => x.AssignedByUserId).HasMaxLength(450);
        modelBuilder.Entity<TaskAssignment>().Property(x => x.AssignedToUserId).HasMaxLength(450);
        modelBuilder.Entity<TaskAssignment>().Property(x => x.PreviousUserId).HasMaxLength(450);
        modelBuilder.Entity<TaskHistoryAccess>().HasKey(x => new { x.TaskItemId, x.UserId });
        modelBuilder.Entity<TaskHistoryAccess>().HasOne(x => x.TaskItem).WithMany(x => x.HistoryAccess)
            .HasForeignKey(x => x.TaskItemId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TaskLabel>().HasIndex(x => x.NormalizedName).IsUnique();
        modelBuilder.Entity<TaskItemLabel>().HasKey(x => new { x.TaskItemId, x.TaskLabelId });
        modelBuilder.Entity<TaskItemLabel>().HasOne(x => x.TaskItem).WithMany(x => x.Labels)
            .HasForeignKey(x => x.TaskItemId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TaskItemLabel>().HasOne(x => x.TaskLabel).WithMany(x => x.Tasks)
            .HasForeignKey(x => x.TaskLabelId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TaskItemLabel>().HasQueryFilter(x => !x.TaskItem.IsDeleted && !x.TaskItem.Project.IsDeleted);
        modelBuilder.Entity<Project>().Property(x => x.FirstAssignedByUserId).HasMaxLength(450);
        modelBuilder.Entity<TaskItem>().Property(x => x.FirstAssignedByUserId).HasMaxLength(450);
        modelBuilder.Entity<ManagerDepartment>().HasQueryFilter(x => !x.Department.IsDeleted);
        modelBuilder.Entity<TaskAssignment>().HasQueryFilter(x => !x.TaskItem.IsDeleted);
        modelBuilder.Entity<TaskHistoryAccess>().HasQueryFilter(x => !x.TaskItem.IsDeleted);
        modelBuilder.Entity<TaskTimeEntry>().HasQueryFilter(x => !x.TaskItem.IsDeleted && !x.TaskItem.Project.IsDeleted);

        modelBuilder.Entity<PipelineStage>()
            .HasOne(x => x.Project).WithMany(x => x.PipelineStages)
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PipelineStage>()
            .HasIndex(x => new { x.ProjectId, x.SortOrder }).IsUnique();
        modelBuilder.Entity<PipelineCheckpoint>()
            .HasOne(x => x.PipelineStage).WithMany(x => x.Checkpoints)
            .HasForeignKey(x => x.PipelineStageId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PipelineStage>().HasQueryFilter(x => !x.Project.IsDeleted);
        modelBuilder.Entity<PipelineCheckpoint>().HasQueryFilter(x => !x.PipelineStage.Project.IsDeleted);
        modelBuilder.Entity<PipelineCheckpoint>()
            .HasOne(x => x.ApprovedByUser).WithMany()
            .HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<PipelineCheckpoint>()
            .HasIndex(x => new { x.PipelineStageId, x.SortOrder }).IsUnique();
        modelBuilder.Entity<TaskItem>()
            .HasOne(x => x.PipelineStage).WithMany(x => x.Tasks)
            .HasForeignKey(x => x.PipelineStageId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<TaskItem>()
            .HasOne(x => x.PipelineCheckpoint).WithMany(x => x.Tasks)
            .HasForeignKey(x => x.PipelineCheckpointId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ProjectRelation>()
            .HasOne(x => x.SourceProject).WithMany(x => x.OutgoingRelations)
            .HasForeignKey(x => x.SourceProjectId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ProjectRelation>()
            .HasOne(x => x.TargetProject).WithMany(x => x.IncomingRelations)
            .HasForeignKey(x => x.TargetProjectId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ProjectRelation>()
            .HasOne(x => x.CreatedByUser).WithMany()
            .HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ProjectRelation>()
            .HasOne(x => x.BlockingCheckpoint).WithMany(x => x.BlockingProjectRelations)
            .HasForeignKey(x => x.BlockingCheckpointId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ProjectRelation>()
            .HasIndex(x => new { x.SourceProjectId, x.TargetProjectId, x.Type }).IsUnique();
        modelBuilder.Entity<ProjectRelation>()
            .HasQueryFilter(x => !x.SourceProject.IsDeleted && !x.TargetProject.IsDeleted);

        modelBuilder.Entity<TaskDependency>()
            .HasOne(x => x.DependentTask).WithMany(x => x.PrerequisiteDependencies)
            .HasForeignKey(x => x.DependentTaskId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<TaskDependency>()
            .HasOne(x => x.PrerequisiteTask).WithMany(x => x.DependentDependencies)
            .HasForeignKey(x => x.PrerequisiteTaskId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<TaskDependency>()
            .HasOne(x => x.CreatedByUser).WithMany()
            .HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<TaskDependency>()
            .HasIndex(x => new { x.DependentTaskId, x.PrerequisiteTaskId }).IsUnique();
        modelBuilder.Entity<TaskDependency>()
            .HasQueryFilter(x => !x.DependentTask.IsDeleted && !x.PrerequisiteTask.IsDeleted
                && !x.DependentTask.Project.IsDeleted && !x.PrerequisiteTask.Project.IsDeleted);

        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.CreatedByUser)
            .WithMany()
            .HasForeignKey(p => p.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.AssignedToUser)
            .WithMany()
            .HasForeignKey(p => p.AssignedToUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.Manager)
            .WithMany()
            .HasForeignKey(p => p.ManagerUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.Project)
            .WithMany(p => p.Members)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false); // ← bunu ekleyin

        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.User)
            .WithMany()
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ProjectMember>()
            .HasIndex(pm => new { pm.ProjectId, pm.UserId })
            .IsUnique();

        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.CreatedByUser)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.AssignedToUser)
            .WithMany()
            .HasForeignKey(t => t.AssignedToUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<AuditLog>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<AuditLog>()
            .HasOne(a => a.Project)
            .WithMany(p => p.AuditLogs)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.ProjectId, a.Timestamp });

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.Department)
            .WithMany()
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.Department)
            .WithMany()
            .HasForeignKey(p => p.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Project)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.TaskItem)
            .WithMany(t => t.Comments)
            .HasForeignKey(c => c.TaskItemId)
            .OnDelete(DeleteBehavior.NoAction);

        // Global query filters for soft deletes
        modelBuilder.Entity<Project>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<TaskItem>().HasQueryFilter(t => !t.IsDeleted);
        modelBuilder.Entity<Comment>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Department>().HasQueryFilter(d => !d.IsDeleted);
        modelBuilder.Entity<Category>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Attachment>().HasQueryFilter(a => !a.IsDeleted);

        modelBuilder.Entity<Attachment>()
            .HasOne(a => a.UploadedByUser)
            .WithMany()
            .HasForeignKey(a => a.UploadedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Attachment>()
            .HasOne(a => a.Project)
            .WithMany(p => p.Attachments)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Attachment>()
            .HasOne(a => a.TaskItem)
            .WithMany(t => t.Attachments)
            .HasForeignKey(a => a.TaskItemId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
