using Clipper2Lib;
using ProjectMinkowski.Entities;
using ProjectMinkowski.Relativity;

namespace ProjectMinkowski.Rendering; //todo: this should be elsewhere

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public abstract class RenderableEntity { //todo: refactor to worldline entity? interface??
    public static readonly List<RenderableEntity> Instances = new();
    public static void ClearAll() => Instances.Clear();
    public RenderableEntity() {
        Instances.Add(this);
    }

    public abstract void Update(float deltaTime);
    public abstract void Draw(SpriteBatch spriteBatch, Player player);
    public abstract void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Player player, Rectangle viewport);
}

public abstract class WorldlineEntity : RenderableEntity
{
    public static readonly List<WorldlineEntity> Instances = new();
    public static void ClearAll() => Instances.Clear();
    public WorldlineEntity() {
        Instances.Add(this);
    }
    
    public void Remove()
    {
        RenderableEntity.Instances.Remove(this);
        Instances.Remove(this);
    }

    public Worldline Worldline;
    public MinkowskiVector Origin; //origin is current position, worldline tracks previous positions
    public PathD Polygon;
    public int Radius = 20;
}

public abstract class TracerEntity : RenderableEntity
{
    public static readonly List<TracerEntity> Instances = new();
    public static void ClearAll() => Instances.Clear();

    public TracerEntity()
    {
        Instances.Add(this);
    }

    public void Remove()
    {
        RenderableEntity.Instances.Remove(this);
        Instances.Remove(this);
    }

    public Line Line;
}

public abstract class WorldconeEntity : RenderableEntity
{
    public static readonly List<WorldconeEntity> Instances = new();
    public static void ClearAll() => Instances.Clear();
    public WorldconeEntity() {
        Instances.Add(this);
    }

    public Worldcone Worldcone;
    //public MinkowskiVector GetOrigin();
}