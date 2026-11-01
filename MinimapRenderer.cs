using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Rays;

public static class MinimapRenderer
{
    public static void Draw(
        Rectangle mapRect,
        Player player,
        float leftAngle,
        float fov,
        float maxRayDist,
        Obstacle[] arenaObstacles,
        Obstacle innerWall,
        Obstacle[] modelObstacles,
        Obstacle[] playerObstacles,
        Obstacle[] allObstacles,
        Vector2[] arenaVertices,
        Vector2[] wallVertices,
        Vector2[] modelVertices,
        Vector2[] playerVertices,
        List<RayHit>[] rayMultiHits,
        List<(Vector2 Start, Vector2 End)>[] rayBounceLines,
        SceneSettings settings)
    {
        // Background panel
        Raylib.DrawRectangleRec(mapRect, new Color(245, 245, 245, 235));

        // Shaded FOV polygon
        DrawMinimapFov(
            player.Position,
            leftAngle,
            fov,
            maxRayDist,
            arenaObstacles,
            allObstacles,
            arenaVertices,
            wallVertices,
            modelVertices,
            settings.WallOpacity,
            settings.ModelOpacity);

        // Primary FOV rays and reflected bounce rays
        Color primaryRayColor = new(255, 255, 255, 105);
        Color reflectedRayColor = new(255, 230, 90, 115);
        float wallAcc = 0.0f;
        float modelAcc = 0.0f;
        int rayCount = rayMultiHits.Length;

        for (int i = 0; i < rayCount; i += 3)
        {
            wallAcc += settings.WallOpacity;
            bool blockWall = wallAcc >= 1.0f;
            if (blockWall) wallAcc -= 1.0f;

            modelAcc += settings.ModelOpacity;
            bool blockModel = modelAcc >= 1.0f;
            if (blockModel) modelAcc -= 1.0f;

            Vector2 endPoint = GetMinimapRayEndpoint(rayMultiHits[i], blockWall, blockModel, player.Position);
            Raylib.DrawLineV(player.Position, endPoint, primaryRayColor);

            List<(Vector2 Start, Vector2 End)> bounces = rayBounceLines[i];
            for (int b = 0; b < bounces.Count; b++)
            {
                Raylib.DrawLineV(bounces[b].Start, bounces[b].End, reflectedRayColor);
            }
        }

        // FOV boundary lines
        Vector2 leftHit = GetMinimapRayEndpoint(rayMultiHits[0], settings.WallOpacity > 0.0f, settings.ModelOpacity > 0.0f, player.Position);
        Vector2 rightHit = GetMinimapRayEndpoint(rayMultiHits[rayCount - 1], settings.WallOpacity > 0.0f, settings.ModelOpacity > 0.0f, player.Position);
        Raylib.DrawLineEx(player.Position, leftHit, 2.0f, Color.DarkGray);
        Raylib.DrawLineEx(player.Position, rightHit, 2.0f, Color.DarkGray);

        // Arena borders
        foreach (Obstacle border in arenaObstacles)
        {
            Raylib.DrawLineEx(border.A, border.B, 3.0f, border.Color);
        }

        // Inner wall
        Raylib.DrawLineEx(innerWall.A, innerWall.B, 4.0f, innerWall.Color);

        // Central star model
        foreach (Obstacle seg in modelObstacles)
        {
            Raylib.DrawLineEx(seg.A, seg.B, 2.5f, seg.Color);
        }

        // Player model
        DrawPlayerModel(player.Position, playerVertices, playerObstacles);
    }

    private static void DrawPlayerModel(Vector2 center, Vector2[] vertices, Obstacle[] obstacles)
    {
        Color hullFill = new(40, 210, 100, 190);
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector2 v1 = vertices[i];
            Vector2 v2 = vertices[(i + 1) % vertices.Length];
            Raylib.DrawTriangle(center, v2, v1, hullFill);
            Raylib.DrawTriangle(center, v1, v2, hullFill);
        }

        foreach (Obstacle seg in obstacles)
        {
            Raylib.DrawLineEx(seg.A, seg.B, 2.0f, seg.Color);
        }

        Raylib.DrawCircleV(center, 2.0f, Color.White);
    }

    private static Vector2 GetMinimapRayEndpoint(
        List<RayHit> hitsFrontToBack,
        bool blockWall,
        bool blockModel,
        Vector2 fallback)
    {
        for (int i = 0; i < hitsFrontToBack.Count; i++)
        {
            RayHit hit = hitsFrontToBack[i];
            if (hit.Group == ObstacleGroup.Wall && !blockWall)
                continue;
            if (hit.Group == ObstacleGroup.Model && !blockModel)
                continue;
            return hit.Point;
        }

        return hitsFrontToBack.Count > 0 ? hitsFrontToBack[^1].Point : fallback;
    }

    private static void DrawMinimapFov(
        Vector2 playerPos,
        float leftAngle,
        float fov,
        float maxRayDist,
        Obstacle[] arenaObstacles,
        Obstacle[] allObstacles,
        Vector2[] arenaVertices,
        Vector2[] wallVertices,
        Vector2[] modelVertices,
        float wallOpacity,
        float modelOpacity)
    {
        float fgOpacity = MathF.Max(wallOpacity, modelOpacity);
        byte passAlpha = (byte)(50 * (1.0f - fgOpacity));
        byte blockedAlpha = (byte)(50 * fgOpacity);

        if (passAlpha > 0)
        {
            DrawFovFill(playerPos, leftAngle, fov, maxRayDist, arenaObstacles, arenaVertices, new Color((byte)0, (byte)0, (byte)0, passAlpha));
        }

        if (blockedAlpha > 0)
        {
            List<Obstacle> activeObstacles = [.. arenaObstacles];
            List<Vector2> activeVertices = [.. arenaVertices];

            foreach (Obstacle obs in allObstacles)
            {
                if (obs.Group == ObstacleGroup.Wall && wallOpacity > 0.01f)
                    activeObstacles.Add(obs);
                else if (obs.Group == ObstacleGroup.Model && modelOpacity > 0.01f)
                    activeObstacles.Add(obs);
            }

            if (wallOpacity > 0.01f)
                activeVertices.AddRange(wallVertices);
            if (modelOpacity > 0.01f)
                activeVertices.AddRange(modelVertices);

            DrawFovFill(
                playerPos,
                leftAngle,
                fov,
                maxRayDist,
                activeObstacles.ToArray(),
                activeVertices.ToArray(),
                new Color((byte)0, (byte)0, (byte)0, blockedAlpha));
        }
    }

    private static void DrawFovFill(
        Vector2 origin,
        float startAngle,
        float fov,
        float maxDist,
        Obstacle[] obstacles,
        Vector2[] vertices,
        Color fillColor)
    {
        List<float> offsets = [0.0f, fov];
        const float eps = 0.0002f;

        foreach (Vector2 v in vertices)
        {
            float vertexAngle = MathF.Atan2(v.Y - origin.Y, v.X - origin.X);
            float delta = NormalizeAnglePositive(vertexAngle - startAngle);

            if (delta - eps > 0.0f && delta - eps < fov)
                offsets.Add(delta - eps);
            if (delta + eps > 0.0f && delta + eps < fov)
                offsets.Add(delta + eps);
        }

        offsets.Sort();

        for (int i = 0; i < offsets.Count - 1; i++)
        {
            Vector2 p1 = RayTracer.CastClosestRay(origin, startAngle + offsets[i], maxDist, obstacles);
            Vector2 p2 = RayTracer.CastClosestRay(origin, startAngle + offsets[i + 1], maxDist, obstacles);
            Raylib.DrawTriangle(origin, p2, p1, fillColor);
        }
    }

    private static float NormalizeAnglePositive(float angle)
    {
        float twoPi = MathF.PI * 2.0f;
        angle %= twoPi;
        if (angle < 0) angle += twoPi;
        return angle;
    }
}
