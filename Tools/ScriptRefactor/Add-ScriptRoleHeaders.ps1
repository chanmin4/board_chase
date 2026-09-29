param(
    [switch]$DryRun,
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'

$projectRoot = if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
}
else {
    [System.IO.Path]::GetFullPath($ProjectRoot)
}
$scriptsRoot = [System.IO.Path]::GetFullPath((Join-Path $projectRoot 'Assets\Scripts'))
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$headerMarker = ' * Role hierarchy:'

function Get-RelativeScriptPath {
    param([string]$FullPath)

    return $FullPath.Substring($scriptsRoot.Length + 1)
}

function Get-NewLine {
    param([byte[]]$Payload)

    for ($i = 0; $i -lt $Payload.Length; $i++) {
        if ($Payload[$i] -eq 10) {
            if ($i -gt 0 -and $Payload[$i - 1] -eq 13) {
                return "`r`n"
            }

            return "`n"
        }
    }

    return "`r`n"
}

function Get-PrimaryTypeName {
    param(
        [string]$SourceText,
        [string]$FallbackName
    )

    $pattern = '(?m)^\s*(?:public|internal|private|protected)?\s*(?:(?:abstract|sealed|static|partial|readonly|ref)\s+)*(?:class|struct|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)'
    $match = [System.Text.RegularExpressions.Regex]::Match($SourceText, $pattern)
    if ($match.Success) {
        return $match.Groups[1].Value
    }

    return $FallbackName
}

function Get-ExecutionBoundary {
    param([string]$RelativePath)

    if ($RelativePath -like 'StateMachine\Editor\*') {
        return 'Editor only (VSplatter.StateMachine.Editor)'
    }

    if ($RelativePath -like 'StateMachine\*') {
        return 'Runtime (VSplatter.StateMachine)'
    }

    $segments = $RelativePath -split '[\\/]'
    if ($segments -contains 'Editor') {
        return 'Editor only (Assembly-CSharp-Editor)'
    }

    return 'Runtime (Assembly-CSharp)'
}

function Get-Responsibility {
    param(
        [string]$TypeName,
        [string]$RelativePath
    )

    if ($RelativePath -like 'WIP\*') {
        return "Isolates the legacy or deferred $TypeName implementation from active production code."
    }

    if ($RelativePath -match '[\\/]Editor[\\/]' -or $TypeName -match '(Editor|Window|Inspector|Drawer|Wizard)$') {
        return "Provides the $TypeName authoring tool and its editor workflow."
    }

    if ($TypeName -match '(EventChannel|EventChannelSO)$') {
        return "Defines the $TypeName publish-subscribe contract to reduce direct system coupling."
    }

    if ($TypeName -match '(StateMachine|State|ActionSO|ConditionSO|TransitionSO|DecisionSO)$') {
        return "Defines $TypeName state behavior or transitions that drive the runtime flow."
    }

    if ($TypeName -match '(ConfigSO|SettingsSO|RulesSO|ProfileSO|PresetSO|TableSO|DefinitionSO|LoadoutSO|ItemSO|WeaponSO|BulletSO|SO)$') {
        return "Defines the $TypeName data asset used to configure runtime rules in the Inspector."
    }

    if ($TypeName -match '(Panel|Widget|View|HUD|UI|Presenter)$') {
        return "Presents $TypeName state and handles its user-facing UI interaction."
    }

    if ($TypeName -match '(Inventory|Equipment|Loadout)') {
        return "Manages $TypeName item storage, equipment, transfer state, and related rules."
    }

    if ($TypeName -match '(Projectile|Bullet|Grenade|Throwable|Attack|Damage|Hit|Health|Infection|Mark)') {
        return "Handles $TypeName combat resolution, damage, projectile, or status changes."
    }

    if ($TypeName -match '(Audio|Sound|Music)') {
        return "Handles $TypeName audio playback and sound-stimulus delivery."
    }

    if ($TypeName -match '(Camera|Vision|Sight|Aim|Range)') {
        return "Handles $TypeName camera, vision, range, or aiming calculations and presentation."
    }

    if ($TypeName -match '(Sector|Stage|Portal|Room|Map|NavMesh|Spawn)') {
        return "Manages $TypeName stage, sector, navigation, spawn, or spatial progression state."
    }

    if ($TypeName -match '(Interaction|Interact|Pickup|Loot|Shop|Treasure|Storage)') {
        return "Handles $TypeName interaction, pickup, trade, loot, or storage flow."
    }

    if ($TypeName -match '(Input|Key|Binding)') {
        return "Collects $TypeName input and forwards it as gameplay action requests."
    }

    if ($TypeName -match '(Save|Persistence|Repository|Storage)') {
        return "Stores and restores $TypeName runtime data and persistent state."
    }

    if ($TypeName -match '(Manager|Controller|Runtime|System|Service|Router|Coordinator|Hub)$') {
        return "Coordinates the $TypeName subsystem lifecycle and collaborating objects."
    }

    if ($TypeName -match '^I[A-Z]' -or $TypeName -match '(Interface|Contract)$') {
        return "Defines the shared contract implemented by the $TypeName role group."
    }

    return "Implements the focused $TypeName responsibility and collaborates with its role group."
}

function Test-ByteRangeEqual {
    param(
        [byte[]]$Expected,
        [byte[]]$Actual,
        [int]$ActualOffset
    )

    if ($Actual.Length -lt $ActualOffset + $Expected.Length) {
        return $false
    }

    for ($i = 0; $i -lt $Expected.Length; $i++) {
        if ($Expected[$i] -ne $Actual[$ActualOffset + $i]) {
            return $false
        }
    }

    return $true
}

$scripts = Get-ChildItem -LiteralPath $scriptsRoot -Recurse -File -Filter '*.cs' |
    Sort-Object FullName
$changed = 0
$skipped = 0

foreach ($script in $scripts) {
    $originalBytes = [System.IO.File]::ReadAllBytes($script.FullName)
    $hasBom = $originalBytes.Length -ge 3 -and
        $originalBytes[0] -eq 0xEF -and
        $originalBytes[1] -eq 0xBB -and
        $originalBytes[2] -eq 0xBF
    $bomLength = if ($hasBom) { 3 } else { 0 }
    $payloadLength = $originalBytes.Length - $bomLength
    $payload = New-Object byte[] $payloadLength
    if ($payloadLength -gt 0) {
        [System.Array]::Copy($originalBytes, $bomLength, $payload, 0, $payloadLength)
    }

    $sourceText = [System.Text.Encoding]::UTF8.GetString($payload)
    if ($sourceText.StartsWith('/*') -and
        $sourceText.Substring(0, [Math]::Min(512, $sourceText.Length)).Contains($headerMarker)) {
        $skipped++
        continue
    }

    $relativePath = Get-RelativeScriptPath $script.FullName
    $relativeDirectory = [System.IO.Path]::GetDirectoryName($relativePath)
    $rolePath = if ([string]::IsNullOrWhiteSpace($relativeDirectory)) {
        'Root'
    }
    else {
        $relativeDirectory -replace '[\\/]', ' > '
    }
    $typeName = Get-PrimaryTypeName $sourceText ([System.IO.Path]::GetFileNameWithoutExtension($script.Name))
    $responsibility = Get-Responsibility $typeName $relativePath
    $boundary = Get-ExecutionBoundary $relativePath
    $newLine = Get-NewLine $payload
    $header = @(
        '/*',
        " * Role hierarchy: $rolePath",
        " * Primary responsibility: $responsibility",
        " * Execution boundary: $boundary",
        ' */',
        '',
        ''
    ) -join $newLine
    $headerBytes = $utf8NoBom.GetBytes($header)

    if ($DryRun) {
        Write-Host "[DRY] $relativePath -> $typeName"
        $changed++
        continue
    }

    $resultLength = $bomLength + $headerBytes.Length + $payload.Length
    $result = New-Object byte[] $resultLength
    if ($hasBom) {
        [System.Array]::Copy($originalBytes, 0, $result, 0, $bomLength)
    }
    [System.Array]::Copy($headerBytes, 0, $result, $bomLength, $headerBytes.Length)
    if ($payload.Length -gt 0) {
        [System.Array]::Copy($payload, 0, $result, $bomLength + $headerBytes.Length, $payload.Length)
    }

    [System.IO.File]::WriteAllBytes($script.FullName, $result)
    $readBack = [System.IO.File]::ReadAllBytes($script.FullName)
    if ($hasBom -and !(Test-ByteRangeEqual $originalBytes[0..2] $readBack 0)) {
        throw "BOM changed while adding header: $relativePath"
    }
    if (!(Test-ByteRangeEqual $payload $readBack ($bomLength + $headerBytes.Length))) {
        throw "Original source payload changed while adding header: $relativePath"
    }

    $changed++
}

Write-Host "[DONE] Role headers: changed=$changed, alreadyPresent=$skipped, scripts=$($scripts.Count), dryRun=$DryRun"
