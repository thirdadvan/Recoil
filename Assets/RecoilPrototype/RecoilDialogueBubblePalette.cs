using UnityEngine;

// Fixed colors chosen from captured NPC surroundings. No runtime light/image sampling.
public static class RecoilDialogueBubblePalette
{
    public static Color NpcFill(string speaker,string sceneName)
    {
        if(speaker=="The Counter Sales")
            return Surface(sceneName.Contains("ServiceStore")?0xCDBCA9:0xBDCCC4);
        if(speaker=="The Disguised Man")return Surface(0xC3CBB6);
        if(speaker=="The Manager")
            return Surface(sceneName.Contains("ManagerOffice")?0xCAD2BC:0xB8C8BC);
        return Surface(0xC3CDC5);
    }
    // GUI tints are linear in this project's Linear color space. Keep the authored
    // hex values in display sRGB so a muted swatch doesn't render almost white.
    public static Color Surface(int rgb)
    {
        var color=RecoilPrototype.Hex(rgb);
        return QualitySettings.activeColorSpace==ColorSpace.Linear?color.linear:color;
    }
}
