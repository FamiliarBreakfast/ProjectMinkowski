using Clipper2Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Minkowski.Gameplay.Relativity;
using Minkowski.Rendering;

namespace Minkowski.Gameplay.Entities.Particles;

public class Particle : WorldlineEntity
{
    private const int DecayTime = 8;
    private const int GlobalDecayTime = DecayTime; // 8 seconds max lifetime

    private float _globalDecayTimer = 0;

    public LinearWorldline LinearWorldline;
    public Vector2 Velocity;
    public Color Color;

    public Particle(MinkowskiVector origin, Vector2 velocity, float rotationSpeed, Color color, PathD? path)
    {
        if (path == null)
        {
            Polygon = new PathD
            {
                new PointD(0, 0.66),
                new PointD(-0.57, -0.33),
                new PointD(0.57, -0.33)
            };
        }

        Color = color;
        Origin = origin;
        Velocity = velocity;

        // Use analytical worldline instead of event-based sampling
        LinearWorldline = new LinearWorldline(origin, velocity);
        LinearWorldline.RotationSpeed = rotationSpeed;
    }

    public override void Update(float deltaTime)
    {
        _globalDecayTimer += deltaTime;
        if (_globalDecayTimer > GlobalDecayTime) Despawn();

        Origin.T += deltaTime;
        Origin.X += Velocity.X * deltaTime;
        Origin.Y += Velocity.Y * deltaTime;
    }

    public override void RelativityUpdate(float deltaTime, Ship ship)
    { }

    public override void Draw(SpriteBatch spriteBatch, Ship ship)
    { }

    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Ship ship)
    {
        var visiblePos = LinearWorldline.GetVisiblePosition(ship.Origin);
        if (visiblePos == null) return;

        Vector2 position = visiblePos.Value;
        float rotation = LinearWorldline.GetVisibleRotation(ship.Origin);
        float decayT = LinearWorldline.GetVisibleDecay(ship.Origin, DecayTime) / DecayTime;

        Vector2 relativeVelocity = ship.Frame.LorentzTransformVelocity(Velocity);

        Color color = Config.DopplerEffect switch
        {
            true => ColorHelper.DopplerShift(Color, relativeVelocity),
            _ => Color
        };

        var vertices =
            Transformations.ToVertexArray(
                FrameOfReference.ApplyTerrelPenroseEffect(
                    Transformations.Translate(
                        Transformations.Rotate(Polygon, rotation),
                        position.X, position.Y),
                    position, relativeVelocity, ship.Position),
                Color.Lerp(color, Color.Transparent, decayT));

        ship.Shapes.Add(vertices);
    }
}

