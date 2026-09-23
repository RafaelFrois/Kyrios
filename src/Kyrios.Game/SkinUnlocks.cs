namespace Kyrios.Game;

/// <summary>Estado de desbloqueio das skins — separado tanto dos modos de jogo quanto do desenho.</summary>
public static class SkinUnlocks
{
    public static bool IsUnlocked(CarSkin skin, SaveData progress) =>
        skin.Requirement is AlwaysUnlockedRequirement || progress.UnlockedSkinIds.Contains(skin.Id);

    /// <summary>Quantas skins do catálogo precisam ser conquistadas (todas menos as liberadas desde o início).</summary>
    public static int EarnableCount => CarSkins.All.Count(skin => skin.Requirement is not AlwaysUnlockedRequirement);

    /// <summary>Quantas dessas já foram conquistadas.</summary>
    public static int EarnedCount(SaveData progress) =>
        CarSkins.All.Count(skin => skin.Requirement is not AlwaysUnlockedRequirement && progress.UnlockedSkinIds.Contains(skin.Id));

    /// <summary>
    /// Libera (anotando em <paramref name="progress"/>) toda skin ainda bloqueada cujo requisito já foi
    /// cumprido, e devolve quais foram — na ordem do catálogo. Repete até estabilizar, pra que requisitos que
    /// dependam de outra skin ("libere X antes") também fechem na mesma checagem. Quem junta isso com as
    /// conquistas (que também podem depender de skins, e vice-versa) é <see cref="Progression.CheckUnlocks"/>.
    /// </summary>
    public static List<CarSkin> UnlockNewlyEarned(SaveData progress, IReadOnlyList<CarSkin> catalog = null)
    {
        catalog ??= CarSkins.All;
        var earned = new List<CarSkin>();

        bool changed;
        do
        {
            changed = false;
            foreach (CarSkin skin in catalog)
            {
                if (!IsUnlocked(skin, progress) && skin.Requirement.IsMet(progress))
                {
                    progress.UnlockedSkinIds.Add(skin.Id);
                    earned.Add(skin);
                    changed = true;
                }
            }
        }
        while (changed);

        return earned;
    }
}
