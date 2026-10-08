BEGIN TRANSACTION;
GO

ALTER TABLE [Tickets] ADD [ExternalConversationId] nvarchar(500) NULL;
GO

ALTER TABLE [Tickets] ADD [SourceMailboxAddress] nvarchar(320) NULL;
GO

ALTER TABLE [TicketMessages] ADD [ExternalConversationId] nvarchar(500) NULL;
GO

ALTER TABLE [TicketMessages] ADD [ProviderMessageId] nvarchar(1000) NULL;
GO

ALTER TABLE [TicketIntakes] ADD [ExternalConversationId] nvarchar(500) NULL;
GO

ALTER TABLE [TicketIntakes] ADD [ProviderMessageId] nvarchar(1000) NULL;
GO

ALTER TABLE [TicketIntakes] ADD [SourceMailboxAddress] nvarchar(320) NULL;
GO

CREATE TABLE [TicketMailboxSyncStates] (
    [Id] int NOT NULL IDENTITY,
    [MailboxAddress] nvarchar(320) NOT NULL,
    [SyncLink] nvarchar(max) NULL,
    [LastAttemptAt] datetime2 NULL,
    [LastSuccessfulSyncAt] datetime2 NULL,
    [LastError] nvarchar(2000) NULL,
    [ConsecutiveFailures] int NOT NULL,
    CONSTRAINT [PK_TicketMailboxSyncStates] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_Tickets_SourceMailboxAddress_ExternalConversationId] ON [Tickets] ([SourceMailboxAddress], [ExternalConversationId]);
GO

CREATE UNIQUE INDEX [IX_TicketMailboxSyncStates_MailboxAddress] ON [TicketMailboxSyncStates] ([MailboxAddress]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260929113216_Microsoft365TicketMailSync', N'8.0.31');
GO

COMMIT;
GO
