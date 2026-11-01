using System;
using System.Numerics;
using Raylib_cs;

namespace Rays;

public sealed class Player(Vector2 startPosition, float startAngle = 0.0f)
{
    public Vector2 Position { get; private set; } = startPosition;
    public float Angle { get; private set; } = startAngle;
    public float Speed { get; set; } = 90.0f;
    public float RotationSpeed { get; set; } = 2.5f;
    public float Radius { get; set; } = 5.0f;

    public void Update(float dt, Rectangle arenaBounds)
    {
        if (Raylib.IsKeyDown(KeyboardKey.Left) || Raylib.IsKeyDown(KeyboardKey.Q))
            Angle -= RotationSpeed * dt;
        if (Raylib.IsKeyDown(KeyboardKey.Right) || Raylib.IsKeyDown(KeyboardKey.E))
            Angle += RotationSpeed * dt;

        Vector2 forward = new(MathF.Cos(Angle), MathF.Sin(Angle));
        Vector2 right = new(-forward.Y, forward.X);

        Vector2 moveDir = Vector2.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.W)) moveDir += forward;
        if (Raylib.IsKeyDown(KeyboardKey.S)) moveDir -= forward;
        if (Raylib.IsKeyDown(KeyboardKey.D)) moveDir += right;
        if (Raylib.IsKeyDown(KeyboardKey.A)) moveDir -= right;

        if (moveDir != Vector2.Zero)
        {
            Position += Vector2.Normalize(moveDir) * Speed * dt;
        }

        Position = new Vector2(
            Math.Clamp(Position.X, arenaBounds.X + Radius, arenaBounds.X + arenaBounds.Width - Radius),
            Math.Clamp(Position.Y, arenaBounds.Y + Radius, arenaBounds.Y + arenaBounds.Height - Radius));
    }
}
