/*
 * Role hierarchy: Gameplay > Progression > Difficulty
 * Primary responsibility: Implements the focused GameDifficulty responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

public enum GameDifficulty
{
    Normal,
    Hard,
    VeryHard
}
