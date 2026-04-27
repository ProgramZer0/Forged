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

    // Current vertex positions in local space. Updated before each deformation pass.
    private Vector3[] vertices;

    // The original Y values of each vertex at awake time.
    // Kept for reference — could be used later for reset or thickness enforcement.
    private float[] initialHeights;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    void Awake()
    {
        // Grab the mesh from the MeshFilter on this object and start working on it.
        deformingMesh = GetComponent<MeshFilter>().mesh;
        vertices = deformingMesh.vertices;

        // Store the initial Y height of every vertex for reference.
        initialHeights = new float[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            initialHeights[i] = vertices[i].y;
    }

    // -------------------------------------------------------------------------
    // Public Entry Point
    // -------------------------------------------------------------------------

    // Called by AnvilCrafting every time the player lands a hammer strike during shaping.
    // hit         — the raycast hit from the click on the metal surface
    // force       — how hard the strike is (driven by the Force slider in AnvilCrafting)
    // hammerType  — Flat or Peen, determines spread pattern
    // currentMode — Normal (easier, auto-straightens) or Expert (precise, player-controlled)
    // autoStraighten — only applies in Normal mode, nudges the metal straight after each hit
    // hitType     — where on the anvil the hit landed (Main, Edge, WarpInward/horn)
    // hammerRightWorld — the right vector of the hammer object in world space, used by peen
    //                    to know which direction to elongate
    // _anvilCollider — passed in so ClampToAnvil has a fresh reference each strike
    public void OnHammerHit(
        RaycastHit hit,
        float force,
        AnvilMode hammerType,
        SmithingMode currentMode,
        bool autoStraighten,
        AnvilHitType hitType,
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

        // WarpInward (hitting the horn area) bends the metal to conform to the
        // horn's curved surface — handled separately from flat-face strikes.
        if (hammerType == AnvilMode.Flat && hitType == AnvilHitType.WarpInward)
        {
            BendToHornSurface(localHit, force);
        }
        else
        {
            // Normal mode: simpler spread, more forgiving, auto-straightens.
            // Expert mode: directional and precise, no auto-correction.
            if (currentMode == SmithingMode.Normal)
                ApplyNormal(localHit, force);
            else
                ApplyExpert(localHit, force, hammerType, hammerRightWorld);

            // In Normal mode, gently nudge vertices back toward center X after
            // each hit so the metal doesn't wander sideways unintentionally.
            if (autoStraighten && currentMode == SmithingMode.Normal)
                AutoStraighten(0.1f);
        }

        // Push the modified vertices back to the mesh and refresh geometry.
        deformingMesh.vertices = vertices;
        deformingMesh.RecalculateNormals();
        deformingMesh.RecalculateBounds();
    }

    // -------------------------------------------------------------------------
    // Horn Bending
    // -------------------------------------------------------------------------

    // Bends vertices in the hit radius toward the curved horn surface.
    // Instead of pushing straight down (which would sink into the horn),
    // each vertex rays toward the horn collider and lerps to where it lands.
    // Edge vertices are biased more strongly since that's where bending is most visible.
    private void BendToHornSurface(Vector3 center, float force)
    {
        float radius = hitRadius;
        float sqrRadius = radius * radius;
        float invSqrRadius = 1f / sqrRadius;

        // How strongly vertices move toward the horn surface per hit.
        float bendStrength = force * 0.015f;

        // Max distance to search for the horn surface from each vertex.
        float maxRayDistance = 0.4f;

        Vector3 meshCenter = deformingMesh.bounds.center;
        Vector3 worldCenter = transform.TransformPoint(center);

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldVertex = transform.TransformPoint(vertices[i]);

            Vector3 offset = worldVertex - worldCenter;
            float distSqr = offset.sqrMagnitude;

            // Skip vertices outside the hit radius.
            if (distSqr > sqrRadius) continue;

            // Falloff: vertices closer to hit point move more.
            float falloff = 1f - (distSqr * invSqrRadius);
            falloff = Mathf.Clamp01(falloff);
            falloff *= falloff; // Squared for a smoother, more natural curve.

            // Edge bias: vertices further from center X get bent more,
            // since the edges are what actually wrap around the horn.
            float edgeDistance = Mathf.Abs(vertices[i].x - meshCenter.x);
            float edgeBias = Mathf.Clamp01(edgeDistance * 2f);

            float finalStrength = bendStrength * falloff * edgeBias;
            if (finalStrength <= 0f) continue;

            // Ray from this vertex toward the horn collider center.
            Vector3 directionToHorn = (anvilCollider.bounds.center - worldVertex).normalized;
            Ray ray = new Ray(worldVertex + directionToHorn * 0.01f, directionToHorn);

            // If the ray hits the horn, lerp this vertex toward that contact point.
            if (anvilCollider.Raycast(ray, out RaycastHit hornHit, maxRayDistance))
            {
                worldVertex = Vector3.Lerp(worldVertex, hornHit.point, finalStrength);
                vertices[i] = transform.InverseTransformPoint(worldVertex);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Normal Mode Deformation
    // -------------------------------------------------------------------------

    // Simpler, more forgiving deformation for Normal smithing mode.
    // Compresses Y (height) and expands X and Z (width and length) around the hit point.
    // The anvil floor clamp prevents any vertex from sinking below the anvil surface.
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

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 offset = vertices[i] - center;
            float distSqr = offset.sqrMagnitude;
            if (distSqr > sqrRadius) continue;

            // Smooth falloff — vertices further from hit move less.
            float falloff = 1f - (distSqr * invSqrRadius);
            falloff = Mathf.Clamp01(falloff);
            falloff *= falloff;

            Vector3 relative = offset;

            // Apply compression/expansion scaled by falloff.
            relative.y *= Mathf.Lerp(1f, compressY, falloff);
            relative.x *= Mathf.Lerp(1f, expandX, falloff);
            relative.z *= Mathf.Lerp(1f, expandZ, falloff);

            Vector3 candidate = center + relative;

            // Prevent the vertex from going below the actual anvil surface.
            candidate = ClampToAnvil(vertices[i], candidate);

            vertices[i] = candidate;
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
        // Measure the extent of affected vertices to determine spread direction.
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

        // Spread more in the direction that already has less extent — like real metal.
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

            Vector3 candidate = center + relative;
            candidate = ClampToAnvil(vertices[i], candidate);

            vertices[i] = candidate;
        }
    }

    // Expert Peen: elongates material strongly in one direction (perpendicular to peen edge).
    // The spread direction is determined by which way the hammer's right vector points.
    // This is how a real cross-peen hammer draws metal out in a specific direction.
    private void ExpertPeen(Vector3 center, float radius, float sqrRadius, float delta, Vector3 hammerRightWorld)
    {
        float compressY = 1f - delta;

        // Primary = direction the peen face points (less spread).
        // Secondary = perpendicular to peen (strong elongation — this is where metal draws out).
        float expandPrimary = 1f + delta * 0.2f;
        float expandSecondary = 1f + delta * 0.9f;

        // Convert the hammer's world-space right vector into this object's local space
        // to determine whether the peen is aligned more with X or Z.
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

            // Always compress downward.
            relative.y *= Mathf.Lerp(1f, compressY, falloff);

            // Spread: primary direction (along peen) gets less; perpendicular gets more.
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

            Vector3 candidate = center + relative;
            candidate = ClampToAnvil(vertices[i], candidate);

            vertices[i] = candidate;
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
        Vector3 meshCenter = deformingMesh.bounds.center;

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
    // originalLocal — where the vertex was before this hit (fallback if no anvil hit)
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