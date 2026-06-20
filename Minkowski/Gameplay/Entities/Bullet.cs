using Clipper2Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Minkowski;
using Minkowski.Gameplay;
using Minkowski.Gameplay.Entities;
using Minkowski.Gameplay.Relativity;
using Minkowski.Rendering;

namespace Minowski.Gameplay.Entities;

public class Bullet : MotileEntity
{
	public Ship Ship;

	public Bullet(MinkowskiVector origin, Ship ship, float rotationSpeed, Vector2 velocity)
	{
		Ship = ship;
		Origin = origin;
		Rotation = ship.Rotation;
		Velocity = velocity;
		Mass = 1;
		Worldline = new Worldline();

		Polygon = new PathD // simple projectile shape
		{
			new PointD(10.0, 0.0),   // front point
			new PointD(-5.0, 3.0),   // back right
			new PointD(-5.0, -3.0)   // back left
		};

		// Record spawn position immediately so observers can see the bullet at its spawn point
		Worldline.AddEvent(this);
	}

	public override void Update(float deltaTime)
	{
		ApplyMovement(deltaTime);
		Worldline.AddEvent(this);

		// Despawn if not visible to any player
		bool visibleToAnyShip = false;
		foreach (var ship in PlayerManager.Ships)
		{
			if (Worldline.HasVisibleEvent(ship.Origin))
			{
				visibleToAnyShip = true;
				break;
			}
		}

		if (!visibleToAnyShip && Worldline.Events.Count > 0)
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
		if (Worldline.HasVisibleEvent(ship.Origin))
		{
			Vector2 position = Worldline.GetVisibleVariable<Vector2>(ship.Origin, "Position", interpolate: true);
			Vector2 velocity = Worldline.GetVisibleVariable<Vector2>(ship.Origin, "Velocity", interpolate: true);
			float rotation = Worldline.GetVisibleVariable<float>(ship.Origin, "Rotation", interpolate: true);

			Vector2 relativeVelocity = ship.Frame.LorentzTransformVelocity(velocity);

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
}