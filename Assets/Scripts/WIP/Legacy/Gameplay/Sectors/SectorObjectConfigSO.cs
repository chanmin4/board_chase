/*
 * Role hierarchy: WIP > Legacy > Gameplay > Sectors
 * Primary responsibility: Isolates the legacy or deferred SectorObjectConfigSO implementation from active production code.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;

[Obsolete("Use PlayerStageObjectConfigSO or EnemyStageObjectConfigSO instead.")]
public class SectorObjectConfigSO : StageObjectConfigSO
{
}
