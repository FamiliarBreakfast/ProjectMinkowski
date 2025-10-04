using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Minkowski.Gameplay;
using Minkowski.Gameplay.Entities;

namespace Minkowski.Rendering;

public static class RenderManager
{
	public static void Render(SpriteBatch batch)
	{
		foreach (Ship ship in PlayerManager.Ships)
		{
			ship.View.Render(batch);
		}
	}
	
	
	/// <summary>
	/// Returns the rectangle of the viewport for the given player index
	/// </summary>
	/// <param name="index">Player index</param>
	/// <param name="count">Total players</param>
	/// <param name="gd">Graphics device</param>
	/// <returns></returns>
	// TODO: Dynamic fractioning of the screen instead of a hardcoded divisioning
	public static Rectangle GetHardcodedViewport(int index, GraphicsDevice gd)
	{
		int count = PlayerManager.Count;
		int w = gd.PresentationParameters.BackBufferWidth;
		int h = gd.PresentationParameters.BackBufferHeight;

		return count switch {
			2 => index == 0
				? new Rectangle(0, 0, w / 2, h)
				: new Rectangle(w / 2, 0, w / 2, h),

			4 => index switch {
				0 => new Rectangle(0, 0, w / 2, h / 2),         // Top-left
				1 => new Rectangle(w / 2, 0, w / 2, h / 2),      // Top-right
				2 => new Rectangle(0, h / 2, w / 2, h / 2),      // Bottom-left
				3 => new Rectangle(w / 2, h / 2, w / 2, h / 2),  // Bottom-right
				_ => new Rectangle(0, 0, w, h),
			},

			_ => new Rectangle(0, 0, w, h)
		};
	}
}