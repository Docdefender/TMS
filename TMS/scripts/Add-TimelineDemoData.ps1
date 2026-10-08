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
IF COL_LENGTH('dbo.TaskItems', 'PlannedStartDate') IS NULL
    THROW 51000, 'TimelineScheduling migration is not applied.', 1;

DECLARE @ProjectId int = 11;
IF NOT EXISTS (SELECT 1 FROM dbo.Projects WHERE Id = @ProjectId AND IsDeleted = 0)
    THROW 51001, 'Visual demo project 11 was not found.', 1;

UPDATE dbo.TaskItems SET
    PlannedStartDate = '2026-08-28', DueDate = '2026-09-04', Status = 3,
    ActualStartedAt = '2026-08-28T09:00:00', CompletedAt = '2026-09-04T16:00:00'
WHERE ProjectId = @ProjectId AND Id = 1016;

UPDATE dbo.TaskItems SET
    PlannedStartDate = '2026-09-03', DueDate = '2026-09-17', Status = 1,
    ActualStartedAt = '2026-09-03T10:00:00', CompletedAt = NULL
WHERE ProjectId = @ProjectId AND Id = 1017;

UPDATE dbo.TaskItems SET
    PlannedStartDate = '2026-09-08', DueDate = '2026-09-18', Status = 2,
    ActualStartedAt = '2026-09-08T11:00:00', CompletedAt = NULL
WHERE ProjectId = @ProjectId AND Id = 1018;

UPDATE dbo.TaskItems SET
    PlannedStartDate = '2026-09-18', DueDate = '2026-10-01', Status = 0,
    ActualStartedAt = NULL, CompletedAt = NULL
WHERE ProjectId = @ProjectId AND Id = 1019;

SELECT COUNT(*) FROM dbo.TaskItems
WHERE ProjectId = @ProjectId AND PlannedStartDate IS NOT NULL;
'@
        $updated = [int]$command.ExecuteScalar()
        if ($updated -ne 4) { throw "Expected four scheduled demo tasks, found $updated." }
        $transaction.Commit()
        Write-Output 'Timeline demo data applied to four tasks.'
    }
    catch {
        $transaction.Rollback()
        throw
    }
}
finally {
    $connection.Dispose()
}
