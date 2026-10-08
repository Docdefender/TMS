$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$config = Get-Content (Join-Path $root 'appsettings.json') -Raw | ConvertFrom-Json
$builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($config.ConnectionStrings.DefaultConnection)
if ($builder.DataSource -ne 'localhost\SQLEXPRESS' -or $builder.InitialCatalog -ine 'tms') {
    throw 'Unexpected target. This demo procedure is limited to localhost\SQLEXPRESS / Tms.'
}

$conn = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
try {
    $conn.Open()
    $transaction = $conn.BeginTransaction()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.Transaction = $transaction
        $cmd.CommandText = @'
IF OBJECT_ID(N'[ProjectRelations]', N'U') IS NULL
    THROW 50001, 'ProjectRelations migration is not applied.', 1;
IF COL_LENGTH('ProjectRelations', 'BlockingCheckpointId') IS NULL
    THROW 50004, 'CheckpointProjectDependencies migration is not applied.', 1;

IF (SELECT COUNT(*) FROM [Projects] WHERE [Id] IN (11, 13, 14)) <> 3
    THROW 50002, 'Required visual demo projects 11, 13 and 14 were not found.', 1;

DECLARE @ManagerId nvarchar(450) = (SELECT TOP (1) [Id] FROM [AspNetUsers] WHERE [NormalizedEmail] = 'DENIZ.MANAGER@DEMO.DIZGE.TEST');
IF @ManagerId IS NULL
    THROW 50003, 'Deniz demo Manager was not found.', 1;

IF NOT EXISTS (SELECT 1 FROM [ProjectMembers] WHERE [ProjectId] = 13 AND [UserId] = @ManagerId)
    INSERT INTO [ProjectMembers] ([ProjectId], [UserId], [AddedAt]) VALUES (13, @ManagerId, SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM [ProjectMembers] WHERE [ProjectId] = 14 AND [UserId] = @ManagerId)
    INSERT INTO [ProjectMembers] ([ProjectId], [UserId], [AddedAt]) VALUES (14, @ManagerId, SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM [ProjectRelations] WHERE [SourceProjectId] = 11 AND [TargetProjectId] = 13 AND [Type] = 0)
    INSERT INTO [ProjectRelations] ([SourceProjectId], [TargetProjectId], [Type], [CreatedAt], [CreatedByUserId])
    VALUES (11, 13, 0, SYSUTCDATETIME(), NULL);

IF NOT EXISTS (SELECT 1 FROM [ProjectRelations] WHERE [SourceProjectId] = 11 AND [TargetProjectId] = 14 AND [Type] = 1)
    INSERT INTO [ProjectRelations] ([SourceProjectId], [TargetProjectId], [Type], [CreatedAt], [CreatedByUserId])
    VALUES (11, 14, 1, SYSUTCDATETIME(), NULL);

DECLARE @CheckpointId int = (
    SELECT TOP (1) c.[Id] FROM [PipelineCheckpoints] c
    INNER JOIN [PipelineStages] s ON s.[Id] = c.[PipelineStageId]
    WHERE s.[ProjectId] = 11 AND c.[Name] = N'Geliştirme tamamlandı'
);
IF @CheckpointId IS NULL
    THROW 50005, 'Demo checkpoint was not found.', 1;
UPDATE [ProjectRelations] SET [BlockingCheckpointId] = @CheckpointId
WHERE [SourceProjectId] = 11 AND [TargetProjectId] = 14 AND [Type] = 1;
'@
        $null = $cmd.ExecuteNonQuery()
        $transaction.Commit()
        Write-Output 'Project relation demo data applied to project 11.'
    }
    catch {
        $transaction.Rollback()
        throw
    }
}
finally {
    $conn.Dispose()
}
