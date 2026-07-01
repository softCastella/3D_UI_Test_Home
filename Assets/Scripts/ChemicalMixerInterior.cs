using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class ChemicalMixerInterior : MonoBehaviour
{
    const string GeneratedRootName = "Generated_MixerInterior";

    [Header("Vessel Dimensions")]
    [Min(0.5f)] public float radius = 3f;
    [Min(0.5f)] public float wallHeight = 4f;
    [Min(0.1f)] public float domeHeight = 1.45f;
    [Min(0.05f)] public float bottomDishDepth = 0.55f;
    [Range(16, 128)] public int radialSegments = 64;
    [Range(3, 24)] public int domeSegments = 12;

    [Header("Mixer Assembly")]
    [Min(0.03f)] public float shaftRadius = 0.12f;
    [Range(1, 6)] public int bladeLevels = 3;
    [Min(0.1f)] public float bladeLength = 2.15f;
    [Min(0.03f)] public float bladeThickness = 0.11f;
    public float bladeTwistDegrees = 32f;

    [Header("Appearance")]
    public Color steelColor = new Color(0.42f, 0.48f, 0.5f, 1f);
    public Color darkSteelColor = new Color(0.08f, 0.11f, 0.12f, 1f);
    [Range(0f, 1f)] public float smoothness = 0.62f;
    [Range(0f, 0.3f)] public float brushStrength = 0.08f;

    Material generatedMaterial;
    bool rebuildQueued;

    void OnEnable()
    {
        Rebuild();
    }

    void OnValidate()
    {
        radius = Mathf.Max(0.5f, radius);
        wallHeight = Mathf.Max(0.5f, wallHeight);
        domeHeight = Mathf.Max(0.1f, domeHeight);
        bladeLength = Mathf.Clamp(bladeLength, 0.1f, radius * 0.9f);

        if (!isActiveAndEnabled || rebuildQueued)
            return;

        rebuildQueued = true;
        Application.onBeforeRender -= RebuildBeforeRender;
        Application.onBeforeRender += RebuildBeforeRender;
    }

    void RebuildBeforeRender()
    {
        Application.onBeforeRender -= RebuildBeforeRender;
        rebuildQueued = false;
        if (this != null && isActiveAndEnabled)
            Rebuild();
    }

    [ContextMenu("Rebuild Mixer Interior")]
    public void Rebuild()
    {
        RemoveGeneratedRoot();
        CreateMaterial();

        var generatedRoot = new GameObject(GeneratedRootName);
        generatedRoot.transform.SetParent(transform, false);

        CreateVessel(generatedRoot.transform);
        CreateShaft(generatedRoot.transform);
        CreateBlades(generatedRoot.transform);
        CreateStructuralRings(generatedRoot.transform);
    }

    void CreateMaterial()
    {
        if (generatedMaterial != null)
            DestroyObject(generatedMaterial);

        var shader = Shader.Find("Project/Chemical Mixer Interior");
        if (shader == null)
        {
            Debug.LogError("Chemical Mixer Interior shader was not found.", this);
            return;
        }

        generatedMaterial = new Material(shader)
        {
            name = "ChemicalMixerInterior_RuntimeMaterial",
            hideFlags = HideFlags.HideAndDontSave
        };
        generatedMaterial.SetColor("_BaseColor", steelColor);
        generatedMaterial.SetColor("_DarkColor", darkSteelColor);
        generatedMaterial.SetFloat("_Smoothness", smoothness);
        generatedMaterial.SetFloat("_BrushStrength", brushStrength);
    }

    void CreateVessel(Transform parent)
    {
        var vessel = new GameObject("Vessel_InsideShell");
        vessel.transform.SetParent(parent, false);
        var filter = vessel.AddComponent<MeshFilter>();
        var renderer = vessel.AddComponent<MeshRenderer>();
        filter.sharedMesh = BuildVesselMesh();
        filter.sharedMesh.name = "ChemicalMixer_InsideShell";
        renderer.sharedMaterial = generatedMaterial;
    }

    Mesh BuildVesselMesh()
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        var bottomY = -wallHeight * 0.5f;
        var topY = wallHeight * 0.5f;

        AddRing(vertices, normals, uvs, radius, bottomY, 0f);
        AddRing(vertices, normals, uvs, radius, topY, 0.58f);

        for (var ring = 1; ring <= domeSegments; ring++)
        {
            var t = ring / (float)domeSegments;
            var angle = t * Mathf.PI * 0.5f;
            var ringRadius = radius * Mathf.Cos(angle);
            var y = topY + domeHeight * Mathf.Sin(angle);
            AddRing(vertices, normals, uvs, Mathf.Max(0.001f, ringRadius), y, Mathf.Lerp(0.58f, 1f, t));
        }

        ConnectRings(triangles, domeSegments + 1);

        var bottomStart = vertices.Count;
        AddRing(vertices, normals, uvs, radius, bottomY, 0f);
        for (var ring = 1; ring <= domeSegments; ring++)
        {
            var t = ring / (float)domeSegments;
            var angle = t * Mathf.PI * 0.5f;
            AddRing(vertices, normals, uvs, Mathf.Max(0.001f, radius * Mathf.Cos(angle)),
                bottomY - bottomDishDepth * Mathf.Sin(angle), Mathf.Lerp(0f, 0.16f, t));
        }
        ConnectRings(triangles, domeSegments, bottomStart / (radialSegments + 1));

        var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    void AddRing(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, float ringRadius, float y, float v)
    {
        for (var segment = 0; segment <= radialSegments; segment++)
        {
            var u = segment / (float)radialSegments;
            var angle = u * Mathf.PI * 2f;
            var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            vertices.Add(radial * ringRadius + Vector3.up * y);
            normals.Add(-radial);
            uvs.Add(new Vector2(u, v));
        }
    }

    void ConnectRings(List<int> triangles, int connectionCount, int startRing = 0)
    {
        var stride = radialSegments + 1;
        for (var ring = 0; ring < connectionCount; ring++)
        {
            var current = (startRing + ring) * stride;
            var next = current + stride;
            for (var segment = 0; segment < radialSegments; segment++)
            {
                var a = current + segment;
                var b = current + segment + 1;
                var c = next + segment;
                var d = next + segment + 1;
                // Wind the shell toward the vessel interior. With back-face
                // culling this makes the enclosure invisible from outside.
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(b); triangles.Add(d); triangles.Add(c);
            }
        }
    }

    void CreateShaft(Transform parent)
    {
        var height = wallHeight + domeHeight + bottomDishDepth * 0.7f;
        var shaft = CreatePrimitive(PrimitiveType.Cylinder, "Central_Shaft", parent);
        shaft.transform.localPosition = new Vector3(0f, (domeHeight - bottomDishDepth) * 0.5f, 0f);
        shaft.transform.localScale = new Vector3(shaftRadius * 2f, height * 0.5f, shaftRadius * 2f);
    }

    void CreateBlades(Transform parent)
    {
        for (var level = 0; level < bladeLevels; level++)
        {
            var t = bladeLevels == 1 ? 0.5f : level / (float)(bladeLevels - 1);
            var y = Mathf.Lerp(-wallHeight * 0.32f, wallHeight * 0.32f, t);
            var assembly = new GameObject($"Blade_Level_{level + 1}");
            assembly.transform.SetParent(parent, false);
            assembly.transform.localPosition = Vector3.up * y;
            assembly.transform.localRotation = Quaternion.Euler(0f, level * 67f, 0f);

            for (var side = 0; side < 2; side++)
            {
                var direction = side == 0 ? 1f : -1f;
                var blade = CreatePrimitive(PrimitiveType.Cube, $"Blade_{side + 1}", assembly.transform);
                blade.transform.localPosition = Vector3.right * direction * bladeLength * 0.5f;
                blade.transform.localScale = new Vector3(bladeLength, bladeThickness, 0.38f);
                blade.transform.localRotation = Quaternion.Euler(direction * bladeTwistDegrees, 0f, direction * 8f);
            }
        }
    }

    void CreateStructuralRings(Transform parent)
    {
        var ringYs = new[] { -wallHeight * 0.5f + 0.08f, 0f, wallHeight * 0.5f - 0.08f };
        foreach (var y in ringYs)
        {
            var ring = new GameObject("Reinforcement_Ring");
            ring.transform.SetParent(parent, false);
            ring.transform.localPosition = Vector3.up * y;
            var filter = ring.AddComponent<MeshFilter>();
            var renderer = ring.AddComponent<MeshRenderer>();
            filter.sharedMesh = BuildTorusMesh(radius * 0.995f, 0.035f, radialSegments, 8);
            filter.sharedMesh.name = "ChemicalMixer_ReinforcementRing";
            renderer.sharedMaterial = generatedMaterial;
        }
    }

    static Mesh BuildTorusMesh(float majorRadius, float tubeRadius, int majorSegments, int tubeSegments)
    {
        var vertices = new Vector3[(majorSegments + 1) * (tubeSegments + 1)];
        var normals = new Vector3[vertices.Length];
        var uvs = new Vector2[vertices.Length];
        var triangles = new int[majorSegments * tubeSegments * 6];

        for (var major = 0; major <= majorSegments; major++)
        {
            var u = major / (float)majorSegments;
            var majorAngle = u * Mathf.PI * 2f;
            var radial = new Vector3(Mathf.Cos(majorAngle), 0f, Mathf.Sin(majorAngle));

            for (var tube = 0; tube <= tubeSegments; tube++)
            {
                var v = tube / (float)tubeSegments;
                var tubeAngle = v * Mathf.PI * 2f;
                var normal = radial * Mathf.Cos(tubeAngle) + Vector3.up * Mathf.Sin(tubeAngle);
                var index = major * (tubeSegments + 1) + tube;
                vertices[index] = radial * majorRadius + normal * tubeRadius;
                normals[index] = normal;
                uvs[index] = new Vector2(u, v);
            }
        }

        var triangleIndex = 0;
        var stride = tubeSegments + 1;
        for (var major = 0; major < majorSegments; major++)
        {
            for (var tube = 0; tube < tubeSegments; tube++)
            {
                var a = major * stride + tube;
                var b = a + 1;
                var c = a + stride;
                var d = c + 1;
                triangles[triangleIndex++] = a;
                triangles[triangleIndex++] = b;
                triangles[triangleIndex++] = c;
                triangles[triangleIndex++] = b;
                triangles[triangleIndex++] = d;
                triangles[triangleIndex++] = c;
            }
        }

        var mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    GameObject CreatePrimitive(PrimitiveType type, string objectName, Transform parent)
    {
        var primitive = GameObject.CreatePrimitive(type);
        primitive.name = objectName;
        primitive.transform.SetParent(parent, false);
        var collider = primitive.GetComponent<Collider>();
        if (collider != null)
            DestroyObject(collider);
        primitive.GetComponent<MeshRenderer>().sharedMaterial = generatedMaterial;
        return primitive;
    }

    void RemoveGeneratedRoot()
    {
        var existing = transform.Find(GeneratedRootName);
        if (existing != null)
            DestroyObject(existing.gameObject);
    }

    static void DestroyObject(Object target)
    {
        if (target == null)
            return;
        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
