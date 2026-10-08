$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$password=((Get-Content (Join-Path $root '.local-backups/demo-users.txt'))[1] -replace '^Ortak test şifresi: ','')
$base='http://127.0.0.1:5088'
foreach($account in @('ece.member','deniz.manager','selin.manager','can.member')){
 $session=[Microsoft.PowerShell.Commands.WebRequestSession]::new()
 $login=Invoke-WebRequest "$base/Account/Login" -WebSession $session
 $token=[regex]::Match($login.Content,'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value
 if(!$token){throw 'Missing login token'}
 $response=Invoke-WebRequest "$base/Account/Login" -Method Post -WebSession $session -Body @{'Input.Email'="$account@demo.dizge.test";'Input.Password'=$password;'__RequestVerificationToken'=$token}
 if($response.BaseResponse.RequestMessage.RequestUri.AbsolutePath -like '*Login*'){throw "Login failed: $account"}
 $timer=[Diagnostics.Stopwatch]::StartNew()
 $project=Invoke-WebRequest "$base/Projects/Details/11" -WebSession $session -SkipHttpErrorCheck
 $timer.Stop()
 $html=[Net.WebUtility]::HtmlDecode($project.Content)
 $allowed=$account -in @('ece.member','deniz.manager')
 if($allowed){
  if($project.StatusCode -ne 200 -or $html -notmatch 'Müşteri Portalı'){throw "Project not visible: $account"}
  if($html -match 'handler=DeleteProject|/Projects/Edit/11'){throw "Unexpected project management: $account"}
  $canStatus=$html -match 'id="project-status-change"'
  if($canStatus -ne ($account -eq 'deniz.manager')){throw "Wrong status permission: $account"}
  if($account -eq 'ece.member'){
   $options=[regex]::Match($html,'(?s)<select[^>]*id="NewTask_AssignedToUserId".*?</select>').Value
   if($options -match 'Deniz|Selin|Can Y|bb|System Admin'){throw 'Member sees forbidden assignee'}
  }
 }else{
  if($project.StatusCode -eq 200 -and $html -match 'PROJE DETAYI'){throw "Outsider can view project: $account"}
 }
 Write-Output "PASS $account login, project visibility and applicable controls; project response $($timer.ElapsedMilliseconds) ms"
}
