using System.Numerics;
using Raylib_cs;

namespace Rays;

public enum ObstacleGroup
{
    Arena,
    Wall,
    Model,
    Player
}

public readonly record struct Obstacle(
    Vector2 A,
    Vector2 B,
    Color Color,
    ObstacleGroup Group,
    float HeightScale = 1.0f);

public readonly record struct ReflectedLayer(
    float TotalDistance,
    Color Color,
    int ObstacleIndex,
    float HeightScale = 1.0f);

public readonly record struct RayHit(
    Vector2 Point,
    float Distance,
    float AngleOffset,
    Color Color,
    int ObstacleIndex,
    ObstacleGroup Group,
    float HeightScale = 1.0f,
    ReflectedLayer? PrimaryReflection = null,
    ReflectedLayer? BackgroundReflection = null);
