using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using ProjectMinkowski.Entities;
using ProjectMinkowski.Relativity;

namespace ProjectMinkowski.Multiplayer.Local;

public static class InputSystem {
    public static void Update(GameTime gameTime) {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var state = Keyboard.GetState();

        if (PlayerManager.Players.Count == 0)
            return;

        var player = PlayerManager.Players[0];
        var ship = player.Ship;

        float moveForward = 0;
        float moveStrafe = 0;
        float rotate = 0;

        if (state.IsKeyDown(Keys.W)) moveForward += 1;
        if (state.IsKeyDown(Keys.S)) moveForward -= 1;
        if (state.IsKeyDown(Keys.D)) moveStrafe += 1;
        if (state.IsKeyDown(Keys.A)) moveStrafe -= 1;
        if (state.IsKeyDown(Keys.E)) rotate += 1;
        if (state.IsKeyDown(Keys.Q)) rotate -= 1;

        if (state.IsKeyDown(Keys.K))
        {
            new Radar(player.Ship.Origin.Clone());
        }

        ship.ApplyMovement(dt, moveForward, moveStrafe, rotate);
    }
}