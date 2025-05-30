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
    public abstract void RelativityUpdate(float deltaTime, Ship ship);
    public abstract void Draw(SpriteBatch spriteBatch, Ship ship);
    public abstract void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Ship ship);
}

public abstract class WorldlineEntity : RenderableEntity
{
    public Worldline Worldline;
    public MinkowskiVector Origin; //origin is current position, worldline tracks previous positions
    public PathD Polygon;
    public int Radius = 20;
    [Worldline] public Vector2 Position => Origin.ToVector2();
    
    public Dictionary<string, object> GetWorldlineData()
    {
        var dict = new Dictionary<string, object>();
        var type = GetType();
        foreach (var field in type.GetFields())
            if (Attribute.IsDefined(field, typeof(WorldlineAttribute)))
                dict[field.Name] = field.GetValue(this);
        foreach (var prop in type.GetProperties())
            if (Attribute.IsDefined(prop, typeof(WorldlineAttribute)) && prop.CanRead)
                dict[prop.Name] = prop.GetValue(this);
        return dict;
    }
}

public abstract class TracerEntity : RenderableEntity
{
    public Line Line;
}

public abstract class WorldconeEntity : RenderableEntity
{
    public Worldcone Worldcone;
}

