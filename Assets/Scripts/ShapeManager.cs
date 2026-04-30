using UnityEngine;

public class ShapeManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Public
    // -------------------------------------------------------------------------

    public Collider anvilCollider;

    [Header("Deformation")]
    public float hitRadius = 0.2f;

    [Header("Normal Mode Assist")]
    public float assistStrength = 0.3f;

    // -------------------------------------------------------------------------
    // Private
    // -------------------------------------------------------------------------

    private Mesh deformingMesh;
    private Vector3[] vertices;
    private int[] weldMap;
    private Vector3 cachedMeshCenter;
    private AnvilManager anvilMgr;

    // -------------------------------------------------------------------------
    // Init
    // -------------------------------------------------------------------------

    void Awake()
    {
        deformingMesh = GetComponentInChildren<MeshFilter>().mesh;
        vertices = deformingMesh.vertices;
        cachedMeshCenter = deformingMesh.bounds.center;
        anvilMgr = FindFirstObjectByType<AnvilManager>();
        BuildWeldMap();
    }

    // -------------------------------------------------------------------------
    // Weld Map
    // -------------------------------------------------------------------------

    private void BuildWeldMap()
    {
        weldMap = new int[vertices.Length];
        float thresholdSq = 0.0001f * 0.0001f;

        for (int i = 0; i < vertices.Length; i++)
        {
            weldMap[i] = i;

            for (int j = 0; j < i; j++)
            {
                if ((vertices[i] - vertices[j]).sqrMagnitude < thresholdSq)
                {
                    weldMap[i] = weldMap[j];
                    break;
                }
            }
        }
    }

    // Applies a delta to a vertex and all nearby welded twins.
    // Twins outside the hit radius are skipped to avoid moving the opposite side.
    private void ApplyWeldedDelta(int i, Vector3 delta, Vector3 localHitCenter)
    {
        int canonical = weldMap[i];
        float twinRadiusSq = hitRadius * hitRadius * 4f;

        for (int j = 0; j < vertices.Length; j++)
        {
            if (weldMap[j] != canonical) continue;
            if ((vertices[j] - localHitCenter).sqrMagnitude > twinRadiusSq) continue;
            vertices[j] += delta;
        }
    }

    // -------------------------------------------------------------------------
    // Entry Point
    // -------------------------------------------------------------------------

    public void OnHammerHit(
    RaycastHit hit,
    float force,
    AnvilMode hammerType,
    SmithingMode currentMode,
    bool autoStraighten,
    Vector3 hammerRightWorld,
    Collider _anvilCollider)
    {
        anvilCollider = _anvilCollider;
        vertices = deformingMesh.vertices;

        // Measure volume once before anything runs.
        // We enforce this exact volume at the end regardless of which path ran.
        float volumeBefore = CalculateVolumeFromVertices(vertices, deformingMesh.triangles, transform);

        Vector3 localHit = transform.InverseTransformPoint(hit.point);

        if (currentMode == SmithingMode.Normal && anvilMgr.sliderOn)
        {
            // Normal mode with slider: nudge all vertices toward target shape.
            ApplyNormalAssist(localHit, force);
        }
        else
        {
            // Physical deformation: thickness-resistant compression with lateral expansion.
            float thickness = MeasureThickness(hit.point, hit.normal);

            // Compression resists exponentially as metal gets thinner.
            float maxCompress = force * 0.04f;
            float compression = maxCompress * (thickness * thickness) / (thickness * thickness + 0.01f);

            // Lateral expansion derived from volume conservation equation.
            float newThickness = Mathf.Max(thickness - compression, 0.001f);
            float lateralExpansion = hitRadius * (Mathf.Sqrt(thickness / newThickness) - 1f);

            ApplyDeformation(localHit, hit.point, hit.normal, compression, lateralExpansion, hammerType, hammerRightWorld);
        }

        // Enforce volume after whichever path ran.
        // Corrects any loss from anvil clamping or floating point drift.
        float volumeAfter = CalculateVolumeFromVertices(vertices, deformingMesh.triangles, transform);

        if (volumeAfter > 0.0001f && volumeBefore > 0.0001f)
        {
            float correction = Mathf.Pow(volumeBefore / volumeAfter, 1f / 3f);

            if (Mathf.Abs(correction - 1f) > 0.0001f)
            {
                Vector3 currentCenter = GetCurrentCenter();

                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 fromCenter = vertices[i] - currentCenter;
                    Vector3 corrected = currentCenter + fromCenter * correction;
                    vertices[i] = ClampToAnvil(vertices[i], corrected);
                }
            }
        }

        deformingMesh.vertices = vertices;
        deformingMesh.RecalculateNormals();
        deformingMesh.RecalculateBounds();

        // Update mesh collider so thickness raycasts are accurate next hit.
        MeshCollider mc = GetComponentInChildren<MeshCollider>();
        if (mc != null)
            mc.sharedMesh = deformingMesh;
    }

    // -------------------------------------------------------------------------
    // Physical Deformation
    // -------------------------------------------------------------------------

    private void ApplyDeformation(
        Vector3 localHit,
        Vector3 worldHit,
        Vector3 worldNormal,
        float compression,
        float lateralExpansion,
        AnvilMode hammerType,
        Vector3 hammerRightWorld)
    {
        float sqrRadius = hitRadius * hitRadius;
        Vector3 localNormal = transform.InverseTransformDirection(worldNormal);

        // For peen: spread direction is perpendicular to the hammer's right vector.
        // The peen draws material along this perpendicular axis.
        Vector3 localPeenPerp = Vector3.zero;
        if (hammerType == AnvilMode.Peen)
        {
            Vector3 localPeenDir = transform.InverseTransformDirection(hammerRightWorld);
            localPeenPerp = Vector3.Cross(localNormal, localPeenDir).normalized;
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            if (weldMap[i] != i) continue;

            Vector3 localPos = vertices[i];
            Vector3 offset = localPos - localHit;
            float distSqr = offset.sqrMagnitude;
            if (distSqr > sqrRadius) continue;

            // Smooth falloff  vertices further from hit center move less.
            float falloff = 1f - (distSqr / sqrRadius);
            falloff *= falloff;

            Vector3 delta = Vector3.zero;

            // Compress inward along hit normal (downward into the anvil).
            delta -= localNormal * compression * falloff;

            if (hammerType == AnvilMode.Flat)
            {
                // Flat hammer: material spreads radially outward in all directions.
                // Project radial direction onto the plane perpendicular to the hit normal
                // so spread stays lateral and doesn't add unwanted vertical movement.
                Vector3 radial = localPos - cachedMeshCenter;
                radial -= Vector3.Dot(radial, localNormal) * localNormal;

                if (radial.sqrMagnitude > 0.0001f)
                {
                    radial.Normalize();
                    delta += radial * lateralExpansion * falloff;
                }
            }
            else // Peen
            {
                // Peen hammer: material draws out strongly along one axis only.
                // Side determines which direction from center  positive or negative.
                float side = Mathf.Sign(Vector3.Dot(localPos - cachedMeshCenter, localPeenPerp));
                delta += localPeenPerp * side * lateralExpansion * falloff;
            }

            Vector3 candidate = ClampToAnvil(localPos, localPos + delta);
            ApplyWeldedDelta(i, candidate - localPos, localHit);
        }
    }

    // -------------------------------------------------------------------------
    // Normal Mode Assist
    // -------------------------------------------------------------------------

    // Nudges ALL vertices toward the target shape every hit, regardless of where
    // the hammer landed. Volume is conserved because the target is derived from
    // the actual mesh volume and ratios that always sum to 1.
    private void ApplyNormalAssist(Vector3 localHit, float force)
    {
        if (anvilMgr == null) return;

        float rx = anvilMgr.GetXSliderHelper();
        float ry = anvilMgr.GetYSliderHelper();
        float rz = anvilMgr.GetZSliderHelper();

        float volume = CalculateMeshVolume(deformingMesh, transform);
        if (volume <= 0f) return;

        float ratioProduct = rx * ry * rz;
        if (ratioProduct <= 0f) return;

        float k = Mathf.Pow(volume / ratioProduct, 1f / 3f);
        Vector3 target = new Vector3(rx * k, ry * k, rz * k);

        // Get current bounds min/max so we can remap proportionally.
        Vector3 min = vertices[0];
        Vector3 max = vertices[0];
        for (int i = 1; i < vertices.Length; i++)
        {
            min = Vector3.Min(min, vertices[i]);
            max = Vector3.Max(max, vertices[i]);
        }

        Vector3 current = max - min;
        Vector3 currentCenter = (min + max) * 0.5f;

        // How far to move this hit — lerp fraction driven by force and assist strength.
        // Small value so it feels like gradual progress, not an instant snap.
        float lerpT = assistStrength * force * 0.05f;
        lerpT = Mathf.Clamp01(lerpT);

        for (int i = 0; i < vertices.Length; i++)
        {
            // No weldMap check needed — process every vertex directly.
            Vector3 v = vertices[i];

            Vector3 normalized = new Vector3(
                current.x > 0.0001f ? (v.x - currentCenter.x) / current.x : 0f,
                current.y > 0.0001f ? (v.y - currentCenter.y) / current.y : 0f,
                current.z > 0.0001f ? (v.z - currentCenter.z) / current.z : 0f
            );

            Vector3 targetPos = new Vector3(
                currentCenter.x + normalized.x * target.x,
                currentCenter.y + normalized.y * target.y,
                currentCenter.z + normalized.z * target.z
            );

            Vector3 candidate = Vector3.Lerp(v, targetPos, lerpT);
            candidate = ClampToAnvil(v, candidate);

            // Set directly — no weld twin lookup needed since every vertex is processed.
            vertices[i] = candidate;
        }
    }
    // -------------------------------------------------------------------------
    // Volume
    // -------------------------------------------------------------------------

    // Calculates volume directly from a vertex array without needing to assign
    // it to the mesh first. Used mid-deformation to avoid a temporary mesh write.
    private static float CalculateVolumeFromVertices(Vector3[] verts, int[] triangles, Transform transform)
    {
        float volume = 0f;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 p1 = verts[triangles[i]];
            Vector3 p2 = verts[triangles[i + 1]];
            Vector3 p3 = verts[triangles[i + 2]];

            if (transform != null)
            {
                p1 = transform.TransformPoint(p1);
                p2 = transform.TransformPoint(p2);
                p3 = transform.TransformPoint(p3);
            }

            volume += Vector3.Dot(p1, Vector3.Cross(p2, p3)) / 6f;
        }

        return Mathf.Abs(volume);
    }

    // Public version that takes a mesh directly  used by NormalAssist and external callers.
    public static float CalculateMeshVolume(Mesh mesh, Transform transform = null)
    {
        return CalculateVolumeFromVertices(mesh.vertices, mesh.triangles, transform);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    // Current bounding dimensions from live vertex data.
    // Used for per-axis error in the assist  not for volume calculations.
    private Vector3 GetCurrentDimensions()
    {
        Vector3 min = vertices[0];
        Vector3 max = vertices[0];

        for (int i = 1; i < vertices.Length; i++)
        {
            min = Vector3.Min(min, vertices[i]);
            max = Vector3.Max(max, vertices[i]);
        }

        return max - min;
    }

    // Current center from live vertex data.
    // Used for volume correction  more accurate than cachedMeshCenter after deformation.
    private Vector3 GetCurrentCenter()
    {
        Vector3 min = vertices[0];
        Vector3 max = vertices[0];

        for (int i = 1; i < vertices.Length; i++)
        {
            min = Vector3.Min(min, vertices[i]);
            max = Vector3.Max(max, vertices[i]);
        }

        return (min + max) * 0.5f;
    }

    // Measures thickness at the hit point by raycasting inward through the mesh.
    // Uses the mesh collider if available (updated each hit), falls back to bounds extent.
    private float MeasureThickness(Vector3 worldPoint, Vector3 worldNormal)
    {
        Ray ray = new Ray(worldPoint + worldNormal * 0.001f, -worldNormal);

        MeshCollider mc = GetComponentInChildren<MeshCollider>();
        if (mc != null && mc.Raycast(ray, out RaycastHit hit, 2f))
            return hit.distance;

        // Fallback: project bounds size onto hit normal direction.
        Vector3 localNormal = transform.InverseTransformDirection(worldNormal);
        float fallback = Mathf.Abs(Vector3.Dot(deformingMesh.bounds.size, localNormal));
        return Mathf.Max(fallback, 0.01f);
    }

    // Prevents a vertex from going below the physical anvil surface.
    // Raycasts against the actual collider so it works on the horn and edges too.
    private Vector3 ClampToAnvil(Vector3 originalLocal, Vector3 candidateLocal)
    {
        if (anvilCollider == null) return candidateLocal;

        Vector3 worldCandidate = transform.TransformPoint(candidateLocal);
        Ray ray = new Ray(worldCandidate + Vector3.up * 0.1f, Vector3.down);

        if (anvilCollider.Raycast(ray, out RaycastHit hit, 0.5f))
        {
            if (worldCandidate.y < hit.point.y)
            {
                worldCandidate.y = hit.point.y;
                return transform.InverseTransformPoint(worldCandidate);
            }
        }

        return candidateLocal;
    }
}