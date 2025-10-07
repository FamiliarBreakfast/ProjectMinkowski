using Clipper2Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Minkowski.Gameplay.Relativity;
using Minkowski.Rendering;

namespace Minkowski.Gameplay.Entities;

public class Mine : MotileEntity
{
    public Ship Ship;
    private float _rotationSpeed;
    [Worldline] public byte Flags = 0;
    
    private static int _DetonationTimerMax = 3;
    [Worldline] public float _DetonationTimer = _DetonationTimerMax;
    
    private static int _FadeTimerMax = 5;
    [Worldline] public float _FadeTimer = _DetonationTimerMax;
    
    public Mine(MinkowskiVector origin, Ship ship, float rotationSpeed, Vector2 velocity)
    {
        Ship = ship;
        Origin = origin;
        _rotationSpeed = rotationSpeed;
        Rotation = 0f;
        Velocity = velocity;
        Mass = 1;
        Worldline = new Worldline();
        
        Polygon = new PathD //unit octagon
        {
            new PointD( 10.000,  0.0000),
            new PointD( 07.071,  07.071),
            new PointD( 00.000,  10.000),
            new PointD(-07.071,  07.071),
            new PointD(-10.000,  00.000),
            new PointD(-07.071, -07.071),
            new PointD( 00.000, -10.000),
            new PointD( 07.071, -07.071)
        };
    }

    public override void Update(float deltaTime)
    {
        ApplyMovement(deltaTime);
        //foreach player if all see flags == 1 then despawn
        int i = 0;
        foreach (Ship ship in PlayerManager.Ships)
        {
            if (Worldline.GetVisibleVariable<float>(ship.Origin, "_FadeTimer", interpolate: false) >= 0)
            {
                i++;
            }
        }
        if (i == 0)
        {
            Despawn();
        }
        
        Rotation += _rotationSpeed * deltaTime;
        // Origin.T += deltaTime;
        // Origin.X += Velocity.X * deltaTime;
        // Origin.Y += Velocity.Y * deltaTime;
        
        _DetonationTimer -= deltaTime; // Convert deltaTime to milliseconds
        if (Flags == 1)
        {
            _FadeTimer -= deltaTime;
        }
        
        Worldline.AddEvent(this);
        
        if (_DetonationTimer <= 0 && Flags == 0)
        {
            Flags = 1;
            new Shockwave(Origin.Clone());
            Sound.Synths.Add(new FunctionSynth(
                t => (float)(Math.Sin(-t) * 144f + 100f),
                44100f,
                0.5f
            ));
        }
    }

    public override void RelativityUpdate(float deltaTime, Ship ship)
    { }
    
    public override void Draw(SpriteBatch spriteBatch, Ship ship)
    { }

    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Ship ship)
    {
        if (Worldline.HasVisibleEvent(ship.Origin)) //todo: cleanup
        {
            byte flags = Worldline.GetVisibleVariable<byte>(ship.Origin, "Flags", interpolate: false);
            if (flags == 0)
            {
                Vector2 position = Worldline.GetVisibleVariable<Vector2>(ship.Origin, "Position", interpolate: true);
                Vector2 velocity = Worldline.GetVisibleVariable<Vector2>(ship.Origin, "Velocity", interpolate: true);
                float rotation = Worldline.GetVisibleVariable<float>(ship.Origin, "Rotation", interpolate: true);
                float t = Worldline.GetVisibleVariable<float>(ship.Origin, "_DetonationTimer", interpolate: true) / _DetonationTimerMax;

                Vector2 relativeVelocity = ship.Frame.LorentzTransformVelocity(velocity);

                Color color = Config.DopplerEffect switch
                {
                    true => ColorHelper.DopplerShift(Ship.Color, relativeVelocity),
                    _ => Ship.Color
                };
                
                var vertices =
                    Transformations.ToVertexArray(
                        FrameOfReference.ApplyTerrelPenroseEffect(
                            Transformations.Translate(
                                Transformations.Rotate(Polygon, rotation),
                                position.X, position.Y),
                            position, relativeVelocity, ship.Position),
                        Color.Lerp(color, Color.White, t));

                ship.Shapes.Add(vertices);
            } else
            {
                // Draw explosion effect
                Vector2 position = Worldline.GetVisibleVariable<Vector2>(ship.Origin, "Position", interpolate: true);
                Vector2 velocity = Worldline.GetVisibleVariable<Vector2>(ship.Origin, "Velocity", interpolate: true);
                float t = Worldline.GetVisibleVariable<float>(ship.Origin, "_FadeTimer", interpolate: true) / _FadeTimerMax;
                
                Vector2 relativeVelocity = ship.Frame.LorentzTransformVelocity(velocity);
                
                Color color = Config.DopplerEffect switch
                {
                    true => ColorHelper.DopplerShift(Ship.Color, relativeVelocity),
                    _ => Ship.Color
                };
                
                var vertices =
                    Transformations.ToVertexArray(
                        FrameOfReference.ApplyTerrelPenroseEffect(
                            Transformations.Translate(
                                Transformations.Scale(Polygon, Shockwave.Radius / 10f),
                                position.X, position.Y),
                            position, relativeVelocity, ship.Position),
                        Color.Lerp(Color.Transparent, color, t));

                ship.Shapes.Add(vertices);
            }
        }
    }
}