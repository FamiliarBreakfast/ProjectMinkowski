using Clipper2Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Minkowski.Gameplay.Relativity;
using Minkowski.Rendering;

namespace Minkowski.Gameplay.Entities.Particles;

public class Particle : WorldlineEntity
{
    private const int DecayTime = 8;
    private const int GlobalDecayTime = DecayTime * 8; //good up to 0.99c time dilation
    
    public float _rotationSpeed;
    private float _globalDecayTimer = 0;
    
    [Worldline] public float _decayTimer = 0;
    [Worldline] public float Rotation;
    [Worldline] public Vector2 Velocity;

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
        Worldline = new Worldline();
        Color = color;
        Origin = origin;
        _rotationSpeed = rotationSpeed;
        Velocity = velocity;
    }
    
    public override void Update(float deltaTime)
    {
        _globalDecayTimer += deltaTime;
        _decayTimer += deltaTime;
        if (_decayTimer >= DecayTime) _decayTimer = DecayTime;
        if (_globalDecayTimer > GlobalDecayTime) Despawn();
        
        Rotation += _rotationSpeed * deltaTime;
        Origin.T += deltaTime;
        Origin.X += Velocity.X * deltaTime;
        Origin.Y += Velocity.Y * deltaTime;
        
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
            float rotation = Worldline.GetVisibleVariable<float>(ship.Origin, "Rotation", interpolate: true);
            float t = Worldline.GetVisibleVariable<float>(ship.Origin, "_decayTimer", interpolate: true) / DecayTime;

            Vector2 relativeVelocity = ship.Frame.LorentzTransformVelocity(velocity);

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
                    Color.Lerp(color, Color.Transparent, t));

            ship.Shapes.Add(vertices);
        }
    }
}

