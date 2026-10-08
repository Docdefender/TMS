$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$config = Get-Content (Join-Path $root 'appsettings.json') -Raw | ConvertFrom-Json
$builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($config.ConnectionStrings.DefaultConnection)
if ($builder.DataSource -ne 'localhost\SQLEXPRESS' -or $builder.InitialCatalog -ine 'tms') {
    throw 'Unexpected target. This script is limited to localhost\SQLEXPRESS / Tms.'
}

$connection = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
try {
    $connection.Open()
    $transaction = $connection.BeginTransaction()
    try {
        $command = $connection.CreateCommand()
        $command.Transaction = $transaction
        $command.CommandText = @'
IF OBJECT_ID('dbo.TaskDependencies', 'U') IS NULL
    THROW 51000, 'TaskDependencies migration is not applied.', 1;

DECLARE @ProjectId int = 11;
IF NOT EXISTS (SELECT 1 FROM dbo.Projects WHERE Id = @ProjectId AND IsDeleted = 0 AND LEFT(Name, 6) = N'[TEST]')
    THROW 51001, 'Visual demo project 11 was not found.', 1;

DECLARE @ActorId nvarchar(450) = (
    SELECT TOP (1) Id FROM dbo.AspNetUsers WHERE NormalizedEmail = 'DENIZ.MANAGER@DEMO.DIZGE.TEST'
);
IF @ActorId IS NULL
    THROW 51002, 'Demo Manager Deniz was not found.', 1;

DECLARE @Analysis int = 1016;
DECLARE @Development int = 1017;
DECLARE @Review int = 1018;
DECLARE @Delivery int = 1019;
IF (SELECT COUNT(*) FROM dbo.TaskItems WHERE ProjectId = @ProjectId AND IsDeleted = 0
    AND Id IN (@Analysis, @Development, @Review, @Delivery)) <> 4
    THROW 51003, 'Expected demo tasks were not found.', 1;

DECLARE @Added TABLE (Id int, DependentTaskId int, PrerequisiteTaskId int);

INSERT dbo.TaskDependencies (DependentTaskId, PrerequisiteTaskId, CreatedAt, CreatedByUserId)
OUTPUT inserted.Id, inserted.DependentTaskId, inserted.PrerequisiteTaskId INTO @Added
SELECT pair.DependentTaskId, pair.PrerequisiteTaskId, SYSUTCDATETIME(), @ActorId
FROM (VALUES
    (@Development, @Analysis),
    (@Review, @Development),
    (@Delivery, @Review)
) pair(DependentTaskId, PrerequisiteTaskId)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.TaskDependencies existing
    WHERE existing.DependentTaskId = pair.DependentTaskId
      AND existing.PrerequisiteTaskId = pair.PrerequisiteTaskId
);

INSERT dbo.AuditLogs (Action, EntityType, EntityId, UserId, Timestamp, Details, ProjectId)
SELECT 'TaskDependencyCreated', 'TaskDependency', added.Id, @ActorId, SYSUTCDATETIME(),
       CONCAT('Task dependency ', added.DependentTaskId, ' -> ', added.PrerequisiteTaskId, ' created.'), @ProjectId
FROM @Added added;

SELECT COUNT(*) FROM dbo.TaskDependencies dependency
JOIN dbo.TaskItems task ON task.Id = dependency.DependentTaskId
WHERE task.ProjectId = @ProjectId;
'@
        $count = [int]$command.ExecuteScalar()
        if ($count -lt 3) { throw "Expected at least three demo task dependencies, found $count." }
        $transaction.Commit()
        Write-Output "Task dependency demo chain is ready with $count links in project 11."
    }
    catch {
        $transaction.Rollback()
        throw
    }
}
finally {
    $connection.Dispose()
}
