using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Minkowski.Gameplay;
using Minkowski.Gameplay.Entities;

namespace Minkowski.Rendering;

public class PlayerView
{
	public Ship Ship;
	public Viewport Viewport;
	public RotatableCamera2D Camera;
	private Rectangle ProjectionArea => Config.Players == 2 ? new Rectangle(-1000, -1000, 2000, 2000) : new Rectangle(-1000, -1000, 2000, 1000);
	
	public PlayerView(Ship ship)
	{
		Ship = ship;
		Viewport = new Viewport(ViewRectangle(ship.Id));
		Camera = new RotatableCamera2D(Config.Game.GraphicsDevice);
	}
	
	public void Render(SpriteBatch batch)
	{
		var effect = GameResources.BasicEffect!;
		var graphics = Config.Game.GraphicsDevice;
		
		var originalViewport = graphics.Viewport;
		
		Viewport.Bounds = ViewRectangle(Ship.Id);
		
		graphics.Viewport = Viewport;
		
		//set camera position and zoom
		Camera.Position = Ship.Origin.ToVector2();
		Camera.Zoom = 1.5f + Ship._zoom * 1f;

		//rotate camera
		if (Config.RotateWorld)
		{
			Camera.Rotation = (float)(-Ship.Rotation + -0.5 * Math.PI);
		}
		
		graphics.RasterizerState = RasterizerState.CullNone;
		
		//set projection matrix
		//transforms game coordinates to screen coordinates
		
		effect.Projection = Matrix.CreateOrthographicOffCenter(
			-ProjectionArea.Width / 2f, ProjectionArea.Width / 2f,
			ProjectionArea.Height / 2f, -ProjectionArea.Height / 2f,
			0, 1
		);
		effect.View = Camera.GetViewMatrix();
		effect.World = Matrix.Identity;
		
		
		//VERTEX RENDER BEGIN
		batch.End();
		
		foreach (var entity in EntityManager.Entities)
			entity.VertexDraw(graphics, effect, Ship);

		HUD.VertexDraw(batch, Ship);
		
		graphics.BlendState = BlendState.AlphaBlend;
        
        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
        {
            foreach (VertexPositionColor[] shape in Ship.Shapes)
            {
                pass.Apply();
                graphics.DepthStencilState = DepthStencilState.None;
                graphics.BlendState = BlendState.NonPremultiplied;
                effect.VertexColorEnabled = true;
                graphics.DrawUserPrimitives(
                    PrimitiveType.LineStrip,
                    shape,
                    0,
                    shape.Length - 1
                );
            }
            Ship.Shapes.Clear();
        }
        //VERTEX RENDER END
        
        //SPRITE RENDER BEGIN
        batch.Begin(
            transformMatrix: Camera.GetViewMatrix(),
            samplerState: SamplerState.PointClamp
        );
        
        foreach (var entity in EntityManager.Entities) {
            entity.Draw(batch, Ship);
        }

        //SPRITE RENDER END
        batch.End();

        //Do projection matrix
        effect.View = Matrix.Identity;
        
        //UNPROJECTED VERTEX RENDER BEGIN
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
        //UNPROJECTED VERTEX RENDER END

		//UNPROJECTED SPRITE RENDER BEGIN
        batch.Begin();
        HUD.Draw(batch, Ship);
        //UNPROJECTED SPRITE RENDER END
        batch.End();
        batch.Begin();
        graphics.Viewport = originalViewport;
	}
	
	/// <summary>
	/// Returns the rectangle of the viewport for the given player index
	/// </summary>
	/// <param name="index">Player index</param>
	/// <returns></returns>
	// TODO: Dynamic fractioning of the screen instead of a hardcoded divisioning
	public static Rectangle ViewRectangle(int index)
	{
		int count = PlayerManager.Count;
		int w = Config.Game.GraphicsDevice.PresentationParameters.BackBufferWidth;
		int h = Config.Game.GraphicsDevice.PresentationParameters.BackBufferHeight;

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