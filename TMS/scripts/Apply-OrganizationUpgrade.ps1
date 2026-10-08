param([switch]$Commit)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$config = Get-Content (Join-Path $root 'appsettings.json') -Raw | ConvertFrom-Json
$builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($config.ConnectionStrings.DefaultConnection)
if ($builder.DataSource -ne 'localhost\SQLEXPRESS' -or $builder.InitialCatalog -ine 'tms') { throw 'Unexpected database target.' }
$conn = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
$transaction = $null
try {
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId='20260909090121_OrganizationAndAccess'"
    if ([int]$cmd.ExecuteScalar() -gt 0) { Write-Output 'Organization migration is already recorded; no changes made.'; return }
    & (Join-Path $PSScriptRoot 'Test-OrganizationBaseline.ps1')
    $manifestFile = Get-ChildItem (Join-Path $root '.local-backups/organization-*/manifest.json') | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (!$manifestFile) { throw 'No completed backup manifest.' }
    $manifest = Get-Content $manifestFile.FullName -Raw | ConvertFrom-Json
    if (!$manifest.BackupChecksumMetadataVerified -or $manifest.Database -ine 'Tms' -or $manifest.Server -ne $builder.DataSource) { throw 'Backup does not match target.' }
    if ((Get-FileHash -LiteralPath $manifest.LocalBackup -Algorithm SHA256).Hash -ne $manifest.Sha256) { throw 'Backup hash mismatch.' }
    if ((Get-Date) - [datetime]$manifest.CreatedAt -gt [timespan]::FromHours(2)) { throw 'Create a fresh backup before retrying.' }
    $cmd.CommandText = 'SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId'
    $reader = $cmd.ExecuteReader(); $history = [Collections.Generic.List[string]]::new()
    while ($reader.Read()) { $history.Add($reader.GetString(0)) }; $reader.Close()
    if ($history.Count -ne 8 -or $history[$history.Count-1] -ne '20260518080150_RenameBlockedToInReview') { throw 'Unexpected migration history; re-review baseline.' }
    $countsSql = "SELECT (SELECT COUNT(*) FROM Projects) AS Projects,(SELECT COUNT(*) FROM TaskItems) AS Tasks,(SELECT COUNT(*) FROM AspNetUsers) AS Users,(SELECT COUNT(*) FROM Attachments) AS Attachments,(SELECT COUNT(*) FROM Comments) AS Comments"
    $cmd.CommandText = $countsSql
    $reader = $cmd.ExecuteReader(); $null = $reader.Read(); $before = @(); for($i=0;$i -lt 5;$i++){ $before += $reader.GetInt32($i) }; $reader.Close()
    $transaction = $conn.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    $cmd.Transaction = $transaction; $cmd.CommandTimeout = 120
    $cmd.CommandText = "DECLARE @result int; EXEC @result=sp_getapplock @Resource='Dizge.OrganizationUpgrade',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; IF @result < 0 THROW 50000,'Upgrade lock unavailable',1;"
    $null = $cmd.ExecuteNonQuery()
    $upgrade = Get-Content (Join-Path $root 'docs/organization-and-access.sql') -Raw
    $upgrade = [regex]::Replace($upgrade, '(?m)^\s*(BEGIN TRANSACTION;|COMMIT;)\s*$', '')
    foreach ($batch in [regex]::Split($upgrade, '(?m)^\s*GO\s*$')) {
        if (![string]::IsNullOrWhiteSpace($batch)) { $cmd.CommandText = $batch; $null = $cmd.ExecuteNonQuery() }
    }
    # This is a schema baseline reconciliation, NOT a replay of seed deletion migrations.
    # Original historical entries and all existing category/department rows are preserved.
    $cmd.CommandText = @'
INSERT INTO __EFMigrationsHistory (MigrationId,ProductVersion) VALUES
('20260602075800_InitialCreate','8.0.29'),('20260710130019_RemoveHasDataSeeds','8.0.29');
INSERT INTO AuditLogs (Action,EntityType,EntityId,UserId,Timestamp,Details)
VALUES ('SchemaBaseline','Database',NULL,NULL,SYSUTCDATETIME(),
'Legacy 8 migrations preserved. Current baseline reconciled after 115-column schema comparison; seed deletion was NOT replayed. OrganizationAndAccess applied.');
'@
    $null = $cmd.ExecuteNonQuery()
    $cmd.CommandText = $countsSql
    $reader = $cmd.ExecuteReader(); $null=$reader.Read(); $after=@(); for($i=0;$i -lt 5;$i++){$after+=$reader.GetInt32($i)}; $reader.Close()
    if (($before -join ',') -ne ($after -join ',')) { throw 'Existing record counts changed unexpectedly.' }
    $cmd.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId IN ('20260602075800_InitialCreate','20260710130019_RemoveHasDataSeeds','20260909090121_OrganizationAndAccess');"
    if ([int]$cmd.ExecuteScalar() -ne 3) { throw 'Migration verification failed.' }
    if ($Commit) { $transaction.Commit(); $state='Committed' } else { $transaction.Rollback(); $state='Rehearsed and rolled back' }
    $transaction.Dispose(); $transaction=$null
    [ordered]@{State=$state;At=(Get-Date).ToString('o');Database='Tms';OriginalMigrations=@($history);RecordCounts=$after;Backup=$manifest.LocalBackup} |
        ConvertTo-Json -Depth 4 | Set-Content (Join-Path $manifestFile.DirectoryName ($state.Replace(' ','-')+'.json')) -Encoding utf8
    Write-Output "$state. Existing counts unchanged: Projects=$($after[0]), Tasks=$($after[1]), Users=$($after[2]), Attachments=$($after[3]), Comments=$($after[4])."
} finally {
    if ($transaction) { $transaction.Rollback(); $transaction.Dispose() }
    $conn.Dispose()
}
