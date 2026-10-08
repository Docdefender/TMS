BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915125427_TaskDependencies'
)
BEGIN
    CREATE TABLE [TaskDependencies] (
        [Id] int NOT NULL IDENTITY,
        [DependentTaskId] int NOT NULL,
        [PrerequisiteTaskId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_TaskDependencies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TaskDependencies_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_TaskDependencies_TaskItems_DependentTaskId] FOREIGN KEY ([DependentTaskId]) REFERENCES [TaskItems] ([Id]),
        CONSTRAINT [FK_TaskDependencies_TaskItems_PrerequisiteTaskId] FOREIGN KEY ([PrerequisiteTaskId]) REFERENCES [TaskItems] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915125427_TaskDependencies'
)
BEGIN
    CREATE INDEX [IX_TaskDependencies_CreatedByUserId] ON [TaskDependencies] ([CreatedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915125427_TaskDependencies'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TaskDependencies_DependentTaskId_PrerequisiteTaskId] ON [TaskDependencies] ([DependentTaskId], [PrerequisiteTaskId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915125427_TaskDependencies'
)
BEGIN
    CREATE INDEX [IX_TaskDependencies_PrerequisiteTaskId] ON [TaskDependencies] ([PrerequisiteTaskId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915125427_TaskDependencies'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260915125427_TaskDependencies', N'8.0.31');
END;
GO

COMMIT;
GO
