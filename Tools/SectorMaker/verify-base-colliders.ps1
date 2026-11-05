$ErrorActionPreference = 'Stop'
$projectPath = 'E:\github\board_chase\board_chase'
$editorPath = 'E:\Program Files\Unity\Hub\Editor\6000.0.47f1\Editor\Unity.exe'
$running = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object {
    $_.CommandLine -match 'board_chase' -and $_.CommandLine -notmatch 'AssetImportWorker'
}
if ($running) { throw 'Project Editor is still running; close it before collider authoring.' }
$reportFolder = Join-Path $projectPath 'Library\SectorBaseColliderVerification'
New-Item -ItemType Directory -Path $reportFolder -Force | Out-Null
$requestFiles = @(
    (Join-Path $projectPath 'Library\SectorMakerAssetsVerification\request.json'),
    (Join-Path $projectPath 'Library\SectorEnemySpawnerEncounterVerification\request.json')
)
$heldFiles = @()
try {
    foreach ($requestFile in $requestFiles) {
        if (Test-Path -LiteralPath $requestFile) {
            $heldPath = $requestFile + '.collider-held'
            if (Test-Path -LiteralPath $heldPath) { throw "Existing held request: $heldPath" }
            Move-Item -LiteralPath $requestFile -Destination $heldPath
            $heldFiles += $requestFile
        }
    }
    $arguments = @('-sectorBaseColliderVerification', '-projectPath', ('"' + $projectPath + '"'),
        '-executeMethod', 'SectorBaseColliderVerification.Run', '-logFile', ('"' + (Join-Path $reportFolder 'unity.log') + '"'))
    $process = Start-Process -FilePath $editorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    Write-Output ("Unity collider verification PID: " + $process.Id)
    $process.WaitForExit()
    Write-Output ("Unity exit: " + $process.ExitCode)
    if ($process.ExitCode -ne 0) { exit $process.ExitCode }
} finally {
    foreach ($requestFile in $heldFiles) {
        Move-Item -LiteralPath ($requestFile + '.collider-held') -Destination $requestFile
    }
}
