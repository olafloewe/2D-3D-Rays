namespace Rays;

public sealed class SceneSettings
{
    public float WallOpacityPercent = 100.0f;
    public float WallReflectionPercent = 40.0f;

    public float ModelOpacityPercent = 100.0f;
    public float ModelReflectionPercent = 50.0f;
    public float ModelScale = 26.0f;
    public float ModelRotationDeg = 0.0f;
    public float ModelSpinSpeed = 20.0f;

    public float WallOpacity => WallOpacityPercent / 100.0f;
    public float WallReflectivity => WallReflectionPercent / 100.0f;
    public float ModelOpacity => ModelOpacityPercent / 100.0f;
    public float ModelReflectivity => ModelReflectionPercent / 100.0f;

    public float GetOpacity(ObstacleGroup group) => group switch
    {
        ObstacleGroup.Wall => WallOpacity,
        ObstacleGroup.Model => ModelOpacity,
        _ => 1.0f
    };

    public float GetReflectivity(ObstacleGroup group) => group switch
    {
        // Outer Arena walls and Player never reflect light
        ObstacleGroup.Wall => WallReflectivity,
        ObstacleGroup.Model => ModelReflectivity,
        _ => 0.0f
    };
}
