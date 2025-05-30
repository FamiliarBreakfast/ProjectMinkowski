using Clipper2Lib;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMinkowski.Relativity;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Entities;

public class Ship : WorldlineEntity
{
    public List<VertexPositionColor[]> Shapes = new();
    
    public int Id;
    
    [Worldline] public float Rotation; //todo: cleanup variables
    [Worldline] public Vector2 Velocity;

    [Worldline] public int Health = 100;
    
    public float ThrustPower = 15f;
    public float StrafePower = 6f;
    public float RotationSpeed = 3f;
    
    public Color Color;
    public FrameOfReference Frame;

    [Worldline] public byte Flags = 0;

    public List<int> AttackHash = new(); //used to track attacks, so we don't double-count them

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
    //batch.DrawString(GameResources.DefaultFont, "Player " + ship.Id, new Vector2(10, 10), Color.White);
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
        Rotation += rotateInput * RotationSpeed * dt;

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
        Frame.Lightcone.Apex.X = Origin.X;
        Frame.Lightcone.Apex.Y = Origin.Y;
        Frame.Lightcone.Apex.T = Origin.T;
        Frame.Velocity = Velocity;
        
        Origin.X += Velocity.X * deltaTime;
        Origin.Y += Velocity.Y * deltaTime;
        Origin.T += deltaTime; //ensure we proceed forward through time. generally considered a good thing
        
        Worldline.AddEvent(this);
        
        Flags = 0b0;
        //Console.WriteLine($"[Ship {GetHashCode()}] added event: Vel = {Velocity.Length():0.000} | T = {AbsolutePosition.T:0.000}");
    }
    
    public override void RelativityUpdate(float deltaTime, Ship ship)
    { }

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
