using Minkowski.Gameplay.Entities;
using Minkowski.Gameplay.Relativity;

namespace Minkowski.Gameplay;

public static class PlayerManager {
    public static void Initialize() {
        new Ship(new MinkowskiVector(0, 0, 0));
    }
}