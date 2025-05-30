using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;
using ProjectMinkowski.Entities;

namespace ProjectMinkowski.Multiplayer.Local;

public static class InputSystem {
    public static void Update(GameTime gameTime) {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        KeyboardStateExtended state = KeyboardExtended.GetState();

        foreach (var ship in PlayerManager.Ships)
        {
            float moveForward = 0;
            float moveStrafe = 0;
            float rotate = 0;
            
            switch (ship.Id)
            {
                case 0:
                    if (state.IsKeyDown(Keys.W)) moveForward += 1;
                    if (state.IsKeyDown(Keys.S)) moveForward -= 1;
                    if (state.IsKeyDown(Keys.D)) moveStrafe += 1;
                    if (state.IsKeyDown(Keys.A)) moveStrafe -= 1;
                    if (state.IsKeyDown(Keys.E)) rotate += 1;
                    if (state.IsKeyDown(Keys.Q)) rotate -= 1;
                    if (state.WasKeyPressed(Keys.Space)) //todo: IMPORTANT: make own input system, monogame extended sucks
                    {
                       var bullet = new Bullet(ship.Origin.Clone(), ship);
                       bullet.Tracers[ship] = new BulletTracer(ship, bullet.Ship.Color, ship.Origin.ToVector2(), bullet.Line.Phi);
                       ship.Flags = 0b1;
                    }
                    if (state.WasKeyPressed(Keys.Z)) //todo: IMPORTANT: make own input system, monogame extended sucks
                    {
                        var mine = new Mine(ship.Origin.Clone(), ship, ship.Rotation, ship.Velocity);
                    }
                    break;
                case 1:
                    if (state.IsKeyDown(Keys.Up)) moveForward += 1;
                    if (state.IsKeyDown(Keys.Down)) moveForward -= 1;
                    if (state.IsKeyDown(Keys.Right)) moveStrafe += 1;
                    if (state.IsKeyDown(Keys.Left)) moveStrafe -= 1;
                    if (state.IsKeyDown(Keys.OemOpenBrackets)) rotate += 1;
                    if (state.IsKeyDown(Keys.OemCloseBrackets)) rotate -= 1;
                    if (state.WasKeyPressed(Keys.RightShift))
                    {
                        var bullet = new Bullet(ship.Origin.Clone(), ship);
                        bullet.Tracers[ship] = new BulletTracer(ship, bullet.Ship.Color, ship.Origin.ToVector2(), bullet.Line.Phi);
                        ship.Flags = 0b1;
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