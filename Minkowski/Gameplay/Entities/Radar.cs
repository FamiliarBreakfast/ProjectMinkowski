using Microsoft.Xna.Framework.Graphics;
using ProjectMinkowski.Relativity;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Entities;

public class Radar : WorldconeEntity
{
    public Radar(MinkowskiVector origin)
    {
        Worldcone = new Worldcone(origin, 1f, 1);
    }

    public override void Update(float deltaTime)
    {
        //radar stuff
    }

    public override void Draw(SpriteBatch spriteBatch, Player player) { }
    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Player player)
    {
        //make pretty things
    }
}