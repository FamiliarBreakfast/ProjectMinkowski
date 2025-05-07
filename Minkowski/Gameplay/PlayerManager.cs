using Microsoft.Xna.Framework;
using ProjectMinkowski;
using ProjectMinkowski.Entities;

public static class PlayerManager {
    private static readonly List<Player> players = new();

    public static IReadOnlyList<Player> Players => players;

    public static void AddPlayer(Player player)
    {
        players.Add(player);

        // Ship owns the frame
        var ship = new Ship(new MinkowskiVector(0, 25 * player.Id, 0), player);
        //player.Ship = ship;
        //player.ViewFrame = ship.ReferenceFrame; // bind after construction
    }

    public static Player? GetPlayerById(int id) =>
        players.FirstOrDefault(p => p.Id == id);
}