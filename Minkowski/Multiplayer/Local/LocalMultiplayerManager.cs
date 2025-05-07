namespace ProjectMinkowski.Multiplayer.Local;

using ProjectMinkowski;
using Entities;

public static class LocalMultiplayerManager {
    public static void InitializeLocalPlayers(int count) {
        for (int i = 0; i < count; i++) {
            var player = new Player(i, $"Player {i + 1}");
            PlayerManager.AddPlayer(player);
        }
    }
}