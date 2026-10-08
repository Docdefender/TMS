$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$config = Get-Content (Join-Path $root 'appsettings.json') -Raw | ConvertFrom-Json
$localConfig = Join-Path $root 'appsettings.Local.json'
$connectionString = $env:ConnectionStrings__DefaultConnection
if ([string]::IsNullOrWhiteSpace($connectionString) -and (Test-Path -LiteralPath $localConfig)) {
    $connectionString = (Get-Content -LiteralPath $localConfig -Raw | ConvertFrom-Json).ConnectionStrings.DefaultConnection
}
if ([string]::IsNullOrWhiteSpace($connectionString)) { $connectionString = $config.ConnectionStrings.DefaultConnection }
if ([string]::IsNullOrWhiteSpace($connectionString)) { throw 'Local database connection is not configured.' }
$builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionString)
if ($builder.DataSource -ne 'localhost\SQLEXPRESS' -or $builder.InitialCatalog -ine 'tms') {
    throw 'Unexpected target. This backup procedure was reviewed for localhost\SQLEXPRESS / Tms only.'
}
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$local = Join-Path $root ".local-backups/organization-$stamp"
$null = New-Item -ItemType Directory -Path $local
$conn = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
try {
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))"
    $directory = [string]$cmd.ExecuteScalar()
    if ([string]::IsNullOrWhiteSpace($directory)) { throw 'SQL Server backup directory could not be determined.' }
    $backup = Join-Path $directory "Tms-before-organization-$stamp.bak"
    $cmd.CommandTimeout = 180
    $cmd.CommandText = 'BACKUP DATABASE [Tms] TO DISK = @path WITH COPY_ONLY, CHECKSUM;'
    $null = $cmd.Parameters.AddWithValue('@path', $backup)
    $null = $cmd.ExecuteNonQuery()
    $cmd.CommandText = 'RESTORE VERIFYONLY FROM DISK = @path WITH CHECKSUM;'
    $verifyOnly = $false
    try { $null = $cmd.ExecuteNonQuery(); $verifyOnly = $true }
    catch {
        if ($_.Exception.Message -notmatch 'CREATE DATABASE permission denied') { throw }
        Write-Output 'RESTORE VERIFYONLY is unavailable to this SQL account; checking backup checksum metadata instead.'
    }
    $cmd.Parameters.Clear()
    $cmd.CommandText = 'SELECT COUNT(*) FROM msdb.dbo.backupset b JOIN msdb.dbo.backupmediafamily f ON b.media_set_id=f.media_set_id WHERE f.physical_device_name=@path AND b.database_name=''Tms'' AND b.has_backup_checksums=1 AND b.is_damaged=0 AND b.backup_finish_date IS NOT NULL;'
    $null = $cmd.Parameters.AddWithValue('@path', $backup)
    if ([int]$cmd.ExecuteScalar() -ne 1) { throw 'Backup checksum metadata could not be verified.' }
    Copy-Item -LiteralPath $backup -Destination (Join-Path $local 'Tms.bak')
    $uploads = Join-Path $root 'wwwroot/uploads'
    if (Test-Path $uploads) { Copy-Item -LiteralPath $uploads -Destination (Join-Path $local 'uploads') -Recurse }
    $manifest = [ordered]@{ CreatedAt = (Get-Date).ToString('o'); Server = $builder.DataSource; Database = 'Tms';
        ServerBackup = $backup; LocalBackup = (Join-Path $local 'Tms.bak'); BackupChecksumMetadataVerified = $true; RestoreVerifyOnlyPassed = $verifyOnly;
        Sha256 = (Get-FileHash -LiteralPath (Join-Path $local 'Tms.bak') -Algorithm SHA256).Hash }
    $manifest | ConvertTo-Json | Set-Content (Join-Path $local 'manifest.json') -Encoding utf8
    Write-Output "Verified backup: $local"
} finally { $conn.Dispose() }
