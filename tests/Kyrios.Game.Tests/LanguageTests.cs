using Kyrios.Game;
using Xunit;

// O idioma é global (L.Current): os testes deste projeto rodam um de cada vez pra um não trocar o idioma do outro.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Kyrios.Game.Tests;

/// <summary>O jogo inteiro precisa existir em inglês: nenhum texto de catálogo pode ficar sem tradução.</summary>
public class LanguageTests
{
    private static IEnumerable<UnlockRequirement> WithParts(UnlockRequirement requirement) =>
        requirement is AllOfRequirement all ? [requirement, .. all.Parts.SelectMany(WithParts)] : [requirement];

    /// <summary>Todo texto de catálogo que aparece em alguma tela, no idioma atual.</summary>
    internal static List<string> CatalogTexts()
    {
        var progress = new SaveData();
        var texts = new List<string>();
        foreach (Achievement a in Achievements.All)
        {
            texts.Add(a.Name);
            texts.Add(a.Description);
            texts.AddRange(WithParts(a.Condition).SelectMany(r => new[] { r.Description, r.ProgressText(progress) }));
        }

        foreach (CarSkin s in CarSkins.All)
        {
            texts.Add(s.Name);
            texts.AddRange(WithParts(s.Requirement).SelectMany(r => new[] { r.Description, r.ProgressText(progress) }));
        }

        foreach (TrackTheme t in TrackThemes.All)
        {
            texts.AddRange([t.Name, t.Tagline, t.SecretName, t.SecretDescription]);
            texts.AddRange(WithParts(t.Requirement).SelectMany(r => new[] { r.Description, r.ProgressText(progress) }));
        }

        foreach (SkinCategory category in Enum.GetValues<SkinCategory>())
        {
            texts.AddRange([SkinCategories.Singular(category), SkinCategories.Plural(category), SkinCategories.ShortPlural(category)]);
        }

        return [.. texts.Where(text => !string.IsNullOrEmpty(text))];
    }

    [Fact]
    public void English_EveryCatalogText_IsTranslated()
    {
        try
        {
            L.Current = Language.English;
            L.Missing.Clear();
            List<string> texts = CatalogTexts();
            Assert.True(L.Missing.Count == 0, "Sem traducao:\n" + string.Join("\n", L.Missing.Order()));

            // Frases montadas com números e nomes: nenhuma palavra típica do português pode sobrar.
            string[] portugueseWords = ["VENCA", "JOGUE", "PARTIDA", "CORRIDA", "PISTA", "FACA", "COM", "SEM", "NUMA", "DESBLOQUEIE", "CONQUISTA", "VOLTA", "RELOGIO", "MORTAL", "SEGREDO", "ACHE", "PARA", "DE", "DA", "NA", "UMA", "UM", "SEU", "AINDA", "MELHOR", "TODAS", "DIFERENTES", "SEGUIDAS"];
            string[] offenders = [.. texts.Where(text => text.Split(' ', ',', '.', '(', ')').Intersect(portugueseWords).Any()).Distinct()];
            Assert.True(offenders.Length == 0, "Portugues no modo ingles:\n" + string.Join("\n", offenders));
        }
        finally
        {
            L.Current = Language.Portuguese;
        }
    }

    [Fact]
    public void Portuguese_IsTheDefault_AndTheChoiceIsSaved()
    {
        Assert.Equal("pt", new SaveData().Language);
        Assert.Equal("pt", SaveData.FromJson("""{"GamesPlayed":3}""").Language);
        Assert.Equal("pt", SaveData.FromJson("""{"Language":"klingon"}""").Language);
        Assert.Equal("en", SaveData.FromJson(new SaveData { Language = "en" }.ToJson()).Language);
    }

    [Fact]
    public void Ordinals_FollowTheLanguage()
    {
        try
        {
            Assert.Equal("1º", L.Ordinal(1));
            L.Current = Language.English;
            Assert.Equal(["1ST", "2ND", "3RD", "4TH", "10TH", "11TH", "12TH", "13TH", "21ST"], new[] { 1, 2, 3, 4, 10, 11, 12, 13, 21 }.Select(L.Ordinal));
        }
        finally
        {
            L.Current = Language.Portuguese;
        }
    }
}
