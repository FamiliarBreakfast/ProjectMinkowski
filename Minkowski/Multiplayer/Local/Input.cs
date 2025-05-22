using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using ProjectMinkowski.Entities;

namespace ProjectMinkowski.Multiplayer.Local;

public static class InputSystem {
    public static void Update(GameTime gameTime) {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var state = Keyboard.GetState();

        foreach (var player in PlayerManager.Players)
        {
            var ship = player.Ship;
            
            float moveForward = 0;
            float moveStrafe = 0;
            float rotate = 0;
            
            switch (player.Id)
            {
                case 0:
                    if (state.IsKeyDown(Keys.W)) moveForward += 1;
                    if (state.IsKeyDown(Keys.S)) moveForward -= 1;
                    if (state.IsKeyDown(Keys.D)) moveStrafe += 1;
                    if (state.IsKeyDown(Keys.A)) moveStrafe -= 1;
                    if (state.IsKeyDown(Keys.E)) rotate += 1;
                    if (state.IsKeyDown(Keys.Q)) rotate -= 1;
                    if (state.IsKeyDown(Keys.Space))
                    {
                        new Bullet(player.Ship.Origin.Clone(), player.Ship);
                    }
                    break;
                case 1:
                    if (state.IsKeyDown(Keys.Up)) moveForward += 1;
                    if (state.IsKeyDown(Keys.Down)) moveForward -= 1;
                    if (state.IsKeyDown(Keys.Right)) moveStrafe += 1;
                    if (state.IsKeyDown(Keys.Left)) moveStrafe -= 1;
                    if (state.IsKeyDown(Keys.OemOpenBrackets)) rotate += 1;
                    if (state.IsKeyDown(Keys.OemCloseBrackets)) rotate -= 1;
                    if (state.IsKeyDown(Keys.RightShift))
                    {
                        new Bullet(player.Ship.Origin.Clone(), player.Ship);
                    }
                    break;
                case 2:
                    break;
                case 3:
                    break;
            }
            ship.ApplyMovement(dt, moveForward, moveStrafe, rotate);
        }
    }
}