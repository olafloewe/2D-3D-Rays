using Raylib_cs;

namespace Rays;

internal class Program
{
    public static void Main(string[] args)
    {
        const int screenWidth = 800;
        const int screenHeight = 600;

        Raylib.InitWindow(screenWidth, screenHeight, "2D 3D Rays");
        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RayWhite);

            Raylib.DrawText("Hello, Raylib!", 12, 12, 20, Color.DarkGray);
            Raylib.DrawFPS(screenWidth - 90, 12);

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
