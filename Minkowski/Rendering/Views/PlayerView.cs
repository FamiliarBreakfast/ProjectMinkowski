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
	
	public PlayerView(Ship ship)
	{
		Ship = ship;
		Viewport = new Viewport(RenderManager.GetHardcodedViewport(ship.Id, Config.Game.GraphicsDevice));
		Camera = new RotatableCamera2D(Config.Game.GraphicsDevice);
	}
	
	public void Render(SpriteBatch batch)
	{
		var effect = GameResources.BasicEffect!;
		var graphics = Config.Game.GraphicsDevice;
		
		var originalViewport = graphics.Viewport;
		
		Viewport.Bounds = RenderManager.GetHardcodedViewport(Ship.Id, Config.Game.GraphicsDevice);
		
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
			-graphics.Viewport.Width / 2f, graphics.Viewport.Width / 2f,   // left, right
			graphics.Viewport.Height / 2f, -graphics.Viewport.Height / 2f, // top, bottom
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
                new VertexPositionColor(new Vector3(-graphics.Viewport.Width/2+1, -graphics.Viewport.Height/2-1, 0), //top left
                    borderColor),
                new VertexPositionColor(new Vector3(-graphics.Viewport.Width/2+1, graphics.Viewport.Height/2-1, 0), //bottom left
                    borderColor),
                new VertexPositionColor(new Vector3(graphics.Viewport.Width/2, graphics.Viewport.Height/2-1, 0), //bottom right
                    borderColor),
                new VertexPositionColor(new Vector3(graphics.Viewport.Width/2, -graphics.Viewport.Height/2-1, 0), //top right
                    borderColor)
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
}