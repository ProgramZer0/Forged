using UnityEngine;

public class ShapeManager : MonoBehaviour
{
    public float hitRadius = 0.2f;
    public float minThicknessRatio = 0.15f;

    public float desiredLength = 1.5f;

    private Mesh deformingMesh;
    private Vector3[] vertices;
    private float[] initialHeights;
    private Collider hornCollider;

    void Awake()
    {
        deformingMesh = GetComponent<MeshFilter>().mesh;
        vertices = deformingMesh.vertices;

        initialHeights = new float[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
            initialHeights[i] = vertices[i].y;
    }

    public void OnHammerHit(
        RaycastHit hit, 
        float force, 
        AnvilMode hammerType, 
        SmithingMode currentMode,
        bool autoStraighten, 
        AnvilHitType hitType, 
        Vector3 hammerRightWorld,
        Collider _hornCollider)
    {
        hornCollider = _hornCollider;
        vertices = deformingMesh.vertices;
        Vector3 localHit = transform.InverseTransformPoint(hit.point);
        if (hammerType == AnvilMode.Flat && hitType == AnvilHitType.WarpInward)
        {
            BendToHornSurface(localHit, force);
        }
        else
        {
            if (currentMode == SmithingMode.Normal)
                ApplyNormal(localHit, force);

            else
                ApplyExpert(localHit, force, hammerType, hammerRightWorld);

            if (autoStraighten && currentMode == SmithingMode.Normal)
                AutoStraighten(0.1f);
        }
        

        deformingMesh.vertices = vertices;
        deformingMesh.RecalculateNormals();
        deformingMesh.RecalculateBounds();
    }

    private void BendToHornSurface(Vector3 center, float force)
    {
        float radius = hitRadius;
        float sqrRadius = radius * radius;
        float invSqrRadius = 1f / sqrRadius;

        float bendStrength = force * 0.015f;   // Base bending amount
        float maxRayDistance = 0.4f;

        Vector3 meshCenter = deformingMesh.bounds.center;

        Vector3 worldCenter = transform.TransformPoint(center);

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldVertex = transform.TransformPoint(vertices[i]);

            Vector3 offset = worldVertex - worldCenter;
            float distSqr = offset.sqrMagnitude;
            if (distSqr > sqrRadius) continue;

            float falloff = 1f - (distSqr * invSqrRadius);
            falloff = Mathf.Clamp01(falloff);
            falloff *= falloff;

            float edgeDistance = Mathf.Abs(vertices[i].x - meshCenter.x);
            float edgeBias = Mathf.Clamp01(edgeDistance * 2f);

            float finalStrength = bendStrength * falloff * edgeBias;

            if (finalStrength <= 0f) continue;

            Vector3 directionToHorn = (hornCollider.bounds.center - worldVertex).normalized;

            Ray ray = new Ray(worldVertex + directionToHorn * 0.01f, directionToHorn);

            if (hornCollider.Raycast(ray, out RaycastHit hornHit, maxRayDistance))
            {
                Vector3 target = hornHit.point;

                worldVertex = Vector3.Lerp(worldVertex, target, finalStrength);

                vertices[i] = transform.InverseTransformPoint(worldVertex);
            }
        }
    }

    private void ApplyNormal(Vector3 center, float force)
    {
        float radius = hitRadius;
        float sqrRadius = radius * radius;
        float invSqrRadius = 1f / sqrRadius;

        float delta = force * 0.03f;

        float compressY = 1f - delta * 0.5f;
        float expandX = 1f + delta * 1.2f;
        float expandZ = 1f + delta * 0.2f;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 offset = vertices[i] - center;
            float distSqr = offset.sqrMagnitude;
            if (distSqr > sqrRadius) continue;

            float falloff = 1f - (distSqr * invSqrRadius);
            falloff = Mathf.Clamp01(falloff);
            falloff *= falloff;

            Vector3 relative = offset;

            relative.y *= Mathf.Lerp(1f, compressY, falloff);
            relative.x *= Mathf.Lerp(1f, expandX, falloff);
            relative.z *= Mathf.Lerp(1f, expandZ, falloff);

            vertices[i] = center + relative;
        }
    }

    private void ApplyExpert(Vector3 center, float force, AnvilMode hammerType, Vector3 hammerRightWorld)
    {
        float radius = hitRadius;
        float sqrRadius = radius * radius;
        float delta = force * 0.02f;

        if (hammerType == AnvilMode.Flat)
            ExpertFlat(center, radius, sqrRadius, delta);
        else
            ExpertPeen(center, radius, sqrRadius, delta, hammerRightWorld);
    }

    private void AutoStraighten(float strength)
    {
        Vector3 meshCenter = deformingMesh.bounds.center;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].x = Mathf.Lerp(vertices[i].x, meshCenter.x, strength);
        }
    }

    private void ExpertFlat(
    Vector3 center,
    float radius,
    float sqrRadius,
    float delta)
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 offset = vertices[i] - center;
            if (offset.sqrMagnitude > sqrRadius) continue;

            if (vertices[i].x < minX) minX = vertices[i].x;
            if (vertices[i].x > maxX) maxX = vertices[i].x;
            if (vertices[i].z < minZ) minZ = vertices[i].z;
            if (vertices[i].z > maxZ) maxZ = vertices[i].z;
        }

        float width = maxX - minX;
        float length = maxZ - minZ;
        float total = width + length;
        if (total <= 0f) return;

        float spreadX = length / total;
        float spreadZ = width / total;

        float compressY = 1f - delta;
        float expandX = 1f + delta * spreadX;
        float expandZ = 1f + delta * spreadZ;

        float invSqrRadius = 1f / sqrRadius;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 offset = vertices[i] - center;
            float distSqr = offset.sqrMagnitude;
            if (distSqr > sqrRadius) continue;

            float falloff = 1f - (distSqr * invSqrRadius);
            falloff = Mathf.Clamp01(falloff);
            falloff *= falloff;

            Vector3 relative = offset;

            relative.y *= Mathf.Lerp(1f, compressY, falloff);
            relative.x *= Mathf.Lerp(1f, expandX, falloff);
            relative.z *= Mathf.Lerp(1f, expandZ, falloff);

            vertices[i] = center + relative;
        }
    }

    private void ExpertPeen(
    Vector3 center,
    float radius,
    float sqrRadius,
    float delta,
    Vector3 hammerRightWorld)
    {
        float compressY = 1f - delta;

        float expandPrimary = 1f + delta * 0.2f;
        float expandSecondary = 1f + delta * 0.9f;

        Vector3 localPeenDir = transform.InverseTransformDirection(hammerRightWorld);
        bool alongX = Mathf.Abs(localPeenDir.x) > Mathf.Abs(localPeenDir.z);

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 offset = vertices[i] - center;
            float distSqr = offset.sqrMagnitude;
            if (distSqr > sqrRadius) continue;

            float falloff = 1f - (distSqr / sqrRadius);
            falloff = Mathf.Clamp01(falloff);
            falloff *= falloff;

            Vector3 relative = offset;

            relative.y *= Mathf.Lerp(1f, compressY, falloff);

            if (alongX)
            {
                relative.x *= Mathf.Lerp(1f, expandPrimary, falloff);
                relative.z *= Mathf.Lerp(1f, expandSecondary, falloff);
            }
            else
            {
                relative.z *= Mathf.Lerp(1f, expandPrimary, falloff);
                relative.x *= Mathf.Lerp(1f, expandSecondary, falloff);
            }

            vertices[i] = center + relative;
        }
    }
}