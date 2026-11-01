using System;
using System.Numerics;
using Raylib_cs;

namespace Rays;

public static class GeometryBuilder
{
    public static (Obstacle[] Obstacles, Vector2[] Vertices) BuildArena(Rectangle rect)
    {
        Vector2 tL = new(rect.X, rect.Y);
        Vector2 tR = new(rect.X + rect.Width, rect.Y);
        Vector2 bL = new(rect.X, rect.Y + rect.Height);
        Vector2 bR = new(rect.X + rect.Width, rect.Y + rect.Height);

        Obstacle[] obstacles =
        [
            new Obstacle(tL, bL, Color.Red, ObstacleGroup.Arena),
            new Obstacle(tL, tR, Color.Red, ObstacleGroup.Arena),
            new Obstacle(tR, bR, Color.Red, ObstacleGroup.Arena),
            new Obstacle(bL, bR, Color.Red, ObstacleGroup.Arena)
        ];

        return (obstacles, [tL, tR, bR, bL]);
    }

    public static (Obstacle[] Obstacles, Vector2[] Vertices) BuildStarModel(
        Vector2 center,
        float outerRadius,
        float innerRadius,
        int points,
        float rotationRad)
    {
        int vertexCount = points * 2;
        Vector2[] vertices = new Vector2[vertexCount];
        Obstacle[] obstacles = new Obstacle[vertexCount];

        Color facetColorA = new(0, 205, 225, 255);
        Color facetColorB = new(255, 185, 40, 255);

        float angleStep = MathF.PI / points;
        for (int i = 0; i < vertexCount; i++)
        {
            float r = (i % 2 == 0) ? outerRadius : innerRadius;
            float angle = rotationRad + i * angleStep;
            vertices[i] = center + new Vector2(MathF.Cos(angle) * r, MathF.Sin(angle) * r);
        }

        for (int i = 0; i < vertexCount; i++)
        {
            Vector2 a = vertices[i];
            Vector2 b = vertices[(i + 1) % vertexCount];
            Color color = (i % 2 == 0) ? facetColorA : facetColorB;
            obstacles[i] = new Obstacle(a, b, color, ObstacleGroup.Model);
        }

        return (obstacles, vertices);
    }

    public static (Obstacle[] Obstacles, Vector2[] Vertices) BuildPlayerModel(
        Vector2 center,
        float angleRad,
        float radius = 7.5f,
        float heightScale = 0.48f)
    {
        Vector2 forward = new(MathF.Cos(angleRad), MathF.Sin(angleRad));
        Vector2 right = new(-forward.Y, forward.X);

        Vector2 nose = center + forward * (radius * 1.35f);
        Vector2 frontRight = center + forward * (radius * 0.35f) + right * (radius * 0.85f);
        Vector2 rearRight = center - forward * (radius * 0.95f) + right * (radius * 0.90f);
        Vector2 tailNotch = center - forward * (radius * 0.55f);
        Vector2 rearLeft = center - forward * (radius * 0.95f) - right * (radius * 0.90f);
        Vector2 frontLeft = center + forward * (radius * 0.35f) - right * (radius * 0.85f);

        Vector2[] vertices = [nose, frontRight, rearRight, tailNotch, rearLeft, frontLeft];

        Color frontColor = new(60, 245, 130, 255);
        Color sideColor = new(30, 195, 95, 255);
        Color rearColor = new(20, 145, 70, 255);

        Obstacle[] obstacles =
        [
            new Obstacle(nose, frontRight, frontColor, ObstacleGroup.Player, heightScale),
            new Obstacle(frontRight, rearRight, sideColor, ObstacleGroup.Player, heightScale),
            new Obstacle(rearRight, tailNotch, rearColor, ObstacleGroup.Player, heightScale),
            new Obstacle(tailNotch, rearLeft, rearColor, ObstacleGroup.Player, heightScale),
            new Obstacle(rearLeft, frontLeft, sideColor, ObstacleGroup.Player, heightScale),
            new Obstacle(frontLeft, nose, frontColor, ObstacleGroup.Player, heightScale)
        ];

        return (obstacles, vertices);
    }
}
