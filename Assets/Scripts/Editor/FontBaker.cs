#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.TextCore.Text;

public static class FontBaker
{
    /// <summary>
    /// Creates dynamic TMP font asset from the downloaded TTF so every TMP label
    /// uses the kid-friendly font by fixed filename.
    /// Batch 6 font: font_main = Baloo 2 Bold (SIL OFL 1.1 — see Fonts/OFL-Baloo2.txt).
    /// </summary>
    [MenuItem("Lumo/Setup/Bake Fonts")]
    public static void Bake()
    {
        EnsureTmpEssentials();
        BakeFont("Assets/Resources/Fonts/font_main.ttf", "Assets/Resources/Fonts/font_main.asset");
    }

    /// <summary>
    /// Deletes the baked TMP font asset and re-bakes it from the current TTF.
    /// Run once in Unity after swapping the font file so the TMP asset picks up
    /// the new glyphs and metrics.
    /// </summary>
    [MenuItem("Lumo/Setup/Rebake Fonts")]
    public static void Rebake()
    {
        AssetDatabase.DeleteAsset("Assets/Resources/Fonts/font_main.asset");
        AssetDatabase.SaveAssets();
        Bake();
    }

    /// <summary>
    /// Imports TMP Essential Resources (TMP Settings + default shader materials) from the
    /// ugui package programmatically, so CreateFontAsset works without any interactive dialog.
    /// </summary>
    static void EnsureTmpEssentials()
    {
        if (Resources.Load<TMP_Settings>("TMP Settings") != null) return;
        // remove any hand-made settings asset (we now use the official essentials import)
        AssetDatabase.DeleteAsset("Assets/Resources/TMP Settings.asset");

        const string pkg = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
        AssetDatabase.ImportPackage(pkg, false);
        AssetDatabase.Refresh();
        if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            Debug.LogError("[FontBaker] TMP Settings still missing after essentials import — bake may fail");
        else
            Debug.Log("[FontBaker] TMP Essential Resources imported");
    }

    static void BakeFont(string ttfPath, string assetPath)
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null) { Debug.Log("[FontBaker] already exists: " + assetPath); return; }

        var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (font == null) { Debug.LogError("[FontBaker] TTF not found/imported: " + ttfPath); return; }

        var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9,
            UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
            TMPro.AtlasPopulationMode.Dynamic, true);
        if (fa == null) { Debug.LogError("[FontBaker] CreateFontAsset failed for " + ttfPath); return; }

        fa.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        AssetDatabase.CreateAsset(fa, assetPath);
        if (fa.atlasTexture != null) AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
        if (fa.material != null) AssetDatabase.AddObjectToAsset(fa.material, fa);
        AssetDatabase.SaveAssets();
        Debug.Log("[FontBaker] baked " + assetPath + " (dynamic atlas)");
    }
}
#endif
