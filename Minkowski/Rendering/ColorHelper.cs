using Microsoft.Xna.Framework;
using ProjectMinkowski;
using ProjectMinkowski.Relativity;

public static class ColorHelper
{
    public static Color GetColorFromID(int id)
    {
        // Use golden ratio conjugate to spread hues more uniformly
        float goldenRatioConjugate = 0.61803398875f;
        float hue = (id * goldenRatioConjugate) % 1f;

        return HsvToRgb(hue, 1f, 1f); // Moderate saturation and brightness
    }
    
    public static Color DopplerShift(Color baseColor, Vector2 relativeVelocity)
    {
        // Calculate the Doppler effect based on relative velocity
        float speed = relativeVelocity.Length();
        //float dopplerFactor = 1f - (speed / Config.C); // Config.C is the speed of light

        // Ensure the factor is within a valid range
        //dopplerFactor = MathHelper.Clamp(dopplerFactor, 0f, 1f);
        float dopplerFactor = ((float)MinkowskiVector.Gamma(speed) - 1f) / 2;

        // Convert base color to HSV
        RgbToHsv(baseColor, out float h, out float s, out float v);

        // Adjust brightness based on Doppler effect
        h += dopplerFactor;

        // Convert back to RGB
        return HsvToRgb(h, s, v);
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
    
    private static void RgbToHsv(Color color, out float h, out float s, out float v)
    {
        float r = color.R / 255f;
        float g = color.G / 255f;
        float b = color.B / 255f;

        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        v = max;

        float delta = max - min;

        if (max == 0)
        {
            s = 0;
            h = 0;
            return;
        }
        s = delta / max;

        if (delta == 0)
        {
            h = 0;
        }
        else if (max == r)
        {
            h = 60 * (((g - b) / delta) % 6);
        }
        else if (max == g)
        {
            h = 60 * (((b - r) / delta) + 2);
        }
        else
        {
            h = 60 * (((r - g) / delta) + 4);
        }
        if (h < 0) h += 360f;
    }
}