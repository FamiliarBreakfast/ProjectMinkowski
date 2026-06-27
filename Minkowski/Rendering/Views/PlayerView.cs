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
	private Rectangle ProjectionArea
	{
		get
		{
			var vp = ViewRectangle();
			float aspect = (float)vp.Width / vp.Height;
			int height = 2000;
			int width = (int)(height * aspect);
			return new Rectangle(-width / 2, -height / 2, width, height);
		}
	}
	
	public PlayerView(Ship ship)
	{
		Ship = ship;
		Viewport = new Viewport(ViewRectangle());
		Camera = new RotatableCamera2D(Config.Game.GraphicsDevice);
	}
	
	public void Render(SpriteBatch batch)
	{
		var effect = GameResources.BasicEffect!;
		var graphics = Config.Game.GraphicsDevice;
		
		var originalViewport = graphics.Viewport;
		
		Viewport.Bounds = ViewRectangle();
		
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
	/// Returns the rectangle of the viewport (full screen for single player)
	/// </summary>
	public static Rectangle ViewRectangle()
	{
		int w = Config.Game.GraphicsDevice.PresentationParameters.BackBufferWidth;
		int h = Config.Game.GraphicsDevice.PresentationParameters.BackBufferHeight;
		return new Rectangle(0, 0, w, h);
	}
}