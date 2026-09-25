namespace Kyrios.Game;

public enum Language
{
    Portuguese,
    English,
}

/// <summary>
/// Idioma do jogo (escolhido nas configurações; português é o padrão). Dois jeitos de traduzir:
/// <list type="bullet">
/// <item><see cref="T"/>: os dois textos lado a lado no código — usado nas telas e em frases montadas com números
/// ("VENCA 10 CORRIDAS" / "WIN 10 RACES").</item>
/// <item><see cref="Tr"/>: textos fixos dos catálogos (nomes e descrições de conquistas, skins e pistas), que ficam
/// escritos em português no catálogo e têm a versão em inglês na tabela de <c>Lang.English.cs</c>.</item>
/// </list>
/// Tudo em maiúsculas e sem acento (limite da fonte pixelizada).
/// </summary>
public static partial class L
{
    public static Language Current { get; set; } = Language.Portuguese;

    public static bool English => Current == Language.English;

    /// <summary>Textos de catálogo pedidos em inglês que não estão na tabela (os testes exigem que fique vazio).</summary>
    public static HashSet<string> Missing { get; } = [];

    public static string T(string portuguese, string english) => English ? english : portuguese;

    public static string Tr(string portuguese)
    {
        if (!English || string.IsNullOrEmpty(portuguese))
        {
            return portuguese;
        }

        if (EnglishTable.TryGetValue(portuguese, out string english))
        {
            return english;
        }

        Missing.Add(portuguese);
        return portuguese;
    }

    /// <summary>Sufixo de posição: "º" em português; "ST", "ND", "RD" ou "TH" em inglês.</summary>
    public static string OrdinalSuffix(int number)
    {
        if (!English)
        {
            return "º";
        }

        return (number % 100) is >= 11 and <= 13 ? "TH" : (number % 10) switch
        {
            1 => "ST",
            2 => "ND",
            3 => "RD",
            _ => "TH",
        };
    }

    public static string Ordinal(int number) => $"{number}{OrdinalSuffix(number)}";

    public static string Code(Language language) => language == Language.English ? "en" : "pt";

    public static Language FromCode(string code) => code == "en" ? Language.English : Language.Portuguese;
}
