BEGIN TRANSACTION;
GO

ALTER TABLE [TaskItems] ADD [PipelineCheckpointId] int NULL;
GO

ALTER TABLE [TaskItems] ADD [PipelineStageId] int NULL;
GO

CREATE TABLE [PipelineStages] (
    [Id] int NOT NULL IDENTITY,
    [ProjectId] int NOT NULL,
    [Name] nvarchar(120) NOT NULL,
    [Description] nvarchar(500) NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_PipelineStages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PipelineStages_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [PipelineCheckpoints] (
    [Id] int NOT NULL IDENTITY,
    [PipelineStageId] int NOT NULL,
    [Name] nvarchar(120) NOT NULL,
    [Description] nvarchar(500) NULL,
    [SortOrder] int NOT NULL,
    [RequiresApproval] bit NOT NULL,
    [ApprovedAt] datetime2 NULL,
    [ApprovedByUserId] nvarchar(450) NULL,
    CONSTRAINT [PK_PipelineCheckpoints] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PipelineCheckpoints_AspNetUsers_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_PipelineCheckpoints_PipelineStages_PipelineStageId] FOREIGN KEY ([PipelineStageId]) REFERENCES [PipelineStages] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_TaskItems_PipelineCheckpointId] ON [TaskItems] ([PipelineCheckpointId]);
GO

CREATE INDEX [IX_TaskItems_PipelineStageId] ON [TaskItems] ([PipelineStageId]);
GO

CREATE INDEX [IX_PipelineCheckpoints_ApprovedByUserId] ON [PipelineCheckpoints] ([ApprovedByUserId]);
GO

CREATE UNIQUE INDEX [IX_PipelineCheckpoints_PipelineStageId_SortOrder] ON [PipelineCheckpoints] ([PipelineStageId], [SortOrder]);
GO

CREATE UNIQUE INDEX [IX_PipelineStages_ProjectId_SortOrder] ON [PipelineStages] ([ProjectId], [SortOrder]);
GO

ALTER TABLE [TaskItems] ADD CONSTRAINT [FK_TaskItems_PipelineCheckpoints_PipelineCheckpointId] FOREIGN KEY ([PipelineCheckpointId]) REFERENCES [PipelineCheckpoints] ([Id]);
GO

ALTER TABLE [TaskItems] ADD CONSTRAINT [FK_TaskItems_PipelineStages_PipelineStageId] FOREIGN KEY ([PipelineStageId]) REFERENCES [PipelineStages] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260911122715_ProjectPipelineFoundation', N'8.0.31');
GO

COMMIT;
GO
