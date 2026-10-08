BEGIN TRANSACTION;
GO

ALTER TABLE [Tickets] ADD [ResolvedByLinkedTask] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260917102726_TicketTaskResolution', N'8.0.31');
GO

COMMIT;
GO
