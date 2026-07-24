using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class PPERoomSignatureTestBuilder
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room.unity";
    const string RootName = "Signature_Test_Sequence";
    const string ShaderName = "Project/Handwritten Signature Reveal";
    const string MaterialFolder = "Assets/Materials/PPE/Tablet/Signatures";
    const string PlayerTexturePath = "Assets/UIs/sign_stamp/sign_player_rm.png";
    const string ConductorTexturePath = "Assets/UIs/sign_stamp/sign_conductor.png";
    const string PlayerMaterialPath = MaterialFolder + "/PlayerSignature_Handwrite.mat";
    const string ConductorMaterialPath = MaterialFolder + "/ConductorSignature_Handwrite.mat";

    [MenuItem("Tools/PPE/Build Tablet Signature Test")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject tablet = GameObject.Find("Tablet (1)");
        Transform documentPlane = tablet != null ? tablet.transform.Find("GeneratedPlane") : null;
        if (documentPlane == null)
        {
            Debug.LogError("Cannot build signature test because Tablet (1)/GeneratedPlane was not found.");
            return;
        }

        Shader signatureShader = Shader.Find(ShaderName);
        if (signatureShader == null)
        {
            Debug.LogError($"Cannot build signature test because shader '{ShaderName}' was not found.");
            return;
        }

        Texture2D playerTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PlayerTexturePath);
        Texture2D conductorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ConductorTexturePath);
        if (playerTexture == null || conductorTexture == null)
        {
            Debug.LogError($"Cannot build signature test. player={playerTexture != null} conductor={conductorTexture != null}");
            return;
        }

        EnsureFolder(MaterialFolder);
        Material playerMaterial = CreateOrUpdateMaterial(
            PlayerMaterialPath,
            signatureShader,
            playerTexture,
            new Vector4(0.15f, 0.24f, 0.91f, 0.76f),
            true,
            0.62f,
            0.2f);
        Material conductorMaterial = CreateOrUpdateMaterial(
            ConductorMaterialPath,
            signatureShader,
            conductorTexture,
            new Vector4(0.25f, 0.25f, 0.84f, 0.76f),
            true,
            0.62f,
            0.2f);

        Transform existing = documentPlane.Find(RootName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject root = new(RootName);
        root.layer = documentPlane.gameObject.layer;
        root.transform.SetParent(documentPlane, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        MeshRenderer playerRenderer = CreateSignatureQuad(
            root.transform,
            "Player_Signature",
            new Vector3(-2.64f, -1.65f, -0.25f),
            new Vector3(2f, 0.26f, 1f),
            playerMaterial);
        MeshRenderer conductorRenderer = CreateSignatureQuad(
            root.transform,
            "Conductor_Signature",
            new Vector3(-0.03f, -1.65f, -0.25f),
            new Vector3(2f, 0.26f, 1f),
            conductorMaterial);

        HandwrittenSignatureSequence sequence = root.AddComponent<HandwrittenSignatureSequence>();
        SerializedObject serializedSequence = new(sequence);
        serializedSequence.FindProperty("playOnEnable").boolValue = true;
        serializedSequence.FindProperty("useUnscaledTime").boolValue = false;
        serializedSequence.FindProperty("initialDelay").floatValue = 0.8f;

        SerializedProperty steps = serializedSequence.FindProperty("signatures");
        steps.arraySize = 2;
        ConfigureStep(steps.GetArrayElementAtIndex(0), playerRenderer, 0f, 1.6f);
        ConfigureStep(steps.GetArrayElementAtIndex(1), conductorRenderer, 0.35f, 1.5f);
        serializedSequence.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(documentPlane.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = root;
        Debug.Log("Built PPE tablet signature test on work_confirm document.", root);
    }

    static void ConfigureStep(
        SerializedProperty step,
        Renderer renderer,
        float delayBefore,
        float duration)
    {
        step.FindPropertyRelative("targetRenderer").objectReferenceValue = renderer;
        step.FindPropertyRelative("delayBefore").floatValue = delayBefore;
        step.FindPropertyRelative("duration").floatValue = duration;
        step.FindPropertyRelative("revealCurve").animationCurveValue =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    static MeshRenderer CreateSignatureQuad(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject signature = GameObject.CreatePrimitive(PrimitiveType.Quad);
        signature.name = name;
        signature.layer = parent.gameObject.layer;
        signature.transform.SetParent(parent, false);
        signature.transform.localPosition = localPosition;
        signature.transform.localRotation = Quaternion.identity;
        signature.transform.localScale = localScale;

        Collider collider = signature.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        MeshRenderer renderer = signature.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.sortingOrder = 20;
        return renderer;
    }

    static Material CreateOrUpdateMaterial(
        string path,
        Shader shader,
        Texture texture,
        Vector4 cropRect,
        bool useTextureAlpha,
        float threshold,
        float softness)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        material.SetTexture("_MainTex", texture);
        material.SetColor("_InkColor", new Color(0.025f, 0.055f, 0.075f, 0.96f));
        material.SetFloat("_Reveal", 0f);
        material.SetFloat("_RevealSoftness", 0.025f);
        material.SetFloat("_InkThreshold", threshold);
        material.SetFloat("_InkSoftness", softness);
        material.SetFloat("_UseTextureAlpha", useTextureAlpha ? 1f : 0f);
        material.SetVector("_CropRect", cropRect);
        material.enableInstancing = true;
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
