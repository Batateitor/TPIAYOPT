using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ProjectMobileOptimizer
{
    private const int BodyguardTextureSize = 1024;
    private const int LightmapTextureSize = 1024;
    private const string OptimizedBodyguardFolder = "Assets/BodyGuards/Textures/OptimizedMobile";

    private struct TextureConversion
    {
        public string SourcePath;
        public string TargetPath;
        public string MaterialPath;
        public string MaterialProperty;
        public TextureImporterType TextureType;
        public bool Srgb;
        public bool HasAlpha;
        public bool UseJpg;
        public int JpgQuality;
    }

    [MenuItem("Tools/Optimization/Optimize Project For Mobile")]
    public static void OptimizeFromMenu()
    {
        OptimizeProject();
    }

    public static void RunBatch()
    {
        try
        {
            OptimizeProject();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void OptimizeProject()
    {
        EnsureFolder("Assets/BodyGuards/Textures", "OptimizedMobile");

        ConvertBodyguardTextures();
        DeleteUnusedBodyguardTwoAssets();
        ConfigureLightmapImportersOnly();
        OptimizeTextureImporters();
        OptimizeModelImporters();
        OptimizeAudioImporters();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Mobile project optimization complete.");
    }

    private static void ConvertBodyguardTextures()
    {
        TextureConversion[] conversions =
        {
            Bodyguard("01", "D", "_MainTex", TextureImporterType.Default, true, false, true, 84),
            Bodyguard("01", "N", "_BumpMap", TextureImporterType.NormalMap, false, false, false, 100),
            Bodyguard("01", "Ao", "_OcclusionMap", TextureImporterType.Default, false, false, true, 72),
            Bodyguard("01", "S", "_SpecGlossMap", TextureImporterType.Default, false, true, false, 100),
            Bodyguard("03", "D", "_MainTex", TextureImporterType.Default, true, false, true, 84),
            Bodyguard("03", "N", "_BumpMap", TextureImporterType.NormalMap, false, false, false, 100),
            Bodyguard("03", "Ao", "_OcclusionMap", TextureImporterType.Default, false, false, true, 72),
            Bodyguard("03", "S", "_SpecGlossMap", TextureImporterType.Default, false, true, false, 100)
        };

        List<string> replacedSources = new List<string>();

        foreach (TextureConversion conversion in conversions)
        {
            if (!File.Exists(conversion.SourcePath))
            {
                Texture2D existingTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(conversion.TargetPath);
                AssignTextureToMaterial(conversion, existingTexture);
                continue;
            }

            Texture2D resized = LoadAndResizeTexture(conversion.SourcePath, BodyguardTextureSize, !conversion.Srgb, false);
            byte[] encoded = conversion.UseJpg ? resized.EncodeToJPG(conversion.JpgQuality) : resized.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(resized);

            File.WriteAllBytes(conversion.TargetPath, encoded);
            AssetDatabase.ImportAsset(conversion.TargetPath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureTextureImporter(conversion.TargetPath, conversion.TextureType, conversion.Srgb, conversion.HasAlpha, BodyguardTextureSize);

            Texture2D optimizedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(conversion.TargetPath);
            AssignTextureToMaterial(conversion, optimizedTexture);
            replacedSources.Add(conversion.SourcePath);
            Debug.Log($"Optimized bodyguard texture: {conversion.SourcePath} -> {conversion.TargetPath}");
        }

        foreach (string sourcePath in replacedSources)
        {
            AssetDatabase.DeleteAsset(sourcePath);
        }
    }

    private static TextureConversion Bodyguard(string number, string suffix, string materialProperty, TextureImporterType textureType, bool srgb, bool hasAlpha, bool useJpg, int jpgQuality)
    {
        string extension = useJpg ? "jpg" : "png";

        return new TextureConversion
        {
            SourcePath = $"Assets/BodyGuards/Textures/Boduguard_{number}_{suffix}.tga",
            TargetPath = $"{OptimizedBodyguardFolder}/Boduguard_{number}_{suffix}_Mobile.{extension}",
            MaterialPath = $"Assets/BodyGuards/Meshes/Materials/Boduguard_{number}_D.mat",
            MaterialProperty = materialProperty,
            TextureType = textureType,
            Srgb = srgb,
            HasAlpha = hasAlpha,
            UseJpg = useJpg,
            JpgQuality = jpgQuality
        };
    }

    private static void AssignTextureToMaterial(TextureConversion conversion, Texture2D texture)
    {
        if (texture == null)
        {
            throw new InvalidOperationException($"Optimized texture missing: {conversion.TargetPath}");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(conversion.MaterialPath);

        if (material == null)
        {
            throw new FileNotFoundException($"Material not found: {conversion.MaterialPath}");
        }

        material.SetTexture(conversion.MaterialProperty, texture);
        EditorUtility.SetDirty(material);
    }

    private static void DeleteUnusedBodyguardTwoAssets()
    {
        string[] unusedPaths =
        {
            "Assets/BodyGuards/Meshes/SkelMesh_Bodyguard_02.fbx",
            "Assets/BodyGuards/Meshes/Materials/Boduguard_02_D.mat",
            "Assets/BodyGuards/Textures/Boduguard_02_D.tga",
            "Assets/BodyGuards/Textures/Boduguard_02_N.tga",
            "Assets/BodyGuards/Textures/Boduguard_02_Ao.tga",
            "Assets/BodyGuards/Textures/Boduguard_02_S.tga"
        };

        foreach (string path in unusedPaths)
        {
            if (File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"Deleted unused mobile optimization asset: {path}");
            }
        }
    }

    private static void ConfigureLightmapImportersOnly()
    {
        string[] lightmapPaths = Directory.GetFiles("Assets/Scenes", "Lightmap-*", SearchOption.AllDirectories);
        int optimized = 0;

        foreach (string rawPath in lightmapPaths)
        {
            string path = rawPath.Replace('\\', '/');
            string extension = Path.GetExtension(path).ToLowerInvariant();

            if (extension != ".exr" && extension != ".png")
            {
                continue;
            }

            ConfigureLightmapImporter(path, LightmapTextureSize);
            optimized++;
        }

        Debug.Log($"Configured {optimized} lightmap importer(s) for mobile without rewriting baked lightmap data.");
    }

    private static Texture2D LoadAndResizeTexture(string assetPath, int maxSize, bool linear, bool hdr)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer != null)
        {
            importer.isReadable = true;
            importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, maxSize);
            importer.SaveAndReimport();
        }

        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

        if (source == null)
        {
            throw new FileNotFoundException($"Texture not found: {assetPath}");
        }

        int width = source.width;
        int height = source.height;
        int largestSide = Mathf.Max(width, height);

        if (largestSide > maxSize)
        {
            float scale = maxSize / (float)largestSide;
            width = Mathf.Max(1, Mathf.RoundToInt(width * scale));
            height = Mathf.Max(1, Mathf.RoundToInt(height * scale));
        }

        RenderTextureReadWrite readWrite = linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB;
        RenderTextureFormat renderFormat = hdr ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
        TextureFormat textureFormat = hdr ? TextureFormat.RGBAHalf : TextureFormat.RGBA32;
        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 0, renderFormat, readWrite);
        RenderTexture previous = RenderTexture.active;

        Graphics.Blit(source, renderTexture);
        RenderTexture.active = renderTexture;

        Texture2D resized = new Texture2D(width, height, textureFormat, false, linear);
        resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        resized.Apply(false, false);

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);
        return resized;
    }

    private static void ConfigureTextureImporter(string path, TextureImporterType textureType, bool srgb, bool hasAlpha, int maxSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            return;
        }

        importer.textureType = textureType;
        importer.sRGBTexture = srgb;
        importer.alphaSource = hasAlpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
        importer.mipmapEnabled = true;
        importer.streamingMipmaps = true;
        importer.maxTextureSize = maxSize;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.crunchedCompression = textureType == TextureImporterType.Default;
        importer.compressionQuality = 50;
        importer.isReadable = false;
        importer.SetPlatformTextureSettings(GetAndroidTextureSettings(maxSize, hasAlpha || textureType == TextureImporterType.NormalMap));
        importer.SaveAndReimport();
    }

    private static void ConfigureLightmapImporter(string path, int maxSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            return;
        }

        importer.maxTextureSize = maxSize;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.crunchedCompression = false;
        importer.compressionQuality = 40;
        importer.streamingMipmaps = true;
        importer.isReadable = false;
        importer.SetPlatformTextureSettings(GetAndroidTextureSettings(maxSize, true));
        importer.SaveAndReimport();
    }

    private static TextureImporterPlatformSettings GetAndroidTextureSettings(int maxSize, bool hasAlpha)
    {
        return new TextureImporterPlatformSettings
        {
            name = "Android",
            overridden = true,
            maxTextureSize = maxSize,
            format = hasAlpha ? TextureImporterFormat.ETC2_RGBA8 : TextureImporterFormat.ETC2_RGB4,
            textureCompression = TextureImporterCompression.CompressedHQ,
            compressionQuality = 50
        };
    }

    private static void OptimizeTextureImporters()
    {
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" });
        int optimized = 0;

        foreach (string guid in textureGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (path.StartsWith("Assets/Scenes/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("Assets/BodyGuards/Textures/OptimizedMobile/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                continue;
            }

            bool hasAlpha = importer.DoesSourceTextureHaveAlpha();
            importer.maxTextureSize = Mathf.Min(importer.maxTextureSize, 1024);
            importer.mipmapEnabled = importer.textureType != TextureImporterType.Sprite;
            importer.streamingMipmaps = importer.mipmapEnabled;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.crunchedCompression = importer.textureType == TextureImporterType.Default;
            importer.compressionQuality = 50;
            importer.isReadable = false;
            importer.SetPlatformTextureSettings(GetAndroidTextureSettings(importer.maxTextureSize, hasAlpha || importer.textureType == TextureImporterType.NormalMap));
            importer.SaveAndReimport();
            optimized++;
        }

        Debug.Log($"Optimized {optimized} texture importer(s) for mobile.");
    }

    private static void OptimizeModelImporters()
    {
        string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets" });
        int optimized = 0;

        foreach (string guid in modelGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

            if (importer == null)
            {
                continue;
            }

            bool isScenario = path.StartsWith("Assets/Scenarios/", StringComparison.OrdinalIgnoreCase);
            bool isAnimation = path.StartsWith("Assets/Animations/", StringComparison.OrdinalIgnoreCase);

            importer.isReadable = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.meshCompression = isScenario ? ModelImporterMeshCompression.High : ModelImporterMeshCompression.Medium;

            if (isAnimation || path.Contains("Bodyguard", StringComparison.OrdinalIgnoreCase))
            {
                importer.animationCompression = ModelImporterAnimationCompression.Optimal;
                importer.animationRotationError = 0.5f;
                importer.animationPositionError = 0.5f;
                importer.animationScaleError = 0.5f;
            }

            importer.SaveAndReimport();
            optimized++;
        }

        Debug.Log($"Optimized {optimized} model importer(s) for mobile.");
    }

    private static void OptimizeAudioImporters()
    {
        string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/AudioFolder" });
        int optimized = 0;

        foreach (string guid in audioGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;

            if (importer == null)
            {
                continue;
            }

            importer.forceToMono = path.Contains("/SFX/", StringComparison.OrdinalIgnoreCase);

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.45f;
            settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            optimized++;
        }

        Debug.Log($"Optimized {optimized} audio importer(s) for mobile.");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string fullPath = $"{parent}/{child}";

        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
