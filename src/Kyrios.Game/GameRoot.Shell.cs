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
    /// pequena na janela quanto grande em tela cheia.</summary>
    private Texture2D LoadLogo()
    {
        using Stream stream = typeof(GameRoot).Assembly.GetManifestResourceStream("MegRace.Logo.png");
        if (stream is null)
        {
            return null;
        }

        using Texture2D source = Texture2D.FromStream(GraphicsDevice, stream);
        int width = source.Width;
        int height = source.Height;
        var level = new Color[width * height];
        source.GetData(level);
        for (int i = 0; i < level.Length; i++)
        {
            level[i] = Color.FromNonPremultiplied(level[i].R, level[i].G, level[i].B, level[i].A);
        }

        var texture = new Texture2D(GraphicsDevice, width, height, mipmap: true, SurfaceFormat.Color);
        for (int mip = 0; mip < texture.LevelCount; mip++)
        {
            texture.SetData(mip, null, level, 0, level.Length);
            (level, width, height) = HalveImage(level, width, height);
        }

        return texture;
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

        var origin = new Vector2(_logo.Width / 2f, _logo.Height / 2f);
        float fit = scale * LogoBaseWidth / _logo.Width;
        var size = new Vector2(fit * (1f + (squash * 0.1f)), fit * (1f - (squash * 0.12f)));
        Color color = tint ?? Color.White;

        _spriteBatch.End();
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: _screenTransform);
        _spriteBatch.Draw(_logo, center + new Vector2(3f, 4f), null, Color.Black * (0.35f * (color.A / 255f)), 0f, origin, size, SpriteEffects.None, 0f);
        _spriteBatch.Draw(_logo, center, null, color, 0f, origin, size, SpriteEffects.None, 0f);
        _spriteBatch.End();
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _screenTransform);
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
        DrawCenteredText(new Rectangle(0, 0, (int)AreaWidth, 0), "UM JOGO DOMUS ARCIS", 378f, 1.6f, StatBadgeLabelColor * credit);
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
