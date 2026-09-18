namespace Kyrios.Core;

/// <summary>Vetor 2D simples em ponto flutuante usado para posições e direções no mundo da pista.</summary>
public readonly struct Vector2D
{
    public float X { get; }
    public float Y { get; }

    public Vector2D(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static Vector2D Zero => new(0f, 0f);

    public static Vector2D FromAngle(float angleRadians) => new(MathF.Cos(angleRadians), MathF.Sin(angleRadians));

    public float Length() => MathF.Sqrt((X * X) + (Y * Y));

    public float DistanceTo(Vector2D other) => (this - other).Length();

    public static float Dot(Vector2D a, Vector2D b) => (a.X * b.X) + (a.Y * b.Y);

    public static Vector2D operator +(Vector2D a, Vector2D b) => new(a.X + b.X, a.Y + b.Y);

    public static Vector2D operator -(Vector2D a, Vector2D b) => new(a.X - b.X, a.Y - b.Y);

    public static Vector2D operator *(Vector2D a, float scalar) => new(a.X * scalar, a.Y * scalar);

    public override string ToString() => $"({X:0.00}, {Y:0.00})";
}
