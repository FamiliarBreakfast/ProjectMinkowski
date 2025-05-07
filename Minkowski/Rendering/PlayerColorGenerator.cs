using Microsoft.Xna.Framework;

public static class PlayerColorGenerator
{
    public static Color GetColorFromID(int id)
    {
        // Use golden ratio conjugate to spread hues more uniformly
        float goldenRatioConjugate = 0.61803398875f;
        float hue = (id * goldenRatioConjugate) % 1f;

        return HsvToRgb(hue, 0.6f, 0.95f); // Moderate saturation and brightness
    }

    // Converts HSV [0-1] to RGB Color
    private static Color HsvToRgb(float h, float s, float v)
    {
        float r = 0, g = 0, b = 0;

        int i = (int)(h * 6f);
        float f = (h * 6f) - i;
        float p = v * (1f - s);
        float q = v * (1f - f * s);
        float t = v * (1f - (1f - f) * s);

        switch (i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
        }

        return new Color(r, g, b);
    }
}