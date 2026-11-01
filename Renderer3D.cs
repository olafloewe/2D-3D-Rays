using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Rays;

public static class Renderer3D
{
    private const float WallHeightFactor = 28000.0f;

    public static void Draw3DView(
        Rectangle screenRect,
        List<RayHit>[] rayMultiHits,
        SceneSettings settings,
        float maxDistanceShade = 380.0f)
    {
        float halfHeight = screenRect.Height * 0.5f;
        Raylib.DrawRectangleRec(
            new Rectangle(screenRect.X, screenRect.Y, screenRect.Width, halfHeight),
            new Color(30, 34, 42, 255));
        Raylib.DrawRectangleRec(
            new Rectangle(screenRect.X, screenRect.Y + halfHeight, screenRect.Width, halfHeight),
            new Color(55, 55, 60, 255));

        int rayCount = rayMultiHits.Length;
        int sliceCount = rayCount - 1;
        float sliceWidth = screenRect.Width / sliceCount;

        for (int i = 0; i < sliceCount; i++)
        {
            List<RayHit> hitsLeft = rayMultiHits[i];
            List<RayHit> hitsRight = rayMultiHits[i + 1];

            float x1 = screenRect.X + i * sliceWidth;
            float x2 = screenRect.X + (i + 1) * sliceWidth;

            // Back-to-front painter's algorithm
            for (int h = hitsLeft.Count - 1; h >= 0; h--)
            {
                RayHit hit1 = hitsLeft[h];
                float alphaScale = settings.GetOpacity(hit1.Group);
                if (alphaScale <= 0.001f)
                    continue;

                float reflectivity = settings.GetReflectivity(hit1.Group);
                byte alpha = (byte)Math.Clamp(alphaScale * 255.0f, 0.0f, 255.0f);

                float perpDist1 = MathF.Max(hit1.Distance * MathF.Cos(hit1.AngleOffset), 1.0f);
                float h1 = Math.Clamp((WallHeightFactor * hit1.HeightScale) / perpDist1, 0.0f, screenRect.Height);
                float top1 = screenRect.Y + (screenRect.Height - h1) * 0.5f;
                float bot1 = top1 + h1;

                bool foundNeighbor = TryFindMatchingHit(hitsRight, hit1, perpDist1, out RayHit hit2, out float perpDist2);

                float avgDist = foundNeighbor ? (perpDist1 + perpDist2) * 0.5f : perpDist1;
                float shade = Math.Clamp(1.0f - (avgDist / maxDistanceShade), 0.18f, 1.0f);

                float baseWeight = 1.0f - reflectivity * 0.35f;
                Color baseColor = hit1.Color;
                Color shadedColor = new(
                    (byte)Math.Clamp(baseColor.R * shade * baseWeight, 0.0f, 255.0f),
                    (byte)Math.Clamp(baseColor.G * shade * baseWeight, 0.0f, 255.0f),
                    (byte)Math.Clamp(baseColor.B * shade * baseWeight, 0.0f, 255.0f),
                    alpha);

                float top2 = top1;
                float bot2 = bot1;

                if (foundNeighbor)
                {
                    float h2 = Math.Clamp((WallHeightFactor * hit2.HeightScale) / perpDist2, 0.0f, screenRect.Height);
                    top2 = screenRect.Y + (screenRect.Height - h2) * 0.5f;
                    bot2 = top2 + h2;

                    DrawTrapezoid(x1, top1, bot1, x2, top2, bot2, shadedColor);
                }
                else
                {
                    Raylib.DrawRectangleRec(new Rectangle(x1, top1, MathF.Ceiling(sliceWidth), h1), shadedColor);
                }

                // Draw ray-traced mirror reflections (background wall first if primary reflection is shorter, like the Player)
                if (reflectivity > 0.001f && hit1.PrimaryReflection is ReflectedLayer primaryRefl1)
                {
                    if (hit1.BackgroundReflection is ReflectedLayer bgRefl1)
                    {
                        ReflectedLayer? bgRefl2 = foundNeighbor ? hit2.BackgroundReflection : null;
                        DrawReflectedSlice(
                            screenRect, x1, x2, sliceWidth, top1, bot1, top2, bot2,
                            maxDistanceShade, alphaScale, reflectivity,
                            hit1.AngleOffset, bgRefl1,
                            bgRefl2.HasValue && bgRefl2.Value.ObstacleIndex == bgRefl1.ObstacleIndex ? (hit2.AngleOffset, bgRefl2.Value) : null);
                    }

                    ReflectedLayer? primaryRefl2 = foundNeighbor ? hit2.PrimaryReflection : null;
                    DrawReflectedSlice(
                        screenRect, x1, x2, sliceWidth, top1, bot1, top2, bot2,
                        maxDistanceShade, alphaScale, reflectivity,
                        hit1.AngleOffset, primaryRefl1,
                        primaryRefl2.HasValue && primaryRefl2.Value.ObstacleIndex == primaryRefl1.ObstacleIndex ? (hit2.AngleOffset, primaryRefl2.Value) : null);
                }
            }
        }
    }

    private static void DrawReflectedSlice(
        Rectangle screenRect,
        float x1,
        float x2,
        float sliceWidth,
        float mirrorTop1,
        float mirrorBot1,
        float mirrorTop2,
        float mirrorBot2,
        float maxDistanceShade,
        float alphaScale,
        float reflectivity,
        float angleOffset1,
        ReflectedLayer layer1,
        (float AngleOffset, ReflectedLayer Layer)? neighbor)
    {
        float reflPerpDist1 = MathF.Max(layer1.TotalDistance * MathF.Cos(angleOffset1), 1.0f);
        float reflH1 = Math.Clamp((WallHeightFactor * layer1.HeightScale) / reflPerpDist1, 0.0f, screenRect.Height);
        float reflTop1 = Math.Clamp(screenRect.Y + (screenRect.Height - reflH1) * 0.5f, mirrorTop1, mirrorBot1);
        float reflBot1 = Math.Clamp(screenRect.Y + (screenRect.Height + reflH1) * 0.5f, mirrorTop1, mirrorBot1);

        float reflShade = Math.Clamp(1.0f - (reflPerpDist1 / (maxDistanceShade * 1.35f)), 0.15f, 1.0f);
        byte reflAlpha = (byte)Math.Clamp(alphaScale * reflectivity * 235.0f, 0.0f, 255.0f);
        Color rCol = layer1.Color;
        Color reflectedShaded = new(
            (byte)(rCol.R * reflShade),
            (byte)(rCol.G * reflShade),
            (byte)(rCol.B * reflShade),
            reflAlpha);

        if (neighbor is (float angleOffset2, ReflectedLayer layer2))
        {
            float reflPerpDist2 = MathF.Max(layer2.TotalDistance * MathF.Cos(angleOffset2), 1.0f);
            float reflH2 = Math.Clamp((WallHeightFactor * layer2.HeightScale) / reflPerpDist2, 0.0f, screenRect.Height);
            float reflTop2 = Math.Clamp(screenRect.Y + (screenRect.Height - reflH2) * 0.5f, mirrorTop2, mirrorBot2);
            float reflBot2 = Math.Clamp(screenRect.Y + (screenRect.Height + reflH2) * 0.5f, mirrorTop2, mirrorBot2);

            DrawTrapezoid(x1, reflTop1, reflBot1, x2, reflTop2, reflBot2, reflectedShaded);
        }
        else
        {
            Raylib.DrawRectangleRec(
                new Rectangle(x1, reflTop1, MathF.Ceiling(sliceWidth), MathF.Max(0.0f, reflBot1 - reflTop1)),
                reflectedShaded);
        }
    }

    private static void DrawTrapezoid(float x1, float top1, float bot1, float x2, float top2, float bot2, Color color)
    {
        Vector2 tl = new(x1, top1);
        Vector2 bl = new(x1, bot1);
        Vector2 tr = new(x2, top2);
        Vector2 br = new(x2, bot2);

        Raylib.DrawTriangle(tl, bl, br, color);
        Raylib.DrawTriangle(tl, br, tr, color);
    }

    private static bool TryFindMatchingHit(
        List<RayHit> neighborHits,
        RayHit currentHit,
        float perpDist1,
        out RayHit matchedHit,
        out float perpDist2)
    {
        for (int j = 0; j < neighborHits.Count; j++)
        {
            RayHit candidate = neighborHits[j];
            float candidatePerpDist = MathF.Max(candidate.Distance * MathF.Cos(candidate.AngleOffset), 1.0f);

            if (candidate.ObstacleIndex == currentHit.ObstacleIndex && MathF.Abs(perpDist1 - candidatePerpDist) < 25.0f)
            {
                matchedHit = candidate;
                perpDist2 = candidatePerpDist;
                return true;
            }

            if (candidate.Group == currentHit.Group && MathF.Abs(perpDist1 - candidatePerpDist) < 6.0f)
            {
                matchedHit = candidate;
                perpDist2 = candidatePerpDist;
                return true;
            }
        }

        matchedHit = default;
        perpDist2 = perpDist1;
        return false;
    }
}
