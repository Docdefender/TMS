BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915114546_PipelineHistoryProjectScope'
)
BEGIN
    ALTER TABLE [AuditLogs] ADD [ProjectId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915114546_PipelineHistoryProjectScope'
)
BEGIN
    UPDATE audit
    SET ProjectId = stage.ProjectId
    FROM AuditLogs audit
    INNER JOIN PipelineStages stage ON stage.Id = audit.EntityId
    WHERE audit.EntityType = 'PipelineStage' AND audit.ProjectId IS NULL;

    UPDATE audit
    SET ProjectId = stage.ProjectId
    FROM AuditLogs audit
    INNER JOIN PipelineCheckpoints checkpointItem ON checkpointItem.Id = audit.EntityId
    INNER JOIN PipelineStages stage ON stage.Id = checkpointItem.PipelineStageId
    WHERE audit.EntityType = 'PipelineCheckpoint' AND audit.ProjectId IS NULL;

    UPDATE audit
    SET ProjectId = taskItem.ProjectId
    FROM AuditLogs audit
    INNER JOIN TaskItems taskItem ON taskItem.Id = audit.EntityId
    WHERE audit.EntityType = 'TaskItem' AND audit.ProjectId IS NULL;

    UPDATE audit
    SET ProjectId = relation.SourceProjectId
    FROM AuditLogs audit
    INNER JOIN ProjectRelations relation ON relation.Id = audit.EntityId
    WHERE audit.EntityType = 'ProjectRelation' AND audit.ProjectId IS NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915114546_PipelineHistoryProjectScope'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_ProjectId_Timestamp] ON [AuditLogs] ([ProjectId], [Timestamp]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915114546_PipelineHistoryProjectScope'
)
BEGIN
    ALTER TABLE [AuditLogs] ADD CONSTRAINT [FK_AuditLogs_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915114546_PipelineHistoryProjectScope'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260915114546_PipelineHistoryProjectScope', N'8.0.31');
END;
GO

COMMIT;
GO
