BEGIN TRANSACTION;
GO

CREATE TABLE [ProjectRelations] (
    [Id] int NOT NULL IDENTITY,
    [SourceProjectId] int NOT NULL,
    [TargetProjectId] int NOT NULL,
    [Type] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedByUserId] nvarchar(450) NULL,
    CONSTRAINT [PK_ProjectRelations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProjectRelations_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_ProjectRelations_Projects_SourceProjectId] FOREIGN KEY ([SourceProjectId]) REFERENCES [Projects] ([Id]),
    CONSTRAINT [FK_ProjectRelations_Projects_TargetProjectId] FOREIGN KEY ([TargetProjectId]) REFERENCES [Projects] ([Id])
);
GO

CREATE INDEX [IX_ProjectRelations_CreatedByUserId] ON [ProjectRelations] ([CreatedByUserId]);
GO

CREATE UNIQUE INDEX [IX_ProjectRelations_SourceProjectId_TargetProjectId_Type] ON [ProjectRelations] ([SourceProjectId], [TargetProjectId], [Type]);
GO

CREATE INDEX [IX_ProjectRelations_TargetProjectId] ON [ProjectRelations] ([TargetProjectId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260914125751_ProjectRelations', N'8.0.31');
GO

COMMIT;
GO
