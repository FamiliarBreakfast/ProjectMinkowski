using Microsoft.Xna.Framework;
using ProjectMinkowski;
using ProjectMinkowski.Entities;
using ProjectMinkowski.Relativity;

public static class PlayerManager {
    private static readonly List<Player> players = new();

    public static IReadOnlyList<Player> Players => players;

    public static void AddPlayer(Player player)
    {
        players.Add(player);

        // Ship owns the frame
        var ship = new Ship(new MinkowskiVector(0, 30 * player.Id, 0), player);
        //player.Ship = ship;
        //player.ViewFrame = ship.ReferenceFrame; // bind after construction
        for (int i = 20; i < 40; i++)
        {
            new Asteroid(new MinkowskiVector(0, i * 10, i * 10));
            new Asteroid(new MinkowskiVector(0, i * -8, i * 15));
        }
    }

    public static Player? GetPlayerById(int id) =>
        players.FirstOrDefault(p => p.Id == id);
}