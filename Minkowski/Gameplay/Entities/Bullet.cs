using Clipper2Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Minkowski;
using Minkowski.Gameplay;
using Minkowski.Gameplay.Entities;
using Minkowski.Gameplay.Relativity;
using Minkowski.Rendering;

namespace Minowski.Gameplay.Entities;

public class Bullet : WorldlineEntity
{
	public Ship Ship;

	// Use analytical worldline instead of event-based sampling
	public LinearWorldline LinearWorldline;
	public Vector2 Velocity;
	public float Rotation;

	public Bullet(MinkowskiVector origin, Ship ship, float rotationSpeed, Vector2 velocity)
	{
		Ship = ship;
		Origin = origin;
		Rotation = ship.Rotation;
		Velocity = velocity;

		// Polygon = new PathD // simple projectile shape
		// {
		// 	new PointD(10.0, 0.0),   // front point
		// 	new PointD(-5.0, 3.0),   // back right
		// 	new PointD(-5.0, -3.0)   // back left
		// };

		Polygon = new PathD
		{
			new PointD( 0.000,  1.000),
			new PointD( 1.000,  0.000),
			new PointD( 0.000,  -1.000),
			new PointD(-1.000,  0.000)
		};

		LinearWorldline = new LinearWorldline(origin, velocity);
		LinearWorldline.Rotation = ship.Rotation;
		LinearWorldline.RotationSpeed = rotationSpeed;
	}

	public override void Update(float deltaTime)
	{
		Origin.T += deltaTime;
		Origin.X += Velocity.X * deltaTime;
		Origin.Y += Velocity.Y * deltaTime;

		// despawn after 10 lightseconds
		var player = Ship.Instance.Origin.Clone();
		double dx = Origin.X - player.X;
		double dy = Origin.Y - player.Y;
		if (dx * dx + dy * dy > (10 * Config.C) * (10 * Config.C)) //pythagorean
		{
			EntityManager.Despawn(this);
		}
	}

	public override void RelativityUpdate(float deltaTime, Ship ship)
	{ }

	public override void Draw(SpriteBatch spriteBatch, Ship ship)
	{ }

	public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Ship ship)
	{
		var visiblePos = LinearWorldline.GetVisiblePosition(ship.Origin);
		if (visiblePos == null) return;

		Vector2 position = visiblePos.Value;
		float rotation = LinearWorldline.GetVisibleRotation(ship.Origin);

		Vector2 relativeVelocity = ship.Frame.LorentzTransformVelocity(Velocity);

		Color color = Config.DopplerEffect switch
		{
			true => ColorHelper.DopplerShift(Ship.Color, relativeVelocity),
			_ => Ship.Color
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