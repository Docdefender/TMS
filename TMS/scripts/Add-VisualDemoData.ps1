$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$config = Get-Content (Join-Path $root 'appsettings.json') -Raw | ConvertFrom-Json
$builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($config.ConnectionStrings.DefaultConnection)
if ($builder.DataSource -ne 'localhost\SQLEXPRESS' -or $builder.InitialCatalog -ine 'tms') { throw 'Only local Tms is allowed.' }
$backup = Get-ChildItem (Join-Path $root '.local-backups') -Filter manifest.json -Recurse | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (!$backup -or $backup.LastWriteTime -lt (Get-Date).AddHours(-2)) { throw 'A fresh backup is required.' }
$conn = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
$conn.Open()
$tx = $conn.BeginTransaction()
function Query($sql, $values = @{}) {
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx; $cmd.CommandText = $sql
    foreach ($key in $values.Keys) { $null = $cmd.Parameters.AddWithValue('@'+$key, $values[$key]) }
    $value = $cmd.ExecuteScalar(); $cmd.Dispose(); return $value
}
$createdFiles = [System.Collections.Generic.List[string]]::new()
try {
    if ((Query "SELECT COUNT(*) FROM Projects WHERE Name LIKE N'[[]TEST] Görsel Demo %'") -gt 0) { throw 'Demo batch already exists; refusing duplicates.' }
    $manager = Query "SELECT TOP 1 u.Id FROM AspNetUsers u JOIN AspNetUserRoles ur ON ur.UserId=u.Id JOIN AspNetRoles r ON r.Id=ur.RoleId WHERE r.Name='Manager' AND NOT EXISTS(SELECT 1 FROM AspNetUserRoles ar JOIN AspNetRoles a ON a.Id=ar.RoleId WHERE ar.UserId=u.Id AND a.Name='Admin') ORDER BY u.Id"
    $member = Query "SELECT TOP 1 u.Id FROM AspNetUsers u JOIN AspNetUserRoles ur ON ur.UserId=u.Id JOIN AspNetRoles r ON r.Id=ur.RoleId WHERE r.Name='Member' AND NOT EXISTS(SELECT 1 FROM AspNetUserRoles ar JOIN AspNetRoles a ON a.Id=ar.RoleId WHERE ar.UserId=u.Id AND a.Name='Admin') ORDER BY u.Id"
    $admin = Query "SELECT TOP 1 ur.UserId FROM AspNetUserRoles ur JOIN AspNetRoles r ON r.Id=ur.RoleId WHERE r.Name='Admin' ORDER BY ur.UserId"
    if (!$manager -or !$member -or !$admin) { throw 'Existing Manager, Member and Admin required.' }
    $department = Query 'SELECT TOP 1 DepartmentId FROM ManagerDepartments WHERE UserId=@manager ORDER BY IsDefault DESC, DepartmentId' @{manager=$manager}
    $category = Query 'SELECT TOP 1 Id FROM Categories ORDER BY Id'
    if (!$department -or !$category) { throw 'Department and category required.' }
    $names = @('Müşteri Portalı','Mobil Saha Uygulaması','Raporlama Merkezi','Tedarikçi Entegrasyonu','Eski Sistem Geçişi')
    $offsets = @(21,-3,-7,5,14)
    $ids = @()
    for ($i=0; $i -lt 5; $i++) {
        $values = @{name="[TEST] Görsel Demo $($i+1) — $($names[$i])"; description='Görsel kabul ve Kanban testi için örnek kayıt. Kapsam: analiz, geliştirme, ekip incelemesi ve teslim. Başarı ölçütü: örnek iş akışının izlenebilir biçimde tamamlanması. Gerçek müşteri veya operasyon verisi içermez.'; start=(Get-Date).Date.AddDays(-14); end=(Get-Date).Date.AddDays($offsets[$i]); status=$i; department=$department; category=$category; admin=$admin; manager=$manager}
        $id = Query 'INSERT INTO Projects(Name,Description,StartDate,EndDate,Status,DepartmentId,CategoryId,CreatedByUserId,ManagerUserId,FirstAssignedByUserId,IsDeleted) OUTPUT INSERTED.Id VALUES(@name,@description,@start,@end,@status,@department,@category,@admin,@manager,@admin,0)' $values
        $ids += $id
        foreach ($person in @($manager,$member)) { $null=Query 'INSERT INTO ProjectMembers(ProjectId,UserId,AddedAt) VALUES(@id,@person,SYSUTCDATETIME())' @{id=$id;person=$person} }
        $null=Query 'INSERT INTO Comments(Content,CreatedAt,UserId,ProjectId,IsDeleted) VALUES(@text,GETDATE(),@user,@id,0)' @{text='[TEST] Kapsam ve teslim ölçütleri ekip tarafından incelendi. Görev kartları farklı durumları örneklemek için hazırlandı.';user=$manager;id=$id}
        for ($j=0; $j -lt 4; $j++) {
            $titles=@('İhtiyaç analizi ve kapsam','Arayüz ve uygulama geliştirme','Ekip incelemesi ve kalite kontrol','Teslim dokümanı ve kabul')
            $status= if($i -eq 2){3}elseif($i -eq 0){0}else{$j}
            $assignee=if($j % 2 -eq 0){$member}else{$manager}
            $task=Query 'INSERT INTO TaskItems(Title,Description,Status,CreatedDate,DueDate,ProjectId,CategoryId,CreatedByUserId,AssignedToUserId,FirstAssignedByUserId,IsDeleted) OUTPUT INSERTED.Id VALUES(@title,@description,@status,GETDATE(),@due,@project,@category,@creator,@assignee,@creator,0)' @{title=$titles[$j];description='[TEST] Beklenen çıktı: gözden geçirilebilir çalışma ve kısa teslim notu. Kontrol: açıklama, sorumlu, kategori, son tarih, yorum ve dosya görünümü.';status=$status;due=(Get-Date).Date.AddDays($j-2);project=$id;category=$category;creator=$manager;assignee=$assignee}
            $null=Query 'INSERT INTO TaskAssignments(TaskItemId,AssignedToUserId,AssignedByUserId,AssignedAt) VALUES(@task,@person,@creator,SYSUTCDATETIME())' @{task=$task;person=$assignee;creator=$manager}
            $null=Query 'INSERT INTO Comments(Content,CreatedAt,UserId,TaskItemId,IsDeleted) VALUES(@text,GETDATE(),@person,@task,0)' @{text='[TEST] Örnek çalışma notu: gereksinimler kontrol edildi; teslim maddeleri açıklamada listelendi.';person=$assignee;task=$task}
            if ($j -eq 0) { $firstTask=$task }
        }
        foreach ($target in @(@{kind='projects';id=$id;column='ProjectId'},@{kind='tasks';id=$firstTask;column='TaskItemId'})) {
            $relative="/uploads/$($target.kind)/$($target.id)/demo-kabul-notu.txt"
            $path=Join-Path $root ('wwwroot'+$relative)
            $null=New-Item -ItemType Directory -Force -Path (Split-Path $path)
            if (Test-Path $path) { throw 'Attachment already exists.' }
            [IO.File]::WriteAllText($path,"TEST VERİSİ — $($names[$i])`nKapsam: analiz, geliştirme, inceleme ve teslim.`nBu dosya görsel kabul ve yetkili indirme kontrolü içindir.",[Text.UTF8Encoding]::new($false)); $createdFiles.Add($path)
            $null=Query "INSERT INTO Attachments(FileName,FilePath,ContentType,FileSize,UploadedAt,UploadedByUserId,$($target.column),IsDeleted) VALUES(@name,@path,'text/plain',@size,GETDATE(),@user,@id,0)" @{name='demo-kabul-notu.txt';path=$relative;size=(Get-Item $path).Length;user=$manager;id=$target.id}
        }
    }
    $tx.Commit()
    Write-Output "Created projects: $($ids -join ', '); 20 tasks, 25 comments, 10 attachments. Existing users and roles unchanged."
} catch {
    $tx.Rollback()
    foreach($path in $createdFiles) { Remove-Item -LiteralPath $path }
    throw
} finally { $conn.Dispose() }
