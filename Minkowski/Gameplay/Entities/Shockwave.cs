using Clipper2Lib;
using Microsoft.Xna.Framework.Graphics;
using ProjectMinkowski.Relativity;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Entities;

public class Shockwave : WorldconeEntity
{
    public const int Radius = 150; // Default maximum radius of the shockwave (technically, this is the height of the shockwave cone in Minkowski space)
    
    public Shockwave(MinkowskiVector origin)
    {
        Worldcone = new Worldcone(origin, 1, 1, (Radius / Config.C * 1));
    }

    public override void Update(float deltaTime)
    { }

    public override void RelativityUpdate(float deltaTime, Ship ship)
    { }

    public override void Draw(SpriteBatch spriteBatch, Ship ship)
    { }

    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Ship ship)
    { }
}