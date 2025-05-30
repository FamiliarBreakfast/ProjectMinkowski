using MonoGame.Extended;

namespace ProjectMinkowski.Rendering.SplitScreen;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMinkowski;
using Entities;
using FontStashSharp;

public class SplitScreenRenderer {
    private readonly GraphicsDevice _graphics;
    private readonly List<OrthographicCamera> _cameras = new();

    public SplitScreenRenderer(GraphicsDevice graphics) {
        _graphics = graphics;
        for (int i = 0; i < PlayerManager.Count; i++) { //todo: this should be dynamic based on player count
            _cameras.Add(new OrthographicCamera(_graphics));
        }
    }

    public void RenderAllPlayers(SpriteBatch batch) {
        var originalViewport = _graphics.Viewport;
        var players = PlayerManager.Ships;

        for (int i = 0; i < players.Count; i++) {
            var viewportRect = GetHardcodedViewport(i, players.Count, _graphics);
            var viewport = new Viewport(viewportRect);
            _graphics.Viewport = viewport;

            // Update camera for this viewport
            var camera = _cameras[i];
            // You may need to set camera.Limits or attach a ViewportAdapter
            camera.Position = players[i].Origin.ToVector2();  // Assuming your Ship has a Vector2 Position2D
            camera.Zoom = 1.0f; // or whatever zoom level you want

            RenderPlayerView(players[i], batch, camera);
        }

        _graphics.Viewport = originalViewport;
    }


    private void RenderPlayerView(Ship ship, SpriteBatch batch, OrthographicCamera camera)
    {
        var effect = GameResources.BasicEffect!;
        var graphics = _graphics;

        graphics.RasterizerState = RasterizerState.CullNone;

        // Use camera for View & Projection
        effect.Projection = Matrix.CreateOrthographicOffCenter( //todo: replace with own camera matrix that supports rotation
            -_graphics.Viewport.Height / 2f, _graphics.Viewport.Width / 2f,
            _graphics.Viewport.Height / 2f, -_graphics.Viewport.Width / 2f, // note: Y axis may be flipped, adjust if needed
            0, 1
        );
        effect.View = camera.GetViewMatrix();             // Use camera's view
        effect.World = Matrix.Identity;                   // Your world matrix

        batch.End();

        foreach (var entity in EntityManager.Entities)
            entity.VertexDraw(graphics, effect, ship);

        graphics.BlendState = BlendState.AlphaBlend;
        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
        {
            foreach (VertexPositionColor[] shape in ship.Shapes)
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
            ship.Shapes.Clear();
        }

        // SpriteBatch Begin with camera transform!
        batch.Begin(
            transformMatrix: camera.GetViewMatrix(),
            samplerState: SamplerState.PointClamp
        );
        
        foreach (var entity in EntityManager.Entities) {
            entity.Draw(batch, ship);
        }

        batch.End();
        batch.Begin();
        ship.DrawHud(batch);
        batch.End();
        batch.Begin();
    }

    private static Rectangle GetHardcodedViewport(int index, int count, GraphicsDevice gd) {
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

