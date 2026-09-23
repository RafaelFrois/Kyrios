using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Kyrios.Game;

/// <summary>
/// Desenha formas no espaço local de um carro — x = pra frente, y = pro lado, em "células" (1 unidade =
/// uma célula da pista durante a corrida) — já girando, posicionando e escalando pro mundo. É o que cada
/// <see cref="CarSkin"/> usa pra se desenhar sem se preocupar com rotação, tamanho de tela ou com o carro
/// estar eliminado (quando está, todas as cores saem escurecidas automaticamente).
/// </summary>
public sealed class CarPainter
{
    private static readonly Color DefaultShadow = new(0, 0, 0, 90);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly Texture2D _circle;

    private Vector2 _center;
    private float _angle;
    private float _unit;
    private float _shade = 1f;
    private bool _monochrome;

    public CarPainter(SpriteBatch spriteBatch, Texture2D pixel, Texture2D circle)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
        _circle = circle;
    }

    /// <summary>Cor "do dono" do carro (amarelo do jogador, cor de cada rival) — skins que não têm cor
    /// própria (como o carro clássico) pintam a lataria com ela.</summary>
    public Color BodyColor { get; private set; }

    public bool Eliminated { get; private set; }

    /// <summary>Relógio visual em segundos, pra pequenas animações (asas batendo, rabo balançando...).</summary>
    public float Time { get; private set; }

    /// <param name="silhouette">Desenha quase preto (só um vulto) — usado pras skins ainda bloqueadas.</param>
    /// <param name="monochrome">Desenha em tons de cinza — usado nos ícones de conquistas ainda bloqueadas.</param>
    public void Begin(Vector2 center, float angle, float unit, Color bodyColor, bool eliminated, float time, bool silhouette = false, bool monochrome = false)
    {
        _center = center;
        _angle = angle;
        _unit = unit;
        _shade = silhouette ? 0.18f : eliminated ? 0.4f : 1f;
        _monochrome = monochrome;
        BodyColor = bodyColor;
        Eliminated = eliminated;
        Time = time;
    }

    public static Color Darken(Color color, float factor) =>
        new((byte)(color.R * factor), (byte)(color.G * factor), (byte)(color.B * factor), color.A);

    public void Circle(float x, float y, float radius, Color color)
    {
        float worldRadius = radius * _unit;
        if (worldRadius <= 0f)
        {
            return;
        }

        float scale = (worldRadius * 2f) / _circle.Width;
        _spriteBatch.Draw(_circle, ToWorld(x, y), null, Shade(color), 0f, new Vector2(_circle.Width / 2f), scale, SpriteEffects.None, 0f);
    }

    /// <summary>Retângulo centrado em (x, y), com o comprimento ao longo da frente do carro — girado por
    /// <paramref name="rotation"/> a mais em relação ao carro, se precisar.</summary>
    public void Rect(float x, float y, float length, float width, Color color, float rotation = 0f)
    {
        if (length <= 0f || width <= 0f)
        {
            return;
        }

        _spriteBatch.Draw(_pixel, ToWorld(x, y), null, Shade(color), _angle + rotation, new Vector2(0.5f, 0.5f), new Vector2(length * _unit, width * _unit), SpriteEffects.None, 0f);
    }

    /// <summary>Retângulo com as pontas arredondadas (a silhueta base do carro clássico e de muita skin).</summary>
    public void Capsule(float x, float y, float length, float width, Color color, float rotation = 0f)
    {
        float straight = MathF.Max(length - width, 0f);
        Rect(x, y, straight, width, color, rotation);

        float offsetX = MathF.Cos(rotation) * straight / 2f;
        float offsetY = MathF.Sin(rotation) * straight / 2f;
        Circle(x + offsetX, y + offsetY, width / 2f, color);
        Circle(x - offsetX, y - offsetY, width / 2f, color);
    }

    /// <summary>Sombra projetada no chão (deslocada pra baixo/direita no mundo, nunca escurecida junto).</summary>
    public void Shadow(float length, float width) => WithShadowOffset(() => Capsule(0f, 0f, length, width, DefaultShadow));

    public void ShadowCircle(float radius) => WithShadowOffset(() => Circle(0f, 0f, radius, DefaultShadow));

    private void WithShadowOffset(Action draw)
    {
        Vector2 savedCenter = _center;
        float savedShade = _shade;

        // Mesmo deslocamento de sempre (2, 3 px numa célula de 22 px), só que proporcional ao tamanho, pra
        // continuar parecendo igual no carro gigante da prévia do menu.
        _center += new Vector2(0.09f, 0.14f) * _unit;
        _shade = 1f;
        draw();

        _center = savedCenter;
        _shade = savedShade;
    }

    private Vector2 ToWorld(float x, float y)
    {
        float localX = x * _unit;
        float localY = y * _unit;
        float cos = MathF.Cos(_angle);
        float sin = MathF.Sin(_angle);
        return _center + new Vector2((localX * cos) - (localY * sin), (localX * sin) + (localY * cos));
    }

    private Color Shade(Color color)
    {
        if (_monochrome)
        {
            color = Grayscale(color);
        }

        return _shade >= 1f ? color : Darken(color, _shade);
    }

    /// <summary>Cinza apagado com a mesma luminosidade relativa da cor original (mantém o desenho legível).</summary>
    public static Color Grayscale(Color color)
    {
        float luminance = (color.R * 0.3f) + (color.G * 0.59f) + (color.B * 0.11f);
        var gray = (byte)(28f + (luminance * 0.42f));
        return new Color(gray, gray, (byte)Math.Min(255, gray + 6), color.A);
    }
}
