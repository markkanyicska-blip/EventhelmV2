param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = "Stop"

$packageRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRootPath = (Resolve-Path $ProjectRoot).Path
$backendTarget = Join-Path $projectRootPath "backend\EventHelm.Api"
$frontendTarget = Join-Path $projectRootPath "frontend\eventhelm-admin"

Copy-Item (Join-Path $packageRoot "backend\EventHelm.Api\*") $backendTarget -Recurse -Force
Copy-Item (Join-Path $packageRoot "frontend\eventhelm-admin\*") $frontendTarget -Recurse -Force

$programPath = Join-Path $backendTarget "Program.cs"
$program = Get-Content $programPath -Raw
$registration = "builder.Services.AddSingleton<IRadioAssignmentService, JsonRadioAssignmentService>();"

if ($program -notmatch [regex]::Escape($registration)) {
    $program = $program.Replace(
        "var app = builder.Build();",
        "$registration`r`n`r`nvar app = builder.Build();")
    Set-Content $programPath $program -Encoding UTF8
}

$appPath = Join-Path $frontendTarget "src\App.tsx"
$app = Get-Content $appPath -Raw
$import = 'import RadioAssignmentsPage from "./pages/RadioAssignmentsPage";'

if ($app -notmatch [regex]::Escape($import)) {
    $app = $app.Replace(
        'import OfficialsPage from "./pages/OfficialsPage";',
        "import OfficialsPage from `"./pages/OfficialsPage`";`r`n$import")
}

if ($app -notmatch 'path="/radio-assignments"') {
    $route = @'
                    <Route
                        path="/radio-assignments"
                        element={<RadioAssignmentsPage />}
                    />

'@
    $anchor = '                    <Route' + "`r`n" + '                        path="/users"'
    $index = $app.IndexOf($anchor)
    if ($index -lt 0) { throw "Az App.tsx útvonalhorgonya nem található." }
    $app = $app.Insert($index, $route)
}

Set-Content $appPath $app -Encoding UTF8

$sidebarPath = Join-Path $frontendTarget "src\components\Sidebar.tsx"
$sidebar = Get-Content $sidebarPath -Raw

if ($sidebar -notmatch 'RadioTower') {
    $sidebar = $sidebar.Replace(
        "    LayoutDashboard,",
        "    LayoutDashboard,`r`n    RadioTower,")
}

if ($sidebar -notmatch '"/radio-assignments"') {
    $anchor = '    ["/events", "Rendezvények", CalendarDays],'
    $sidebar = $sidebar.Replace(
        $anchor,
        "$anchor`r`n    [`"/radio-assignments`", `"Rádiókiosztás`", RadioTower],")
}

Set-Content $sidebarPath $sidebar -Encoding UTF8

Write-Host "A rádió-hozzárendelési modul telepítve." -ForegroundColor Green
