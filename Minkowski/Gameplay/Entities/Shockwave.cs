using Microsoft.Xna.Framework.Graphics;
using Minkowski.Gameplay.Relativity;

namespace Minkowski.Gameplay.Entities;

public class Shockwave : WorldconeEntity
{
    public const int Radius = 150; // Default maximum radius of the shockwave (technically, this is the height of the shockwave cone in Minkowski space)
    private float _lifetime = 0;
    private const float MaxLifetime = Radius / Config.C + 1; // Time for cone to fully expand + buffer

    public Shockwave(MinkowskiVector origin)
    {
        Worldcone = new Worldcone(origin, 1, 1, (Radius / Config.C * 1));
    }

    public override void Update(float deltaTime)
    {
        _lifetime += deltaTime;
        if (_lifetime > MaxLifetime)
            Despawn();
    }

    public override void RelativityUpdate(float deltaTime, Ship ship)
    { }

    public override void Draw(SpriteBatch spriteBatch, Ship ship)
    { }

    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Ship ship)
    { }
}