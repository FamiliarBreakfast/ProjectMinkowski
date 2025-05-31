using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class RotatableCamera2D
{
    public Vector2 Position { get; set; } = Vector2.Zero;
    public float Rotation { get; set; } = 0f; // Radians
    public float Zoom { get; set; } = 1f;     // 1.0 = normal, 2.0 = double size

    private GraphicsDevice _graphicsDevice;

    public RotatableCamera2D(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public Matrix GetViewMatrix()
    {
        var viewport = _graphicsDevice.Viewport;
        var screenCenter = new Vector2(viewport.Width * 0.5f, viewport.Height * 0.5f);

        return
            Matrix.CreateTranslation(-Position.X, -Position.Y, 0f) *
            Matrix.CreateRotationZ(Rotation) *
            Matrix.CreateScale(Zoom, Zoom, 1f);
    }

    // Converts screen (pixel) coordinates to world coordinates
    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        return Vector2.Transform(screenPosition, Matrix.Invert(GetViewMatrix()));
    }

    // Converts world coordinates to screen (pixel) coordinates
    public Vector2 WorldToScreen(Vector2 worldPosition)
    {
        return Vector2.Transform(worldPosition, GetViewMatrix());
    }
}