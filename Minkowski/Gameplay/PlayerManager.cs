using Microsoft.Xna.Framework;
using ProjectMinkowski;
using ProjectMinkowski.Entities;
using ProjectMinkowski.Relativity;

public static class PlayerManager {
    public static List<Ship> Ships = new();
    public static int Count => Ships.Count;
    public static void InitializeLocalPlayers(int count) {
        for (int i = 0; i < count; i++) {
            Ships.Add(new Ship(new MinkowskiVector(0, 25 * i, 0), i));
        }
    }
    
    public static Ship? GetShipById(int id) =>
        Ships.FirstOrDefault(p => p.Id == id);
}