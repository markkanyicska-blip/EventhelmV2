$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$targetRoot = (Resolve-Path (Join-Path $root "..\..")).Path

$backendSource = Join-Path $root "backend\EventHelm.Api"
$backendTarget = Join-Path $targetRoot "backend\EventHelm.Api"

$frontendSource = Join-Path $root "frontend\eventhelm-admin"
$frontendTarget = Join-Path $targetRoot "frontend\eventhelm-admin"

if (-not (Test-Path $backendTarget)) {
    throw "A backend projekt nem található: $backendTarget"
}

if (-not (Test-Path $frontendTarget)) {
    throw "A frontend projekt nem található: $frontendTarget"
}

Copy-Item `
    (Join-Path $backendSource "*") `
    $backendTarget `
    -Recurse `
    -Force

Copy-Item `
    (Join-Path $frontendSource "*") `
    $frontendTarget `
    -Recurse `
    -Force

$programPath = Join-Path $backendTarget "Program.cs"
$programContent = Get-Content $programPath -Raw

$usingLine = "using EventHelm.Api.Services;"
$registrationLine = "builder.Services.AddSingleton<IRaceControlService, JsonRaceControlService>();"

if ($programContent -notmatch [regex]::Escape($usingLine)) {
    $programContent = "$usingLine`r`n$programContent"
}

if ($programContent -notmatch [regex]::Escape($registrationLine)) {
    $marker = "var app = builder.Build();"

    if ($programContent -notmatch [regex]::Escape($marker)) {
        throw "A Program.cs fájlban nem található a következő sor: $marker"
    }

    $programContent = $programContent.Replace(
        $marker,
        "$registrationLine`r`n`r`n$marker")
}

Set-Content `
    -Path $programPath `
    -Value $programContent `
    -Encoding UTF8

Write-Host ""
Write-Host "Race Control backend és frontend telepítve." -ForegroundColor Green
Write-Host ""
Write-Host "Backend indítása:"
Write-Host "  cd $backendTarget"
Write-Host "  dotnet run"
Write-Host ""
Write-Host "Frontend ellenőrzése:"
Write-Host "  cd $frontendTarget"
Write-Host "  npm run build"
Write-Host "  npm run dev"
