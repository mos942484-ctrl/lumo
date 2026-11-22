#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;

public static class LumoSetup
{
    [MenuItem("Lumo/Setup/Clean Template")]
    public static void CleanTemplate()
    {
        bool r1 = AssetDatabase.DeleteAsset("Assets/Readme.asset");
        bool r2 = AssetDatabase.DeleteAsset("Assets/TutorialInfo");
        AssetDatabase.SaveAssets();
        Debug.Log("[LumoSetup] CleanTemplate readme=" + r1 + " tutorialInfo=" + r2);
    }

    /// <summary>
    /// Disables the editor's background throttling so play-mode tests run at full speed
    /// while the editor window is unfocused (automation runs without focus).
    /// </summary>
    [MenuItem("Lumo/Setup/Unthrottle Editor Loop")]
    public static void UnthrottleEditorLoop()
    {
        EditorPrefs.SetInt("InteractionMode", 1); // 1 = No Throttling
        Debug.Log("[LumoSetup] editor interaction mode set to 'No Throttling' (takes effect on next play session)");
    }

    /// <summary>Converts every downloaded art image under Assets/Resources/Art to a Sprite import (UI + gameplay sprites) and configures 9-slice borders on UI frames.</summary>
    [MenuItem("Lumo/Setup/Fix Sprite Imports")]
    public static void FixSpriteImports()
    {
        var borders = new System.Collections.Generic.Dictionary<string, Vector4>
        {
            { "ui_panel.png", new Vector4(22, 22, 22, 22) },
            { "ui_btn.png", new Vector4(16, 16, 16, 16) },
            { "ui_btn_gold.png", new Vector4(16, 16, 16, 16) },
            { "ui_bar_bg.png", new Vector4(16, 16, 16, 16) },
            { "ui_bar_fill.png", new Vector4(16, 16, 16, 16) },
        };

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Art" });
        int fixedCount = 0;
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) continue;

            string fileName = System.IO.Path.GetFileName(path);
            bool hasTargetBorder = borders.TryGetValue(fileName, out Vector4 targetBorder);
            bool needsSprite = importer.textureType != TextureImporterType.Sprite;
            bool needsBorder = hasTargetBorder && importer.spriteBorder != targetBorder;
            if (!needsSprite && !needsBorder) continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            if (hasTargetBorder) importer.spriteBorder = targetBorder;
            importer.SaveAndReimport();
            fixedCount++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[LumoSetup] sprite imports fixed: " + fixedCount + " textures updated");
    }

    /// <summary>Builds the Windows x86_64 player to ../Build/Lumo/.</summary>
    [MenuItem("Lumo/Build/Build Windows")]
    public static void BuildWindows()
    {
        var scenes = new System.Collections.Generic.List<string>();
        foreach (var s in EditorBuildSettings.scenes)
            if (s.enabled) scenes.Add(s.path);
        if (scenes.Count == 0) { Debug.LogError("[LumoBuild] no enabled scenes in build settings"); return; }

        var options = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = "D:/Unity/Lumo/Build/Lumo/Lumo.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;
        Debug.Log("[LumoBuild] result=" + summary.result + " size=" + (summary.totalSize / 1048576.0).ToString("0.0") +
                  " MB errors=" + summary.totalErrors + " warnings=" + summary.totalWarnings +
                  " output=" + options.locationPathName);
    }

    /// <summary>Creates the single runtime scene (camera + GameRoot) and registers it as the only build scene.</summary>
    [MenuItem("Lumo/Setup/Create Main Scene")]
    public static void CreateMainScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.96f, 0.93f, 0.85f, 1f);
        camGo.AddComponent<UniversalAdditionalCameraData>();

        var root = new GameObject("GameRoot");
        root.AddComponent<Lumo.UI.GameRoot>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/LumoMain.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/LumoMain.unity", true) };
        Debug.Log("[LumoSetup] LumoMain scene created and set as build scene");
    }

    [MenuItem("Lumo/Setup/Create Folders")]
    public static void CreateFolders()
    {
        string[] folders = {
            "Assets/Scripts",
            "Assets/Resources",
            "Assets/Scripts/Core",
            "Assets/Scripts/Modules",
            "Assets/Scripts/UI",
            "Assets/Resources/Data",
            "Assets/Resources/Art",
            "Assets/Resources/Art/Characters",
            "Assets/Resources/Art/Backgrounds",
            "Assets/Resources/Art/Objects",
            "Assets/Resources/Art/UI",
            "Assets/Resources/Audio",
            "Assets/Resources/Audio/Voice",
            "Assets/Resources/Audio/SFX",
            "Assets/Resources/Audio/Music",
            "Assets/Resources/Fonts",
            "Assets/Tests"
        };
        int created = 0, existed = 0;
        foreach (var f in folders)
        {
            if (AssetDatabase.IsValidFolder(f)) { existed++; continue; }
            string parent = f.Substring(0, f.LastIndexOf('/'));
            string name = f.Substring(f.LastIndexOf('/') + 1);
            AssetDatabase.CreateFolder(parent, name);
            created++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[LumoSetup] Folders created=" + created + " existed=" + existed + " total=" + folders.Length);
        foreach (var f in folders)
            Debug.Log("[LumoSetup]   " + f + " valid=" + AssetDatabase.IsValidFolder(f));
    }
}
#endif