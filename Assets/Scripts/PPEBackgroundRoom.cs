using UnityEngine;

/// <summary>
/// Builds the PPE room shell from dedicated wall, floor, and ceiling references.
/// The four walls share the same orthographic wall treatment while the horizontal
/// surfaces retain their own square texture layouts.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class PPEBackgroundRoom : MonoBehaviour
{
    [SerializeField] private Texture2D wallTexture;
    [SerializeField] private Texture2D floorTexture;
    [SerializeField] private Texture2D ceilingTexture;
    [SerializeField] private Texture2D doorTexture;
    [SerializeField] private Shader doorCutoutShader;
    [SerializeField, Min(1f)] private float roomWidth = 25f;
    [SerializeField, Min(1f)] private float roomDepth = 25f;
    [SerializeField, Min(1f)] private float roomHeight = 7.5f;
    [SerializeField] private Color floorColor = Color.white;
    [SerializeField] private Color ceilingColor = Color.white;
    [SerializeField] private Color wallColor = Color.white;
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

        Material wallMaterial = CreateMaterial("PPE Wall", wallColor, wallTexture);
        Material floorMaterial = CreateMaterial("PPE Floor", floorColor, floorTexture);
        Material ceilingMaterial = CreateMaterial("PPE Ceiling", ceilingColor, ceilingTexture);

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

        // Match the two luminaires painted into the ceiling texture.
        CreateCeilingLight(root.transform, "Ceiling Light Front", 2.1f);
        CreateCeilingLight(root.transform, "Ceiling Light Rear", -2.1f);
        // The image door lives outside Generated Image Room so rebuilding the room
        // never deletes or resets a scene-authored door Transform.
        CreateImageDoor(transform);

        // Keep the door's scene-authored Transform. PositionDoor remains available
        // from the component context menu when an automatic reset is explicitly wanted.
    }

    private void CreateImageDoor(Transform parent)
    {
        if (doorTexture == null)
            return;

        Transform existingDoor = FindSceneObject("PPE Room Door Image");
        bool isNewDoor = existingDoor == null;
        GameObject imageDoor = isNewDoor
            ? GameObject.CreatePrimitive(PrimitiveType.Quad)
            : existingDoor.gameObject;

        imageDoor.name = "PPE Room Door Image";
        if (isNewDoor)
        {
            Transform doorReference = FindSceneObject("PPE_Room_Door");
            if (doorReference == null)
            {
                if (Application.isPlaying)
                    Destroy(imageDoor);
                else
                    DestroyImmediate(imageDoor);
                return;
            }

            imageDoor.transform.SetParent(parent, true);
            imageDoor.transform.SetPositionAndRotation(doorReference.position, doorReference.rotation);
            const float imageHeight = 4f;
            float imageAspect = (float)doorTexture.width / doorTexture.height;
            imageDoor.transform.localScale = new Vector3(imageHeight * imageAspect, imageHeight, 1f);
        }

        Shader doorShader = doorCutoutShader;
        if (doorShader == null)
            doorShader = Shader.Find("Universal Render Pipeline/Unlit");
        Material material = new(doorShader) { name = "PPE Image Door" };
        material.hideFlags = HideFlags.DontSave;
        material.SetTexture("_BaseMap", doorTexture);
        material.SetColor("_BaseColor", new Color(0.72f, 0.72f, 0.72f, 1f));
        material.SetFloat("_Cutoff", 0.9f);
        imageDoor.GetComponent<MeshRenderer>().sharedMaterial = material;

        Transform previousGlass = imageDoor.transform.Find("Door Window Glass");
        if (previousGlass != null)
        {
            if (Application.isPlaying)
                Destroy(previousGlass.gameObject);
            else
                DestroyImmediate(previousGlass.gameObject);
        }
        CreateDoorGlass(imageDoor.transform);

        Collider collider = imageDoor.GetComponent<Collider>();
        if (Application.isPlaying)
            Destroy(collider);
        else
            DestroyImmediate(collider);
    }

    private static void CreateDoorGlass(Transform door)
    {
        GameObject glass = new("Door Window Glass");
        glass.name = "Door Window Glass";
        glass.transform.SetParent(door, false);
        glass.transform.localPosition = new Vector3(0.002f, -0.002f, -0.003f);

        const float halfWidth = 0.243f;
        const float halfHeight = 0.39f;
        const float cornerX = 0.042f;
        const float cornerY = 0.021f;
        Vector3[] vertices =
        {
            new(-halfWidth + cornerX, halfHeight, 0f),
            new(halfWidth - cornerX, halfHeight, 0f),
            new(halfWidth, halfHeight - cornerY, 0f),
            new(halfWidth, -halfHeight + cornerY, 0f),
            new(halfWidth - cornerX, -halfHeight, 0f),
            new(-halfWidth + cornerX, -halfHeight, 0f),
            new(-halfWidth, -halfHeight + cornerY, 0f),
            new(-halfWidth, halfHeight - cornerY, 0f),
            Vector3.zero
        };
        int[] triangles =
        {
            8, 0, 1, 8, 1, 2, 8, 2, 3, 8, 3, 4,
            8, 4, 5, 8, 5, 6, 8, 6, 7, 8, 7, 0
        };

        Mesh mesh = new() { name = "PPE Door Octagonal Glass" };
        mesh.hideFlags = HideFlags.DontSave;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        glass.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = glass.AddComponent<MeshRenderer>();

        Material material = CreateMaterial("PPE Door Glass",
            new Color(0.52f, 0.76f, 0.86f, 0.28f), null);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", 5f);
        material.SetFloat("_DstBlend", 10f);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_Cull", 0f);
        material.SetFloat("_Smoothness", 0.92f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        renderer.sharedMaterial = material;
    }

    private void CreateCeilingLight(Transform parent, string objectName, float zPosition)
    {
        GameObject lightObject = new(objectName);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = new Vector3(0f, roomHeight - 0.08f, zPosition);
        lightObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        Light ceilingLight = lightObject.AddComponent<Light>();
        ceilingLight.type = LightType.Spot;
        ceilingLight.color = new Color(1f, 0.96f, 0.88f);
        ceilingLight.intensity = zPosition > 0f ? 180f : 300f;
        ceilingLight.range = roomHeight + 2f;
        ceilingLight.spotAngle = 90f;
        ceilingLight.innerSpotAngle = 55f;
        ceilingLight.shadows = LightShadows.None;
    }

    [ContextMenu("Position PPE Room Door")]
    private void PositionDoor()
    {
        Transform door = FindSceneObject("PPE_Room_Door");
        if (door == null)
            return;

        door.SetParent(transform, false);
        door.localPosition = Vector3.zero;
        // The door sits on the right wall and faces inward toward the room.
        door.localRotation = Quaternion.Euler(0f, -90f, 0f);
        door.localScale = Vector3.one;

        if (!TryGetRendererBounds(door, out Bounds bounds) || bounds.size.y <= Mathf.Epsilon)
            return;

        float scale = doorHeight / bounds.size.y;
        door.localScale = Vector3.one * scale;

        if (!TryGetRendererBounds(door, out bounds))
            return;

        Vector3 desiredWallCenterBottom = transform.TransformPoint(new Vector3(
            roomWidth * 0.5f - doorWallInset, 0f, 0f));

        door.position += new Vector3(
            desiredWallCenterBottom.x - bounds.max.x,
            desiredWallCenterBottom.y - bounds.min.y,
            desiredWallCenterBottom.z - bounds.center.z);
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
