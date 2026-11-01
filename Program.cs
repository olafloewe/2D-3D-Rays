using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Rays;

internal static class Program
{
    public static void Main(string[] args)
    {
        const int screenWidth = 1200;
        const int screenHeight = 800;
        const int rayCount = 300;
        const float fov = 90.0f * (MathF.PI / 180.0f);
        const float maxRayDist = 1000.0f;

        Raylib.InitWindow(screenWidth, screenHeight, "2D 3D Rays - Ray Casting & Ray Tracing");
        Raylib.SetTargetFPS(120);

        Rectangle minimapRect = new(16, 16, 320, 240);
        Rectangle view3DRect = new(0, 0, screenWidth, screenHeight);

        (Obstacle[] arenaObstacles, Vector2[] arenaVertices) = GeometryBuilder.BuildArena(minimapRect);

        // Inner purple wall
        Vector2 w1 = new(minimapRect.X + minimapRect.Width * 0.62f, minimapRect.Y + minimapRect.Height * 0.18f);
        Vector2 w2 = new(minimapRect.X + minimapRect.Width * 0.85f, minimapRect.Y + minimapRect.Height * 0.48f);
        Obstacle innerWall = new(w1, w2, Color.Purple, ObstacleGroup.Wall);
        Vector2[] wallVertices = [w1, w2];

        Vector2 modelCenter = new(minimapRect.X + minimapRect.Width * 0.5f, minimapRect.Y + minimapRect.Height * 0.5f);
        Player player = new(new Vector2(minimapRect.X + minimapRect.Width * 0.2f, minimapRect.Y + minimapRect.Height * 0.5f));
        SceneSettings settings = new();
        SliderUI sliderUI = new();

        List<RayHit>[] rayMultiHits = new List<RayHit>[rayCount];
        List<(Vector2 Start, Vector2 End)>[] rayBounceLines = new List<(Vector2 Start, Vector2 End)>[rayCount];
        for (int i = 0; i < rayCount; i++)
        {
            rayMultiHits[i] = new List<RayHit>(8);
            rayBounceLines[i] = new List<(Vector2 Start, Vector2 End)>(4);
        }

        float halfFovTan = MathF.Tan(fov * 0.5f);

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();

            // 1. Update player & scene state
            player.Update(dt, minimapRect);
            settings.ModelRotationDeg = (settings.ModelRotationDeg + settings.ModelSpinSpeed * dt) % 360.0f;

            // 2. Build dynamic models (central star & player hull)
            (Obstacle[] modelObstacles, Vector2[] modelVertices) = GeometryBuilder.BuildStarModel(
                modelCenter,
                outerRadius: settings.ModelScale,
                innerRadius: settings.ModelScale * 0.45f,
                points: 5,
                rotationRad: settings.ModelRotationDeg * (MathF.PI / 180.0f));

            (Obstacle[] playerObstacles, Vector2[] playerVertices) = GeometryBuilder.BuildPlayerModel(
                player.Position,
                player.Angle);

            Obstacle[] allObstacles = [.. arenaObstacles, innerWall, .. modelObstacles, .. playerObstacles];

            // 3. Cast & trace all rays across the camera plane
            for (int i = 0; i < rayCount; i++)
            {
                float cameraX = (2.0f * i / (rayCount - 1)) - 1.0f;
                float angleOffset = MathF.Atan(cameraX * halfFovTan);
                float rayAngle = player.Angle + angleOffset;

                RayTracer.CastAndTraceRay(
                    player.Position,
                    rayAngle,
                    angleOffset,
                    maxRayDist,
                    allObstacles,
                    settings,
                    rayMultiHits[i],
                    rayBounceLines[i]);
            }

            // 4. Render 3D scene, 2D minimap, and UI sliders
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            Renderer3D.Draw3DView(view3DRect, rayMultiHits, settings);

            MinimapRenderer.Draw(
                minimapRect,
                player,
                player.Angle - fov * 0.5f,
                fov,
                maxRayDist,
                arenaObstacles,
                innerWall,
                modelObstacles,
                playerObstacles,
                allObstacles,
                arenaVertices,
                wallVertices,
                modelVertices,
                playerVertices,
                rayMultiHits,
                rayBounceLines,
                settings);

            sliderUI.UpdateAndDraw(minimapRect, settings);

            Raylib.DrawFPS(screenWidth - 90, 12);
            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
