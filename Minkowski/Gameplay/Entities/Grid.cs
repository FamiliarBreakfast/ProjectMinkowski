using Clipper2Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Minkowski.Gameplay.Relativity;
using Minkowski.Rendering;

namespace Minkowski.Gameplay.Entities;

public class GridPoint : WorldlineEntity
{
    private static readonly PathD PointShape = new PathD
    {
        new PointD(-1, -1),
        new PointD(1, -1),
        new PointD(1, 1),
        new PointD(-1, 1)
    };

    public GridPoint(MinkowskiVector absolutePosition)
    {
        Origin = absolutePosition;
        Worldline = new Worldline();
        Polygon = PointShape;
        Radius = 5;
        Origin.T = -1000;
    }

    public override void Update(float deltaTime)
    {
        bool inRange = false;
        foreach (Ship ship in PlayerManager.Ships)
        {
            if (Vector2.DistanceSquared(ship.Origin.ToVector2(), Origin.ToVector2()) <
                Math.Pow(Config.GridLoadRadius * Config.GridSpacing, 2))
            {
                inRange = true;
                break;
            }
        }

        if (!inRange)
        {
            GridManager.Points.Remove(Origin.ToVector2());
            Despawn();
            return;
        }

        Origin.T += deltaTime;
        Worldline.AddEvent(this);
    }

    public override void RelativityUpdate(float deltaTime, Ship ship)
    { }

    public override void Draw(SpriteBatch spriteBatch, Ship ship)
    { }

    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Ship ship)
    {
        if (Worldline.HasVisibleEvent(ship.Origin))
        {
            Vector2 position = Worldline.GetVisibleVariable<Vector2>(ship.Origin, "Position", interpolate: true);
            Vector2 velocity = Worldline.GetVisibleVariable<Vector2>(ship.Origin, "Velocity", interpolate: true);

            Vector2 relativeVelocity = ship.Frame.LorentzTransformVelocity(velocity);

            Color color = Config.DopplerEffect switch
            {
                true => ColorHelper.DopplerShift(Color.Gray, relativeVelocity),
                _ => Color.Gray
            };

            var vertices =
                Transformations.ToVertexArray(
                    FrameOfReference.ApplyTerrelPenroseEffect(
                        Transformations.Translate(Polygon, position.X, position.Y),
                        position, relativeVelocity, ship.Position),
                    color);

            ship.Shapes.Add(vertices);
        }
    }
}

public static class GridManager
{
    public static HashSet<Vector2> Points { get; } = new HashSet<Vector2>();

    public static void UpdatePlayer(Ship ship, int gridRadius, int gridSpacing)
    {
        int snappedX = (int)Math.Round(ship.Origin.X / gridSpacing) * gridSpacing;
        int snappedY = (int)Math.Round(ship.Origin.Y / gridSpacing) * gridSpacing;
        Vector2 origin = new Vector2(snappedX, snappedY);

        for (int dx = -gridRadius; dx <= gridRadius; dx++)
        {
            for (int dy = -gridRadius; dy <= gridRadius; dy++)
            {
                Vector2 spawnPos = origin + new Vector2(dx * gridSpacing, dy * gridSpacing);
                GridPoint? point = TrySpawnPoint(spawnPos);
                if (point != null)
                {
                    EntityManager.Spawn(point);
                }
            }
        }
    }

    private static GridPoint? TrySpawnPoint(Vector2 position)
    {
        if (Points.Contains(position))
            return null;

        Points.Add(position);
        return new GridPoint(new MinkowskiVector(0, position.X, position.Y));
    }
}