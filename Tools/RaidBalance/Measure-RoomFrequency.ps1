param(
    [double]$ThemeRatio = 0.20,
    [double]$SpecialRatio = 0.30,
    [int]$SpecialTypeCount = 6,
    [switch]$FirstStageGeneratorOnly
)

# Read-only planning calculation, NOT a Unity map-generation or Play Mode test.
# Each stage is a fresh draw; "no repeats" means within that one stage.
# Current V3 reserves one hatch, one boss-role room, and the configured merchant count.
# Generator occupies a slot INSIDE the proposed special-room quota, not an extra room.
if ($ThemeRatio -lt 0 -or $SpecialRatio -lt 0 -or
    $ThemeRatio + $SpecialRatio -gt 1 -or $SpecialTypeCount -lt 1) {
    throw 'Invalid category ratios or special type count.'
}

$taskRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
foreach ($stageIndex in 1..6) {
    $assetPath = Join-Path $taskRoot "Assets/ScriptableObject/GameScene/Stage/Stage${stageIndex}Definition.asset"
    $assetText = Get-Content -LiteralPath $assetPath -Raw
    $sizeMatch = [regex]::Match($assetText, '(?m)^\s+_roomGridSize:\s*(\d+)\s*$')
    $densityMatch = [regex]::Match($assetText, '(?m)^\s+roomDensity:\s*([\d.]+)\s*$')
    $merchantMatch = [regex]::Match($assetText, '(?m)^\s+merchantRoomCount:\s*(\d+)\s*$')
    if (-not $sizeMatch.Success -or -not $densityMatch.Success -or -not $merchantMatch.Success -or
        $assetText -notmatch '(?m)^\s+_mapPolicy:\s*2\s*$' -or
        $assetText -match '(?m)^\s+vaultUsesInternalCombatLock:\s*0\s*$') {
        throw "Unsupported/missing V3 settings in $assetPath. Recheck the analytical model before using it."
    }

    $size = [int]$sizeMatch.Groups[1].Value
    $density = [double]::Parse($densityMatch.Groups[1].Value, [Globalization.CultureInfo]::InvariantCulture)
    # Structural core = full horizontal/vertical spokes + square ring - four intersections.
    $core = 2 * $size - 1 + 8 * [Math]::Max(1, [Math]::Floor($size / 5)) - 4
    $total = [int][Math]::Max($core, [Math]::Round($size * $size * $density, [MidpointRounding]::ToEven))
    $fixed = 2 + [int]$merchantMatch.Groups[1].Value
    $countable = $total - $fixed
    $theme = [int][Math]::Round($countable * $ThemeRatio, [MidpointRounding]::AwayFromZero)
    $special = [int][Math]::Round($countable * $SpecialRatio, [MidpointRounding]::AwayFromZero)
    $generator = if ($FirstStageGeneratorOnly -and $stageIndex -ne 1) { 0 } else { 1 }
    if ($special -lt $generator) { throw "Stage ${stageIndex}: special quota cannot fit its guaranteed generator." }
    $draws = $special - $generator
    $noRepeat = [Math]::Min($special, $SpecialTypeCount + $generator)
    $appearance = 1 - [Math]::Pow(1 - 1.0 / $SpecialTypeCount, $draws)

    [pscustomobject]@{
        Stage = $stageIndex
        Grid = "${size}x${size}"
        TotalRooms = $total
        FixedRoomsExcluded = $fixed
        RatioEligibleRooms = $countable
        ThemeRooms = $theme
        SpecialRooms = $special
        CommonCombatRooms = $countable - $theme - $special
        GeneratorWithinSpecialQuota = $generator
        SixTypeDraws = $draws
        RepeatExpectedPerType = [Math]::Round($draws / [double]$SpecialTypeCount, 3)
        RepeatOneTypeAppearancePercent = [Math]::Round(100 * $appearance, 5)
        NoRepeatSpecialMaximum = $noRepeat
        NoRepeatSpecialPercent = [Math]::Round(100 * $noRepeat / $countable, 3)
        NoRepeatOneTypeAppearancePercent = [Math]::Round(100 * [Math]::Min(1, $draws / [double]$SpecialTypeCount), 3)
    }
}
