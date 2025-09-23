using Clipper2Lib;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMinkowski;
using ProjectMinkowski.Entities;
using ProjectMinkowski.Relativity;

namespace Minowski.Gameplay;

public static class HUD
{
    public static PathD Polygon = new PathD
    {
        new PointD(6.66,0),
        new PointD(-2.22,3.33),
        new PointD(-2.22,-3.33)
    };
    
    public static void Draw(SpriteBatch spriteBatch, Ship ship)
    {
        var speed = ship.Velocity.Length();
        var fraction = speed / Config.C;

        string text = $"Player {ship.Id}    Speed: {fraction:0.00}c    Health: {ship.Health}";

        var position = new Vector2(10, 10); // top-left of player's viewport
        var font = GameResources.DefaultFont;

        spriteBatch.DrawString(font, text, position, Color.White);
    }

    public static void VertexDraw(SpriteBatch spriteBatch, Ship ship)
    {
        foreach (Ship target in PlayerManager.Ships)
        {
            float distance = Vector2.DistanceSquared(ship.Position, target.Position);
            if (distance > 5000)
            {
                Vector2 orbit = Vector2.Normalize(target.Position - ship.Position);
                float angle = MathF.Atan2(orbit.Y, orbit.X);

                float x = ship.Position.X + float.Cos(angle) * 30;
                float y = ship.Position.Y + float.Sin(angle) * 30;
                
                var vertices = Transformations.ToVertexArray(
                        Transformations.Translate(
                            Transformations.Rotate(Polygon, angle), 
                            x, y),
                    target.Color);
                
                ship.Shapes.Add(vertices);
            }
        }
    }
}

