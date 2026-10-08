BEGIN TRANSACTION;
GO

ALTER TABLE [TaskItems] ADD [DeletionBatchId] uniqueidentifier NULL;
GO

ALTER TABLE [TaskItems] ADD [FirstAssignedByUserId] nvarchar(450) NULL;
GO

ALTER TABLE [Projects] ADD [DeletionBatchId] uniqueidentifier NULL;
GO

ALTER TABLE [Projects] ADD [FirstAssignedByUserId] nvarchar(450) NULL;
GO

CREATE TABLE [ManagerDepartments] (
    [DepartmentId] int NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [IsDefault] bit NOT NULL,
    CONSTRAINT [PK_ManagerDepartments] PRIMARY KEY ([DepartmentId], [UserId]),
    CONSTRAINT [FK_ManagerDepartments_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_ManagerDepartments_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id])
);
GO

CREATE TABLE [TaskAssignments] (
    [Id] int NOT NULL IDENTITY,
    [TaskItemId] int NOT NULL,
    [PreviousUserId] nvarchar(450) NULL,
    [AssignedToUserId] nvarchar(450) NULL,
    [AssignedByUserId] nvarchar(450) NOT NULL,
    [AssignedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TaskAssignments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TaskAssignments_TaskItems_TaskItemId] FOREIGN KEY ([TaskItemId]) REFERENCES [TaskItems] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [TaskHistoryAccesses] (
    [TaskItemId] int NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [RevokedAt] datetime2 NULL,
    [RevokedByUserId] nvarchar(max) NULL,
    CONSTRAINT [PK_TaskHistoryAccesses] PRIMARY KEY ([TaskItemId], [UserId]),
    CONSTRAINT [FK_TaskHistoryAccesses_TaskItems_TaskItemId] FOREIGN KEY ([TaskItemId]) REFERENCES [TaskItems] ([Id]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_ManagerDepartments_DepartmentId] ON [ManagerDepartments] ([DepartmentId]) WHERE [IsDefault] = 1;
GO

CREATE INDEX [IX_ManagerDepartments_UserId] ON [ManagerDepartments] ([UserId]);
GO

CREATE INDEX [IX_TaskAssignments_TaskItemId] ON [TaskAssignments] ([TaskItemId]);
GO

INSERT INTO ManagerDepartments (DepartmentId, UserId, IsDefault)
SELECT u.DepartmentId, u.Id, 0 FROM AspNetUsers u
INNER JOIN Departments d ON d.Id = u.DepartmentId AND d.IsDeleted = 0
WHERE EXISTS (SELECT 1 FROM AspNetUserRoles ur INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
              WHERE ur.UserId = u.Id AND r.Name = 'Manager')
  AND NOT EXISTS (SELECT 1 FROM AspNetUserRoles ur INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
                  WHERE ur.UserId = u.Id AND r.Name = 'Admin');
UPDATE md SET IsDefault = 1 FROM ManagerDepartments md
WHERE (SELECT COUNT(*) FROM ManagerDepartments other WHERE other.DepartmentId = md.DepartmentId) = 1;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260909090121_OrganizationAndAccess', N'8.0.29');
GO

COMMIT;
GO
