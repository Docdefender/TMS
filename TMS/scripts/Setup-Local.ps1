param(
    [switch]$SkipRun
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$localConfig = Join-Path $projectRoot 'appsettings.Local.json'
$exampleConfig = Join-Path $projectRoot 'appsettings.Local.example.json'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET 8 SDK bulunamadı. Önce https://dotnet.microsoft.com/download/dotnet/8.0 adresinden kurun.'
}

if (-not (Test-Path -LiteralPath $localConfig)) {
    Copy-Item -LiteralPath $exampleConfig -Destination $localConfig
    Write-Host 'appsettings.Local.json oluşturuldu.' -ForegroundColor Green
    Write-Host 'SQL Server adınız localhost\SQLEXPRESS değilse dosyadaki bağlantıyı düzenleyip betiği yeniden çalıştırın.' -ForegroundColor Yellow
}

Push-Location $projectRoot
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'EF aracının kurulumu tamamlanamadı.' }
    dotnet restore
    if ($LASTEXITCODE -ne 0) { throw 'Proje paketleri yüklenemedi.' }
    dotnet ef database update
    if ($LASTEXITCODE -ne 0) { throw 'Veritabanı oluşturulamadı veya güncellenemedi.' }

    Write-Host 'Veritabanı hazır.' -ForegroundColor Green
    if ($SkipRun) { return }

    $securePassword = Read-Host 'İlk kurulumsa Admin parolasını girin; Admin zaten varsa boş bırakın' -AsSecureString
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try { $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
    if (-not [string]::IsNullOrWhiteSpace($plainPassword)) {
        $env:TMS_BOOTSTRAP_ADMIN_PASSWORD = $plainPassword
    }
    try { dotnet run --launch-profile http }
    finally {
        Remove-Item Env:TMS_BOOTSTRAP_ADMIN_PASSWORD -ErrorAction SilentlyContinue
        $plainPassword = $null
    }
} finally {
    Pop-Location
}
