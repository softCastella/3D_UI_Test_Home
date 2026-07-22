using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public sealed class StorageWallRackMaterialPostprocessor : AssetPostprocessor
{
    const string ModelPath = "Assets/FBX/storage wall rack/storage+wall+rack.fbx";
    const string TextureDirectory = "Assets/FBX/storage wall rack/storage+wall+rack.fbm";
    const string MaterialDirectory = "Assets/Materials/PPE/Storage Wall Rack";
    const string AutoReimportSessionKey = "StorageWallRackMaterialPostprocessor.AutoReimported";
    const int ExpectedPartCount = 106;

    static readonly Regex PartIndexPattern = new Regex(
        @"tripo[_+]part[_+](\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static StorageWallRackMaterialPostprocessor()
    {
        EditorApplication.delayCall += AutoReimportIfMaterialsAreMissing;
    }

    public Material OnAssignMaterialModel(Material sourceMaterial, Renderer renderer)
    {
        if (NormalizePath(assetPath) != ModelPath)
            return null;

        if (!TryGetPartIndex(sourceMaterial != null ? sourceMaterial.name : null, out int partIndex) &&
            !TryGetPartIndex(renderer != null ? renderer.name : null, out partIndex) &&
            !TryGetPartIndex(renderer != null && renderer.gameObject != null ? renderer.gameObject.name : null, out partIndex))
        {
            return null;
        }

        return GetOrCreatePartMaterial(partIndex);
    }

    [MenuItem("Tools/PPE/Reimport Storage Wall Rack Materials")]
    public static void ReimportStorageWallRack()
    {
        EnsureAllPartMaterials();
        ApplyImporterMaterialRemaps();
        AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
        AssetDatabase.SaveAssets();
    }

    static void AutoReimportIfMaterialsAreMissing()
    {
        if (SessionState.GetBool(AutoReimportSessionKey, false))
            return;

        if (!File.Exists(ModelPath) || CountExistingPartMaterials() >= ExpectedPartCount)
            return;

        SessionState.SetBool(AutoReimportSessionKey, true);
        ReimportStorageWallRack();
    }

    static void EnsureAllPartMaterials()
    {
        EnsureMaterialDirectory();

        string absoluteTextureDirectory = Path.GetFullPath(TextureDirectory);
        if (!Directory.Exists(absoluteTextureDirectory))
            return;

        foreach (string absoluteTexturePath in Directory.GetFiles(
            absoluteTextureDirectory,
            "storage+wall+rack_tripo_part_*_basecolor.jpg"))
        {
            string texturePath = NormalizePath(absoluteTexturePath);
            int assetsIndex = texturePath.IndexOf("Assets/", System.StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
                texturePath = texturePath.Substring(assetsIndex);

            if (!TryGetPartIndex(Path.GetFileNameWithoutExtension(texturePath), out int partIndex))
                continue;

            GetOrCreatePartMaterial(partIndex);
        }
    }

    static Material GetOrCreatePartMaterial(int partIndex)
    {
        EnsureMaterialDirectory();

        string materialPath = GetMaterialPath(partIndex);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Standard");

            material = new Material(shader)
            {
                name = $"StorageWallRack_tripo_part_{partIndex}",
                enableInstancing = true
            };
            AssetDatabase.CreateAsset(material, materialPath);
        }

        Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(GetTexturePath(partIndex));
        if (baseColor != null)
        {
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", baseColor);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", baseColor);
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);

        EditorUtility.SetDirty(material);
        return material;
    }

    static void ApplyImporterMaterialRemaps()
    {
        ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
            return;

        for (int partIndex = 0; partIndex < ExpectedPartCount; partIndex++)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(GetMaterialPath(partIndex));
            if (material == null)
                continue;

            importer.AddRemap(
                new AssetImporter.SourceAssetIdentifier(typeof(Material), $"tripo_part_{partIndex}"),
                material);
            importer.AddRemap(
                new AssetImporter.SourceAssetIdentifier(typeof(Material), $"storage_wall_rack_tripo_part_{partIndex}_basecolor"),
                material);
            importer.AddRemap(
                new AssetImporter.SourceAssetIdentifier(typeof(Material), $"storage+wall+rack_tripo_part_{partIndex}_basecolor"),
                material);
        }

        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.materialLocation = ModelImporterMaterialLocation.External;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    static bool TryGetPartIndex(string value, out int partIndex)
    {
        partIndex = -1;
        if (string.IsNullOrEmpty(value))
            return false;

        Match match = PartIndexPattern.Match(value);
        return match.Success && int.TryParse(match.Groups[1].Value, out partIndex);
    }

    static void EnsureMaterialDirectory()
    {
        if (AssetDatabase.IsValidFolder(MaterialDirectory))
            return;

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/PPE"))
            AssetDatabase.CreateFolder("Assets/Materials", "PPE");
        if (!AssetDatabase.IsValidFolder(MaterialDirectory))
            AssetDatabase.CreateFolder("Assets/Materials/PPE", "Storage Wall Rack");
    }

    static int CountExistingPartMaterials()
    {
        string absoluteMaterialDirectory = Path.GetFullPath(MaterialDirectory);
        if (!Directory.Exists(absoluteMaterialDirectory))
            return 0;

        return Directory.GetFiles(absoluteMaterialDirectory, "StorageWallRack_tripo_part_*.mat").Length;
    }

    static string GetMaterialPath(int partIndex)
    {
        return $"{MaterialDirectory}/StorageWallRack_tripo_part_{partIndex}.mat";
    }

    static string GetTexturePath(int partIndex)
    {
        return $"{TextureDirectory}/storage+wall+rack_tripo_part_{partIndex}_basecolor.jpg";
    }

    static string NormalizePath(string path)
    {
        return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
    }
}
