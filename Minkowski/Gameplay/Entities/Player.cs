using Clipper2Lib;
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
    public List<VertexPositionColor[]> Shapes = new();
    public Dictionary<WorldconeEntity, Arc> TransformedArcs { get; } = new();

    
    public Player(int id, string name) {
        Id = id;
        Name = name;
    }

    public void Update(float deltaTime)
    {
        Ship.Frame.Lightcone.Apex.T += deltaTime; //ensure we proceed forward through time. generally considered a good thing
    }
}