using UnityEngine;

// ShapeManager is added at runtime to a metal item when a shaping recipe is found for it.
// It handles all mesh deformation when the player hammers the metal on the anvil.
// The core principle: the anvil is a hard floor — downward force cannot push metal through it,
// so all hammer force is redirected outward laterally. Where it spreads depends on hammer type.
public class ShapeManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Public Fields (set by AnvilCrafting when this component is added)
    // -------------------------------------------------------------------------

    // The full collider of the anvil. Used to raycast against so vertices
    // never clip through — works on the horn, flat, and edges automatically.
    public Collider anvilCollider;

    // -------------------------------------------------------------------------
    // Inspector Settings
    // -------------------------------------------------------------------------

    // The radius around the hit point that gets affected by each hammer strike.
    // Larger = broader, softer deformation. Smaller = sharper, more localized.
    public float hitRadius = 0.2f;

    // Not currently enforced per-hit but reserved for future thickness checks.
    public float minThicknessRatio = 0.15f;

    // Target blade length used for shaping guidance (not yet enforced, reserved).
    public float desiredLength = 1.5f;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    // The live mesh being deformed. We work directly on this each hit.
    private Mesh deformingMesh;

    private Vector3 cachedMeshCenter;

    // Current vertex positions in local space. Updated before each deformation pass.
    private Vector3[] vertices;

    // The original Y values of each vertex at awake time.
    // Kept for reference — could be used later for reset or thickness enforcement.
    private float[] initialHeights;

    // Maps each vertex index to a canonical index. Vertices that share the same
    // position get the same canonical index so they always move together.
    // This prevents whole sides shifting when only part of the surface is hit,
    // which happens because Unity splits vertices at UV/normal seams.
    private int[] weldMap;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    void Awake()
    {
        deformingMesh = GetComponentInChildren<MeshFilter>().mesh;
        vertices = deformingMesh.vertices;

        // Cache center once — must not update as mesh deforms or spread math breaks
        cachedMeshCenter = deformingMesh.bounds.center;

        initialHeights = new float[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            initialHeights[i] = vertices[i].y;

        BuildWeldMap();
    }

    // -------------------------------------------------------------------------
    // Weld Map
    // -------------------------------------------------------------------------

    // Builds a map from each vertex index to a canonical index.
    // Any two vertices closer than the threshold are considered the same point
    // and get the same canonical index. This means when one moves, all its
    // duplicates move by the same delta — fixing the whole-side-shifts bug.
    private void BuildWeldMap()
    {
        weldMap = new int[vertices.Length];
        float threshold = 0.0001f;
        float thresholdSq = threshold * threshold;

        for (int i = 0; i < vertices.Length; i++)
        {
            weldMap[i] = i; // default: maps to itself

            for (int j = 0; j < i; j++)
            {
                if ((vertices[i] - vertices[j]).sqrMagnitude < thresholdSq)
                {
                    // Share the canonical index of the earlier duplicate.
                    weldMap[i] = weldMap[j];
                    break;
                }
            }
        }
    }

    // Applies a delta to vertex i and all other vertices that share its canonical index,
    // but only if the twin is within range of the hit point. This prevents symmetric
    // vertices on the opposite side of the mesh from moving when they shouldn't.
    private void ApplyWeldedDelta(int i, Vector3 delta, Vector3 localHitCenter)
    {
        int canonical = weldMap[i];

        // Use a slightly larger radius for twins so seam vertices don't get orphaned,
        // but still exclude vertices that are clearly on the opposite side.
        float twinRadiusSq = hitRadius * hitRadius * 4f;

        for (int j = 0; j < vertices.Length; j++)
        {
            if (weldMap[j] != canonical) continue;

            // Only move twins that are actually near the hit point.
            if ((vertices[j] - localHitCenter).sqrMagnitude > twinRadiusSq) continue;

            vertices[j] += delta;
        }
    }

    // -------------------------------------------------------------------------
    // Public Entry Point
    // -------------------------------------------------------------------------

    // Called by AnvilCrafting every time the player lands a hammer strike during shaping.
    // hit              — the raycast hit from the click on the metal surface
    // force            — how hard the strike is (driven by the Force slider in AnvilCrafting)
    // hammerType       — Flat or Peen, determines spread pattern
    // currentMode      — Normal (easier, auto-straightens) or Expert (precise, player-controlled)
    // autoStraighten   — only applies in Normal mode, nudges the metal straight after each hit
    // hammerRightWorld — the right vector of the hammer object in world space, used by peen
    //                    to know which direction to elongate
    // _anvilCollider   — passed in so ClampToAnvil has a fresh reference each strike
    public void OnHammerHit(
        RaycastHit hit,
        float force,
        AnvilMode hammerType,
        SmithingMode currentMode,
        bool autoStraighten,
        Vector3 hammerRightWorld,
        Collider _anvilCollider)
    {
        // Keep anvilCollider up to date in case it wasn't set at AddComponent time.
        anvilCollider = _anvilCollider;

        // Refresh our vertex array from the mesh before each deformation pass.
        vertices = deformingMesh.vertices;

        // Convert the world-space hit point into this object's local space.
        // All deformation math runs in local space for correctness.
        Vector3 localHit = transform.InverseTransformPoint(hit.point);

        // Normal mode: simpler spread, more forgiving, auto-straightens.
        // Expert mode: directional and precise, no auto-correction.
        if (currentMode == SmithingMode.Normal)
            ApplyNormal(localHit, force);
        else
            ApplyExpert(localHit, force, hammerType, hammerRightWorld);

        // In Normal mode, gently nudge vertices back toward center X after
        // each hit so the metal doesn't wander sideways unintentionally.
        //if (autoStraighten && currentMode == SmithingMode.Normal)
         //   AutoStraighten(0.1f);

        // Push the modified vertices back to the mesh and refresh geometry.
        deformingMesh.vertices = vertices;
        deformingMesh.RecalculateNormals();
        deformingMesh.RecalculateBounds();
    }

    // -------------------------------------------------------------------------
    // Normal Mode Deformation
    // -------------------------------------------------------------------------

    // Simpler, more forgiving deformation for Normal smithing mode.
    // Compresses Y and expands X and Z around the hit point.
    // Expansion is calculated relative to the mesh center, not the hit point —
    // this ensures both sides of the mesh spread outward correctly instead of
    // one side growing and the other shrinking.
    private void ApplyNormal(Vector3 center, float force)
    {
        float radius = hitRadius;
        float sqrRadius = radius * radius;
        float invSqrRadius = 1f / sqrRadius;

        float delta = force * 0.03f;

        // How much to compress height vs expand width/length per hit.
        float compressY = 1f - delta * 0.5f;
        float expandX = 1f + delta * 1.2f;
        float expandZ = 1f + delta * 0.2f;

        // Spread is relative to mesh center so both sides expand outward equally.
        Vector3 meshCenter = cachedMeshCenter;

        for (int i = 0; i < vertices.Length; i++)
        {
            // Only process canonical vertices — duplicates are handled via ApplyWeldedDelta.
            if (weldMap[i] != i) continue;

            // Falloff is based on distance from the hit point.
            Vector3 offset = vertices[i] - center;
            float distSqr = offset.sqrMagnitude;
            if (distSqr > sqrRadius) continue;

            float falloff = 1f - (distSqr * invSqrRadius);
            falloff = Mathf.Clamp01(falloff);
            falloff *= falloff;

            // Expansion relative to mesh center — both sides push outward symmetrically.
            Vector3 fromCenter = vertices[i] - meshCenter;
            fromCenter.y *= Mathf.Lerp(1f, compressY, falloff);
            fromCenter.x *= Mathf.Lerp(1f, expandX, falloff);
            fromCenter.z *= Mathf.Lerp(1f, expandZ, falloff);

            Vector3 candidate = meshCenter + fromCenter;
            candidate = ClampToAnvil(vertices[i], candidate);

            // Apply the delta to this vertex and all its nearby welded twins.
            ApplyWeldedDelta(i, candidate - vertices[i], center);
        }
    }

    // -------------------------------------------------------------------------
    // Expert Mode Deformation
    // -------------------------------------------------------------------------

    // Routes to the correct expert deformation based on hammer type.
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

    // Expert Flat: spreads material proportionally based on the current shape of the metal.
    // Wider metal spreads more lengthwise; longer metal spreads more widthwise.
    // This mimics how a real flat hammer works — metal takes the path of least resistance.
    private void ExpertFlat(Vector3 center, float radius, float sqrRadius, float delta)
    {
        float invSqrRadius = 1f / sqrRadius;

        // Measure the extent of affected vertices to determine spread direction.
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        for (int i = 0; i < vertices.Length; i++)
        {
            if (weldMap[i] != i) continue;

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

        // Spread more in the direction that already has less extent — like real metal.
        float spreadX = length / total;
        float spreadZ = width / total;

        float compressY = 1f - delta;
        float expandX = 1f + delta * spreadX;
        float expandZ = 1f + delta * spreadZ;

        // Spread relative to mesh center so both sides push outward equally.
        Vector3 meshCenter = cachedMeshCenter;

        for (int i = 0; i < vertices.Length; i++)
        {
            if (weldMap[i] != i) continue;

            Vector3 offset = vertices[i] - center;
            float distSqr = offset.sqrMagnitude;
            if (distSqr > sqrRadius) continue;

            float falloff = 1f - (distSqr * invSqrRadius);
            falloff = Mathf.Clamp01(falloff);
            falloff *= falloff;

            Vector3 fromCenter = vertices[i] - meshCenter;
            fromCenter.y *= Mathf.Lerp(1f, compressY, falloff);
            fromCenter.x *= Mathf.Lerp(1f, expandX, falloff);
            fromCenter.z *= Mathf.Lerp(1f, expandZ, falloff);

            Vector3 candidate = meshCenter + fromCenter;
            candidate = ClampToAnvil(vertices[i], candidate);

            ApplyWeldedDelta(i, candidate - vertices[i], center);
        }
    }

    // Expert Peen: elongates material strongly in one direction (perpendicular to peen edge).
    // The spread direction is determined by which way the hammer's right vector points.
    // This is how a real cross-peen hammer draws metal out in a specific direction.
    private void ExpertPeen(Vector3 center, float radius, float sqrRadius, float delta, Vector3 hammerRightWorld)
    {
        float invSqrRadius = 1f / sqrRadius;

        float compressY = 1f - delta;

        // Primary = direction the peen face points (less spread).
        // Secondary = perpendicular to peen (strong elongation — this is where metal draws out).
        float expandPrimary = 1f + delta * 0.2f;
        float expandSecondary = 1f + delta * 0.9f;

        // Convert the hammer's world-space right vector into this object's local space
        // to determine whether the peen is aligned more with X or Z.
        Vector3 localPeenDir = transform.InverseTransformDirection(hammerRightWorld);
        bool alongX = Mathf.Abs(localPeenDir.x) > Mathf.Abs(localPeenDir.z);

        // Spread relative to mesh center so both sides push outward equally.
        Vector3 meshCenter = cachedMeshCenter;

        for (int i = 0; i < vertices.Length; i++)
        {
            if (weldMap[i] != i) continue;

            Vector3 offset = vertices[i] - center;
            float distSqr = offset.sqrMagnitude;
            if (distSqr > sqrRadius) continue;

            float falloff = 1f - (distSqr * invSqrRadius);
            falloff = Mathf.Clamp01(falloff);
            falloff *= falloff;

            Vector3 fromCenter = vertices[i] - meshCenter;

            // Always compress downward.
            fromCenter.y *= Mathf.Lerp(1f, compressY, falloff);

            // Spread: primary direction (along peen) gets less; perpendicular gets more.
            if (alongX)
            {
                fromCenter.x *= Mathf.Lerp(1f, expandPrimary, falloff);
                fromCenter.z *= Mathf.Lerp(1f, expandSecondary, falloff);
            }
            else
            {
                fromCenter.z *= Mathf.Lerp(1f, expandPrimary, falloff);
                fromCenter.x *= Mathf.Lerp(1f, expandSecondary, falloff);
            }

            Vector3 candidate = meshCenter + fromCenter;
            candidate = ClampToAnvil(vertices[i], candidate);

            ApplyWeldedDelta(i, candidate - vertices[i], center);
        }
    }

    // -------------------------------------------------------------------------
    // Auto Straighten (Normal Mode Only)
    // -------------------------------------------------------------------------

    // After each hit in Normal mode, gently nudge all vertices toward the mesh's
    // center X. This prevents the metal from drifting sideways over many hits,
    // making Normal mode more forgiving without removing all challenge.
    private void AutoStraighten(float strength)
    {
        Vector3 meshCenter = cachedMeshCenter;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].x = Mathf.Lerp(vertices[i].x, meshCenter.x, strength);
        }
    }

    // -------------------------------------------------------------------------
    // Anvil Floor Clamp
    // -------------------------------------------------------------------------

    // Prevents a vertex from going below the physical anvil surface.
    // Works by raycasting straight down from the candidate position against the
    // anvil's actual collider. This means it respects the horn's curve, the flat
    // top, and the edges — no hardcoded flat floor assumption.
    // originalLocal  — where the vertex was before this hit (fallback if no anvil hit)
    // candidateLocal — where the deformation wants to move it
    private Vector3 ClampToAnvil(Vector3 originalLocal, Vector3 candidateLocal)
    {
        if (anvilCollider == null) return candidateLocal;

        // Convert to world space so we can raycast against the scene collider.
        Vector3 worldCandidate = transform.TransformPoint(candidateLocal);

        // Ray comes from slightly above and shoots straight down.
        Ray ray = new Ray(worldCandidate + Vector3.up * 0.1f, Vector3.down);

        if (anvilCollider.Raycast(ray, out RaycastHit hit, 0.5f))
        {
            // If the candidate would go below where the anvil surface is, push it back up.
            if (worldCandidate.y < hit.point.y)
            {
                worldCandidate.y = hit.point.y;
                return transform.InverseTransformPoint(worldCandidate);
            }
        }

        return candidateLocal;
    }
}