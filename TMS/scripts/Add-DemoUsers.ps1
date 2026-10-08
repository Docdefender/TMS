$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$config=Get-Content (Join-Path $root 'appsettings.json') -Raw | ConvertFrom-Json
$builder=[System.Data.SqlClient.SqlConnectionStringBuilder]::new($config.ConnectionStrings.DefaultConnection)
if($builder.DataSource -ne 'localhost\SQLEXPRESS' -or $builder.InitialCatalog -ine 'tms'){throw 'Only local Tms allowed.'}
$backup=Get-ChildItem (Join-Path $root '.local-backups') -Filter manifest.json -Recurse | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if(!$backup -or $backup.LastWriteTime -lt (Get-Date).AddHours(-2)){throw 'Fresh backup required.'}
$conn=[System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString); $conn.Open(); $tx=$conn.BeginTransaction()
function Q($sql,$values=@{}) { $cmd=$conn.CreateCommand(); $cmd.Transaction=$tx; $cmd.CommandText=$sql; foreach($key in $values.Keys){$null=$cmd.Parameters.AddWithValue('@'+$key,$values[$key])}; try{return $cmd.ExecuteScalar()}finally{$cmd.Dispose()} }
# ASP.NET Identity V3: marker, network-order PRF/iterations/salt length, salt, subkey.
function PasswordHash($password){
 $salt=[Security.Cryptography.RandomNumberGenerator]::GetBytes(16)
 $subkey=[Security.Cryptography.Rfc2898DeriveBytes]::Pbkdf2($password,$salt,100000,[Security.Cryptography.HashAlgorithmName]::SHA512,32)
 $bytes=[Collections.Generic.List[byte]]::new(); $bytes.Add(1)
 foreach($number in @(2,100000,16)){ $part=[BitConverter]::GetBytes([uint32]$number); if([BitConverter]::IsLittleEndian){[Array]::Reverse($part)}; $bytes.AddRange($part) }
 $bytes.AddRange($salt); $bytes.AddRange($subkey); return [Convert]::ToBase64String($bytes.ToArray())
}
$records=@(
 @('Deniz Yılmaz','deniz.manager','Manager','Marketing'),
 @('Selin Kaya','selin.manager','Manager','Finance'),
 @('Ece Demir','ece.member','Member','Engineering'),
 @('Mert Arslan','mert.member','Member','Engineering'),
 @('İpek Aydın','ipek.member','Member','Marketing'),
 @('Can Yıldız','can.member','Member','Finance')
)
$password='Dizge!'+[Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(9))+'a7'
$credentials=Join-Path $root '.local-backups/demo-users.txt'
try{
 if((Q "SELECT COUNT(*) FROM AspNetUsers WHERE Email LIKE '%@demo.dizge.test'") -gt 0){throw 'Demo users already exist.'}
 $project=Q "SELECT TOP 1 Id FROM Projects WHERE Name LIKE N'[[]TEST] Görsel Demo 1 %' AND IsDeleted=0 ORDER BY Id"
 if(!$project){throw 'Demo project required.'}
 $lines=@('Yerel test hesapları — gerçek kullanım için değildir.',"Ortak test şifresi: $password",'')
 foreach($record in $records){
  $id=[Guid]::NewGuid().ToString(); $email=$record[1]+'@demo.dizge.test'
  $dept=Q 'SELECT Id FROM Departments WHERE Name=@name' @{name=$record[3]}
  $role=Q 'SELECT Id FROM AspNetRoles WHERE Name=@name' @{name=$record[2]}
  if(!$dept -or !$role){throw 'Department or role missing.'}
  $null=Q 'INSERT INTO AspNetUsers(Id,UserName,NormalizedUserName,Email,NormalizedEmail,EmailConfirmed,PasswordHash,SecurityStamp,ConcurrencyStamp,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnabled,AccessFailedCount,FullName,CreatedAt,DepartmentId) VALUES(@id,@email,@normalized,@email,@normalized,0,@hash,@stamp,@stamp,0,0,1,0,@name,GETDATE(),@dept)' @{id=$id;email=$email;normalized=$email.ToUpperInvariant();hash=(PasswordHash $password);stamp=[Guid]::NewGuid().ToString();name=('[TEST] '+$record[0]);dept=$dept}
  $null=Q 'INSERT INTO AspNetUserRoles(UserId,RoleId) VALUES(@id,@role)' @{id=$id;role=$role}
  if($record[2] -eq 'Manager'){ $null=Q 'INSERT INTO ManagerDepartments(DepartmentId,UserId,IsDefault) VALUES(@dept,@id,0)' @{dept=$dept;id=$id} }
  # One cross-department team; Finance users remain outside this project for access checks.
  if($record[3] -ne 'Finance'){ $null=Q 'INSERT INTO ProjectMembers(ProjectId,UserId,AddedAt) VALUES(@project,@id,SYSUTCDATETIME())' @{project=$project;id=$id} }
  $lines += "$($record[0]) | $($record[2]) | $($record[3]) | $email"
 }
 [IO.File]::WriteAllLines($credentials,$lines,[Text.UTF8Encoding]::new($false))
 $tx.Commit()
 Write-Output "Created 6 demo users; 2 Managers and 4 Members. Demo project $project has an Engineering/Marketing team. Credentials saved privately to .local-backups/demo-users.txt. Existing defaults unchanged."
}catch{$tx.Rollback();throw}finally{$conn.Dispose()}
