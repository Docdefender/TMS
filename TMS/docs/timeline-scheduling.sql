BEGIN TRANSACTION;
GO

ALTER TABLE [TaskItems] ADD [ActualStartedAt] datetime2 NULL;
GO

ALTER TABLE [TaskItems] ADD [CompletedAt] datetime2 NULL;
GO

ALTER TABLE [TaskItems] ADD [PlannedStartDate] datetime2 NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260911125653_TimelineScheduling', N'8.0.31');
GO

COMMIT;
GO
