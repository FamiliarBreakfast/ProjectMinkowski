using Clipper2Lib;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using ProjectMinkowski.Multiplayer.Local;
using ProjectMinkowski.Relativity;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Entities;

public class Ship : WorldlineEntity
{
    public int Id;
    
    public List<VertexPositionColor[]> Shapes = new();
    public List<int> AttackHash = new(); //used to track attacks, so we don't double-count them
    
    public Color Color;
    public FrameOfReference Frame;

    public float RotationSpeed => _azimuth * RotationPower;
    [Worldline] public float Rotation;
    [Worldline] public Vector2 Velocity;
    [Worldline] public int Health = 100;
    [Worldline] public byte Flags = 0;
    
    private FunctionSynth _synth;
    
    public const float ThrustPower = 25f;
    public const float StrafePower = 25f;
    public const float RotationPower = 4f;

    public int ParticleTimer = 0;
    
    [Control("Parallel")] public float _parallel; 
    [Control("Perpendicular")] public float _perpindicular;
    [Control("Azimuth")] public float _azimuth;

    [Control("Zoom")] public int _zoom;

    public Ship(MinkowskiVector absolutePosition, int id) {
        Origin = absolutePosition;
        Worldline = new Worldline();
        Frame = new FrameOfReference(absolutePosition, Velocity);
        Id = id;
        Color = ColorHelper.GetColorFromID(Id);

        Polygon = new PathD
        {
            new PointD(20,0),
            new PointD(-6.66,10),
            new PointD(-6.66,-10)
        };
    }

    [Control("Beam")]
    public void FireBeam()
    {
        Sound.Synths.Add(new FunctionSynth(
            t => (float)(1400f * Math.Exp(-9.7*t) + 20f * Math.Sin(2 * Math.PI * 18f * t)),
            44100f,
            0.3f
        ));
        
        var bullet = new Bullet(Origin.Clone(), this);
        bullet.Tracers[this] = new BulletTracer(this, bullet.Ship.Color, Origin.ToVector2(), bullet.Line.Phi);
        //Flags = 0b1;
    }

    [Control("Mine")]
    public void FireMine()
    {
        var mine = new Mine(Origin.Clone(), this, RotationSpeed, Velocity);
    }
    
    public void DrawHud(SpriteBatch batch)
    {
        var speed = Velocity.Length();
        var fraction = speed / Config.C;

        string text = $"Player {Id}    Speed: {fraction:0.00}c    Health: {Health}";

        var position = new Vector2(10, 10); // top-left of player's viewport
        var font = GameResources.DefaultFont;

        batch.DrawString(font, text, position, Color.White);
    }

    public void ApplyMovement(float dt, float forwardInput, float strafeInput, float rotateInput) {
        Rotation += rotateInput * RotationPower * dt;

        Vector2 forward = new((float)Math.Cos(Rotation), (float)Math.Sin(Rotation));
        Vector2 right = new(-forward.Y, forward.X); // perpendicular

        // Proper acceleration in ship's rest frame
        Vector2 properAccel = 
            forward * forwardInput * ThrustPower +
            right * strafeInput * StrafePower;

        // Apply relativistic correction
        float gamma = FrameOfReference.Gamma(Velocity);
        Vector2 coordAccel = properAccel / (gamma * gamma * gamma);

        Velocity += coordAccel * dt;
    }
    
    public override void Update(float deltaTime) //todo: space friction?
    {
        if (Math.Abs(_parallel) > 0.01f || Math.Abs(_perpindicular) > 0.01f)
        {
            if (ParticleTimer < 12) ParticleTimer++; else
            {
                ParticleTimer = 0;
                new Particle(Origin.Clone(), Velocity / 2, RotationSpeed, Color, null);
            }

            if ((Flags & 0b_0000_1000) == 0) //if sound bit off
            {
                Sound.Synths.Add(_synth = new FunctionSynth(
                    t => (float)(150f + 20f * Math.Sin(2 * Math.PI * 18f * t)),
                    44100f,
                    -1
                ));
            }
            
            Flags = (byte)(Flags | 0b_0000_1000); //set sound bit on
            Flags = (byte)(Flags | 0b_0000_0100); //set particle bit on
        }
        else
        {
            //Flags = (byte)(Flags & ~0b_0000_1100); //clear bits
            Flags = 0;
            Sound.Synths.Remove(_synth);
            _synth = null!;
        }
        
        // if (ParticleTimer < 12)
        // {
        //     ParticleTimer++;
        // }
        // else
        // {
        //     ParticleTimer = 0;
        //     if (Math.Abs(_parallel) > 0 || Math.Abs(_perpindicular) > 0)
        //     {
        //         new Particle(Origin.Clone(), Velocity/2, RotationSpeed, Color, null);
        //         Sound.Synths.Add(new FunctionSynth(
        //             t => (float)(150f),
        //             44100f,
        //             -1
        //         ));
        //     }
        // }

        ApplyMovement(deltaTime, _parallel, _perpindicular, _azimuth);
        
        Frame.Lightcone.Apex.X = Origin.X;
        Frame.Lightcone.Apex.Y = Origin.Y;
        Frame.Lightcone.Apex.T = Origin.T;
        Frame.Velocity = Velocity;
        
        Origin.X += Velocity.X * deltaTime;
        Origin.Y += Velocity.Y * deltaTime;
        Origin.T += deltaTime; //ensure we proceed forward through time. generally considered a good thing
        
        Worldline.AddEvent(this);
        
        //Flags = 0b0;
        //Console.WriteLine($"[Ship {GetHashCode()}] added event: Vel = {Velocity.Length():0.000} | T = {AbsolutePosition.T:0.000}");
    }
    
    public override void RelativityUpdate(float deltaTime, Ship ship) { }
    public override void Draw(SpriteBatch spriteBatch, Ship ship) { }
    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Ship ship)
    {
        if (Worldline.HasVisibleEvent(ship.Origin))
        {
            Vector2 position = Worldline.GetVisibleVariable<Vector2>(ship.Origin, "Position", interpolate: true);
            Vector2 velocity = Worldline.GetVisibleVariable<Vector2>(ship.Origin, "Velocity", interpolate: true);
            float rotation = Worldline.GetVisibleVariable<float>(ship.Origin, "Rotation", interpolate: true);

            Vector2 relativeVelocity = ship.Frame.LorentzTransformVelocity(velocity);

            Color color = Config.DopplerEffect switch
            {
                true => ColorHelper.DopplerShift(Color, relativeVelocity),
                _ => Color
            };

            var vertices =
                Transformations.ToVertexArray(
                    FrameOfReference.ApplyLengthContractionInFrame(
                        Transformations.Translate(
                            Transformations.Rotate(Polygon, rotation),
                            position.X, position.Y),
                        position, relativeVelocity),
                    color);

            ship.Shapes.Add(vertices);
        }
    }
}
