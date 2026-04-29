using UnityEngine;

public class ShapeManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Public
    // -------------------------------------------------------------------------

    public Collider anvilCollider;

    [Header("Deformation")]
    public float hitRadius = 0.2f;

    // How strongly the Normal mode assist nudges toward the target shape.
    // Keep small  this should feel like a gentle bias, not a snap.
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
    // Twins that are on the opposite side of the mesh (outside hit radius) are skipped.
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

        Vector3 localHit = transform.InverseTransformPoint(hit.point);

        // Measure thickness at the hit point to drive compression resistance.
        float thickness = MeasureThickness(hit.point, hit.normal);

        // Compression amount resists exponentially as metal gets thinner.
        // As thickness -> 0, compression -> 0 (can't compress paper-thin metal).
        float maxCompress = force * 0.015f;
        float compression = maxCompress * (thickness * thickness) / (thickness * thickness + 0.01f);

        // Volume conservation: derive lateral expansion from compression.
        float newThickness = Mathf.Max(thickness - compression, 0.001f);
        float lateralExpansion = hitRadius * (Mathf.Sqrt(thickness / newThickness) - 1f);

        // --- PHYSICAL DEFORMATION ---
        ApplyDeformation(localHit, hit.point, hit.normal, compression, lateralExpansion, hammerType, hammerRightWorld);

        // --- NORMAL MODE ASSIST ---
        // After physical deformation, gently nudge vertices toward the target
        // shape defined by the sliders in AnvilManager. Volume is always conserved
        // because the target is derived from the actual mesh volume and slider ratios.
        if (currentMode == SmithingMode.Normal)
            ApplyNormalAssist(localHit, force);

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

            float falloff = 1f - (distSqr / sqrRadius);
            falloff *= falloff;

            Vector3 delta = Vector3.zero;

            // Compress inward along hit normal.
            delta -= localNormal * compression * falloff;

            // Lateral expansion flat spreads radially, peen spreads along one axis.
            if (hammerType == AnvilMode.Flat)
            {
                // Radial direction from mesh center, projected onto the anvil plane.
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
                // Spread perpendicular to peen edge vertices on each side push outward.
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

    // Calculates target dimensions from the REAL mesh volume and slider ratios,
    // then nudges each vertex toward those dimensions. Because ratios sum to 1
    // and the target is derived from actual volume, this can never add material 
    // it only redistributes what already exists.
    private void ApplyNormalAssist(Vector3 localHit, float force)
    {
        if (anvilMgr == null) return;

        // Get slider ratios from AnvilManager these always sum to 1.
        float rx = anvilMgr.GetXSliderHelper();
        float ry = anvilMgr.GetYSliderHelper();
        float rz = anvilMgr.GetZSliderHelper();

        // Use the real mesh volume rather than bounding box approximation.
        // Bounding box would be wrong for any non-rectangular shape.
        float volume = CalculateMeshVolume(deformingMesh, transform);
        if (volume <= 0f) return;

        // Current bounding dimensions — used to measure error per axis.
        Vector3 current = GetCurrentDimensions();

        // Derive target dimensions from ratios and real volume.
        // k scales the ratios so the resulting box matches the actual volume.
        float ratioProduct = rx * ry * rz;
        if (ratioProduct <= 0f) return;

        float k = Mathf.Pow(volume / ratioProduct, 1f / 3f);
        Vector3 target = new Vector3(rx * k, ry * k, rz * k);

        // Error = how far current dimensions are from target.
        // Positive = axis needs to grow, negative = axis needs to shrink.
        Vector3 error = target - current;

        // Scale nudge by force and assist strength stronger hits bias more.
        float nudgeScale = assistStrength * force * 0.01f;

        for (int i = 0; i < vertices.Length; i++)
        {
            if (weldMap[i] != i) continue;

            Vector3 v = vertices[i];
            Vector3 fromCenter = v - cachedMeshCenter;

            Vector3 delta = Vector3.zero;

            // For each axis: push outward if that axis needs to grow,
            // inward if it needs to shrink. Vertices near center barely move
            if (Mathf.Abs(fromCenter.x) > 0.001f)
                delta.x = Mathf.Sign(fromCenter.x) * error.x * nudgeScale;

            if (Mathf.Abs(fromCenter.y) > 0.001f)
                delta.y = Mathf.Sign(fromCenter.y) * error.y * nudgeScale;

            if (Mathf.Abs(fromCenter.z) > 0.001f)
                delta.z = Mathf.Sign(fromCenter.z) * error.z * nudgeScale;

            // Clamp assist delta so it never overpowers the physical deformation.
            delta = Vector3.ClampMagnitude(delta, 0.005f);

            if (delta.sqrMagnitude < 0.000001f) continue;

            Vector3 candidate = ClampToAnvil(v, v + delta);

            // Assist applies across the whole mesh so pass mesh center
            // as locality  all canonical vertices qualify.
            ApplyWeldedDelta(i, candidate - v, cachedMeshCenter);
        }
    }

    // -------------------------------------------------------------------------
    // Volume
    // -------------------------------------------------------------------------

    // Calculates the real volume of a mesh using the signed tetrahedron method.
    // Much more accurate than bounding box approximation for non-rectangular shapes.
    // Pass transform if the mesh is scaled or rotated in world space.
    public static float CalculateMeshVolume(Mesh mesh, Transform transform = null)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        float volume = 0f;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 p1 = vertices[triangles[i]];
            Vector3 p2 = vertices[triangles[i + 1]];
            Vector3 p3 = vertices[triangles[i + 2]];

            if (transform != null)
            {
                p1 = transform.TransformPoint(p1);
                p2 = transform.TransformPoint(p2);
                p3 = transform.TransformPoint(p3);
            }

            volume += SignedVolumeOfTriangle(p1, p2, p3);
        }

        return Mathf.Abs(volume);
    }

    private static float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return Vector3.Dot(p1, Vector3.Cross(p2, p3)) / 6f;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    // Returns the current bounding dimensions of the mesh in local space.
    // Used for per-axis error calculation in the assist  not for volume.
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

    // Fires a ray inward through the mesh to measure current thickness at the hit point.
    // Used to drive compression resistance  thin metal resists further compression.
    private float MeasureThickness(Vector3 worldPoint, Vector3 worldNormal)
    {
        Ray ray = new Ray(worldPoint + worldNormal * 0.001f, -worldNormal);

        MeshCollider mc = GetComponentInChildren<MeshCollider>();
        if (mc != null && mc.Raycast(ray, out RaycastHit hit, 2f))
            return hit.distance;

        // Fallback: use mesh bounds extent along hit normal if collider misses.
        Vector3 localNormal = transform.InverseTransformDirection(worldNormal);
        float fallback = Mathf.Abs(Vector3.Dot(deformingMesh.bounds.size, localNormal));
        return Mathf.Max(fallback, 0.01f);
    }

    // Prevents a vertex from going below the physical anvil surface.
    // Raycasts against the actual anvil collider so it works on horn and edges too.
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