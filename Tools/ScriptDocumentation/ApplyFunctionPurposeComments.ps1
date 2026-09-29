<#
.SYNOPSIS
Adds concise, consistent function-purpose comments to active project scripts.

.DESCRIPTION
This is a source-only documentation pass. It never changes method signatures,
method bodies, serialized fields, or runtime behavior. The pass intentionally
excludes WIP and Legacy directories because those files are either quarantined
or still waiting for a dedicated migration/removal review.

Run a preview first:
  powershell -ExecutionPolicy Bypass -File Tools/ScriptDocumentation/ApplyFunctionPurposeComments.ps1

Apply comments:
  powershell -ExecutionPolicy Bypass -File Tools/ScriptDocumentation/ApplyFunctionPurposeComments.ps1 -Apply
#>

[CmdletBinding()]
param(
    [switch]$Apply,
    [switch]$RefreshGeneric
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$targetRoots = @(
    (Join-Path $projectRoot 'Assets\Scripts'),
    (Join-Path $projectRoot 'Assets\Editor')
)

# We only target method declarations with an explicit accessibility modifier.
# This avoids local functions, interface contracts, lambdas, and field initializers.
$methodPattern = '^(?<indent>\s*)(?:public|private|protected|internal)\s+(?:(?:static|virtual|override|abstract|sealed|async|extern|new|partial)\s+)*(?:[\w<>\[\],.?]+\s+)+(?<name>[A-Za-z_]\w*)\s*\('

function ConvertTo-ReadablePhrase {
    param([Parameter(Mandatory = $true)][string]$Identifier)

    $phrase = [regex]::Replace($Identifier, '([a-z0-9])([A-Z])', '$1 $2')
    $phrase = [regex]::Replace($phrase, '([A-Z]+)([A-Z][a-z])', '$1 $2')
    return $phrase.ToLowerInvariant()
}

function Get-FunctionPurpose {
    param([Parameter(Mandatory = $true)][string]$MethodName)

    $tail = ConvertTo-ReadablePhrase $MethodName
    $tailWithoutPrefix = $tail -replace '^(try|can|is|has|get|set|resolve|find|create|build|initialize|configure|refresh|update|apply|remove|add|register|unregister|handle|draw|validate|on|ensure|clear|copy|merge|open|close|count|match|matches|convert|load|save|begin|finish|end|start|stop|spawn|despawn|destroy|move|equip|unequip|toggle|enable|disable|show|hide|sync|calculate|evaluate|collect|release|assign|select|deselect|sort|filter|generate|queue|raise|broadcast|play|pause|resume|cancel|commit|discard|confirm|import|export|repair|rebuild|prepare|cache|store|restore|normalize|sanitize|publish|subscribe|unsubscribe|bind|unbind|enter|exit|tick|roll|trigger|face|explode|fail|notify|request) ', ''

    switch -Regex ($MethodName) {
        '^Awake$' { return 'Caches the component references required before runtime initialization.' }
        '^Start$' { return 'Completes initialization after scene dependencies are ready.' }
        '^OnEnable$' { return 'Activates runtime subscriptions and refreshes the enabled state.' }
        '^OnDisable$' { return 'Releases active subscriptions and transient state while disabled.' }
        '^OnDestroy$' { return 'Releases owned resources before this component is destroyed.' }
        '^Reset$' { return 'Initializes Inspector defaults for this component.' }
        '^OnValidate$' { return 'Normalizes Inspector-authored data after it changes in the Editor.' }
        '^Update$' { return 'Processes this component''s per-frame runtime work.' }
        '^LateUpdate$' { return 'Processes frame work that must run after the regular Update pass.' }
        '^FixedUpdate$' { return 'Processes fixed-timestep work used by physics-aligned behavior.' }
        '^Try' { return "Attempts to $tailWithoutPrefix and reports whether the request succeeded." }
        '^Can' { return "Checks whether $tailWithoutPrefix is currently permitted." }
        '^Is' { return "Checks whether $tailWithoutPrefix is currently true." }
        '^Has' { return "Checks whether this component has $tailWithoutPrefix." }
        '^Get' { return "Returns the current $tailWithoutPrefix." }
        '^Set' { return "Updates the $tailWithoutPrefix state." }
        '^Resolve' { return "Resolves the $tailWithoutPrefix needed by this component." }
        '^Find' { return "Finds the requested $tailWithoutPrefix." }
        '^Create' { return "Creates the $tailWithoutPrefix owned by this workflow." }
        '^Build' { return "Builds the $tailWithoutPrefix from the current authored data." }
        '^Initialize' { return "Initializes $tailWithoutPrefix for use by this component." }
        '^Configure' { return "Configures this component''s $tailWithoutPrefix." }
        '^Refresh' { return "Refreshes the displayed or cached $tailWithoutPrefix." }
        '^Update' { return "Updates the $tailWithoutPrefix from the current runtime state." }
        '^Apply' { return "Applies the requested $tailWithoutPrefix change." }
        '^Remove' { return "Removes the requested $tailWithoutPrefix from this component." }
        '^Add' { return "Adds the requested $tailWithoutPrefix to this component." }
        '^Register' { return "Registers $tailWithoutPrefix with its owning runtime service." }
        '^Unregister' { return "Unregisters $tailWithoutPrefix from its owning runtime service." }
        '^Handle' { return "Handles the incoming $tailWithoutPrefix request or event." }
        '^Draw' { return "Draws the $tailWithoutPrefix authoring UI." }
        '^Validate' { return "Validates $tailWithoutPrefix and reports invalid authored data." }
        '^On' { return "Responds to the $tailWithoutPrefix callback." }
        '^Ensure' { return "Ensures $tailWithoutPrefix is ready before the workflow continues." }
        '^Clear' { return "Clears the current $tailWithoutPrefix state." }
        '^Copy' { return "Copies $tailWithoutPrefix into the destination state." }
        '^Merge' { return "Merges $tailWithoutPrefix into the current state." }
        '^Open' { return "Opens the $tailWithoutPrefix UI or workflow." }
        '^Close' { return "Closes the active $tailWithoutPrefix UI or workflow." }
        '^Count' { return "Counts the available $tailWithoutPrefix." }
        '^Match' { return "Checks whether the current data matches $tailWithoutPrefix." }
        '^Convert' { return "Converts the current data to $tailWithoutPrefix." }
        '^Load' { return "Loads $tailWithoutPrefix into the active runtime state." }
        '^Save' { return "Saves the current $tailWithoutPrefix state." }
        '^Begin|^Start' { return "Begins the $tailWithoutPrefix workflow." }
        '^Finish|^End' { return "Finishes the active $tailWithoutPrefix workflow." }
        '^Stop' { return "Stops the active $tailWithoutPrefix behavior." }
        '^Spawn' { return "Spawns $tailWithoutPrefix for the current runtime context." }
        '^Despawn|^Destroy' { return "Removes $tailWithoutPrefix from the active runtime context." }
        '^Move' { return "Moves $tailWithoutPrefix toward its requested destination." }
        '^Equip' { return "Equips $tailWithoutPrefix for the current owner." }
        '^Unequip' { return "Unequips the current $tailWithoutPrefix." }
        '^Toggle' { return "Toggles the $tailWithoutPrefix state." }
        '^Enable' { return "Enables $tailWithoutPrefix for this component." }
        '^Disable' { return "Disables $tailWithoutPrefix for this component." }
        '^Show' { return "Shows the $tailWithoutPrefix UI or visual state." }
        '^Hide' { return "Hides the $tailWithoutPrefix UI or visual state." }
        '^Sync' { return "Synchronizes $tailWithoutPrefix with the current source state." }
        '^Calculate|^Evaluate' { return "Calculates $tailWithoutPrefix from the current inputs." }
        '^Collect' { return "Collects $tailWithoutPrefix for this workflow." }
        '^Release' { return "Releases the owned $tailWithoutPrefix resource." }
        '^Assign' { return "Assigns $tailWithoutPrefix to its current owner." }
        '^Select' { return "Selects $tailWithoutPrefix for the active workflow." }
        '^Deselect' { return "Clears the active $tailWithoutPrefix selection." }
        '^Sort' { return "Sorts $tailWithoutPrefix for consistent processing." }
        '^Filter' { return "Filters $tailWithoutPrefix against the active criteria." }
        '^Generate' { return "Generates $tailWithoutPrefix from the authored data." }
        '^Queue' { return "Queues $tailWithoutPrefix for deferred processing." }
        '^Raise|^Publish|^Broadcast|^Notify' { return "Publishes the $tailWithoutPrefix notification." }
        '^Play' { return "Plays the requested $tailWithoutPrefix action or effect." }
        '^Pause' { return "Pauses the active $tailWithoutPrefix workflow." }
        '^Resume' { return "Resumes the paused $tailWithoutPrefix workflow." }
        '^Cancel|^Abort' { return "Cancels the active $tailWithoutPrefix workflow." }
        '^Commit|^Confirm' { return "Commits the current $tailWithoutPrefix selection or change." }
        '^Discard' { return "Discards the pending $tailWithoutPrefix state." }
        '^Import' { return "Imports $tailWithoutPrefix into the current authored data." }
        '^Export' { return "Exports $tailWithoutPrefix from the current authored data." }
        '^Repair|^Rebuild' { return "Repairs or rebuilds $tailWithoutPrefix from the current source data." }
        '^Prepare' { return "Prepares $tailWithoutPrefix for the next workflow step." }
        '^Cache|^Store' { return "Caches $tailWithoutPrefix for later reuse." }
        '^Restore' { return "Restores the saved $tailWithoutPrefix state." }
        '^Normalize|^Sanitize' { return "Normalizes $tailWithoutPrefix to a safe authored value." }
        '^Subscribe' { return "Subscribes to $tailWithoutPrefix notifications." }
        '^Unsubscribe' { return "Removes the $tailWithoutPrefix notification subscription." }
        '^Bind' { return "Binds $tailWithoutPrefix to the active runtime or UI target." }
        '^Unbind' { return "Removes the $tailWithoutPrefix binding." }
        '^Enter' { return "Enters the $tailWithoutPrefix state or area." }
        '^Exit' { return "Exits the active $tailWithoutPrefix state or area." }
        '^Tick' { return "Processes one update step for $tailWithoutPrefix." }
        '^Roll' { return "Rolls the $tailWithoutPrefix result from its configured probabilities." }
        '^Trigger' { return "Triggers the $tailWithoutPrefix response." }
        '^Face' { return "Aligns this component toward $tailWithoutPrefix." }
        '^Explode' { return "Executes the $tailWithoutPrefix explosion behavior." }
        '^Fail' { return "Marks the active $tailWithoutPrefix workflow as failed." }
        '^Request' { return "Requests $tailWithoutPrefix from the owning service." }
        default { return "Performs the $tail workflow for this component." }
    }
}

function Has-NearbyPurposeComment {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][AllowEmptyCollection()][string[]]$Lines,
        [Parameter(Mandatory = $true)][int]$MethodLineIndex
    )

    $firstIndex = [Math]::Max(0, $MethodLineIndex - 4)
    for ($index = $firstIndex; $index -lt $MethodLineIndex; $index++) {
        $line = $Lines[$index].Trim()
        if ($line.StartsWith('///') -or
            $line.StartsWith('// Purpose:') -or
            $line.StartsWith('/*')) {
            return $true
        }
    }

    return $false
}

$files = foreach ($targetRoot in $targetRoots) {
    if (Test-Path -LiteralPath $targetRoot) {
        Get-ChildItem -LiteralPath $targetRoot -Recurse -Filter '*.cs'
    }
}

$changedFiles = 0
$insertedComments = 0
$preview = New-Object System.Collections.Generic.List[string]

foreach ($file in $files) {
    if ($file.FullName -match '[\\/](WIP|Legacy)[\\/]') {
        continue
    }

    $lines = [System.IO.File]::ReadAllLines($file.FullName)
    if ($lines.Length -eq 0) {
        continue
    }

    $output = New-Object System.Collections.Generic.List[string]
    $fileInsertions = 0
    $fileRefreshes = 0

    for ($index = 0; $index -lt $lines.Length; $index++) {
        $line = $lines[$index]
        $match = [regex]::Match($line, $methodPattern)

        if ($match.Success -and $RefreshGeneric -and $index -gt 0 -and
            $lines[$index - 1] -match '^(?<indent>\s*)// Purpose: Executes .+$') {
            $purpose = Get-FunctionPurpose -MethodName $match.Groups['name'].Value
            $output[$output.Count - 1] = "$($match.Groups['indent'].Value)// Purpose: $purpose"
            $fileRefreshes++
        }
        elseif ($match.Success -and -not (Has-NearbyPurposeComment -Lines $lines -MethodLineIndex $index)) {
            $purpose = Get-FunctionPurpose -MethodName $match.Groups['name'].Value
            $output.Add("$($match.Groups['indent'].Value)// Purpose: $purpose")
            $fileInsertions++
        }

        $output.Add($line)
    }

    if ($fileInsertions -le 0 -and $fileRefreshes -le 0) {
        continue
    }

    # Windows PowerShell 5.1 runs on .NET Framework and does not provide Path.GetRelativePath.
    $relativePath = $file.FullName.Substring($projectRoot.Length).TrimStart([char[]]'\\/')
    $preview.Add("$relativePath : +$fileInsertions, refreshed $fileRefreshes")
    $insertedComments += $fileInsertions

    if ($Apply) {
        [System.IO.File]::WriteAllLines($file.FullName, $output, [System.Text.UTF8Encoding]::new($false))
        $changedFiles++
    }
}

$mode = if ($Apply) { 'APPLIED' } else { 'PREVIEW' }
Write-Output "Function-purpose comment pass: $mode"
Write-Output "Target files changed: $changedFiles"
Write-Output "Comments to insert: $insertedComments"
$preview | Select-Object -First 30
