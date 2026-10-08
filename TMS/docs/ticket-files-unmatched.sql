BEGIN TRANSACTION;
GO

ALTER TABLE [Attachments] ADD [IsInternal] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [Attachments] ADD [TicketId] int NULL;
GO

CREATE TABLE [TicketIntakes] (
    [Id] int NOT NULL IDENTITY,
    [SenderEmail] nvarchar(320) NOT NULL,
    [Subject] nvarchar(200) NOT NULL,
    [Body] nvarchar(max) NOT NULL,
    [ExternalMessageId] nvarchar(500) NULL,
    [ReceivedAt] datetime2 NOT NULL,
    [SupportDepartmentId] int NOT NULL,
    [MatchedTicketId] int NULL,
    [MatchedAt] datetime2 NULL,
    [MatchedByUserId] nvarchar(max) NULL,
    CONSTRAINT [PK_TicketIntakes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TicketIntakes_Departments_SupportDepartmentId] FOREIGN KEY ([SupportDepartmentId]) REFERENCES [Departments] ([Id]),
    CONSTRAINT [FK_TicketIntakes_Tickets_MatchedTicketId] FOREIGN KEY ([MatchedTicketId]) REFERENCES [Tickets] ([Id])
);
GO

CREATE INDEX [IX_Attachments_TicketId] ON [Attachments] ([TicketId]);
GO

CREATE UNIQUE INDEX [IX_TicketIntakes_ExternalMessageId] ON [TicketIntakes] ([ExternalMessageId]) WHERE [ExternalMessageId] IS NOT NULL;
GO

CREATE INDEX [IX_TicketIntakes_MatchedTicketId] ON [TicketIntakes] ([MatchedTicketId]);
GO

CREATE INDEX [IX_TicketIntakes_SupportDepartmentId_MatchedTicketId] ON [TicketIntakes] ([SupportDepartmentId], [MatchedTicketId]);
GO

ALTER TABLE [Attachments] ADD CONSTRAINT [FK_Attachments_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [Tickets] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260917122957_TicketFilesAndUnmatchedIntake', N'8.0.31');
GO

COMMIT;
GO
