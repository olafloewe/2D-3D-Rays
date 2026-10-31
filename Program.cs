using System.Numerics;
using System.Runtime.CompilerServices;
using Raylib_cs;

namespace Rays;

internal class Program{
    public static void Main(string[] args){
        const int screenWidth = 800;
        const int screenHeight = 600;
        const int margin = 12;
     
        // initialize window   
        Raylib.InitWindow(screenWidth, screenHeight, "2D 3D Rays");
        Raylib.SetTargetFPS(120);

        // arena boundaries
        Vector2 tL = new Vector2(margin, margin);
        Vector2 tR = new Vector2(screenWidth -margin, margin);
        Vector2 bL = new Vector2(margin, screenHeight - margin);
        Vector2 bR = new Vector2(screenWidth - margin, screenHeight - margin); 

        // wall
        Vector2 w1 = new Vector2(screenWidth * 0.5f, screenHeight * 0.25f); 
        Vector2 w2 = new Vector2(screenWidth * 0.75f, screenHeight * 0.5f); 

        // player state
        Vector2 playerPos = new Vector2(screenWidth / 2f, screenHeight / 2f);
        float playerSpeed = 200.0f;
        float playerRadius = 10.0f;

        // main loop
        while (!Raylib.WindowShouldClose()){
            // update player movement
            float dt = Raylib.GetFrameTime();
            Vector2 moveDir = Vector2.Zero;

            // key inputs
            if (Raylib.IsKeyDown(KeyboardKey.W)) moveDir.Y -= 1;
            if (Raylib.IsKeyDown(KeyboardKey.S)) moveDir.Y += 1;
            if (Raylib.IsKeyDown(KeyboardKey.A)) moveDir.X -= 1;
            if (Raylib.IsKeyDown(KeyboardKey.D)) moveDir.X += 1;

            // move if needed
            if (moveDir != Vector2.Zero){
                moveDir = Vector2.Normalize(moveDir);
                // take "step"
                playerPos += moveDir * playerSpeed * dt;
            }

            // clamp player inside the arena boundaries
            playerPos.X = Math.Clamp(playerPos.X, margin + playerRadius, screenWidth - margin - playerRadius);
            playerPos.Y = Math.Clamp(playerPos.Y, margin + playerRadius, screenHeight - margin - playerRadius);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RayWhite);

            // Raylib.DrawText("Hello, Raylib!", margin, margin, 20, Color.Black);
            Raylib.DrawFPS(screenWidth - 90, margin);

            // draw "arena"
            Raylib.DrawLineEx(tL,bL, 4.0f,Color.Red);
            Raylib.DrawLineEx(tL,tR, 4.0f,Color.Red);
            Raylib.DrawLineEx(tR,bR, 4.0f,Color.Red);
            Raylib.DrawLineEx(bL,bR, 4.0f,Color.Red);

            // draw wall
            Raylib.DrawLineEx(w1,w2, 5.0f,Color.Purple);

            // draw "player"
            Raylib.DrawCircleV(playerPos, playerRadius, Color.Green);

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
