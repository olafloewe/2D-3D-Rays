using System;
using System.Numerics;
using Raylib_cs;

namespace Rays;

public sealed class SliderUI
{
    private int _activeSliderId = -1;

    public void UpdateAndDraw(Rectangle minimapRect, SceneSettings settings)
    {
        float x = minimapRect.X;
        float width = minimapRect.Width;

        Rectangle wallPanelRect = new(x, minimapRect.Y + minimapRect.Height + 8, width, 98);
        Rectangle wallOpacityTrack = new(x + 12, wallPanelRect.Y + 32, width - 24, 10);
        Rectangle wallReflectionTrack = new(x + 12, wallPanelRect.Y + 76, width - 24, 10);

        Rectangle modelPanelRect = new(x, wallPanelRect.Y + wallPanelRect.Height + 10, width, 192);
        Rectangle modelOpacityTrack = new(x + 12, modelPanelRect.Y + 32, width - 24, 10);
        Rectangle modelReflectionTrack = new(x + 12, modelPanelRect.Y + 76, width - 24, 10);
        Rectangle modelScaleTrack = new(x + 12, modelPanelRect.Y + 120, width - 24, 10);
        Rectangle modelSpinTrack = new(x + 12, modelPanelRect.Y + 164, width - 24, 10);

        // Update slider interactions
        UpdateSlider(0, wallOpacityTrack, ref settings.WallOpacityPercent, 0.0f, 100.0f);
        UpdateSlider(1, wallReflectionTrack, ref settings.WallReflectionPercent, 0.0f, 100.0f);
        UpdateSlider(2, modelOpacityTrack, ref settings.ModelOpacityPercent, 0.0f, 100.0f);
        UpdateSlider(3, modelReflectionTrack, ref settings.ModelReflectionPercent, 0.0f, 100.0f);
        UpdateSlider(4, modelScaleTrack, ref settings.ModelScale, 10.0f, 55.0f);
        UpdateSlider(5, modelSpinTrack, ref settings.ModelSpinSpeed, 0.0f, 120.0f);

        // Draw Wall Sliders Panel
        DrawPanelBox(wallPanelRect);
        DrawSliderControl(wallOpacityTrack, $"Wall Visibility: {settings.WallOpacityPercent:0}%", settings.WallOpacityPercent, 0.0f, 100.0f, Color.Purple);
        DrawSliderControl(wallReflectionTrack, $"Wall Reflection: {settings.WallReflectionPercent:0}%", settings.WallReflectionPercent, 0.0f, 100.0f, new Color(210, 120, 255, 255));

        // Draw Model Sliders Panel
        DrawPanelBox(modelPanelRect);
        DrawSliderControl(modelOpacityTrack, $"Model Visibility: {settings.ModelOpacityPercent:0}%", settings.ModelOpacityPercent, 0.0f, 100.0f, new Color(0, 190, 210, 255));
        DrawSliderControl(modelReflectionTrack, $"Model Reflection: {settings.ModelReflectionPercent:0}%", settings.ModelReflectionPercent, 0.0f, 100.0f, new Color(100, 235, 255, 255));
        DrawSliderControl(modelScaleTrack, $"Model Size: {settings.ModelScale:0} px", settings.ModelScale, 10.0f, 55.0f, new Color(240, 170, 30, 255));
        DrawSliderControl(modelSpinTrack, $"Model Spin Speed: {settings.ModelSpinSpeed:0} deg/s", settings.ModelSpinSpeed, 0.0f, 120.0f, new Color(80, 200, 120, 255));
    }

    private void UpdateSlider(int id, Rectangle trackRect, ref float value, float min, float max)
    {
        Vector2 mousePos = Raylib.GetMousePosition();
        Rectangle hitBox = new(trackRect.X - 8, trackRect.Y - 10, trackRect.Width + 16, trackRect.Height + 20);

        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mousePos, hitBox))
        {
            _activeSliderId = id;
        }

        if (Raylib.IsMouseButtonReleased(MouseButton.Left) && _activeSliderId == id)
        {
            _activeSliderId = -1;
        }

        if (_activeSliderId == id && Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            float t = Math.Clamp((mousePos.X - trackRect.X) / trackRect.Width, 0.0f, 1.0f);
            value = min + t * (max - min);
        }
    }

    private static void DrawPanelBox(Rectangle panelRect)
    {
        Raylib.DrawRectangleRec(panelRect, new Color(25, 28, 34, 225));
        Raylib.DrawRectangleLinesEx(panelRect, 1.5f, new Color(90, 95, 110, 255));
    }

    private static void DrawSliderControl(Rectangle trackRect, string labelText, float value, float min, float max, Color fillColor)
    {
        Raylib.DrawText(labelText, (int)trackRect.X, (int)trackRect.Y - 22, 15, Color.RayWhite);

        Raylib.DrawRectangleRec(trackRect, new Color(60, 64, 75, 255));

        float t = Math.Clamp((value - min) / (max - min), 0.0f, 1.0f);
        Rectangle fillRect = new(trackRect.X, trackRect.Y, trackRect.Width * t, trackRect.Height);
        Raylib.DrawRectangleRec(fillRect, fillColor);

        Raylib.DrawRectangleLinesEx(trackRect, 1.0f, new Color(130, 135, 150, 255));

        Vector2 knobPos = new(trackRect.X + trackRect.Width * t, trackRect.Y + trackRect.Height * 0.5f);
        Raylib.DrawCircleV(knobPos, 7.5f, Color.RayWhite);
        Raylib.DrawCircleLines((int)knobPos.X, (int)knobPos.Y, 7.5f, Color.Black);
    }
}
