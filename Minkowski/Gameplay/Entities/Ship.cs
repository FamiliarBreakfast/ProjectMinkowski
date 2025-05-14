using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMinkowski.Relativity;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Entities;

public class Ship : WorldlineEntity
{
    public float Rotation;
    //public float Acceleration;
    public Vector2 Velocity;

    public float ThrustPower = 10f;
    public float StrafePower = 3f;
    public float RotationSpeed = 2f;
    
    public Color Color;
    public FrameOfReference Frame;

    public Ship(MinkowskiVector absolutePosition, Player player) {
        Origin = absolutePosition;
        Color = PlayerColorGenerator.GetColorFromID(player.Id);
        Worldline = new Worldline();
        // FIX: Create independent frame
        Frame = new FrameOfReference(absolutePosition, Velocity / (float)Config.C);
    
        player.Ship = this;
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
    
    public override void Update(float deltaTime)
    {
        Origin.X += Velocity.X * deltaTime;
        Origin.Y += Velocity.Y * deltaTime;
        Origin.T += deltaTime; 
        
        Frame.Lightcone.Apex.X = Origin.X; //todo: royally fucked
        Frame.Lightcone.Apex.Y = Origin.Y;
        Frame.Velocity = Velocity / (float)Config.C;
        
            Worldline.AddEvent(new WorldlineEvent(
                new MinkowskiVector(Origin.T, Origin.X, Origin.Y),
                Rotation,
                Velocity
            ));
        //Console.WriteLine($"[Ship {GetHashCode()}] added event: Vel = {Velocity.Length():0.000} | T = {AbsolutePosition.T:0.000}");
    }

    public override void Draw(SpriteBatch spriteBatch, Player player) { }
    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Player player)
    {
        
        player.VisibleRotations.TryGetValue(this, out float rotation);
        
        float size = 20f;
        if (player.RenderedPositions.ContainsKey(this)) //todo: lines instead of solid
        {
            Vector2 entityVelocity = player.VisibleVelocities[this];         // in global frame
            Vector2 observerVelocity = player.Ship.Frame.Velocity * (float)Config.C;              // also in global frame

            Vector2 relativeVelocity = entityVelocity - observerVelocity;
            //Console.WriteLine($"[Player {player.Id}] sees [{this.GetHashCode()}] with rel speed: {relativeVelocity.Length()/Config.C:0.000}c");
            Vector2 tip = player.RenderedPositions[this] + new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation)) * size;
            Vector2 left = player.RenderedPositions[this] + new Vector2((float)Math.Cos(rotation + 2.5f), (float)Math.Sin(rotation + 2.5f)) * size * 0.6f;
            Vector2 right = player.RenderedPositions[this] + new Vector2((float)Math.Cos(rotation - 2.5f), (float)Math.Sin(rotation - 2.5f)) * size * 0.6f;

            Vector2[] points = new[] { tip, left, right };

            //Console.WriteLine($"[Player {player.Id}] sees [{this.GetHashCode()}]");
            //Console.WriteLine($"   Ship vel: {entityVelocity.Length()/Config.C:0.000}c");
            //Console.WriteLine($"   Frame vel: {observerVelocity.Length()/Config.C:0.000}c");
            //Console.WriteLine($"   Relative : {relativeVelocity.Length()/Config.C:0.000}c");
            
            Vector2[] transformed = FrameOfReference.ApplyLengthContractionInFrame(points, player.RenderedPositions[this], player.Ship.Frame, relativeVelocity);
            
            var vertices = new VertexPositionColor[] {
                new(new Vector3(transformed[0], 0), Color),
                new(new Vector3(transformed[1], 0), Color),
                new(new Vector3(transformed[2], 0), Color),
                new(new Vector3(transformed[0], 0), Color)
            };
        
            foreach (var pass in effect.CurrentTechnique.Passes) {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives(PrimitiveType.LineStrip, vertices, 0, 3);
            }
        }
    }
}