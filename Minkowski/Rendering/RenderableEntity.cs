using Clipper2Lib;
using ProjectMinkowski.Entities;
using ProjectMinkowski.Relativity;
using System; // for Type
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Rendering; //todo: this should be elsewhere

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public abstract class RenderableEntity {
    public RenderableEntity() {
        EntityManager.Spawn(this);
    }
    public void Despawn() {
        EntityManager.Despawn(this);
    }
    public abstract void Update(float deltaTime);
    public abstract void Draw(SpriteBatch spriteBatch, Ship ship);
    public abstract void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Ship ship);
}

public abstract class WorldlineEntity : RenderableEntity
{
    public Worldline Worldline;
    public MinkowskiVector Origin; //origin is current position, worldline tracks previous positions
    public PathD Polygon;
    public int Radius = 20;
}

public abstract class TracerEntity : RenderableEntity
{
    public Line Line;
}

public abstract class WorldconeEntity : RenderableEntity
{
    public Worldcone Worldcone;
}

