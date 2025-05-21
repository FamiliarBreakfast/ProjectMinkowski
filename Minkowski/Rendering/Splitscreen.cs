using ProjectMinkowski.Entities;

namespace ProjectMinkowski.Rendering.SplitScreen;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMinkowski;
using ProjectMinkowski.Entities;
using FontStashSharp;

public class SplitScreenRenderer {
    private readonly GraphicsDevice _graphics;

    public SplitScreenRenderer(GraphicsDevice graphics) {
        _graphics = graphics;
    }

    public void RenderAllPlayers(SpriteBatch batch) {
        var originalViewport = _graphics.Viewport;
        var players = PlayerManager.Players;

        for (int i = 0; i < players.Count; i++) {
            var viewport = GetHardcodedViewport(i, players.Count, _graphics);
            _graphics.Viewport = new Viewport(viewport);

            //CenterTransformedPositions(players[i], viewport);
            RenderPlayerView(players[i], batch, viewport);
        }

        _graphics.Viewport = originalViewport;
    }

    private void RenderPlayerView(Player player, SpriteBatch batch, Rectangle viewport) {
        var effect = GameResources.BasicEffect!;
        var graphics = _graphics;

        // --- Geometry phase ---
        graphics.RasterizerState = RasterizerState.CullNone;

        effect.Projection = Matrix.CreateOrthographicOffCenter(0, graphics.Viewport.Width,
            graphics.Viewport.Height, 0, 0, 1);
        effect.View = Matrix.Identity;
        effect.World = Matrix.CreateTranslation(
            graphics.Viewport.Width / 2f,
            graphics.Viewport.Height / 2f,
            0
        );


        batch.End();
        
        foreach (var entity in RenderableEntity.Instances) {
            entity.VertexDraw(graphics, effect, player, viewport);
        }
        
        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
        {
            foreach (VertexPositionColor[] shape in player.Shapes)
            {
                pass.Apply();
                graphics.DrawUserPrimitives(
                    PrimitiveType.LineStrip, // or LineList
                    shape,
                    0,
                    shape.Length - 1 // LineStrip: count is N-1 segments
                );
            }
            player.Shapes.Clear();
        }
        
        batch.Begin(samplerState: SamplerState.PointClamp);
        batch.DrawString(GameResources.DefaultFont, "Player " + player.Id, new Vector2(10, 10), Color.White);
        Player.DrawHud(batch, player);
        foreach (var entity in RenderableEntity.Instances) {
            entity.Draw(batch, player);
        }
        batch.End();
        batch.Begin();
    }

    // private void CenterTransformedPositions(Player player, Rectangle viewport) {
    //     float halfWidth = viewport.Width / 2f;
    //     float halfHeight = viewport.Height / 2f;
    //
    //     player.RenderedPositions.Clear();
    //
    //     foreach (var (entity, localPos) in player.TransformedPositions)
    //     {
    //         var screenX = (float)localPos.X;
    //         var screenY = (float)localPos.Y;
    //         player.RenderedPositions[entity] = new Vector2(screenX, screenY);
    //     }
    // }
    
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
