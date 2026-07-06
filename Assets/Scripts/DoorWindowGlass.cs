using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
public sealed class DoorWindowGlass : MonoBehaviour
{
    [Header("Glass Size")]
    [SerializeField, Min(0.05f)] private float width = 0.486f;
    [SerializeField, Min(0.05f)] private float height = 0.78f;

    public void Configure(float newWidth, float newHeight)
    {
        width = newWidth;
        height = newHeight;
        RebuildMesh();
    }

    private void OnEnable()
    {
        RebuildMesh();
    }

    private void OnValidate()
    {
        RebuildMesh();
    }

    private void RebuildMesh()
    {
        if (!TryGetComponent(out MeshFilter meshFilter))
            return;

        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;
        float cornerX = Mathf.Min(0.042f, halfWidth * 0.45f);
        float cornerY = Mathf.Min(0.021f, halfHeight * 0.45f);
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

        Mesh previousMesh = meshFilter.sharedMesh;
        meshFilter.sharedMesh = mesh;
        if (previousMesh == null || !previousMesh.name.StartsWith("PPE Door Octagonal Glass"))
            return;

        Destroy(previousMesh);
    }
}
