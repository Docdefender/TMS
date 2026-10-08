$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$configPath = Join-Path $root 'appsettings.Local.json'
if (!(Test-Path $configPath)) { $configPath = Join-Path $root 'appsettings.json' }
$config = Get-Content $configPath -Raw | ConvertFrom-Json
$builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($config.ConnectionStrings.DefaultConnection)

if ($builder.DataSource -notmatch '^(localhost|\.)(\\SQLEXPRESS)?$' -or $builder.InitialCatalog -ine 'tms') {
    throw 'Only the local Tms database is allowed.'
}

$backup = Get-ChildItem (Join-Path $root '.local-backups') -Filter manifest.json -Recurse |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if (!$backup -or $backup.LastWriteTime -lt (Get-Date).AddHours(-2)) {
    throw 'A fresh local backup is required.'
}

$connection = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
$connection.Open()
$transaction = $connection.BeginTransaction()

function ExecuteScalar($sql, $values = @{}) {
    $command = $connection.CreateCommand()
    $command.Transaction = $transaction
    $command.CommandText = $sql
    foreach ($key in $values.Keys) {
        $null = $command.Parameters.AddWithValue('@' + $key, $values[$key])
    }
    try { return $command.ExecuteScalar() } finally { $command.Dispose() }
}

$labels = @{
    'Analiz'   = '#2563EB'
    'Frontend' = '#7C3AED'
    'Backend'  = '#0891B2'
    'QA'       = '#D97706'
    'Teslim'   = '#16A34A'
}

$taskProfiles = @(
    @{ Title = 'İhtiyaç analizi ve kapsam'; Priority = 2; Labels = @('Analiz') },
    @{ Title = 'Arayüz ve uygulama geliştirme'; Priority = 3; Labels = @('Frontend', 'Backend') },
    @{ Title = 'Ekip incelemesi ve kalite kontrol'; Priority = 2; Labels = @('QA') },
    @{ Title = 'Teslim dokümanı ve kabul'; Priority = 0; Labels = @('Teslim') }
)

try {
    $demoTaskCount = ExecuteScalar @'
SELECT COUNT(*)
FROM TaskItems t
INNER JOIN Projects p ON p.Id = t.ProjectId
WHERE p.Name LIKE N'[[]TEST] Görsel Demo %' AND t.IsDeleted = 0 AND p.IsDeleted = 0
'@
    if ($demoTaskCount -eq 0) { throw 'Visual demo tasks were not found.' }

    foreach ($labelName in $labels.Keys) {
        $normalizedName = $labelName.ToUpperInvariant()
        $null = ExecuteScalar @'
IF NOT EXISTS (SELECT 1 FROM TaskLabels WHERE NormalizedName = @normalizedName)
BEGIN
    INSERT INTO TaskLabels(Name, NormalizedName, Color)
    VALUES(@name, @normalizedName, @color)
END
'@ @{ name = $labelName; normalizedName = $normalizedName; color = $labels[$labelName] }
    }

    foreach ($profile in $taskProfiles) {
        $null = ExecuteScalar @'
UPDATE t
SET Priority = @priority
FROM TaskItems t
INNER JOIN Projects p ON p.Id = t.ProjectId
WHERE p.Name LIKE N'[[]TEST] Görsel Demo %'
  AND p.IsDeleted = 0
  AND t.IsDeleted = 0
  AND t.Title = @title
'@ @{ priority = $profile.Priority; title = $profile.Title }

        foreach ($labelName in $profile.Labels) {
            $normalizedName = $labelName.ToUpperInvariant()
            $null = ExecuteScalar @'
INSERT INTO TaskItemLabels(TaskItemId, TaskLabelId)
SELECT t.Id, l.Id
FROM TaskItems t
INNER JOIN Projects p ON p.Id = t.ProjectId
INNER JOIN TaskLabels l ON l.NormalizedName = @normalizedName
WHERE p.Name LIKE N'[[]TEST] Görsel Demo %'
  AND p.IsDeleted = 0
  AND t.IsDeleted = 0
  AND t.Title = @title
  AND NOT EXISTS (
      SELECT 1
      FROM TaskItemLabels existing
      WHERE existing.TaskItemId = t.Id AND existing.TaskLabelId = l.Id
  )
'@ @{ normalizedName = $normalizedName; title = $profile.Title }
        }
    }

    $linkedLabelCount = ExecuteScalar @'
SELECT COUNT(*)
FROM TaskItemLabels link
INNER JOIN TaskItems t ON t.Id = link.TaskItemId
INNER JOIN Projects p ON p.Id = t.ProjectId
WHERE p.Name LIKE N'[[]TEST] Görsel Demo %' AND t.IsDeleted = 0 AND p.IsDeleted = 0
'@

    $transaction.Commit()
    Write-Output "Updated $demoTaskCount demo tasks; $linkedLabelCount task-label links are available."
}
catch {
    $transaction.Rollback()
    throw
}
finally {
    $connection.Dispose()
}
