#Requires -Version 7
param([string]$ProjectRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path)
$ErrorActionPreference='Stop'
# Production rules with managed Unity/data shims; NOT Play Mode, real storage or physics.
$source=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'ManagedSystemsChecks.cs.txt') -Raw
$files=@(
 'Assets/Scripts/Gameplay/Entities/Common/Equipment/RaidArmorPenetration.cs',
 'Assets/Scripts/Gameplay/Progression/CombatBalanceRules.cs',
 'Assets/Scripts/Gameplay/Progression/CraftingCatalogSO.cs',
 'Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Definitions/FactionDiscoveryRulesSO.cs',
 'Assets/Scripts/Gameplay/World/Sectors/StageMap/LandmarkMapGenerator.cs',
 'Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidExtractionPortalPolicy.cs'
)
foreach($file in $files){
 $source += [Environment]::NewLine+((Get-Content -LiteralPath (Join-Path $ProjectRoot $file) -Raw) -replace '(?m)^using [^\r\n]+;\r?\n','')
}
Add-Type -TypeDefinition $source
[SystemsPatchChecks]::Run()
