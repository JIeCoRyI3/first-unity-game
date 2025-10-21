using UnityEngine;

public static class PixelFontProvider
{
    private static Font cachedFont;

    public static Font Get()
    {
        if (cachedFont != null) return cachedFont;
        cachedFont = Resources.Load<Font>("Fonts/ThaleahFat");
        if (cachedFont == null)
        {
            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        return cachedFont;
    }
}
