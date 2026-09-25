namespace Kyrios.Game;

/// <summary>Atalhos pro estado de desbloqueio das skins (a regra em si é a de <see cref="Unlockables"/>).</summary>
public static class SkinUnlocks
{
    public static bool IsUnlocked(CarSkin skin, SaveData progress) => Unlockables.IsUnlocked(skin, progress.UnlockedSkinIds);
}
