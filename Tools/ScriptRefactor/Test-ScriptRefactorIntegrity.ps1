param(
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

if (!(Test-Path -LiteralPath $scriptsRoot -PathType Container)) {
    throw "Assets/Scripts was not found below project root: $projectRoot"
}

function Get-ExpectedBoundary {
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

function Add-Failure {
    param(
        [System.Collections.Generic.List[string]]$Failures,
        [string]$Message
    )

    $Failures.Add($Message)
}

$scripts = @(Get-ChildItem -LiteralPath $scriptsRoot -Recurse -Filter '*.cs' -File |
    Sort-Object FullName)
$failures = New-Object 'System.Collections.Generic.List[string]'
$guidOwners = @{}
$boundaryCounts = @{}
$headerCount = 0

foreach ($script in $scripts) {
    $relativePath = $script.FullName.Substring($scriptsRoot.Length + 1)
    $relativeDirectory = [System.IO.Path]::GetDirectoryName($relativePath)
    $expectedRole = if ([string]::IsNullOrWhiteSpace($relativeDirectory)) {
        'Root'
    }
    else {
        $relativeDirectory -replace '[\\/]', ' > '
    }
    $expectedBoundary = Get-ExpectedBoundary $relativePath

    $metaPath = "$($script.FullName).meta"
    if (!(Test-Path -LiteralPath $metaPath -PathType Leaf)) {
        Add-Failure $failures "Missing script meta: $relativePath"
    }
    else {
        $guidLine = Get-Content -LiteralPath $metaPath -TotalCount 8 |
            Where-Object { $_ -match '^guid:\s*([0-9a-fA-F]+)\s*$' } |
            Select-Object -First 1

        if ($null -eq $guidLine -or $guidLine -notmatch '^guid:\s*([0-9a-fA-F]+)\s*$') {
            Add-Failure $failures "Unreadable script GUID: $relativePath.meta"
        }
        else {
            $guid = $Matches[1].ToLowerInvariant()
            if ($guidOwners.ContainsKey($guid)) {
                Add-Failure $failures "Duplicate script GUID $guid`: $($guidOwners[$guid]) and $relativePath"
            }
            else {
                $guidOwners[$guid] = $relativePath
            }
        }
    }

    $headerText = (Get-Content -LiteralPath $script.FullName -TotalCount 8) -join "`n"
    $roleLine = " * Role hierarchy: $expectedRole"
    $boundaryLine = " * Execution boundary: $expectedBoundary"

    if ($headerText.Contains(' * Role hierarchy:') -and
        $headerText.Contains(' * Primary responsibility:') -and
        $headerText.Contains(' * Execution boundary:')) {
        $headerCount++
    }
    else {
        Add-Failure $failures "Missing role header: $relativePath"
    }

    if (!$headerText.Contains($roleLine)) {
        Add-Failure $failures "Role hierarchy mismatch: $relativePath (expected '$expectedRole')"
    }

    if (!$headerText.Contains($boundaryLine)) {
        Add-Failure $failures "Execution boundary mismatch: $relativePath (expected '$expectedBoundary')"
    }

    if (!$boundaryCounts.ContainsKey($expectedBoundary)) {
        $boundaryCounts[$expectedBoundary] = 0
    }
    $boundaryCounts[$expectedBoundary]++
}

$requiredAsmdefs = @(
    'StateMachine\VSplatter.StateMachine.asmdef',
    'StateMachine\Editor\VSplatter.StateMachine.Editor.asmdef'
)
foreach ($relativeAsmdef in $requiredAsmdefs) {
    $asmdefPath = Join-Path $scriptsRoot $relativeAsmdef
    if (!(Test-Path -LiteralPath $asmdefPath -PathType Leaf)) {
        Add-Failure $failures "Missing required asmdef: $relativeAsmdef"
    }
}

if ($failures.Count -gt 0) {
    Write-Host "[FAIL] Script refactor integrity found $($failures.Count) issue(s)." -ForegroundColor Red
    foreach ($failure in $failures) {
        Write-Host " - $failure" -ForegroundColor Red
    }

    exit 1
}

$wipCount = @(Get-ChildItem -LiteralPath (Join-Path $scriptsRoot 'WIP') -Recurse -Filter '*.cs' -File).Count
Write-Host "[OK] Scripts: $($scripts.Count)"
Write-Host "[OK] Adjacent script metas and unique GUIDs: $($guidOwners.Count)"
Write-Host "[OK] Role headers and path alignment: $headerCount"
Write-Host "[OK] WIP scripts: $wipCount"
foreach ($boundary in ($boundaryCounts.Keys | Sort-Object)) {
    Write-Host "[OK] $boundary`: $($boundaryCounts[$boundary])"
}
