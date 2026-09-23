namespace Kyrios.Game;

/// <summary>Formatação de tempos de corrida — compartilhada pelo HUD, pelos recordes e pelos textos dos
/// requisitos de skins, pra todo lugar mostrar o tempo do mesmo jeito.</summary>
public static class TimeFormat
{
    /// <summary>Tempo completo, com milissegundos: "01:24.370".</summary>
    public static string Precise(float seconds)
    {
        var span = TimeSpan.FromSeconds(MathF.Max(0f, seconds));
        return $"{(int)span.TotalMinutes:00}:{span.Seconds:00}.{span.Milliseconds:000}";
    }

    /// <summary>Tempo curto pra textos de meta: "01:20", ou "00:09.5" quando a meta tem fração.</summary>
    public static string Short(float seconds)
    {
        var span = TimeSpan.FromSeconds(MathF.Max(0f, seconds));
        string whole = $"{(int)span.TotalMinutes:00}:{span.Seconds:00}";
        int tenths = span.Milliseconds / 100;
        return tenths == 0 ? whole : $"{whole}.{tenths}";
    }
}
