using ProjectMinkowski.Entities;

namespace ProjectMinkowski.Rendering;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public abstract class RenderableEntity {
    private static readonly List<RenderableEntity> _all = new();

    public static IEnumerable<RenderableEntity> All => _all;
    
    public Worldline Worldline { get; } = new();
    public MinkowskiVector AbsolutePosition { get; set; }

    public RenderableEntity() {
        _all.Add(this);
    }

    public virtual void Draw(SpriteBatch spriteBatch, Player player) { }

    public virtual void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Player player) { }

    public static void ClearAll() => _all.Clear();
    public abstract void Update(float dt);
}