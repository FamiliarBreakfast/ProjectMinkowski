using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    public FrameOfReference ReferenceFrame { get; }

    public Ship(MinkowskiVector absolutePosition, Player player) {
        AbsolutePosition = absolutePosition;
        Color = PlayerColorGenerator.GetColorFromID(player.Id);
        ReferenceFrame = player.ViewFrame;
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
        AbsolutePosition.X += Velocity.X * deltaTime;
        AbsolutePosition.Y += Velocity.Y * deltaTime;
        
        ReferenceFrame.Origin.X = AbsolutePosition.X;
        ReferenceFrame.Origin.Y = AbsolutePosition.Y;
        AbsolutePosition.T += deltaTime;
        
        Worldline.AddEvent(new WorldlineEvent((float)AbsolutePosition.T, new Vector2((float)AbsolutePosition.X, (float)AbsolutePosition.Y), Rotation, Velocity));
    }

    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Player player) {
        if (!player.VisibleRotations.TryGetValue(this, out float rotation))
            rotation = Rotation; // fallback if not available
        
        float size = 20f;
        if (player.RenderedPositions.ContainsKey(this))
        {
            Vector2 tip = player.RenderedPositions[this] + new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation)) * size;
            Vector2 left = player.RenderedPositions[this] + new Vector2((float)Math.Cos(rotation + 2.5f), (float)Math.Sin(rotation + 2.5f)) * size * 0.6f;
            Vector2 right = player.RenderedPositions[this] + new Vector2((float)Math.Cos(rotation - 2.5f), (float)Math.Sin(rotation - 2.5f)) * size * 0.6f;

            var vertices = new VertexPositionColor[] {
                new(new Vector3(tip, 0), Color),
                new(new Vector3(left, 0), Color),
                new(new Vector3(right, 0), Color)
            };

            foreach (var pass in effect.CurrentTechnique.Passes) {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 1);
            }
        }
    }
}