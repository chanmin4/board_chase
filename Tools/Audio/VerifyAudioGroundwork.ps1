param([string]$ProjectRoot = (Join-Path $PSScriptRoot '../..'))
$ErrorActionPreference = 'Stop'
$projectPath = (Resolve-Path -LiteralPath $ProjectRoot).Path
$yamlDll = Get-ChildItem -LiteralPath (Join-Path $projectPath 'Library/PackageCache') -Recurse -Filter 'Unity.VisualScripting.YamlDotNet.dll' | Select-Object -First 1
if ($null -eq $yamlDll) { throw 'Import the Unity project first; its existing YAML parser is required.' }
[void][System.Reflection.Assembly]::LoadFrom($yamlDll.FullName)
function Read-ProjectText([string]$path) { [IO.File]::ReadAllText((Join-Path $projectPath $path)) }
function Require([bool]$condition, [string]$message) { if (!$condition) { throw $message } }

$cuePaths = @(
    'UI/UI_Navigate', 'UI/Quest_Accepted', 'UI/Quest_Completed',
    'Interactable/Search_Start', 'Interactable/Search_Loop', 'Interactable/Search_Complete', 'Interactable/Search_Cancel',
    'Interactable/NPC_Greeting_Temp', 'Interactable/NPC_Farewell_Temp', 'Interactable/NPC_Accept_Temp', 'Interactable/NPC_Reject_Temp'
) | ForEach-Object { "Assets/ScriptableObject/Audio/AudioCue/SFX/$_.asset" }
$configPaths = Get-ChildItem -LiteralPath (Join-Path $projectPath 'Assets/ScriptableObject/Audio/AudioConfiguration') -Filter '*.asset' | ForEach-Object { [IO.Path]::GetRelativePath($projectPath, $_.FullName) }
$otherPaths = @(
    'Assets/Audio/Audio/Mixer/Master.mixer', 'Assets/Prefabs/AudioMaster.prefab',
    'Assets/ScriptableObject/Audio/NpcAudioProfile_Default.asset', 'Assets/ScriptableObject/Audio/AudioCueLibrary.asset',
    'Assets/Scenes/Manager/PersistenceManager.unity', 'Assets/Scenes/Manager/GamePlay.unity',
    'Assets/Prefabs/GameScene/Shooter/HomeNPC/HomeQuestNpc.prefab',
    'Assets/Prefabs/GameScene/Shooter/HomeNPC/HomeItemNpc.prefab',
    'Assets/Prefabs/GameScene/Shooter/HomeNPC/HomeStatsNpc.prefab'
)
$documents = 0
foreach ($path in @($cuePaths) + @($configPaths) + $otherPaths) {
    $yaml = Read-ProjectText $path
    $yaml = [regex]::Replace($yaml, '(?m)^%.*\r?\n', '')
    $yaml = [regex]::Replace($yaml, '(?m)^--- !u!\d+ &-?\d+(?: stripped)?\r?$', '---')
    $stream = New-Object Unity.VisualScripting.YamlDotNet.RepresentationModel.YamlStream
    $reader = New-Object IO.StringReader($yaml)
    try { $stream.Load($reader); $documents += $stream.Documents.Count } finally { $reader.Dispose() }
}

$mixer = Read-ProjectText 'Assets/Audio/Audio/Mixer/Master.mixer'
foreach ($name in @('MasterVolume','MusicVolume','SFXVolume')) { Require ($mixer.Contains("name: $name")) "Missing mixer parameter $name" }
foreach ($name in @('Music','SFX','UI','Voice','Ambience')) { Require ($mixer.Contains("m_Name: $name")) "Missing mixer group $name" }
$ids = @([regex]::Matches($mixer, '(?m)^--- !u!\d+ &(-?\d+)') | ForEach-Object { $_.Groups[1].Value })
Require (($ids | Sort-Object -Unique).Count -eq $ids.Count) 'Duplicate mixer fileID'
foreach ($reference in [regex]::Matches($mixer, 'fileID: (-?\d+)')) {
    $id = $reference.Groups[1].Value
    Require ($id -eq '0' -or $ids -contains $id) "Missing internal mixer object $id"
}
foreach ($path in $configPaths) {
    $config = Read-ProjectText $path
    Require ($config -notmatch 'outputMixerGroup: \{fileID: 0\}') "Unrouted configuration $path"
    $group = [regex]::Match($config, 'outputMixerGroup: \{fileID: (\d+), guid: ([a-f0-9]{32})')
    Require ($group.Success -and $ids -contains $group.Groups[1].Value) "Invalid output group in $path"
    Require ($group.Groups[2].Value -eq '4ec18c545f8f1864cbc96259630c5eec') "Unexpected mixer in $path"
}

# Resolve references against all Assets, including ignored vendor sound packs.
$metaIndex = @{}
$metaLines = & rg --no-ignore -n '^guid: [a-f0-9]{32}' (Join-Path $projectPath 'Assets') -g '*.meta'
foreach ($line in $metaLines) {
    if ($line -match '^(.*\.meta):\d+:guid: ([a-f0-9]{32})') {
        $id = $Matches[2]; $asset = $Matches[1].Substring(0, $Matches[1].Length - 5)
        if (!$metaIndex.ContainsKey($id)) { $metaIndex[$id] = @() }
        $metaIndex[$id] += $asset
    }
}
$library = Read-ProjectText 'Assets/ScriptableObject/Audio/AudioCueLibrary.asset'
$references = [Collections.Generic.HashSet[string]]::new()
foreach ($path in @($cuePaths) + @($configPaths) + @('Assets/ScriptableObject/Audio/NpcAudioProfile_Default.asset')) {
    $body = Read-ProjectText $path
    foreach ($match in [regex]::Matches($body, 'guid: ([a-f0-9]{32})')) { [void]$references.Add($match.Groups[1].Value) }
}
foreach ($id in $references) {
    Require ($metaIndex.ContainsKey($id) -and $metaIndex[$id].Count -eq 1) "Missing or duplicate reference $id"
    Require (Test-Path -LiteralPath $metaIndex[$id][0]) "Missing asset for $id"
}
foreach ($path in $cuePaths) {
    $meta = Read-ProjectText "$path.meta"
    $id = [regex]::Match($meta, '(?m)^guid: ([a-f0-9]{32})').Groups[1].Value
    Require ($id.Length -eq 32 -and $library.Contains($id)) "Cue missing from library $path"
    $cue = Read-ProjectText $path
    Require ($cue.Contains("_cueId: $id")) "Cue identity mismatch $path"
    Require ($cue -match 'fileID: 8300000, guid:') "Empty cue $path"
    $expectedLoop = if ($path.EndsWith('Search_Loop.asset')) { 1 } else { 0 }
    Require ($cue.Contains("looping: $expectedLoop")) "Wrong loop policy $path"
}
$persistence = Read-ProjectText 'Assets/Scenes/Manager/PersistenceManager.unity'
Require ($persistence.Contains('_autoBindSelectables: 1')) 'UI auto binding is disabled'
Require ($persistence.Contains('guid: b807819b412d438b8c9e9ed6530a3101')) 'Search listener missing from persistence scene'
$gameplay = Read-ProjectText 'Assets/Scenes/Manager/GamePlay.unity'
Require ($gameplay.Contains('_uiAudioEventChannel: {fileID: 11400000, guid: ec5ebdbee56024d418a0e5616c32978f')) 'Overlay audio channel missing'
foreach ($path in ($otherPaths | Where-Object { $_ -like '*HomeNPC*' })) {
    Require ((Read-ProjectText $path).Contains('_conversationAudio: {fileID: 11400000, guid: b807819b412d438b8c9e9ed6530a3103')) "NPC audio profile missing from $path"
}
Write-Output "PASS: $documents YAML documents; $($configPaths.Count) routed configurations; $($references.Count) resolved references; $($cuePaths.Count) library cues; UI/search/NPC scene wiring."
