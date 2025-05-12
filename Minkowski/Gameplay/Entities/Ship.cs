using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMinkowski.Relativity;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Entities;

public class Ship : RenderableEntity
{
    public float Rotation;
    public float Acceleration;
    public Vector2 Velocity;

    public float ThrustPower = 10f;
    public float StrafePower = 3f;
    public float RotationSpeed = 2f;
    
    public Color Color;
    public FrameOfReference ReferenceFrame;

    public Ship(MinkowskiVector absolutePosition, Player player) {
        AbsolutePosition = absolutePosition;
        Color = PlayerColorGenerator.GetColorFromID(player.Id);
    
        // FIX: Create independent frame
        ReferenceFrame = new FrameOfReference(absolutePosition, Velocity / (float)Config.C);
    
        player.Ship = this;
        player.ViewFrame = ReferenceFrame; // safe: only now do this
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
        AbsolutePosition.X += Velocity.X * deltaTime;
        AbsolutePosition.Y += Velocity.Y * deltaTime;
        AbsolutePosition.T += deltaTime;
        
        ReferenceFrame.Lightcone.Apex.X = AbsolutePosition.X; //todo: royally fucked
        ReferenceFrame.Lightcone.Apex.Y = AbsolutePosition.Y;
        ReferenceFrame.Velocity = Velocity / (float)Config.C;
        
        
        Worldline.AddEvent(new WorldlineEvent((float)AbsolutePosition.T, new Vector2((float)AbsolutePosition.X, (float)AbsolutePosition.Y), Rotation, Velocity));
        //Console.WriteLine($"[Ship {GetHashCode()}] added event: Vel = {Velocity.Length():0.000} | T = {AbsolutePosition.T:0.000}");
    }

    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Player player)
    {
        player.VisibleRotations.TryGetValue(this, out float rotation);
        
        float size = 20f;
        if (player.RenderedPositions.ContainsKey(this)) //todo: lines instead of solid
        {
            Vector2 entityVelocity = player.VisibleVelocities[this];         // in global frame
            Vector2 observerVelocity = player.ViewFrame.Velocity * (float)Config.C;              // also in global frame

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
            
            Vector2[] transformed = FrameOfReference.ApplyLengthContractionInFrame(points, player.RenderedPositions[this], player.ViewFrame, relativeVelocity);
            
            var vertices = new VertexPositionColor[] {
                new(new Vector3(transformed[0], 0), Color),
                new(new Vector3(transformed[1], 0), Color),
                new(new Vector3(transformed[2], 0), Color)
            };
        
            foreach (var pass in effect.CurrentTechnique.Passes) {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 1);
            }
        }
    }
}