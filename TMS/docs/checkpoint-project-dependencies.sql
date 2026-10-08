BEGIN TRANSACTION;
GO

ALTER TABLE [ProjectRelations] ADD [BlockingCheckpointId] int NULL;
GO

CREATE INDEX [IX_ProjectRelations_BlockingCheckpointId] ON [ProjectRelations] ([BlockingCheckpointId]);
GO

ALTER TABLE [ProjectRelations] ADD CONSTRAINT [FK_ProjectRelations_PipelineCheckpoints_BlockingCheckpointId] FOREIGN KEY ([BlockingCheckpointId]) REFERENCES [PipelineCheckpoints] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260915113520_CheckpointProjectDependencies', N'8.0.31');
GO

COMMIT;
GO
