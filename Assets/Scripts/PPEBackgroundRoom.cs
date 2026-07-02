using UnityEngine;

/// <summary>
/// Builds a lightweight, image-based room for evaluating a single perspective
/// background in VR. This is intentionally a prototype: the image is repeated
/// on four walls, so seams and baked perspective remain visible.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class PPEBackgroundRoom : MonoBehaviour
{
    [SerializeField] private Texture2D background;
    [SerializeField, Min(1f)] private float roomWidth = 10f;
    [SerializeField, Min(1f)] private float roomDepth = 10f;
    [SerializeField, Min(1f)] private float roomHeight = 5.625f;
    [SerializeField] private Color floorColor = new(0.72f, 0.74f, 0.76f, 1f);
    [SerializeField] private Color ceilingColor = new(0.72f, 0.77f, 0.82f, 1f);
    [SerializeField] private Color wallColor = new(0.48f, 0.68f, 0.82f, 1f);
    [SerializeField] private Color trimColor = new(0.25f, 0.31f, 0.35f, 1f);
    [SerializeField, Min(0.5f)] private float doorHeight = 2.3f;
    [SerializeField, Min(0.01f)] private float doorWallInset = 0.08f;

    private const string GeneratedRootName = "Generated Image Room";

    private void OnEnable()
    {
        BuildRoom();
    }

    [ContextMenu("Rebuild Room")]
    public void BuildRoom()
    {
        Transform previous = transform.Find(GeneratedRootName);
        if (previous != null)
        {
            if (Application.isPlaying)
                Destroy(previous.gameObject);
            else
                DestroyImmediate(previous.gameObject);
        }

        GameObject root = new(GeneratedRootName);
        root.transform.SetParent(transform, false);
        if (!Application.isPlaying)
            root.hideFlags = HideFlags.DontSaveInEditor;

        Material wallMaterial = CreateMaterial("PPE Wall", wallColor, null);
        Material floorMaterial = CreateMaterial("PPE Floor", floorColor, null);
        Material ceilingMaterial = CreateMaterial("PPE Ceiling", ceilingColor, null);
        Material trimMaterial = CreateMaterial("PPE Wall Trim", trimColor, null);

        CreateWall(root.transform, "Front Wall", new Vector3(0f, roomHeight * 0.5f, roomDepth * 0.5f),
            new Vector3(0f, 180f, 0f), new Vector2(roomWidth, roomHeight), wallMaterial);
        CreateWall(root.transform, "Rear Wall", new Vector3(0f, roomHeight * 0.5f, -roomDepth * 0.5f),
            Vector3.zero, new Vector2(roomWidth, roomHeight), wallMaterial);
        CreateWall(root.transform, "Left Wall", new Vector3(-roomWidth * 0.5f, roomHeight * 0.5f, 0f),
            new Vector3(0f, 90f, 0f), new Vector2(roomDepth, roomHeight), wallMaterial);
        CreateWall(root.transform, "Right Wall", new Vector3(roomWidth * 0.5f, roomHeight * 0.5f, 0f),
            new Vector3(0f, -90f, 0f), new Vector2(roomDepth, roomHeight), wallMaterial);

        CreateSurface(root.transform, "Floor", new Vector3(0f, -0.03f, 0f),
            new Vector3(roomWidth, 0.05f, roomDepth), floorMaterial);
        CreateSurface(root.transform, "Ceiling", new Vector3(0f, roomHeight + 0.03f, 0f),
            new Vector3(roomWidth, 0.05f, roomDepth), ceilingMaterial);

        CreateWallDetails(root.transform, trimMaterial);

        PositionDoor();
    }

    private void CreateWallDetails(Transform parent, Material material)
    {
        const float seamWidth = 0.025f;
        const float seamDepth = 0.018f;
        const float panelWidth = 2f;

        for (float x = -roomWidth * 0.5f + panelWidth; x < roomWidth * 0.5f; x += panelWidth)
        {
            CreateSurface(parent, "Front Panel Seam", new Vector3(x, roomHeight * 0.5f, roomDepth * 0.5f - seamDepth),
                new Vector3(seamWidth, roomHeight, seamDepth), material);
            CreateSurface(parent, "Rear Panel Seam", new Vector3(x, roomHeight * 0.5f, -roomDepth * 0.5f + seamDepth),
                new Vector3(seamWidth, roomHeight, seamDepth), material);
        }

        for (float z = -roomDepth * 0.5f + panelWidth; z < roomDepth * 0.5f; z += panelWidth)
        {
            CreateSurface(parent, "Left Panel Seam", new Vector3(-roomWidth * 0.5f + seamDepth, roomHeight * 0.5f, z),
                new Vector3(seamDepth, roomHeight, seamWidth), material);
            CreateSurface(parent, "Right Panel Seam", new Vector3(roomWidth * 0.5f - seamDepth, roomHeight * 0.5f, z),
                new Vector3(seamDepth, roomHeight, seamWidth), material);
        }

        CreateHorizontalTrim(parent, "Baseboard", 0.1f, 0.2f, material);
        CreateHorizontalTrim(parent, "Ceiling Trim", roomHeight - 0.1f, 0.2f, material);
    }

    private void CreateHorizontalTrim(Transform parent, string prefix, float y, float height, Material material)
    {
        const float depth = 0.06f;
        CreateSurface(parent, prefix + " Front", new Vector3(0f, y, roomDepth * 0.5f - depth),
            new Vector3(roomWidth, height, depth), material);
        CreateSurface(parent, prefix + " Rear", new Vector3(0f, y, -roomDepth * 0.5f + depth),
            new Vector3(roomWidth, height, depth), material);
        CreateSurface(parent, prefix + " Left", new Vector3(-roomWidth * 0.5f + depth, y, 0f),
            new Vector3(depth, height, roomDepth), material);
        CreateSurface(parent, prefix + " Right", new Vector3(roomWidth * 0.5f - depth, y, 0f),
            new Vector3(depth, height, roomDepth), material);
    }

    [ContextMenu("Position PPE Room Door")]
    private void PositionDoor()
    {
        Transform door = FindSceneObject("PPE_Room_Door");
        if (door == null)
            return;

        door.SetParent(transform, false);
        door.localPosition = Vector3.zero;
        door.localRotation = Quaternion.Euler(0f, 180f, 0f);
        door.localScale = Vector3.one;

        if (!TryGetRendererBounds(door, out Bounds bounds) || bounds.size.y <= Mathf.Epsilon)
            return;

        float scale = doorHeight / bounds.size.y;
        door.localScale = Vector3.one * scale;

        if (!TryGetRendererBounds(door, out bounds))
            return;

        Vector3 desiredRightBottom = transform.TransformPoint(new Vector3(
            roomWidth * 0.5f - 1.35f, 0f, 0f));
        float wallInsideZ = transform.TransformPoint(new Vector3(
            0f, 0f, roomDepth * 0.5f - doorWallInset)).z;

        door.position += new Vector3(
            desiredRightBottom.x - bounds.center.x,
            desiredRightBottom.y - bounds.min.y,
            wallInsideZ - bounds.max.z);
    }

    private static Transform FindSceneObject(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (Transform candidate in transforms)
        {
            if (candidate.name == objectName && candidate.gameObject.scene.IsValid())
                return candidate;
        }

        return null;
    }

    private static bool TryGetRendererBounds(Transform target, out Bounds bounds)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    private static Material CreateMaterial(string materialName, Color color, Texture texture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Texture");

        Material material = new(shader) { name = materialName };
        material.hideFlags = HideFlags.DontSave;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else
            material.color = color;
        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", 0f);
        material.mainTexture = texture;
        return material;
    }

    private static void CreateWall(Transform parent, string objectName, Vector3 position,
        Vector3 rotation, Vector2 size, Material material)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
        wall.name = objectName;
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = position;
        wall.transform.localEulerAngles = rotation;
        wall.transform.localScale = new Vector3(size.x, size.y, 1f);
        wall.GetComponent<MeshRenderer>().sharedMaterial = material;

        Collider collider = wall.GetComponent<Collider>();
        if (collider == null)
            return;

        if (Application.isPlaying)
            Destroy(collider);
        else
            DestroyImmediate(collider);
    }

    private static void CreateSurface(Transform parent, string objectName, Vector3 position,
        Vector3 scale, Material material)
    {
        GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        surface.name = objectName;
        surface.transform.SetParent(parent, false);
        surface.transform.localPosition = position;
        surface.transform.localScale = scale;
        surface.GetComponent<MeshRenderer>().sharedMaterial = material;
    }
}
