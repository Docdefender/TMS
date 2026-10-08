BEGIN TRANSACTION;
GO

CREATE TABLE [Tickets] (
    [Id] int NOT NULL IDENTITY,
    [Subject] nvarchar(200) NOT NULL,
    [Status] int NOT NULL,
    [RequesterUserId] nvarchar(450) NOT NULL,
    [RequesterDepartmentId] int NULL,
    [SupportDepartmentId] int NULL,
    [AssignedToUserId] nvarchar(450) NULL,
    [LinkedTaskId] int NULL,
    [HasNewReply] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Tickets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tickets_AspNetUsers_AssignedToUserId] FOREIGN KEY ([AssignedToUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_Tickets_AspNetUsers_RequesterUserId] FOREIGN KEY ([RequesterUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_Tickets_Departments_SupportDepartmentId] FOREIGN KEY ([SupportDepartmentId]) REFERENCES [Departments] ([Id]),
    CONSTRAINT [FK_Tickets_TaskItems_LinkedTaskId] FOREIGN KEY ([LinkedTaskId]) REFERENCES [TaskItems] ([Id])
);
GO

CREATE TABLE [TicketEvents] (
    [Id] int NOT NULL IDENTITY,
    [TicketId] int NOT NULL,
    [ActorUserId] nvarchar(450) NULL,
    [Type] nvarchar(40) NOT NULL,
    [Details] nvarchar(1000) NULL,
    [OccurredAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TicketEvents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TicketEvents_AspNetUsers_ActorUserId] FOREIGN KEY ([ActorUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_TicketEvents_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [Tickets] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [TicketMessages] (
    [Id] int NOT NULL IDENTITY,
    [TicketId] int NOT NULL,
    [AuthorUserId] nvarchar(450) NULL,
    [Body] nvarchar(max) NOT NULL,
    [IsInternal] bit NOT NULL,
    [IsIncoming] bit NOT NULL,
    [ExternalMessageId] nvarchar(500) NULL,
    [SentAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TicketMessages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TicketMessages_AspNetUsers_AuthorUserId] FOREIGN KEY ([AuthorUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_TicketMessages_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [Tickets] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [TicketViewers] (
    [TicketId] int NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [IsFollowing] bit NOT NULL,
    CONSTRAINT [PK_TicketViewers] PRIMARY KEY ([TicketId], [UserId]),
    CONSTRAINT [FK_TicketViewers_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_TicketViewers_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [Tickets] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_TicketEvents_ActorUserId] ON [TicketEvents] ([ActorUserId]);
GO

CREATE INDEX [IX_TicketEvents_TicketId] ON [TicketEvents] ([TicketId]);
GO

CREATE INDEX [IX_TicketMessages_AuthorUserId] ON [TicketMessages] ([AuthorUserId]);
GO

CREATE UNIQUE INDEX [IX_TicketMessages_ExternalMessageId] ON [TicketMessages] ([ExternalMessageId]) WHERE [ExternalMessageId] IS NOT NULL;
GO

CREATE INDEX [IX_TicketMessages_TicketId] ON [TicketMessages] ([TicketId]);
GO

CREATE INDEX [IX_Tickets_AssignedToUserId] ON [Tickets] ([AssignedToUserId]);
GO

CREATE UNIQUE INDEX [IX_Tickets_LinkedTaskId] ON [Tickets] ([LinkedTaskId]) WHERE [LinkedTaskId] IS NOT NULL;
GO

CREATE INDEX [IX_Tickets_RequesterUserId] ON [Tickets] ([RequesterUserId]);
GO

CREATE INDEX [IX_Tickets_SupportDepartmentId_Status_AssignedToUserId] ON [Tickets] ([SupportDepartmentId], [Status], [AssignedToUserId]);
GO

CREATE INDEX [IX_TicketViewers_UserId] ON [TicketViewers] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260917084108_TicketCoreFoundation', N'8.0.31');
GO

COMMIT;
GO
