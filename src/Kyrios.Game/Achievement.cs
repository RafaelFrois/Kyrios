using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>Dificuldade de uma conquista, skin ou pista — organiza os catálogos e o equilíbrio da progressão
/// (não aparece na tela).</summary>
public enum Difficulty
{
    Easy,
    Medium,
    Hard,

    /// <summary>Rara / especial: condições incomuns, combinações ou feitos específicos.</summary>
    Rare,
}

public enum AchievementCategory
{
    DeathRace,
    TimeAttack,
    Tracks,
    Skins,
    General,

    /// <summary>Conquistas idiotas/absurdas — existem só pra fazer rir.</summary>
    Silly,
}

/// <summary>
/// Ícone de uma conquista: ou um desenho pixel-art (<see cref="Pixels"/>, uma linha de texto por fileira,
/// cada caractere uma cor da <see cref="AchievementIcons.Palette"/>, '.' = transparente), ou a própria skin
/// de um carro (<see cref="SkinId"/>) — mais um <see cref="Badge"/> opcional ("10", "1K"...) no canto.
/// Bloqueada, a conquista usa o mesmo ícone em tons de cinza, a menos que tenha um ícone próprio pra isso.
/// </summary>
public sealed record AchievementIcon(string[] Pixels = null, string SkinId = null, string Badge = null);

/// <summary>
/// Uma conquista. Tudo aqui é dado — a lógica de desbloquear é a mesma pra todas
/// (<see cref="Progression.CheckUnlocks"/>): quando <see cref="Condition"/> passa a ser cumprida pelo
/// progresso salvo, o id entra em <see cref="SaveData.UnlockedAchievementIds"/> e o jogador é avisado.
/// </summary>
public sealed record Achievement(
    string Id,
    string Name,
    string Description,
    AchievementCategory Category,
    Difficulty Difficulty,
    UnlockRequirement Condition,
    AchievementIcon Icon,
    bool IsSecret = false,
    AchievementIcon LockedIcon = null) : IUnlockable
{
    UnlockRequirement IUnlockable.Requirement => Condition;
}

/// <summary>Nomes e cores das categorias de conquista (página de conquistas e notificações).</summary>
public static class ProgressionStyle
{
    public static readonly AchievementCategory[] CategoryOrder =
    [
        AchievementCategory.DeathRace,
        AchievementCategory.TimeAttack,
        AchievementCategory.Tracks,
        AchievementCategory.Skins,
        AchievementCategory.General,
        AchievementCategory.Silly,
    ];

    public static string CategoryName(AchievementCategory category) => category switch
    {
        AchievementCategory.DeathRace => "MORTAL",
        AchievementCategory.TimeAttack => "RELOGIO",
        AchievementCategory.Tracks => "PISTAS",
        AchievementCategory.Skins => "SKINS",
        AchievementCategory.General => "GERAL",
        _ => "IDIOTAS",
    };

    public static Color CategoryColor(AchievementCategory category) => category switch
    {
        AchievementCategory.DeathRace => new Color(235, 85, 75),
        AchievementCategory.TimeAttack => new Color(80, 200, 235),
        AchievementCategory.Tracks => new Color(120, 230, 130),
        AchievementCategory.Skins => new Color(215, 125, 235),
        AchievementCategory.General => new Color(255, 200, 40),
        _ => new Color(255, 140, 70),
    };
}
