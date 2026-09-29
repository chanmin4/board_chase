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

function Assert-InScriptsRoot {
    param([string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (!$fullPath.StartsWith($scriptsRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase) -and
        $fullPath -ne $scriptsRoot) {
        throw "Path is outside Assets/Scripts: $fullPath"
    }

    return $fullPath
}

function Get-UnityFolderMeta {
    $guid = [Guid]::NewGuid().ToString('N')
    return @"
fileFormatVersion: 2
guid: $guid
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
"@
}

function Ensure-UnityFolder {
    param([string]$FolderPath)

    $fullPath = Assert-InScriptsRoot $FolderPath
    if (!(Test-Path -LiteralPath $fullPath)) {
        if ($DryRun) {
            Write-Host "[DRY] Create folder: $fullPath"
            return
        }

        [System.IO.Directory]::CreateDirectory($fullPath) | Out-Null
    }

    $metaPath = "$fullPath.meta"
    if (!(Test-Path -LiteralPath $metaPath) -and !$DryRun) {
        [System.IO.File]::WriteAllText(
            $metaPath,
            (Get-UnityFolderMeta),
            [System.Text.UTF8Encoding]::new($false))
    }
}

function Ensure-UnityFolderChain {
    param([string]$FolderPath)

    $fullPath = Assert-InScriptsRoot $FolderPath
    if ($fullPath -eq $scriptsRoot) {
        return
    }

    $parent = [System.IO.Directory]::GetParent($fullPath)
    if ($null -ne $parent -and $parent.FullName -ne $scriptsRoot) {
        Ensure-UnityFolderChain $parent.FullName
    }

    Ensure-UnityFolder $fullPath
}

function Move-UnityAsset {
    param(
        [Parameter(Mandatory = $true)][string]$SourceRelative,
        [Parameter(Mandatory = $true)][string]$DestinationRelative,
        [switch]$Optional
    )

    $source = Assert-InScriptsRoot (Join-Path $scriptsRoot $SourceRelative)
    $destination = Assert-InScriptsRoot (Join-Path $scriptsRoot $DestinationRelative)

    if (!(Test-Path -LiteralPath $source)) {
        if (Test-Path -LiteralPath $destination) {
            Write-Host "[SKIP] Already moved: $DestinationRelative"
            return
        }

        if ($Optional) {
            Write-Warning "Optional source not found: $SourceRelative"
            return
        }

        throw "Source not found: $SourceRelative"
    }

    if (Test-Path -LiteralPath $destination) {
        throw "Destination already exists: $DestinationRelative"
    }

    $destinationParent = [System.IO.Directory]::GetParent($destination).FullName
    Ensure-UnityFolderChain $destinationParent

    $sourceMeta = "$source.meta"
    $destinationMeta = "$destination.meta"
    if (Test-Path -LiteralPath $destinationMeta) {
        throw "Destination meta already exists: $DestinationRelative.meta"
    }

    if ($DryRun) {
        Write-Host "[DRY] Move: $SourceRelative -> $DestinationRelative"
        return
    }

    Move-Item -LiteralPath $source -Destination $destination
    if (Test-Path -LiteralPath $sourceMeta) {
        Move-Item -LiteralPath $sourceMeta -Destination $destinationMeta
    }
    elseif ([System.IO.Path]::GetExtension($source) -eq '.cs') {
        throw "Script meta is missing: $SourceRelative.meta"
    }

    Write-Host "[MOVE] $SourceRelative -> $DestinationRelative"
}

function Get-ScriptIntegrityMap {
    $result = @{}
    $scripts = Get-ChildItem -LiteralPath $scriptsRoot -Recurse -File -Filter '*.cs'

    foreach ($script in $scripts) {
        $metaPath = "$($script.FullName).meta"
        if (!(Test-Path -LiteralPath $metaPath)) {
            throw "Missing .meta for script: $($script.FullName)"
        }

        $guidLine = Get-Content -LiteralPath $metaPath -TotalCount 4 |
            Where-Object { $_ -match '^guid:\s*([0-9a-fA-F]+)\s*$' } |
            Select-Object -First 1

        if ($null -eq $guidLine -or $guidLine -notmatch '^guid:\s*([0-9a-fA-F]+)\s*$') {
            throw "Unable to read GUID from: $metaPath"
        }

        $guid = $Matches[1].ToLowerInvariant()
        if ($result.ContainsKey($guid)) {
            throw "Duplicate script GUID detected: $guid"
        }

        $result[$guid] = [PSCustomObject]@{
            Hash = (Get-FileHash -LiteralPath $script.FullName -Algorithm SHA256).Hash
            Path = $script.FullName.Substring($projectRoot.Length + 1)
        }
    }

    return $result
}

function Assert-ScriptIntegrity {
    param([hashtable]$Before)

    $after = Get-ScriptIntegrityMap
    if ($Before.Count -ne $after.Count) {
        throw "Script count changed during migration. before=$($Before.Count), after=$($after.Count)"
    }

    foreach ($guid in $Before.Keys) {
        if (!$after.ContainsKey($guid)) {
            throw "Script GUID disappeared during migration: $guid ($($Before[$guid].Path))"
        }

        if ($Before[$guid].Hash -ne $after[$guid].Hash) {
            throw "Script contents changed during migration: $guid ($($Before[$guid].Path) -> $($after[$guid].Path))"
        }
    }

    Write-Host "[OK] Script count, GUIDs, and contents preserved: $($after.Count) files"
}

$before = Get-ScriptIntegrityMap
Write-Host "[START] Script hierarchy migration. scripts=$($before.Count), dryRun=$DryRun"

# Top-level responsibility groups.
Move-UnityAsset 'GameScene' 'Gameplay'
Move-UnityAsset 'Audio' 'Presentation\Audio'
Move-UnityAsset 'BaseClass' 'Core\Foundation'
Move-UnityAsset 'BattleTestScene' 'Scenes\BattleTest'
Move-UnityAsset 'Debug' 'Diagnostics\Runtime\UI'
Move-UnityAsset 'EditorTools' 'Authoring'
Move-UnityAsset 'Event' 'Core\Events'
Move-UnityAsset 'GamePlayUI' 'Presentation\UI\Gameplay'
Move-UnityAsset 'Input' 'Core\Input'
Move-UnityAsset 'Menu' 'Presentation\UI\Menu'
Move-UnityAsset 'RuntimeAnchor' 'Core\Anchors'
Move-UnityAsset 'SaveSystem' 'Core\Persistence'
Move-UnityAsset 'SceneManagement' 'Core\SceneFlow'
Move-UnityAsset 'UI' 'Presentation\UI\Common'
Move-UnityAsset 'Interaction' 'Gameplay\Interaction\Core'
Move-UnityAsset 'Risk' 'Gameplay\Progression\Risk'
Move-UnityAsset 'ProgressManager.cs' 'Gameplay\Progression\ProgressManager.cs'
Move-UnityAsset 'ResolutionManager.cs' 'Presentation\UI\Display\ResolutionManager.cs'
Move-UnityAsset 'TimeUtils.cs' 'Core\Utilities\TimeUtils.cs'

# Gameplay domain split.
Move-UnityAsset 'Gameplay\Camera' 'Presentation\Camera'
Move-UnityAsset 'Gameplay\Character' 'Gameplay\Entities'
Move-UnityAsset 'Gameplay\Item' 'Gameplay\Items'
Move-UnityAsset 'Gameplay\Sector' 'Gameplay\World\Sectors'
Move-UnityAsset 'Gameplay\Difficulty' 'Gameplay\Progression\Difficulty'
Move-UnityAsset 'Gameplay\METAUpgrade' 'Gameplay\Progression\Meta'
Move-UnityAsset 'Gameplay\Level' 'Gameplay\Progression\Level'
Move-UnityAsset 'Gameplay\Infection' 'Gameplay\Combat\Infection'
Move-UnityAsset 'Gameplay\Paint' 'Gameplay\Combat\PaintMark'
Move-UnityAsset 'Gameplay\Projectile' 'Gameplay\Combat\Projectiles'
Move-UnityAsset 'Gameplay\QTE' 'Gameplay\Interaction\QTE'
Move-UnityAsset 'Gameplay\GameUI' 'Presentation\UI\HUD\World'
Move-UnityAsset 'Gameplay\UI' 'Presentation\UI\HUD\Legacy'
Move-UnityAsset 'Gameplay\System' 'Core\Application\Pause'
Move-UnityAsset 'Gameplay\ScriptableObject' 'Gameplay\Progression\Stage\LegacyDefinitions'
Move-UnityAsset 'Gameplay\Not Using' 'WIP\Legacy\Gameplay\NotUsing'
Move-UnityAsset 'WIP\Legacy\Gameplay\NotUsing\Board\SectorEdge.cs' 'Gameplay\World\Sectors\Portal\SectorEdge.cs'

# Managers are placed with the systems they own.
Move-UnityAsset 'Gameplay\Manager\InfectionControlManager.cs' 'Gameplay\Combat\Infection\InfectionControlManager.cs'
Move-UnityAsset 'Gameplay\Manager\SectorOccupancyManager.cs' 'Gameplay\World\Sectors\Runtime\SectorOccupancyManager.cs'
Move-UnityAsset 'Gameplay\Manager\SectorPortalManager.cs' 'Gameplay\World\Sectors\Runtime\SectorPortalManager.cs'
Move-UnityAsset 'Gameplay\Manager\SectorStateManager.cs' 'Gameplay\World\Sectors\Runtime\SectorStateManager.cs'
Move-UnityAsset 'Gameplay\Manager\StageProgressionManager.cs' 'Gameplay\Progression\Stage\StageProgressionManager.cs'

# Entity hierarchy.
Move-UnityAsset 'Gameplay\Entities\ShooterEntity' 'Gameplay\Entities\Shooter'
Move-UnityAsset 'Gameplay\Entities\CreatureEnemy' 'Gameplay\Entities\Creature'
Move-UnityAsset 'Gameplay\Entities\CommonStateMachine' 'Gameplay\Entities\Common\StateMachine'
Move-UnityAsset 'Gameplay\Entities\Config' 'Gameplay\Entities\Common\Config'
Move-UnityAsset 'Gameplay\Entities\Equipment' 'Gameplay\Entities\Common\Equipment'
Move-UnityAsset 'Gameplay\Entities\KnockBack' 'Gameplay\Entities\Common\KnockBack'
Move-UnityAsset 'Gameplay\Entities\ScriptableObject' 'Gameplay\Entities\Common\Definitions'
Move-UnityAsset 'Gameplay\Entities\StatusEffects' 'Gameplay\Entities\Common\StatusEffects'
Move-UnityAsset 'Gameplay\Entities\tempSci-fi' 'Gameplay\Entities\Animation\LegacySciFi'

$entityRootScripts = @(
    'CharacterAudio.cs',
    'Damageable.cs',
    'DamageFlashController.cs',
    'DropGroup.cs',
    'DropItem.cs',
    'EnemyCommonSettings.cs',
    'EntityComponentResolver.cs',
    'InvulnerabilityController.cs',
    'ZoneTriggerController.cs'
)
foreach ($file in $entityRootScripts) {
    Move-UnityAsset "Gameplay\Entities\$file" "Gameplay\Entities\Common\Core\$file"
}

Move-UnityAsset 'Gameplay\Entities\Shooter\VSplatter(Player)' 'Gameplay\Entities\Shooter\Player'
Move-UnityAsset 'Gameplay\Entities\Shooter\Effect' 'Gameplay\Entities\Shooter\Common\Effects'
Move-UnityAsset 'Gameplay\Entities\Shooter\StateMachine' 'Gameplay\Entities\Shooter\Common\StateMachine'
Move-UnityAsset 'Gameplay\Entities\Shooter\Vision' 'Gameplay\Entities\Shooter\Common\Vision'

# Specialize code that was previously mixed into Shooter/Common.
$enemyStateMachineScripts = @(
    'EnemyShooterAcquireTargetActionSO.cs',
    'EnemyShooterAttackActionSO.cs',
    'EnemyShooterChaseActionSO.cs',
    'EnemyShooterClearCombatActionSO.cs',
    'EnemyShooterCombatConditionSO.cs',
    'EnemyShooterCombatController.cs',
    'EnemyShooterMeleeActionSO.cs',
    'EnemyShooterMoveActionSO.cs',
    'EnemyShooterPatrolActionSO.cs',
    'EnemyShooterStateMachineUtility.cs',
    'EnemyShooterThrowableActionSO.cs'
)
foreach ($file in $enemyStateMachineScripts) {
    Move-UnityAsset "Gameplay\Entities\Shooter\Common\StateMachine\$file" "Gameplay\Entities\Shooter\Enemy\StateMachine\$file"
}

Move-UnityAsset 'Gameplay\Entities\Shooter\Common\Effects\EnemyShooterEffectController.cs' 'Gameplay\Entities\Shooter\Enemy\Effects\EnemyShooterEffectController.cs'
Move-UnityAsset 'Gameplay\Entities\Shooter\Player\Effect' 'Gameplay\Entities\Shooter\Player\Effects'
Move-UnityAsset 'Gameplay\Entities\Shooter\Common\Effects\PlayerShooterEffectController.cs' 'Gameplay\Entities\Shooter\Player\Effects\PlayerShooterEffectController.cs'
Move-UnityAsset 'Gameplay\Entities\Shooter\Common\Vision\PlayerVisionFogOverlayUI.cs' 'Presentation\UI\Gameplay\Vision\PlayerVisionFogOverlayUI.cs'

$shooterRootScripts = @(
    'EntityFootstepSoundEmitter.cs',
    'EntityRange.cs',
    'EntityShootCore.cs',
    'EntityShooterCharacterAudio.cs',
    'EntityWeaponHolder.cs',
    'IMeleeAttackProfile.cs',
    'InventoryRuntimeBase.cs',
    'ProjectileSoundStimulusEmitter.cs',
    'ShooterAimTargetAnchor.cs',
    'ShooterDashController.cs',
    'ShooterDeathCorpseController.cs',
    'ShooterEquipmentController.cs',
    'ShooterInfection.cs',
    'ShooterLoadoutSO.cs',
    'ShooterLoadoutTableSO.cs',
    'ShooterMeleeAttack.cs',
    'ShooterMeleeHitZone.cs',
    'ShooterStatConfigSO.cs',
    'ShooterStatsRuntime.cs',
    'ShooterStuckRecovery.cs',
    'ShooterThrowableAttack.cs',
    'SoundStimulus.cs',
    'StatModifierTypes.cs',
    'UnarmedAttackProfileSO.cs'
)
foreach ($file in $shooterRootScripts) {
    Move-UnityAsset "Gameplay\Entities\Shooter\$file" "Gameplay\Entities\Shooter\Common\Core\$file"
}

Move-UnityAsset 'Gameplay\Entities\Shooter\Enemy\ScriptableObjects' 'Gameplay\Entities\Shooter\Enemy\Definitions'
Move-UnityAsset 'Gameplay\Entities\Shooter\Player\ScriptableObject' 'Gameplay\Entities\Shooter\Player\Definitions'
Move-UnityAsset 'Gameplay\Entities\Shooter\Player\WIP' 'WIP\Legacy\Gameplay\Player\ExistingWIP'

$enemyRootScripts = @(
    'EnemyLootInventoryRuntime.cs',
    'EnemyShooter.cs',
    'EnemyShooterAimLaserController.cs',
    'EnemyShooterCharacterAudio.cs',
    'EnemyShooterDashController.cs',
    'EnemyShooterDeathCorpseController.cs',
    'EnemyShooterInfection.cs',
    'EnemyShooterShoot.cs',
    'EnemyShooterStatsRuntime.cs',
    'EnemyShooterVisionController.cs'
)
foreach ($file in $enemyRootScripts) {
    Move-UnityAsset "Gameplay\Entities\Shooter\Enemy\$file" "Gameplay\Entities\Shooter\Enemy\Common\$file"
}

# Item hierarchy.
Move-UnityAsset 'Gameplay\Items\CharacterItem\ScriptableObjects' 'Gameplay\Items\Definitions'
Move-UnityAsset 'Gameplay\Items\CharacterItem\Weapon\ScriptableObjects' 'Gameplay\Items\Weapons\Definitions'
Move-UnityAsset 'Gameplay\Items\CharacterItem\Weapon' 'Gameplay\Items\Weapons\Runtime'

$worldItemScripts = @(
    'RotatingPickupVisual.cs',
    'WorldItemPickup.cs',
    'WorldItemPickupArcMotion.cs',
    'WorldItemPickupSpawner.cs'
)
foreach ($file in $worldItemScripts) {
    Move-UnityAsset "Gameplay\Items\CharacterItem\$file" "Gameplay\Items\World\$file"
}

# Main-scene functionality and inactive legacy scripts.
Move-UnityAsset 'MainScene\Skininventory\SkinGridItem.cs' 'Presentation\UI\Customization\SkinGridItem.cs'
Move-UnityAsset 'MainScene' 'WIP\Legacy\MainScene'
Move-UnityAsset 'Authoring\Editor\SceneSelector' 'WIP\Legacy\Authoring\Editor\SceneSelector'
Move-UnityAsset 'Authoring\Editor\ScriptableObjectBrowser' 'WIP\Legacy\Authoring\Editor\ScriptableObjectBrowser'

$inactiveRuntimeMoves = @(
    @('Gameplay\Items\CharacterItem\CollectableItem.cs', 'WIP\Legacy\Gameplay\Items\CollectableItem.cs'),
    @('Gameplay\Entities\Shooter\Player\VSplatterAimUI.cs', 'WIP\Legacy\Gameplay\Player\VSplatterAimUI.cs'),
    @('Gameplay\Entities\Shooter\Player\VSplatterWeaponHolder.cs', 'WIP\Legacy\Gameplay\Player\VSplatterWeaponHolder.cs'),
    @('Gameplay\Entities\Shooter\Enemy\Raider\RaiderAudio.cs', 'WIP\Legacy\Gameplay\Enemy\RaiderAudio.cs'),
    @('Gameplay\Items\Weapons\Runtime\SpecialBullet.cs', 'WIP\Legacy\Gameplay\Items\SpecialBullet.cs'),
    @('Gameplay\GameSceneManager.cs', 'WIP\Legacy\Gameplay\GameSceneManager.cs'),
    @('Gameplay\World\Sectors\StageSectorUnlockSO.cs', 'WIP\Legacy\Gameplay\Sectors\StageSectorUnlockSO.cs'),
    @('Gameplay\World\Sectors\ScriptableObject\SectorObjectConfigSO.cs', 'WIP\Legacy\Gameplay\Sectors\SectorObjectConfigSO.cs'),
    @('Presentation\UI\HUD\Legacy\CooldownHUD.cs', 'WIP\Legacy\Presentation\CooldownHUD.cs'),
    @('Presentation\UI\HUD\Legacy\SurvivalSuccessManager.cs', 'WIP\Legacy\Presentation\SurvivalSuccessManager.cs'),
    @('Presentation\UI\HUD\Legacy\SurvivalTimerHUD.cs', 'WIP\Legacy\Presentation\SurvivalTimerHUD.cs'),
    @('Presentation\UI\HUD\Legacy\ZoneHitHUD.cs', 'WIP\Legacy\Presentation\ZoneHitHUD.cs'),
    @('Gameplay\Entities\Creature\NormalEnemy\CommonNormalEnemyStateMachine\Conditions\IsPlayerInCurrentSectorConditionSO .cs', 'WIP\Legacy\Gameplay\Creature\IsPlayerInCurrentSectorConditionSO.cs'),
    @('Gameplay\Items\Weapons\Definitions\PistolSO.cs', 'WIP\Legacy\Gameplay\Items\PistolWeaponSO.cs')
)
foreach ($move in $inactiveRuntimeMoves) {
    Move-UnityAsset $move[0] $move[1]
}

# File names are aligned with their primary declared type; type names and APIs are unchanged.
$typeNameAlignmentMoves = @(
    @('Presentation\Audio\Sound\UI\UI_ButtonClick.cs', 'Presentation\Audio\Sound\UI\ButtonClick.cs'),
    @('Core\Events\ScriptableObject\MutarusQTEPatternResetRequestEventChannel.cs', 'Core\Events\ScriptableObject\MutarusQTEPatternResetRequestEventChannelSO.cs'),
    @('Gameplay\Entities\Common\StateMachine\Actions\CreatureFaceVSplatterSO.cs', 'Gameplay\Entities\Common\StateMachine\Actions\CreatureFacePlayerSO.cs'),
    @('Gameplay\Entities\Common\Config\GetHitEffectConfigSO.cs', 'Gameplay\Entities\Common\Config\DamageFlash_GetHitConfigSO.cs'),
    @('Gameplay\Entities\Creature\CommonEnemyStateMachine\AttackExecutor\EnemyProjectileAttackRuntime.cs', 'Gameplay\Entities\Creature\CommonEnemyStateMachine\AttackExecutor\EnemyProjectileAttackExecutor.cs'),
    @('Gameplay\Entities\Creature\CommonEnemyStateMachine\AttackExecutor\EnemySelfDestructAttackRuntime.cs', 'Gameplay\Entities\Creature\CommonEnemyStateMachine\AttackExecutor\EnemySelfDestructAttackExecutor.cs'),
    @('Gameplay\Entities\Creature\EnemyContactAttackBox.cs', 'Gameplay\Entities\Creature\EnemyContactAttack.cs'),
    @('Gameplay\Entities\Shooter\Player\PlayerChracter.cs', 'Gameplay\Entities\Shooter\Player\PlayerCharacter.cs'),
    @('Core\Application\Pause\PauseException.cs', 'Core\Application\Pause\PauseExceptionRouter.cs'),
    @('Presentation\UI\Common\ESCMenu\SettingSystem.cs', 'Presentation\UI\Common\ESCMenu\SettingsSystem.cs'),
    @('Presentation\UI\Common\ESCMenu\UISettingGraphicComponent.cs', 'Presentation\UI\Common\ESCMenu\UISettingGraphicsComponent.cs')
)
foreach ($move in $typeNameAlignmentMoves) {
    Move-UnityAsset $move[0] $move[1]
}

$playerInfectionRule = Get-ChildItem -LiteralPath (Join-Path $scriptsRoot 'Gameplay\Entities\Shooter\Player') -Recurse -File -Filter 'PlayerInfectionRuleSO.cs' |
    Select-Object -First 1
if ($null -ne $playerInfectionRule) {
    $sourceRelative = $playerInfectionRule.FullName.Substring($scriptsRoot.Length + 1)
    $destinationRelative = Join-Path $playerInfectionRule.Directory.FullName 'PlayerInfectionRulesSO.cs'
    $destinationRelative = $destinationRelative.Substring($scriptsRoot.Length + 1)
    Move-UnityAsset $sourceRelative $destinationRelative
}

if (!$DryRun) {
    Assert-ScriptIntegrity $before
}

Write-Host '[DONE] Script hierarchy migration completed.'
