param([ValidateSet('Audit','Create')][string]$Method = 'Audit')
$ErrorActionPreference = 'Stop'
$projectPath = 'E:\github\board_chase\board_chase'
$editorPath = 'E:\Program Files\Unity\Hub\Editor\6000.0.47f1\Editor\Unity.exe'
$running = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object {
    $_.CommandLine -match 'board_chase' -and $_.CommandLine -notmatch 'AssetImportWorker'
}
if ($running) { throw 'Project Editor is still running; save and close it before hatch import.' }
$reportFolder = Join-Path $projectPath 'Library\HatchPrefabAuthoring'
New-Item -ItemType Directory -Path $reportFolder -Force | Out-Null
$requestFiles = @(
    (Join-Path $projectPath 'Library\SectorMakerAssetsVerification\request.json'),
    (Join-Path $projectPath 'Library\SectorEnemySpawnerEncounterVerification\request.json')
)
$heldFiles = @()
try {
    foreach ($requestFile in $requestFiles) {
        if (Test-Path -LiteralPath $requestFile) {
            $heldPath = $requestFile + '.hatch-held'
            if (Test-Path -LiteralPath $heldPath) { throw "Existing held request: $heldPath" }
            Move-Item -LiteralPath $requestFile -Destination $heldPath
            $heldFiles += $requestFile
        }
    }
    $arguments = @('-hatchPrefabAuthoring', '-projectPath', ('"' + $projectPath + '"'),
        '-executeMethod', ('HatchPrefabAuthoring.' + $Method), '-logFile', ('"' + (Join-Path $reportFolder ($Method.ToLower() + '-unity.log')) + '"'))
    $process = Start-Process -FilePath $editorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    Write-Output ("Unity hatch " + $Method + " PID: " + $process.Id)
    $process.WaitForExit()
    Write-Output ("Unity exit: " + $process.ExitCode)
    if ($process.ExitCode -ne 0) { exit $process.ExitCode }
} finally {
    foreach ($requestFile in $heldFiles) {
        Move-Item -LiteralPath ($requestFile + '.hatch-held') -Destination $requestFile
    }
}
