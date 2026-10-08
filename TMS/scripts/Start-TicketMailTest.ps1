param(
    [Parameter(Mandatory = $true)][string]$TenantId,
    [Parameter(Mandatory = $true)][string]$ClientId,
    [string]$MailboxAddress = 'test.kullanici@example.com'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$secureSecret = Read-Host 'Microsoft Entra geçici Client Secret' -AsSecureString
$secretPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureSecret)

try {
    $env:Ticketing__Microsoft365__Enabled = 'true'
    $env:Ticketing__Microsoft365__MailboxAddress = $MailboxAddress
    $env:Ticketing__Microsoft365__RequiredSubjectPrefix = '[DIZGE TEST]'
    $env:Ticketing__Microsoft365__InitialLookbackDays = '1'
    $env:Ticketing__Microsoft365__TenantId = $TenantId
    $env:Ticketing__Microsoft365__ClientId = $ClientId
    $env:Ticketing__Microsoft365__ClientSecret = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($secretPointer)
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_URLS = 'http://127.0.0.1:5088'
    Push-Location $root
    try { dotnet run --no-build }
    finally { Pop-Location }
}
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($secretPointer)
    Remove-Item Env:Ticketing__Microsoft365__ClientSecret -ErrorAction SilentlyContinue
}
