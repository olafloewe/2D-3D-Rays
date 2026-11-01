using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Rays;

public static class RayTracer
{
    public static Vector2 CastClosestRay(Vector2 origin, float angle, float maxDist, Obstacle[] obstacles)
    {
        Vector2 dir = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 closestHit = origin + dir * maxDist;
        float minDistSq = maxDist * maxDist;

        for (int i = 0; i < obstacles.Length; i++)
        {
            Obstacle obs = obstacles[i];
            if (obs.Group == ObstacleGroup.Player)
                continue;

            Vector2 hitPoint = Vector2.Zero;
            if (Raylib.CheckCollisionLines(origin, closestHit, obs.A, obs.B, ref hitPoint))
            {
                float distSq = Vector2.DistanceSquared(origin, hitPoint);
                if (distSq < minDistSq)
                {
                    minDistSq = distSq;
                    closestHit = hitPoint;
                }
            }
        }

        return closestHit;
    }

    public static void CastAndTraceRay(
        Vector2 origin,
        float angle,
        float angleOffset,
        float maxDist,
        Obstacle[] obstacles,
        SceneSettings settings,
        List<RayHit> resultsFrontToBack,
        List<(Vector2 Start, Vector2 End)> bounceLines)
    {
        resultsFrontToBack.Clear();
        bounceLines.Clear();

        Vector2 dir = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 rayEnd = origin + dir * maxDist;

        for (int i = 0; i < obstacles.Length; i++)
        {
            Obstacle obs = obstacles[i];
            if (obs.Group == ObstacleGroup.Player)
                continue;

            Vector2 hitPoint = Vector2.Zero;
            if (Raylib.CheckCollisionLines(origin, rayEnd, obs.A, obs.B, ref hitPoint))
            {
                float dist = Vector2.Distance(origin, hitPoint);
                resultsFrontToBack.Add(new RayHit(hitPoint, dist, angleOffset, obs.Color, i, obs.Group, obs.HeightScale));
            }
        }

        resultsFrontToBack.Sort(static (a, b) => a.Distance.CompareTo(b.Distance));

        // Trim any intersections behind the first solid outer Arena wall
        for (int i = 0; i < resultsFrontToBack.Count; i++)
        {
            if (resultsFrontToBack[i].Group == ObstacleGroup.Arena)
            {
                if (i + 1 < resultsFrontToBack.Count)
                {
                    resultsFrontToBack.RemoveRange(i + 1, resultsFrontToBack.Count - (i + 1));
                }
                break;
            }
        }

        bool recordedMinimapBounce = false;
        for (int i = 0; i < resultsFrontToBack.Count; i++)
        {
            RayHit hit = resultsFrontToBack[i];
            float opacity = settings.GetOpacity(hit.Group);
            float reflectivity = settings.GetReflectivity(hit.Group);

            if (opacity <= 0.001f || reflectivity <= 0.001f)
                continue;

            if (TraceReflectionBounce(
                hit.Point,
                dir,
                hit.ObstacleIndex,
                hit.Distance,
                maxDist,
                obstacles,
                settings,
                maxBounces: 2,
                out Vector2 firstBounceEnd,
                out ReflectedLayer primaryRefl,
                out ReflectedLayer? backgroundRefl))
            {
                resultsFrontToBack[i] = hit with
                {
                    PrimaryReflection = primaryRefl,
                    BackgroundReflection = backgroundRefl
                };

                if (!recordedMinimapBounce)
                {
                    bounceLines.Add((hit.Point, firstBounceEnd));
                    recordedMinimapBounce = true;
                }
            }
        }
    }

    private static bool TraceReflectionBounce(
        Vector2 hitPoint,
        Vector2 incidentDir,
        int hitObstacleIndex,
        float baseDistanceToMirror,
        float maxDist,
        Obstacle[] obstacles,
        SceneSettings settings,
        int maxBounces,
        out Vector2 firstBouncePoint,
        out ReflectedLayer primaryReflection,
        out ReflectedLayer? backgroundReflection)
    {
        firstBouncePoint = hitPoint;
        primaryReflection = default;
        backgroundReflection = null;

        float totalBounceDistance = 0.0f;
        Color reflectedColor = Color.Black;
        int reflectedObstacleIndex = -1;
        float reflectedHeightScale = 1.0f;

        Vector2 currentOrigin = hitPoint;
        Vector2 currentDir = incidentDir;
        int currentObstacleIdx = hitObstacleIndex;

        for (int bounce = 0; bounce < maxBounces; bounce++)
        {
            Obstacle surface = obstacles[currentObstacleIdx];
            Vector2 tangent = Vector2.Normalize(surface.B - surface.A);
            Vector2 normal = new(-tangent.Y, tangent.X);

            if (Vector2.Dot(currentDir, normal) > 0.0f)
            {
                normal = -normal;
            }

            Vector2 reflectedDir = Vector2.Normalize(currentDir - 2.0f * Vector2.Dot(currentDir, normal) * normal);
            Vector2 rayStart = currentOrigin + normal * 0.25f;
            Vector2 rayEnd = rayStart + reflectedDir * maxDist;

            float minDist = maxDist;
            int bestIdx = -1;
            Vector2 bestPoint = Vector2.Zero;

            float minBehindDist = maxDist;
            int bestBehindIdx = -1;

            for (int i = 0; i < obstacles.Length; i++)
            {
                if (i == currentObstacleIdx)
                    continue;

                Obstacle obs = obstacles[i];
                if (settings.GetOpacity(obs.Group) <= 0.01f)
                    continue;

                Vector2 pt = Vector2.Zero;
                if (Raylib.CheckCollisionLines(rayStart, rayEnd, obs.A, obs.B, ref pt))
                {
                    float d = Vector2.Distance(rayStart, pt);
                    if (d < minDist)
                    {
                        minDist = d;
                        bestIdx = i;
                        bestPoint = pt;
                    }

                    if (obs.Group != ObstacleGroup.Player && d < minBehindDist)
                    {
                        minBehindDist = d;
                        bestBehindIdx = i;
                    }
                }
            }

            if (bestIdx < 0)
                break;

            Obstacle hitObs = obstacles[bestIdx];

            if (bounce == 0)
            {
                firstBouncePoint = bestPoint;
                reflectedObstacleIndex = bestIdx;
                reflectedHeightScale = hitObs.HeightScale;
                reflectedColor = hitObs.Color;

                if (hitObs.HeightScale < 1.0f && bestBehindIdx >= 0)
                {
                    Obstacle behindObs = obstacles[bestBehindIdx];
                    backgroundReflection = new ReflectedLayer(
                        baseDistanceToMirror + minBehindDist,
                        behindObs.Color,
                        bestBehindIdx,
                        behindObs.HeightScale);
                }
            }
            else
            {
                float blend = settings.GetReflectivity(hitObs.Group) * 0.5f;
                reflectedColor = new Color(
                    (byte)Math.Clamp(reflectedColor.R * (1.0f - blend) + hitObs.Color.R * blend, 0.0f, 255.0f),
                    (byte)Math.Clamp(reflectedColor.G * (1.0f - blend) + hitObs.Color.G * blend, 0.0f, 255.0f),
                    (byte)Math.Clamp(reflectedColor.B * (1.0f - blend) + hitObs.Color.B * blend, 0.0f, 255.0f),
                    (byte)255);
            }

            totalBounceDistance += minDist;
            float nextReflectivity = settings.GetReflectivity(hitObs.Group);
            if (nextReflectivity <= 0.01f)
                break;

            currentOrigin = bestPoint;
            currentDir = reflectedDir;
            currentObstacleIdx = bestIdx;
        }

        if (reflectedObstacleIndex < 0)
            return false;

        primaryReflection = new ReflectedLayer(
            baseDistanceToMirror + totalBounceDistance,
            reflectedColor,
            reflectedObstacleIndex,
            reflectedHeightScale);
        return true;
    }
}
