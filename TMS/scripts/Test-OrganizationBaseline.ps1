$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$sql = Get-Content (Join-Path $root '.local-backups/baseline-schema.sql') -Raw
$expected = [Collections.Generic.List[string]]::new()
foreach ($table in [regex]::Matches($sql, '(?ms)CREATE TABLE \[(?<name>\w+)\] \((?<body>.*?)^\s*\);')) {
    foreach ($column in [regex]::Matches($table.Groups['body'].Value, '(?m)^\s*\[(?<name>\w+)\] (?<type>\w+(?:\([^)]*\))?) (?<null>NOT NULL|NULL)(?<identity> IDENTITY)?')) {
        $expected.Add(($table.Groups['name'].Value + '|' + $column.Groups['name'].Value + '|' + $column.Groups['type'].Value + '|' + ($column.Groups['null'].Value -eq 'NULL') + '|' + $column.Groups['identity'].Success).ToLowerInvariant())
    }
}
if ($expected.Count -lt 100) { throw 'Baseline SQL could not be parsed completely.' }
$config = Get-Content (Join-Path $root 'appsettings.json') -Raw | ConvertFrom-Json
$conn = [System.Data.SqlClient.SqlConnection]::new($config.ConnectionStrings.DefaultConnection)
try {
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @'
SELECT t.name AS TableName,c.name AS ColumnName,ty.name +
CASE WHEN ty.name IN ('nvarchar','nchar') THEN '('+CASE WHEN c.max_length=-1 THEN 'max' ELSE CONVERT(varchar(10),c.max_length/2) END+')'
WHEN ty.name IN ('varchar','char','varbinary','binary') THEN '('+CASE WHEN c.max_length=-1 THEN 'max' ELSE CONVERT(varchar(10),c.max_length) END+')' ELSE '' END AS TypeName,
c.is_nullable,c.is_identity FROM sys.tables t JOIN sys.columns c ON t.object_id=c.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id WHERE t.is_ms_shipped=0;
'@
    $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($cmd)
    $data = [System.Data.DataTable]::new()
    $null = $adapter.Fill($data)
    $actual = @($data.Rows | ForEach-Object { (($_.TableName,$_.ColumnName,$_.TypeName,$_.is_nullable,$_.is_identity) -join '|').ToLowerInvariant() })
    $differences = Compare-Object ($expected | Sort-Object) ($actual | Sort-Object)
    if ($differences) { $differences | Format-Table; throw 'Existing schema differs from baseline. Do not reconcile migration history.' }
    Write-Output "Baseline matches $($expected.Count) columns, types, nullability and identity properties."
    $expectedKeys = @([regex]::Matches($sql, '(?:CONSTRAINT \[(?<name>(?:PK_|FK_)[^\]]+)\]|CREATE (?:UNIQUE )?INDEX \[(?<name>[^\]]+)\])') | ForEach-Object { $_.Groups['name'].Value })
    $cmd.CommandText = 'SELECT name FROM sys.foreign_keys UNION ALL SELECT i.name FROM sys.indexes i JOIN sys.tables t ON i.object_id=t.object_id WHERE t.is_ms_shipped=0 AND i.index_id>0 AND i.is_hypothetical=0;'
    $keys = [System.Data.DataTable]::new()
    $null = $adapter.Fill($keys)
    $keyDifferences = Compare-Object ($expectedKeys | Sort-Object) (@($keys.Rows | ForEach-Object { $_.name }) | Sort-Object)
    if ($keyDifferences) { $keyDifferences | Format-Table; throw 'Baseline key/index inventory differs.' }
    Write-Output "Baseline matches $($expectedKeys.Count) primary key, foreign key and index names."
} finally { $conn.Dispose() }
