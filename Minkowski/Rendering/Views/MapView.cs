using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Minkowski.Rendering;

public static class MapView
{
	public static Viewport Viewport = new Viewport();
	private static Rectangle ProjectionArea = new Rectangle(-100, -100, 200, 200);
	public static void Render(SpriteBatch batch)
	{
		var effect = GameResources.BasicEffect!;
		var graphics = Config.Game.GraphicsDevice;
		
		var originalViewport = graphics.Viewport;
		
		Viewport.Bounds = ViewRectangle();
		
		graphics.Viewport = Viewport;
		graphics.RasterizerState = RasterizerState.CullNone;
		
		effect.Projection = Matrix.CreateOrthographicOffCenter(
			-ProjectionArea.Width / 2f, ProjectionArea.Width / 2f,
			ProjectionArea.Height / 2f, -ProjectionArea.Height / 2f,
			0, 1
		);
		effect.View = Matrix.Identity;
		effect.World = Matrix.Identity;
		
		batch.End();
		foreach (EffectPass pass in effect.CurrentTechnique.Passes)
		{
			pass.Apply();
            
			//Draw viewport borders
			//Todo: generalize
			Color borderColor = Color.White;
			VertexPositionColor[] border = new VertexPositionColor[]
			{
				new VertexPositionColor(new Vector3(-ProjectionArea.Width/2+1, -ProjectionArea.Height/2+1, 0), //top left
					borderColor),
				new VertexPositionColor(new Vector3(-ProjectionArea.Width/2+1,ProjectionArea.Height/2-1, 0), //bottom left
					borderColor),
				new VertexPositionColor(new Vector3(ProjectionArea.Width/2, ProjectionArea.Height/2-1, 0), //bottom right
					borderColor),
				new VertexPositionColor(new Vector3(ProjectionArea.Width/2, -ProjectionArea.Height/2+1, 0), //top right
					borderColor),
				new VertexPositionColor(new Vector3(-ProjectionArea.Width/2+1, -ProjectionArea.Height/2+1, 0), //top left
					borderColor),
			};
			graphics.DrawUserPrimitives(
				PrimitiveType.LineStrip,
				border,
				0,
				border.Length - 1
			);
		}
		batch.Begin();
		graphics.Viewport = originalViewport;
	}

	public static Rectangle ViewRectangle()
	{
		int w = Config.Game.GraphicsDevice.PresentationParameters.BackBufferWidth;
		int h = Config.Game.GraphicsDevice.PresentationParameters.BackBufferHeight;

		int squareSize = (int)(h * 0.25);
		return new Rectangle(
			(w - squareSize) / 2,  // x: center horizontally
			(h - squareSize) / 2,  // y: center vertically
			squareSize,             // width
			squareSize              // height
		);
	}
}