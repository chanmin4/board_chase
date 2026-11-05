$ErrorActionPreference = 'Stop'
$projectPath = 'E:\github\board_chase\board_chase'
$editorPath = 'E:\Program Files\Unity\Hub\Editor\6000.0.47f1\Editor\Unity.exe'
$reportFolder = Join-Path $projectPath 'Library\SectorAppearanceVerification'
New-Item -ItemType Directory -Path $reportFolder -Force | Out-Null
$requestFiles = @(
    (Join-Path $projectPath 'Library\SectorMakerAssetsVerification\request.json'),
    (Join-Path $projectPath 'Library\SectorEnemySpawnerEncounterVerification\request.json')
)
$heldFiles = @()
try {
    foreach ($requestFile in $requestFiles) {
        if (Test-Path -LiteralPath $requestFile) {
            $heldPath = $requestFile + '.appearance-held'
            if (Test-Path -LiteralPath $heldPath) { throw "Existing held request: $heldPath" }
            Move-Item -LiteralPath $requestFile -Destination $heldPath
            $heldFiles += $requestFile
        }
    }
    $arguments = @('-sectorAppearanceVerification', '-projectPath', ('"' + $projectPath + '"'),
        '-executeMethod', 'SectorAppearanceVerification.Run',
        '-logFile', ('"' + (Join-Path $reportFolder 'unity.log') + '"'))
    $process = Start-Process -FilePath $editorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    Write-Output ("Unity verification PID: " + $process.Id)
    $process.WaitForExit()
    Write-Output ("Unity exit: " + $process.ExitCode)
    if ($process.ExitCode -ne 0) { exit $process.ExitCode }
} finally {
    foreach ($requestFile in $heldFiles) {
        Move-Item -LiteralPath ($requestFile + '.appearance-held') -Destination $requestFile
    }
}
