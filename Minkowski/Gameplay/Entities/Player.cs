using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Entities;

using ProjectMinkowski;

public class Player {
    public static void DrawHud(SpriteBatch batch, Player player)
    {
        var ship = player.Ship;
        var speed = ship.Velocity.Length();
        var fraction = speed / Config.C;

        string text = $"Speed: {fraction:0.00}c";

        var position = new Vector2(100, 10); // top-left of player's viewport
        var font = GameResources.DefaultFont;

        batch.DrawString(font, text, position, Color.White);
    }
    public int Id { get; }
    public string Name { get; set; }
    public FrameOfReference ViewFrame { get; set; }
    public Ship Ship;
    public Dictionary<RenderableEntity, MinkowskiVector> TransformedPositions { get; } = new(); //entity positions transformed to the current players frame of reference
    public Dictionary<RenderableEntity, Vector2> RenderedPositions { get; } = new(); //transformed positions converted to vector2 rendering positions
    public Dictionary<RenderableEntity, float> VisibleRotations { get; } = new();
    public Dictionary<RenderableEntity, Vector2> VisibleVelocities { get; } = new(); //todo: fix ineffiecient use of dicts (tuple?)

    
    public Player(int id, string name) {
        Id = id;
        Name = name;
    }

    public void TransformPositions() {
        foreach (var entity in RenderableEntity.All) {
            var evt = entity.Worldline.GetVisibleFrom(ViewFrame.Origin);

            if (evt != null) {
                var minkowski = evt.Value.ToMinkowski();
                var local = ViewFrame.ToLocal(minkowski);
                TransformedPositions[entity] = local;
                VisibleRotations[entity] = evt.Value.Rotation;
                VisibleVelocities[entity] = evt.Value.Velocity;
            }
        }
    }

    public void Update(float deltaTime)
    {
        ViewFrame.Origin.T += deltaTime;
        TransformPositions();
        //process input
        //transform positions
        //render positions
    }
}