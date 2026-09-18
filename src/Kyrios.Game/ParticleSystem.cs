using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>Uma partícula solta: círculo simples que encolhe/esmaece com a vida e desacelera com o tempo.</summary>
public struct Particle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Life;
    public float MaxLife;
    public float Size;
    public Color Color;
}

/// <summary>
/// Sistema de partículas bem simples — poeira ao sair da pista, faíscas de colisão, rastro de turbo —
/// tudo desenhado com o mesmo círculo procedural já usado pro resto do jogo, sem nenhuma textura nova.
/// </summary>
public sealed class ParticleSystem
{
    private const float Drag = 0.92f;
    private readonly List<Particle> _particles = [];

    public IReadOnlyList<Particle> Particles => _particles;

    public void Spawn(Vector2 position, Vector2 velocity, float life, float size, Color color)
    {
        _particles.Add(new Particle
        {
            Position = position,
            Velocity = velocity,
            Life = life,
            MaxLife = life,
            Size = size,
            Color = color,
        });
    }

    public void Update(float dt)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            Particle particle = _particles[i];
            particle.Life -= dt;
            if (particle.Life <= 0f)
            {
                _particles.RemoveAt(i);
                continue;
            }

            particle.Position += particle.Velocity * dt;
            particle.Velocity *= Drag;
            _particles[i] = particle;
        }
    }

    public void Clear() => _particles.Clear();
}
