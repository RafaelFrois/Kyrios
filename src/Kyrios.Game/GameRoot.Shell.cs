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
        if (GamePlatform.Current.DecodedLogo is { } decoded)
        {
            return CreateLogoTexture(decoded);
        }

        using Stream stream = typeof(GameRoot).Assembly.GetManifestResourceStream("MegRace.Logo.png");
        if (stream is null)
        {
            return null;
        }

        using Texture2D source = Texture2D.FromStream(GraphicsDevice, stream);
        _logoSize = new Point(source.Width, source.Height);
        var pixels = new uint[source.Width * source.Height];
        source.GetData(pixels);

        // Contas com inteiros sobre os pixels empacotados (RGBA em um uint): no navegador o código roda
        // interpretado e isso é várias vezes mais rápido que mexer em Color canal por canal.
        int width = NextPowerOfTwo(source.Width);
        int height = NextPowerOfTwo(source.Height);
        var level = new uint[width * height];
        for (int y = 0; y < source.Height; y++)
        {
            int from = y * source.Width;
            int to = y * width;
            for (int x = 0; x < source.Width; x++)
            {
                level[to + x] = Premultiply(pixels[from + x]);
            }
        }

        var texture = new Texture2D(GraphicsDevice, width, height, true, SurfaceFormat.Color);
        for (int mip = 0; mip < texture.LevelCount; mip++)
        {
            texture.SetData(mip, null, level, 0, width * height);
            (level, width, height) = HalveImage(level, width, height);
        }

        return texture;
    }

    /// <summary>Textura da logo a partir dos níveis já prontos (decodificados e reduzidos pela plataforma).</summary>
    private Texture2D CreateLogoTexture(DecodedImage image)
    {
        _logoSize = new Point(image.Width, image.Height);
        var texture = new Texture2D(GraphicsDevice, image.TextureWidth, image.TextureHeight, true, SurfaceFormat.Color);
        int width = image.TextureWidth;
        int height = image.TextureHeight;
        int offset = 0;
        for (int mip = 0; mip < texture.LevelCount; mip++)
        {
            int bytes = width * height * 4;
            texture.SetData(mip, null, image.MipChain, offset, bytes);
            offset += bytes;
            width = Math.Max(1, width / 2);
            height = Math.Max(1, height / 2);
        }

        return texture;
    }

    private static uint Premultiply(uint packed)
    {
        uint a = packed >> 24;
        if (a == 255)
        {
            return packed;
        }

        uint r = ((packed & 0xFF) * a + 127) / 255;
        uint g = (((packed >> 8) & 0xFF) * a + 127) / 255;
        uint b = (((packed >> 16) & 0xFF) * a + 127) / 255;
        return r | (g << 8) | (b << 16) | (a << 24);
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

    /// <summary>Metade da resolução (lados em potência de dois) pela média de cada bloco 2x2, canal a canal.</summary>
    private static (uint[] Pixels, int Width, int Height) HalveImage(uint[] pixels, int width, int height)
    {
        int halfWidth = Math.Max(1, width / 2);
        int halfHeight = Math.Max(1, height / 2);
        var result = new uint[halfWidth * halfHeight];
        int stepX = width > 1 ? 1 : 0;
        int stepY = height > 1 ? width : 0;
        for (int y = 0; y < halfHeight; y++)
        {
            int row = y * 2 * width;
            for (int x = 0; x < halfWidth; x++)
            {
                int i = row + (x * 2);
                uint p0 = pixels[i];
                uint p1 = pixels[i + stepX];
                uint p2 = pixels[i + stepY];
                uint p3 = pixels[i + stepY + stepX];

                // Dois canais por conta: R e B (depois G e A) ficam em faixas de 16 bits do mesmo uint, e a soma
                // de quatro pixels (até 1020) cabe na faixa sem invadir o canal vizinho.
                uint rb = (p0 & 0x00FF00FF) + (p1 & 0x00FF00FF) + (p2 & 0x00FF00FF) + (p3 & 0x00FF00FF);
                uint ga = ((p0 >> 8) & 0x00FF00FF) + ((p1 >> 8) & 0x00FF00FF) + ((p2 >> 8) & 0x00FF00FF) + ((p3 >> 8) & 0x00FF00FF);
                result[(y * halfWidth) + x] = ((rb >> 2) & 0x00FF00FF) | (((ga >> 2) & 0x00FF00FF) << 8);
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

    /// <summary>Celular/tablet em pé (no app: tela dividida, janela solta): o jogo é deitado (2,25:1), então pede pra
    /// girar em vez de mostrar tudo minúsculo.</summary>
    private bool IsPortraitTouchScreen =>
        (GamePlatform.Current.IsMobile || (GamePlatform.Current.IsWeb && _input.UsingTouch))
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
        string title = L.T("GIRE O DISPOSITIVO", "ROTATE YOUR DEVICE");
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
        if ((_stateTime >= SplashSeconds || skip) && !StillLoading)
        {
            _state = State.MainMenu;
        }
    }

    /// <summary>A galinha de kart entra quicando, o título aparece embaixo e tudo esmaece pro menu.</summary>
    private float? _loadingDoneAt;

    private void DrawSplash()
    {
        float t = _stateTime;
        float alpha = Math.Clamp(MathF.Min(t / 0.35f, StillLoading ? 1f : (SplashSeconds - t) / 0.4f), 0f, 1f);
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
        DrawCenteredText(new Rectangle(0, 0, (int)AreaWidth, 0), L.T("UM JOGO DOMUS ARCIS", "A DOMUS ARCIS GAME"), 378f, MobileUi ? 2f : 1.6f, StatBadgeLabelColor * credit);

        // Celular: a abertura também é a tela de carregamento (a barra só aparece enquanto falta alguma coisa).
        if (GamePlatform.Current.ShowsLoadingScreen)
        {
            // Terminou de carregar: a barra some logo (ou junto com a abertura, se ela já estiver acabando).
            _loadingDoneAt ??= StillLoading ? null : t;
            float barAlpha = StillLoading ? 1f : Math.Clamp(1f - ((t - (_loadingDoneAt ?? t) - 0.4f) / 0.3f), 0f, 1f);
            var bar = new Rectangle((int)(AreaWidth / 2f) - 180, 430, 360, 12);
            DrawProgressBar(bar, LoadingProgress, AccentColor * barAlpha, 6f);
            string label = L.T($"CARREGANDO {(int)(LoadingProgress * 100f)}%", $"LOADING {(int)(LoadingProgress * 100f)}%");
            DrawCenteredText(new Rectangle(0, 0, (int)AreaWidth, 0), label, 450f, 1.8f, StatBadgeLabelColor * barAlpha);
        }
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
