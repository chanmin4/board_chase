param([string]$ProjectPath = 'E:/github/board_chase/board_chase')
$ErrorActionPreference = 'Stop'
$taskProjectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
$taskReportRoot = Join-Path $taskProjectRoot 'Library/MegaBundleImport'
$taskPlan = Get-Content -LiteralPath (Join-Path $taskReportRoot 'plan.json') -Raw | ConvertFrom-Json
$taskAudit = Get-Content -LiteralPath (Join-Path $taskReportRoot 'reference-audit.json') -Raw | ConvertFrom-Json
$taskProtected = @{}
foreach ($taskOriginal in $taskPlan.originals) { $taskProtected[$taskOriginal.path] = $true }
$taskChanges = [System.Collections.Generic.List[object]]::new()
$taskObsoleteVersion = 'da692e001514ec24dbc4cca1949ff7e8'
$taskObsoleteXR = '60e1133243b97e347b653163a8c01b64'
$taskMissingLighting = @('d35ea8da7bf2e53448f6d756251211ef', 'ee2d11691bfc5384b89d821ffe96a394', '80aa856a29e92c64a95e7043ee007b3d')
foreach ($taskReference in $taskAudit.unresolved.PSObject.Properties) {
    $taskGuid = $taskReference.Name
    if ($taskGuid -ne $taskObsoleteVersion -and $taskGuid -ne $taskObsoleteXR -and $taskGuid -notin $taskMissingLighting) {
        throw "Unreviewed unresolved GUID: $taskGuid"
    }
    foreach ($taskRelative in $taskReference.Value) {
        if ($taskProtected.ContainsKey($taskRelative)) { throw "Refusing to change pre-existing file: $taskRelative" }
        $taskAllowed = @($taskPlan.packs | Where-Object { $taskRelative.StartsWith($_.root + '/', [StringComparison]::Ordinal) })
        if ($taskAllowed.Count -ne 1) { throw "Not in a reviewed new pack: $taskRelative" }
        $taskTarget = (Resolve-Path -LiteralPath (Join-Path $taskProjectRoot $taskRelative)).Path
        $taskNewRoot = (Resolve-Path -LiteralPath (Join-Path $taskProjectRoot $taskAllowed[0].root)).Path
        if (-not $taskTarget.StartsWith($taskNewRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw "Target leaves new pack: $taskTarget" }
        $taskBefore = [IO.File]::ReadAllText($taskTarget)
        if (-not $taskBefore.StartsWith('%YAML')) { throw "Expected serialized text: $taskTarget" }
        if ($taskGuid -eq $taskObsoleteVersion) {
            # Keep the valid current URP version stamp; remove only the missing obsolete version subasset.
            $taskPattern = '(?ms)^--- !u!114 &[^\r\n]+\r?\nMonoBehaviour:\r?\n(?:(?!^--- ).)*?m_Script: \{fileID: 11500000, guid: ' + $taskGuid + ', type: 3\}\r?\n(?:(?!^--- ).)*(?=^--- |\z)'
            $taskMatches = [regex]::Matches($taskBefore, $taskPattern)
            if ($taskMatches.Count -ne 1 -or $taskMatches[0].Value -notmatch 'm_ObjectHideFlags: 11' -or $taskMatches[0].Value -notmatch 'm_GameObject: \{fileID: 0\}') { throw "Unexpected obsolete version block: $taskRelative" }
            $taskAfter = [regex]::Replace($taskBefore, $taskPattern, '')
        } elseif ($taskGuid -eq $taskObsoleteXR) {
            # UniversalRendererData in the installed URP no longer has this legacy xrSystemData field.
            $taskPattern = 'xrSystemData: \{fileID: 11400000, guid: ' + $taskGuid + ', type: 2\}'
            if ([regex]::Matches($taskBefore, $taskPattern).Count -ne 1) { throw "Unexpected legacy XR reference: $taskRelative" }
            $taskAfter = [regex]::Replace($taskBefore, $taskPattern, 'xrSystemData: {fileID: 0}')
        } else {
            # Vendor demo scenes reference lighting-settings assets absent from both supplied packages.
            # Null uses Unity's scene defaults and preserves the existing scene lighting values.
            $taskPattern = 'm_LightingSettings: \{fileID: 4890085278179872738, guid: ' + $taskGuid + ', type: 2\}'
            if ([regex]::Matches($taskBefore, $taskPattern).Count -ne 1) { throw "Unexpected missing demo lighting settings: $taskRelative" }
            $taskAfter = [regex]::Replace($taskBefore, $taskPattern, 'm_LightingSettings: {fileID: 0}')
        }
        $taskChanges.Add([pscustomobject]@{ Path = $taskTarget; Relative = $taskRelative; Before = $taskBefore; After = $taskAfter; Guid = $taskGuid })
    }
}
foreach ($taskChange in $taskChanges) {
    if ([IO.File]::ReadAllText($taskChange.Path) -cne $taskChange.Before) { throw "File changed during repair: $($taskChange.Relative)" }
    [IO.File]::WriteAllText($taskChange.Path, $taskChange.After, [Text.UTF8Encoding]::new($false))
    "REPAIRED $($taskChange.Relative): $($taskChange.Guid)"
}
$taskChanges | Select-Object Relative, Guid | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskReportRoot 'vendor-reference-repairs.json') -Encoding UTF8
"Repaired $($taskChanges.Count) new-pack-only serialized references. Existing files excluded."
