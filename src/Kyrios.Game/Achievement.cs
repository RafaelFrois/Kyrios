using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>Dificuldade de uma conquista ou de uma skin — só informativa (cor + rótulo na tela).</summary>
public enum Difficulty
{
    Easy,
    Medium,
    Hard,
}

public enum AchievementCategory
{
    Races,
    DeathRace,
    TimeAttack,
    Records,
    Skins,
    Playing,
    Absurd,
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
    AchievementIcon LockedIcon = null);

/// <summary>Nomes e cores de categorias e dificuldades (compartilhados pela página de conquistas, pelo
/// seletor de skins e pelas notificações).</summary>
public static class ProgressionStyle
{
    public static readonly AchievementCategory[] CategoryOrder =
    [
        AchievementCategory.Races,
        AchievementCategory.DeathRace,
        AchievementCategory.TimeAttack,
        AchievementCategory.Records,
        AchievementCategory.Skins,
        AchievementCategory.Playing,
        AchievementCategory.Absurd,
    ];

    public static string CategoryName(AchievementCategory category) => category switch
    {
        AchievementCategory.Races => "CORRIDAS",
        AchievementCategory.DeathRace => "MORTAL",
        AchievementCategory.TimeAttack => "RELOGIO",
        AchievementCategory.Records => "RECORDES",
        AchievementCategory.Skins => "SKINS",
        AchievementCategory.Playing => "JOGANDO",
        _ => "ABSURDAS",
    };

    public static Color CategoryColor(AchievementCategory category) => category switch
    {
        AchievementCategory.Races => new Color(255, 200, 40),
        AchievementCategory.DeathRace => new Color(235, 85, 75),
        AchievementCategory.TimeAttack => new Color(80, 200, 235),
        AchievementCategory.Records => new Color(120, 230, 130),
        AchievementCategory.Skins => new Color(215, 125, 235),
        AchievementCategory.Playing => new Color(245, 150, 60),
        _ => new Color(255, 120, 175),
    };

    public static string DifficultyName(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => "FACIL",
        Difficulty.Medium => "MEDIA",
        _ => "DIFICIL",
    };

    public static Color DifficultyColor(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => new Color(95, 215, 105),
        Difficulty.Medium => new Color(245, 200, 55),
        _ => new Color(235, 75, 65),
    };
}
