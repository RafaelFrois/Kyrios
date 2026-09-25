using Kyrios.Core;
using Kyrios.Game;
using Microsoft.Xna.Framework;
using Xunit;

namespace Kyrios.Game.Tests;

/// <summary>
/// Versão mobile: layout dos controles em aparelhos de várias proporções (celulares 16:9 a 21:9, tablets, dobrável,
/// com e sem notch), leitura dos toques (vários dedos, toque rápido, segurar, arrastar), joystick, inclinação,
/// aceleração automática, passo fixo em qualquer taxa de quadros e o save versão 2.
/// </summary>
public class MobileTests
{
    /// <summary>Aparelhos de referência (deitados): tamanho em pixels, densidade e área segura.</summary>
    public static readonly TheoryData<string, int, int, float, int, int> Devices = new()
    {
        { "16:9 1080p", 1920, 1080, 2.75f, 0, 0 },
        { "18:9", 2160, 1080, 2.75f, 0, 0 },
        { "19.5:9 notch à direita", 2340, 1080, 2.75f, 0, 110 },
        { "20:9 furo à esquerda", 2400, 1080, 2.625f, 96, 0 },
        { "21:9", 2520, 1080, 2.625f, 80, 0 },
        { "720p pequeno", 1280, 720, 2f, 0, 0 },
        { "celular antigo 480p", 854, 480, 1.5f, 0, 0 },
        { "tablet 16:10", 2560, 1600, 2f, 0, 0 },
        { "tablet 4:3", 2048, 1536, 2f, 0, 0 },
        { "dobrável aberto", 2208, 1840, 2.625f, 0, 0 },
    };

    private static Rectangle Safe(int width, int height, int left, int right) => new(left, 0, width - left - right, height);

    private static IEnumerable<(ControlScheme, ControlSize)> AllSchemesAndSizes() =>
        from scheme in Enum.GetValues<ControlScheme>()
        from size in Enum.GetValues<ControlSize>()
        select (scheme, size);

    [Theory]
    [MemberData(nameof(Devices))]
    public void Controls_StayInsideTheSafeArea_WithoutOverlapping_AndBigEnoughForAThumb(string device, int width, int height, float density, int left, int right)
    {
        Rectangle safe = Safe(width, height, left, right);
        foreach ((ControlScheme scheme, ControlSize size) in AllSchemesAndSizes())
        {
            TouchLayout layout = TouchLayout.Build(scheme, size, safe, density);
            List<TouchButton> buttons = [.. layout.Buttons];
            if (layout.HasJoystick)
            {
                buttons.Add(layout.JoystickRest);
            }

            foreach (TouchButton button in buttons)
            {
                Assert.True(button.Center.X - button.Radius >= safe.Left && button.Center.X + button.Radius <= safe.Right, $"{device} {scheme} {size}: botão fora da área segura na horizontal");
                Assert.True(button.Center.Y - button.Radius >= safe.Top && button.Center.Y + button.Radius <= safe.Bottom, $"{device} {scheme} {size}: botão fora da área segura na vertical");

                // Alvo de toque: pelo menos 48 dp de diâmetro no desenho (a área de toque ainda é 1,5x maior).
                Assert.True(button.Radius * 2f / density >= 48f, $"{device} {scheme} {size}: botão com {button.Radius * 2f / density:0} dp");
            }

            for (int i = 0; i < buttons.Count; i++)
            {
                for (int j = i + 1; j < buttons.Count; j++)
                {
                    float distance = Vector2.Distance(buttons[i].Center, buttons[j].Center);
                    Assert.True(distance >= buttons[i].Radius + buttons[j].Radius, $"{device} {scheme} {size}: botões sobrepostos");
                }
            }

            // Os controles ficam na metade de baixo (a de cima é do HUD e do botão de pausa).
            Assert.All(buttons, button => Assert.True(button.Center.Y - button.Radius > safe.Top + (safe.Height * 0.35f), $"{device} {scheme} {size}: botão alto demais"));
        }
    }

    [Theory]
    [MemberData(nameof(Devices))]
    public void Controls_HaveTheSamePhysicalSize_OnEveryDevice(string device, int width, int height, float density, int left, int right)
    {
        TouchLayout layout = TouchLayout.Build(ControlScheme.Buttons, ControlSize.Medium, Safe(width, height, left, right), density);
        float diameterDp = layout.Gas.Radius * 2f / density;
        Assert.InRange(diameterDp, 64f, 80.01f);
        Assert.True(device.Length > 0);
    }

    private static TouchLayout PhoneLayout(ControlScheme scheme = ControlScheme.Buttons) =>
        TouchLayout.Build(scheme, ControlSize.Medium, new Rectangle(96, 0, 2304, 1080), 2.625f);

    private static TouchPoint Finger(int id, Vector2 position, bool justPressed = false) => new(id, position, justPressed);

    [Fact]
    public void SeveralFingers_WorkAtTheSameTime()
    {
        TouchLayout layout = PhoneLayout();
        var controls = new TouchControls();
        controls.Update([Finger(1, layout.Left.Center), Finger(2, layout.Gas.Center), Finger(3, layout.Boost.Center)], layout);

        CarInput input = controls.ToCarInput(ControlScheme.Buttons, 0f, null, autoAccelerate: false);
        Assert.Equal(-1f, input.Steering);
        Assert.Equal(1f, input.Throttle);
        Assert.True(input.Boost);
    }

    [Fact]
    public void QuickTap_PressesForThatFrameOnly_AndHolding_KeepsItPressed()
    {
        TouchLayout layout = PhoneLayout();
        var controls = new TouchControls();

        controls.Update([Finger(1, layout.Boost.Center, justPressed: true)], layout);
        Assert.True(controls.BoostPressed);
        controls.Update([], layout);
        Assert.False(controls.BoostPressed);

        for (int frame = 0; frame < 300; frame++)
        {
            controls.Update([Finger(2, layout.Gas.Center, justPressed: frame == 0)], layout);
            Assert.True(controls.GasPressed);
        }
    }

    [Fact]
    public void SlidingTheThumb_SwitchesBetweenNeighbouringButtons()
    {
        TouchLayout layout = PhoneLayout();
        var controls = new TouchControls();
        Vector2 from = layout.Left.Center;
        Vector2 to = layout.Right.Center;
        bool sawLeft = false;
        for (int i = 0; i <= 20; i++)
        {
            controls.Update([Finger(1, Vector2.Lerp(from, to, i / 20f))], layout);
            sawLeft |= controls.LeftPressed;
            Assert.False(controls.LeftPressed && controls.RightPressed);
        }

        Assert.True(sawLeft);
        Assert.True(controls.RightPressed);

        controls.Update([Finger(1, layout.Gas.Center)], layout);
        controls.Update([Finger(1, layout.Brake.Center)], layout);
        Assert.True(controls.BrakePressed);
        Assert.False(controls.GasPressed);
        Assert.Equal(-1f, controls.ToCarInput(ControlScheme.Buttons, 0f, null, false).Throttle);
    }

    [Fact]
    public void TouchesAwayFromTheControls_DoNothing()
    {
        TouchLayout layout = PhoneLayout();
        var controls = new TouchControls();
        controls.Update([Finger(1, new Vector2(1200, 300)), Finger(2, new Vector2(1200, 900)), Finger(3, new Vector2(200, 100))], layout);
        Assert.False(controls.AnyActive);
        CarInput input = controls.ToCarInput(ControlScheme.Buttons, 0f, null, false);
        Assert.Equal(0f, input.Throttle);
        Assert.Equal(0f, input.Steering);
    }

    [Fact]
    public void ThumbSlightlyOffTheButton_StillCounts()
    {
        TouchLayout layout = PhoneLayout();
        var controls = new TouchControls();
        controls.Update([Finger(1, layout.Gas.Center + new Vector2(0f, -layout.Gas.Radius * 1.3f))], layout);
        Assert.True(controls.GasPressed || controls.BoostPressed);
        controls.Update([Finger(1, layout.Left.Center + new Vector2(-layout.Left.Radius * 1.2f, 0f))], layout);
        Assert.True(controls.LeftPressed);
    }

    [Fact]
    public void Joystick_TurnsTheCarTowardsWhereTheThumbPoints()
    {
        TouchLayout layout = PhoneLayout(ControlScheme.Joystick);
        var controls = new TouchControls();
        Vector2 start = layout.JoystickRest.Center + new Vector2(40f, -20f);

        // Encostou: a base nasce ali, ainda sem direção.
        controls.Update([Finger(7, start, justPressed: true)], layout);
        Assert.True(controls.StickActive);
        Assert.Equal(0f, controls.ToCarInput(ControlScheme.Joystick, 0f, null, false).Throttle);

        // Empurrou pra cima com o carro virado pra direita: vira pra esquerda e acelera.
        controls.Update([Finger(7, start + new Vector2(0f, -layout.JoystickRest.Radius * 2f))], layout);
        CarInput up = controls.ToCarInput(ControlScheme.Joystick, 0f, null, false);
        Assert.Equal(-1f, up.Steering);
        Assert.True(up.Throttle > 0.9f);
        Assert.True(Vector2.Distance(controls.StickKnob, controls.StickCenter) <= layout.JoystickRest.Radius + 0.01f);

        // Já apontando pra onde o dedo aponta: segue reto.
        CarInput aligned = controls.ToCarInput(ControlScheme.Joystick, -MathF.PI / 2f, null, false);
        Assert.Equal(0.0, aligned.Steering, 3);

        // O dedo do joystick continua sendo dele mesmo saindo da zona; outro dedo na zona não rouba.
        controls.Update([Finger(7, new Vector2(1500, 200)), Finger(8, layout.JoystickRest.Center, justPressed: true)], layout);
        Assert.True(controls.StickActive);
        Assert.True(controls.StickKnob.X > controls.StickCenter.X);

        controls.Update([], layout);
        Assert.False(controls.StickActive);
        Assert.Equal(0f, controls.ToCarInput(ControlScheme.Joystick, 0f, null, false).Throttle);
    }

    [Fact]
    public void Tilt_SteersLikeAWheel_WithBrakeOnTheLeftAndGasOnTheRight()
    {
        TouchLayout layout = PhoneLayout(ControlScheme.Tilt);
        var controls = new TouchControls();
        controls.Update([Finger(1, layout.Gas.Center)], layout);
        CarInput input = controls.ToCarInput(ControlScheme.Tilt, 0f, tilt: 0.6f, autoAccelerate: false);
        Assert.Equal(0.6, input.Steering, 3);
        Assert.Equal(1f, input.Throttle);

        controls.Update([Finger(2, layout.Brake.Center)], layout);
        Assert.True(layout.Brake.Center.X < layout.Safe.Center.X);
        Assert.Equal(-1f, controls.ToCarInput(ControlScheme.Tilt, 0f, -0.3f, false).Throttle);

        // Sem leitura do sensor: não vira sozinho.
        controls.Update([], layout);
        Assert.Equal(0f, controls.ToCarInput(ControlScheme.Tilt, 0f, null, false).Steering);
    }

    [Fact]
    public void AutoAccelerate_Drives_UntilTheBrakeIsPressed()
    {
        TouchLayout layout = PhoneLayout();
        var controls = new TouchControls();
        controls.Update([], layout);
        Assert.Equal(1f, controls.ToCarInput(ControlScheme.Buttons, 0f, null, autoAccelerate: true).Throttle);
        controls.Update([Finger(1, layout.Brake.Center)], layout);
        Assert.Equal(-1f, controls.ToCarInput(ControlScheme.Buttons, 0f, null, autoAccelerate: true).Throttle);
    }

    [Fact]
    public void TouchAndKeyboard_AddUp_WithinLimits()
    {
        CarInput combined = TouchControls.Combine(new CarInput(1f, -1f, brake: true), new CarInput(1f, -1f, boost: true));
        Assert.Equal(1f, combined.Throttle);
        Assert.Equal(-1f, combined.Steering);
        Assert.True(combined.Brake);
        Assert.True(combined.Boost);
    }

    // ---------- Passo fixo: a mesma corrida a 30, 60, 90, 120 e 144 fps ----------

    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(90)]
    [InlineData(120)]
    [InlineData(144)]
    public void Race_IsTheSame_AtAnyFrameRate(int framesPerSecond)
    {
        const int steps = 60 * 25;
        (Vector2D position, float score, float elapsed) reference = RunRace(60, steps);
        (Vector2D position, float score, float elapsed) result = RunRace(framesPerSecond, steps);
        Assert.Equal(reference.position.X, result.position.X);
        Assert.Equal(reference.position.Y, result.position.Y);
        Assert.Equal(reference.score, result.score);
        Assert.Equal(reference.elapsed, result.elapsed);
    }

    /// <summary>Roda a corrida quadro a quadro na taxa pedida até completar o número de passos. O comando depende só do
    /// passo (como um jogador que faz o mesmo em cada instante da corrida).</summary>
    private static (Vector2D Position, float Score, float Elapsed) RunRace(int framesPerSecond, int totalSteps)
    {
        RaceSimulation race = RaceFactory.CreateDefaultRace(RaceMode.TimeAttack, aiOpponents: 4);
        var clock = new FixedStepClock(1.0 / 60.0, 6);
        int done = 0;
        while (done < totalSteps)
        {
            int steps = clock.Advance(1.0 / framesPerSecond);
            for (int i = 0; i < steps && done < totalSteps; i++, done++)
            {
                float steer = MathF.Sin(done * 0.02f);
                race.Update(1f / 60f, new CarInput(1f, steer, boost: done % 300 < 40));
            }
        }

        RaceEntrant player = race.Entrants.First(e => e.Kind == DriverKind.Human);
        return (player.Car.Position, player.Score, race.ElapsedTime);
    }

    [Theory]
    [InlineData(30, 2)]
    [InlineData(60, 1)]
    [InlineData(120, 0)]
    public void FixedStepClock_GivesTheRightNumberOfSteps(int framesPerSecond, int expectedFirstFrameSteps)
    {
        var clock = new FixedStepClock(1.0 / 60.0, 6);
        Assert.Equal(expectedFirstFrameSteps, clock.Advance((1.0 / framesPerSecond) + 1e-9));

        clock.Reset();
        int total = 0;
        for (int frame = 0; frame < framesPerSecond * 10; frame++)
        {
            total += clock.Advance(1.0 / framesPerSecond);
        }

        Assert.InRange(total, 599, 600);
    }

    [Fact]
    public void FixedStepClock_NeverCatchesUpMoreThanTheLimit_AfterAHitch()
    {
        var clock = new FixedStepClock(1.0 / 60.0, 6);
        Assert.Equal(6, clock.Advance(5.0));
        Assert.Equal(0, clock.Advance(double.NaN));
        Assert.Equal(0, clock.Advance(-1.0));
    }

    // ---------- Save versão 2 ----------

    [Fact]
    public void VersionOneSave_OpensWithAllProgress_AndTheMobileDefaults()
    {
        SaveData save = SaveData.FromJson("""{"SaveVersion":1,"EliminationWins":7,"UnlockedSkinIds":["pato"],"BestScoreTimeAttack":1234.5,"Language":"en"}""");
        Assert.Equal(2, save.SaveVersion);
        Assert.Equal(7, save.EliminationWins);
        Assert.Contains("pato", save.UnlockedSkinIds);
        Assert.Equal(1234.5f, save.BestScoreTimeAttack);
        Assert.Equal("en", save.Language);
        Assert.Equal("buttons", save.ControlScheme);
        Assert.Equal("medium", save.ControlSize);
        Assert.Equal("auto", save.GraphicsQuality);
        Assert.True(save.Vibration);
        Assert.False(save.AutoAccelerate);
    }

    [Fact]
    public void MobileSettings_SurviveASaveAndReload_AndUnknownValuesFallBackToDefaults()
    {
        var save = new SaveData { ControlScheme = "tilt", ControlSize = "large", GraphicsQuality = "low", Vibration = false, AutoAccelerate = true };
        SaveData reloaded = SaveData.FromJson(save.ToJson());
        Assert.Equal(ControlScheme.Tilt, MobileSettings.Scheme(reloaded.ControlScheme));
        Assert.Equal(ControlSize.Large, MobileSettings.Size(reloaded.ControlSize));
        Assert.Equal(GraphicsQuality.Low, MobileSettings.Quality(reloaded.GraphicsQuality));
        Assert.False(reloaded.Vibration);
        Assert.True(reloaded.AutoAccelerate);

        SaveData edited = SaveData.FromJson("""{"ControlScheme":"banana","ControlSize":null,"GraphicsQuality":"ultra"}""");
        Assert.Equal("buttons", edited.ControlScheme);
        Assert.Equal("medium", edited.ControlSize);
        Assert.Equal("auto", edited.GraphicsQuality);
    }

    [Fact]
    public void SaveFromANewerVersion_StillOpens_WithoutPretendingToBeOld()
    {
        SaveData future = SaveData.FromJson("""{"SaveVersion":9,"EliminationWins":3,"CampoQueAindaNaoExiste":42}""");
        Assert.NotNull(future);
        Assert.Equal(9, future.SaveVersion);
        Assert.Equal(3, future.EliminationWins);
    }

    /// <summary>O ciclo do celular: desbloqueia skin, pista, conquista e recorde → grava → o app é fechado (inclusive
    /// à força, no meio de uma gravação) → abre de novo → tudo continua lá. Atualizar o app (versão nova lendo o
    /// arquivo) é o mesmo caminho.</summary>
    [Fact]
    public void Progress_SurvivesClosing_ForceClosing_AndUpdating()
    {
        string dir = Path.Combine(Path.GetTempPath(), "megrace-mobile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string path = Path.Combine(dir, "records.json");
            var save = new SaveData();
            save.UnlockedSkinIds.Add("galinha");
            save.UnlockedTrackIds.Add("praia");
            save.UnlockedAchievementIds.Add("primeira-vitoria");
            save.BestScoreTimeAttack = 1500f;
            save.ControlScheme = "joystick";
            save.SaveTo(path);

            // Fechou e abriu.
            SaveData reopened = SaveData.LoadFrom(path);
            Assert.Contains("galinha", reopened.UnlockedSkinIds);
            Assert.Contains("praia", reopened.UnlockedTrackIds);
            Assert.Contains("primeira-vitoria", reopened.UnlockedAchievementIds);
            Assert.Equal(1500f, reopened.BestScoreTimeAttack);
            Assert.Equal("joystick", reopened.ControlScheme);

            // Mais progresso, e o app morre no meio da gravação seguinte: sobra um temporário pela metade.
            reopened.BestScoreTimeAttack = 1800f;
            reopened.SaveTo(path);
            File.WriteAllText(path + ".tmp", "{\"BestScoreTimeAt");
            SaveData afterCrash = SaveData.LoadFrom(path);
            Assert.Equal(1800f, afterCrash.BestScoreTimeAttack);

            // Pior caso: o arquivo principal corrompido — o backup da gravação anterior salva o progresso.
            File.WriteAllText(path, "{ corrompido");
            SaveData recovered = SaveData.LoadFrom(path);
            Assert.Contains("galinha", recovered.UnlockedSkinIds);
            Assert.Equal(1500f, recovered.BestScoreTimeAttack);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
