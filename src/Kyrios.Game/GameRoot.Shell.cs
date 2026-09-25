using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Kyrios.Game;

/// <summary>O "entorno" das telas: abertura com a logo do mascote, transição entre telas, proteção contra cliques
/// que atravessam de uma tela pra outra e as interações escondidas do menu principal.</summary>
public sealed partial class GameRoot
{
    private const float ClickGuardSeconds = 0.2f;
    private const float FadeSeconds = 0.22f;
    private const float SplashSeconds = 2.6f;
    private const float MenuIdleDiscoverySeconds = 120f;
    private const int LogoClicksForDiscovery = 10;

    private static readonly Keys[] SecretCode = [Keys.Up, Keys.Up, Keys.Down, Keys.Down, Keys.Left, Keys.Right, Keys.Left, Keys.Right, Keys.B, Keys.A];

    private Texture2D _logo;
    private Point _logoSize;
    private Matrix _screenTransform;
    private State _lastState;
    private float _stateTime;
    private bool _fadeActive;
    private float _menuIdleTime;
    private int _secretCodeProgress;
    private int _logoClicks;
    private float _logoClickTimer;
    private float _logoBounce;

    /// <summary>Clique do mouse que vale pra tela atual — ignora os primeiros instantes depois de trocar de tela,
    /// pra um clique duplo em "JOGAR" não escolher também o modo que estava embaixo do cursor.</summary>
    private bool MouseClicked => _input.WasMouseLeftJustPressed && _stateTime > ClickGuardSeconds;

    /// <summary>Largura (em pixels lógicos, com escala 1) em que a logo é desenhada — os tamanhos de tela
    /// usam múltiplos disso, independente da resolução da imagem embutida.</summary>
    private const float LogoBaseWidth = 96f;

    /// <summary>A logo do mascote (embutida no executável em alta resolução), já com o alfa pré-multiplicado
    /// como o SpriteBatch espera e com mipmaps feitos à mão, pra ficar nítida e sem serrilhado tanto
    /// pequena na janela quanto grande em tela cheia. A imagem vai pro canto de uma textura com lados em
    /// potência de dois (o resto transparente): é o que o perfil gráfico mais compatível (WebGL 1 nos navegadores)
    /// exige pra ter mipmaps.</summary>
    private Texture2D LoadLogo()
    {
        using Stream stream = typeof(GameRoot).Assembly.GetManifestResourceStream("MegRace.Logo.png");
        if (stream is null)
        {
            return null;
        }

        using Texture2D source = Texture2D.FromStream(GraphicsDevice, stream);
        _logoSize = new Point(source.Width, source.Height);
        var pixels = new Color[source.Width * source.Height];
        source.GetData(pixels);

        int width = NextPowerOfTwo(source.Width);
        int height = NextPowerOfTwo(source.Height);
        var level = new Color[width * height];
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                Color c = pixels[(y * source.Width) + x];
                level[(y * width) + x] = Color.FromNonPremultiplied(c.R, c.G, c.B, c.A);
            }
        }

        var texture = new Texture2D(GraphicsDevice, width, height, true, SurfaceFormat.Color);
        for (int mip = 0; mip < texture.LevelCount; mip++)
        {
            texture.SetData(mip, null, level, 0, level.Length);
            (level, width, height) = HalveImage(level, width, height);
        }

        return texture;
    }

    private static int NextPowerOfTwo(int value)
    {
        int result = 1;
        while (result < value)
        {
            result <<= 1;
        }

        return result;
    }

    /// <summary>Metade da resolução pela média de cada bloco 2x2 (as bordas ímpares repetem o último pixel).</summary>
    private static (Color[] Pixels, int Width, int Height) HalveImage(Color[] pixels, int width, int height)
    {
        int halfWidth = Math.Max(1, width / 2);
        int halfHeight = Math.Max(1, height / 2);
        var result = new Color[halfWidth * halfHeight];
        for (int y = 0; y < halfHeight; y++)
        {
            for (int x = 0; x < halfWidth; x++)
            {
                int r = 0, g = 0, b = 0, a = 0;
                for (int dy = 0; dy < 2; dy++)
                {
                    for (int dx = 0; dx < 2; dx++)
                    {
                        Color c = pixels[(Math.Min((y * 2) + dy, height - 1) * width) + Math.Min((x * 2) + dx, width - 1)];
                        r += c.R;
                        g += c.G;
                        b += c.B;
                        a += c.A;
                    }
                }

                result[(y * halfWidth) + x] = new Color(r / 4, g / 4, b / 4, a / 4);
            }
        }

        return (result, halfWidth, halfHeight);
    }

    /// <summary>Chamar no fim de cada Update: percebe a troca de tela, zera o relógio da tela nova e decide se ela
    /// entra com o esmaecer (pop-ups como pausa e configurações abrem na hora).</summary>
    private void TrackStateChanges()
    {
        if (_state == _lastState)
        {
            return;
        }

        bool popup = _state is State.Paused or State.Settings || _lastState is State.Paused or State.Settings;
        _fadeActive = !popup;
        _stateTime = 0f;
        _lastState = _state;
    }

    private void DrawScreenFade()
    {
        if (_fadeActive && _stateTime < FadeSeconds)
        {
            DimScreen(Color.Black * (0.6f * (1f - (_stateTime / FadeSeconds))));
        }
    }

    /// <summary>Desenha a logo com filtro suave (o resto da interface é pixel art com filtro "ponto"): troca o
    /// modo do SpriteBatch só pra ela e volta ao normal em seguida.</summary>
    private void DrawLogo(Vector2 center, float scale, float squash = 0f, Color? tint = null)
    {
        if (_logo is null)
        {
            return;
        }

        var origin = new Vector2(_logoSize.X / 2f, _logoSize.Y / 2f);
        var sourceRect = new Rectangle(0, 0, _logoSize.X, _logoSize.Y);
        float fit = scale * LogoBaseWidth / _logoSize.X;
        var size = new Vector2(fit * (1f + (squash * 0.1f)), fit * (1f - (squash * 0.12f)));
        Color color = tint ?? Color.White;

        _spriteBatch.End();
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: _screenTransform);
        _spriteBatch.Draw(_logo, center + new Vector2(3f, 4f), sourceRect, Color.Black * (0.35f * (color.A / 255f)), 0f, origin, size, SpriteEffects.None, 0f);
        _spriteBatch.Draw(_logo, center, sourceRect, color, 0f, origin, size, SpriteEffects.None, 0f);
        _spriteBatch.End();
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _screenTransform);
    }

    // ---------- Plataforma (desktop / navegador) ----------

    private bool _loadingReported;
    private bool _gameplayReported;

    /// <summary>"Está jogando de fato": na pista, com o carro do jogador ainda na corrida e sem intervalo comercial.
    /// Pausa, configurações, resultado, menus e assistir depois de eliminado não contam. A plataforma só fica
    /// sabendo quando isso muda — nunca recebe dois "começou" ou dois "parou" seguidos.</summary>
    private void ReportGameplayState()
    {
        bool playing = _state == State.Racing && _raceRequest is null && !_player.Eliminated && !_race.IsRaceOver;
        if (playing == _gameplayReported)
        {
            return;
        }

        _gameplayReported = playing;
        if (playing)
        {
            GamePlatform.Current.GameplayStart();
        }
        else
        {
            GamePlatform.Current.GameplayStop();
        }
    }

    /// <summary>Na web a área de desenho acompanha o tamanho real do canvas (vezes a densidade de pixels da tela);
    /// a cena continua na resolução lógica e é escalada com letterbox, como em tela cheia no desktop.</summary>
    private void ApplyPlatformBackBufferSize()
    {
        if (GamePlatform.Current.DesiredBackBufferSize is not { } desired || desired.Width <= 0 || desired.Height <= 0)
        {
            return;
        }

        if (desired.Width != GraphicsDevice.PresentationParameters.BackBufferWidth || desired.Height != GraphicsDevice.PresentationParameters.BackBufferHeight)
        {
            _graphics.PreferredBackBufferWidth = desired.Width;
            _graphics.PreferredBackBufferHeight = desired.Height;
            _graphics.ApplyChanges();
        }
    }

    /// <summary>Celular/tablet em pé: o jogo é deitado (2,25:1), então pede pra girar em vez de mostrar tudo minúsculo.</summary>
    private bool IsPortraitTouchScreen =>
        GamePlatform.Current.IsWeb && _input.UsingTouch
        && GraphicsDevice.PresentationParameters.BackBufferHeight > GraphicsDevice.PresentationParameters.BackBufferWidth;

    private void DrawPortraitOverlay()
    {
        if (!IsPortraitTouchScreen)
        {
            return;
        }

        int width = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int height = GraphicsDevice.PresentationParameters.BackBufferHeight;
        float unit = width / 360f;
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, width, height), MenuBackground);

        // Um celular em pé girando pra deitar.
        var center = new Vector2(width / 2f, height * 0.38f);
        float turn = MathF.Min(1f, (_visualTime % 2.4f) / 1.2f);
        float angle = MathHelper.PiOver2 * (1f - MathF.Pow(1f - turn, 3f));
        DrawFilledRectRotated(center, 60f * unit, 110f * unit, angle, TextColor);
        DrawFilledRectRotated(center, 50f * unit, 92f * unit, angle, MenuBackground);

        float titleSize = 4f * unit;
        string title = L.T("GIRE O APARELHO", "ROTATE YOUR DEVICE");
        titleSize = MathF.Min(titleSize, (width * 0.9f) / MathF.Max(1f, PixelFont.Measure(title, 1f)));
        PixelFont.DrawShadowed(_spriteBatch, _pixel, title, new Vector2((width - PixelFont.Measure(title, titleSize)) / 2f, height * 0.62f), titleSize, AccentColor);
        string line = L.T("O MEGRACE E JOGADO NA HORIZONTAL", "MEGRACE IS PLAYED IN LANDSCAPE");
        float lineSize = MathF.Min(2f * unit, (width * 0.9f) / MathF.Max(1f, PixelFont.Measure(line, 1f)));
        PixelFont.Draw(_spriteBatch, _pixel, line, new Vector2((width - PixelFont.Measure(line, lineSize)) / 2f, (height * 0.62f) + (titleSize * 12f)), lineSize, StatBadgeLabelColor);
        _spriteBatch.End();
    }

    // ---------- Abertura ----------

    private void UpdateSplash()
    {
        bool skip = _stateTime > 0.35f && (_input.Confirm || _input.Back || MouseClicked || _input.JustPressedKeys.Any());
        if (_stateTime >= SplashSeconds || skip)
        {
            _state = State.MainMenu;
        }
    }

    /// <summary>A galinha de kart entra quicando, o título aparece embaixo e tudo esmaece pro menu.</summary>
    private void DrawSplash()
    {
        float t = _stateTime;
        float alpha = Math.Clamp(MathF.Min(t / 0.35f, (SplashSeconds - t) / 0.4f), 0f, 1f);
        DimScreen(MenuBackground);

        float drop = MathF.Max(0f, 1f - (t / 0.45f));
        float land = t is > 0.45f and < 0.75f ? MathF.Sin((t - 0.45f) / 0.3f * MathF.PI) : 0f;
        var center = new Vector2(AreaWidth / 2f, 170f - (drop * drop * 120f));
        DrawLogo(center, 3f, land, Color.White * alpha);

        float titleIn = Math.Clamp((t - 0.6f) / 0.35f, 0f, 1f);
        const float titleSize = 7f;
        float titleWidth = PixelFont.Measure("MEGRACE", titleSize);
        var titlePos = new Vector2((AreaWidth - titleWidth) / 2f, 300f + ((1f - titleIn) * 20f));
        if (titleIn > 0f)
        {
            DrawGameTitle(titlePos, titleSize, alpha * titleIn);
        }

        float credit = Math.Clamp((t - 1.1f) / 0.4f, 0f, 1f) * alpha;
        DrawCenteredText(new Rectangle(0, 0, (int)AreaWidth, 0), L.T("UM JOGO DOMUS ARCIS", "A DOMUS ARCIS GAME"), 378f, 1.6f, StatBadgeLabelColor * credit);
    }

    // ---------- Segredos do menu principal ----------

    private void UpdateMenuSecrets(float dt, Rectangle logoRect)
    {
        _menuIdleTime = _input.AnyActivity ? 0f : _menuIdleTime + dt;
        if (_menuIdleTime >= MenuIdleDiscoverySeconds)
        {
            _menuIdleTime = 0f;
            RegisterDiscovery(Discovery.MenuIdle);
        }

        foreach (Keys key in _input.JustPressedKeys)
        {
            if (key == SecretCode[_secretCodeProgress])
            {
                _secretCodeProgress++;
            }
            else
            {
                // "cima, cima, cima, baixo..." ainda vale: os dois últimos "cima" continuam contando.
                _secretCodeProgress = key == Keys.Up ? (_secretCodeProgress == 2 ? 2 : 1) : 0;
            }

            if (_secretCodeProgress == SecretCode.Length)
            {
                _secretCodeProgress = 0;
                _audio.PlayUnlock(isSkin: true);
                RegisterDiscovery(Discovery.Konami);
            }
        }

        _logoBounce = MathF.Max(0f, _logoBounce - (dt * 4f));
        _logoClickTimer = MathF.Max(0f, _logoClickTimer - dt);
        if (_logoClickTimer <= 0f)
        {
            _logoClicks = 0;
        }

        if (MouseClicked && logoRect.Contains(LogicalMousePoint()))
        {
            _audio.PlayCluck();
            _logoBounce = 1f;
            _logoClicks++;
            _logoClickTimer = 2.5f;
            if (_logoClicks >= LogoClicksForDiscovery)
            {
                _logoClicks = 0;
                RegisterDiscovery(Discovery.Cocorico);
            }
        }
    }

    /// <summary>Anota uma interação escondida e libera o que ela der (com os avisos de sempre).</summary>
    private void RegisterDiscovery(string id)
    {
        if (_saveData.AddDiscovery(id))
        {
            _saveData.Save();
            CheckUnlocks();
        }
    }
}
