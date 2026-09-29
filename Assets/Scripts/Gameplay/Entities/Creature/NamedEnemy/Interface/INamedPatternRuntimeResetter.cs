/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Interface
 * Primary responsibility: Defines the shared contract implemented by the INamedPatternRuntimeResetter role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

//dead할때 추가정리할게있으면 이함수 구현
public interface INamedPatternRuntimeResetter
{
    void ForceResetNamedPatternRuntime();
}


