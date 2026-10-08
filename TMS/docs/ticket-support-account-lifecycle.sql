BEGIN TRANSACTION;
GO

ALTER TABLE [Departments] ADD [IsTicketSupport] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

UPDATE [Departments] SET [IsTicketSupport] = 1 WHERE [Id] = (SELECT TOP (1) [Id] FROM [Departments] WHERE [Name] = N'Sistem Geliştirme' AND [IsDeleted] = 0 ORDER BY [Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260918090827_TicketSupportDepartment', N'8.0.31');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [AspNetUsers] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
GO

CREATE UNIQUE INDEX [IX_Departments_IsTicketSupport] ON [Departments] ([IsTicketSupport]) WHERE [IsTicketSupport] = 1;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260918091217_AccountLifecycleAndSupportIndex', N'8.0.31');
GO

COMMIT;
GO
