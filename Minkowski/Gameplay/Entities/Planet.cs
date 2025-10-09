using Clipper2Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Minkowski;
using Minkowski.Gameplay.Entities;
using Minkowski.Gameplay.Relativity;
using Minkowski.Rendering;

namespace Minowski.Gameplay.Entities;

public class Planet : MotileEntity
{
	public int Radius;
	
	public Planet(int radius, int mass, MinkowskiVector origin)
	{
		Worldline = new Worldline();
		Radius = radius;
		Mass = mass;
		Origin = origin;

		Polygon = CreateCircularPolygon(radius, 200);
		
		Origin.T = -1000;
		Worldline.AddEvent(this);
	}
	
	public PathD CreateCircularPolygon(double radius, double segmentLength)
	{
		// Calculate the number of segments needed
		double circumference = 2 * Math.PI * radius;
		int segmentCount = (int)Math.Ceiling(circumference / segmentLength);
    
		// Ensure we have at least 3 segments to form a polygon
		if (segmentCount < 3)
			segmentCount = 3;
    
		// Calculate the angle between each point
		double angleStep = 2 * Math.PI / segmentCount;
    
		// Create the polygon
		PathD polygon = new PathD();
    
		for (int i = 0; i < segmentCount; i++)
		{
			double angle = i * angleStep;
			double x = radius * Math.Cos(angle);
			double y = radius * Math.Sin(angle);
        
			polygon.Add(new PointD(x, y));
		}
    
		return polygon;
	}

	public override void Update(float deltaTime)
	{
		Origin.T += deltaTime;
		Worldline.AddEvent(this);
	}

	public override void RelativityUpdate(float deltaTime, Ship ship)
	{ }

	public override void Draw(SpriteBatch spriteBatch, Ship ship)
	{ }

	public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Ship ship)
	{
		//get distance from core
		//find section of crust visible at distance
		//render section
		if (Worldline.HasVisibleEvent(ship.Origin))
		{
			Vector2 position = Worldline.GetVisibleVariable<Vector2>(ship.Origin, "Position", interpolate: true);
			Vector2 velocity = Worldline.GetVisibleVariable<Vector2>(ship.Origin, "Velocity", interpolate: true);
			float rotation = Worldline.GetVisibleVariable<float>(ship.Origin, "Rotation", interpolate: true);

			Vector2 relativeVelocity = ship.Frame.LorentzTransformVelocity(velocity);

			Color color = Config.DopplerEffect switch
			{
				true => ColorHelper.DopplerShift(Color.Lime, relativeVelocity),
				_ => Color.White
			};
            
			var vertices =
				Transformations.ToVertexArray(
					FrameOfReference.ApplyTerrelPenroseEffect(
						Transformations.Translate(
							Transformations.Rotate(Polygon, rotation),
							position.X, position.Y),
						position, relativeVelocity, ship.Position),
					color);

			ship.Shapes.Add(vertices);
		}
	}
}