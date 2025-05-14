using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using ProjectMinkowski.Relativity;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Entities;

using ProjectMinkowski;

//what is a player? a miserable little window through which one views the world...
//the player is the soul. the body is the ship.
public class Player { //todo: IMPORTANT! player -> viewport, ship -> player
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
    public Ship Ship;
    public Dictionary<RenderableEntity, MinkowskiVector> TransformedPositions { get; } = new(); //entity positions transformed to the current players frame of reference
    public Dictionary<RenderableEntity, Vector2> RenderedPositions { get; } = new(); //transformed positions converted to vector2 rendering positions
    public Dictionary<RenderableEntity, float> VisibleRotations { get; } = new();
    public Dictionary<RenderableEntity, Vector2> VisibleVelocities { get; } = new(); //todo: fix ineffiecient use of dicts (tuple?)
    public Dictionary<WorldconeEntity, Arc> TransformedArcs { get; } = new();

    
    public Player(int id, string name) {
        Id = id;
        Name = name;
    }

    private void IntersectLines()
    {
        //get our worldcone
        //for entity in worldlineentity instances
        //run World.Intersects(instance, worldcone)
        //if it does
        //get its minkowskivector
        //transform it
        //store it for rendering
        foreach (var entity in WorldlineEntity.Instances)
        {
            if (entity.Worldline.Events.Count > 0) {
                WorldlineEvent? evt = World.Intersects(entity.Worldline, Ship.Frame.Lightcone);
                if (evt != null)
                {
                    TransformedPositions[entity] = Ship.Frame.ToLocal(evt.Origin);
                    VisibleRotations[entity] = evt.Rotation; //todo: arbitrary event data?
                    VisibleVelocities[entity] = evt.Velocity;
                }
            }
        }
    }

    private void IntersectCones()
    {
        //get our worldcone
        //for entity in worldconeentity instances
        //run World.Intersects(worldcone, worldcone)
        //if it does
        //get its arc
        //transform it
        //store it for rendering
        foreach (var entity in WorldconeEntity.Instances)
        {
            Arc? arc = World.Intersects(entity.Worldcone, Ship.Frame.Lightcone);
            if (arc != null)
            {
                Arc transform = arc.ToLorentzTransformed(Ship.Frame);
                TransformedArcs[entity] = transform;
            }
        }
    }

    public void Update(float deltaTime)
    {
        Ship.Frame.Lightcone.Apex.T += deltaTime; //ensure we proceed forward through time. generally considered a good thing
        IntersectLines();
        IntersectCones();
        //intersectCones
        //doLorentzTransformations?
        
        //makeAsteroids;
    }
}