using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Entities;

public class Bullet : WorldlineEntity
{
    public override void Update(float deltaTime)
    {
    }

    public override void Draw(SpriteBatch spriteBatch, Player player)
    { }

    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Player player, Rectangle viewport)
    {
    }
}