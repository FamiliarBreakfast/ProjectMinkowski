namespace ProjectMinkowski.Multiplayer.Local;

using ProjectMinkowski;
using Entities;

public static class LocalMultiplayerManager {
    public static void InitializeLocalPlayers(int count) {
        PlayerManager.AddPlayer(new Player(0, "Player 1", new FrameOfReference(new(0, 0, 0), new(0, 0))));
        PlayerManager.AddPlayer(new Player(1, "Player 2", new FrameOfReference(new(0, 50, 0), new(0, 0))));

        if (count >= 4) {
            PlayerManager.AddPlayer(new Player(2, "Player 3", new FrameOfReference(new(0, -50, 0), new(0, 0))));
            PlayerManager.AddPlayer(new Player(3, "Player 4", new FrameOfReference(new(0, 100, 0), new(0, 0))));
        }
    }
}