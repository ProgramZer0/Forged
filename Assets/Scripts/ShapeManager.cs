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
    private bool hasBeenBoxed = false;


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
        float volumeBefore = CalculateMeshVolume(deformingMesh, null);

        Debug.Log("volume before change " + volumeBefore);

        Vector3 localHit = transform.InverseTransformPoint(hit.point);

        if (currentMode == SmithingMode.Normal && anvilMgr.sliderOn)
        {
            // Normal mode with slider: nudge all vertices toward target shape.
            ApplyNormalAssist(volumeBefore, force);
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
        deformingMesh.vertices = vertices;
        deformingMesh.RecalculateNormals();
        deformingMesh.RecalculateBounds();

        float volumeAfter = CalculateMeshVolume(deformingMesh, null);

        Debug.Log("volume after change " + volumeAfter);

        /*
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
        }*/



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
    private void ApplyNormalAssist(float currentVol, float force)
    {
        if (anvilMgr == null) return;

        Bounds b = deformingMesh.bounds;

        if (!hasBeenBoxed)
        {
            SnapToBox(currentVol);
            hasBeenBoxed = true;
            return; // let this hit just do the snap, deform next hit
        }

        float ratiox = anvilMgr.GetXSliderHelper();
        float ratioy = anvilMgr.GetYSliderHelper();
        float ratioz = anvilMgr.GetZSliderHelper();

        if (ratiox <= 0f || ratioy <= 0f || ratioz <= 0f) return;

        // Current bounds.
        Vector3 min = vertices[0];
        Vector3 max = vertices[0];
        for (int i = 1; i < vertices.Length; i++)
        {
            min = Vector3.Min(min, vertices[i]);
            max = Vector3.Max(max, vertices[i]);
        }
        Vector3 current = max - min;
        Vector3 currentCenter = (min + max) * 0.5f;

        if (current.x < 0.0001f || current.y < 0.0001f || current.z < 0.0001f) return;

        // Convert current dimensions into ratios by normalizing so they sum to 1.
        // This puts current shape and target shape in the same space for lerping.
        float dimSum = current.x + current.y + current.z;
        float currentRatioX = current.x / dimSum;
        float currentRatioY = current.y / dimSum;
        float currentRatioZ = current.z / dimSum;

        // How far to move ratios this hit — small so it feels gradual.
        float lerpT = Mathf.Clamp01(assistStrength * force * 0.05f);

        // Lerp current ratios toward target ratios.
        // This is what moves — not the dimensions directly.
        float newRatioX = Mathf.Lerp(currentRatioX, ratiox, lerpT);
        float newRatioY = Mathf.Lerp(currentRatioY, ratioy, lerpT);
        float newRatioZ = Mathf.Lerp(currentRatioZ, ratioz, lerpT);

        // Find which axis changed the most — that one is the primary and stays fixed.
        float changeX = Mathf.Abs(newRatioX - currentRatioX);
        float changeY = Mathf.Abs(newRatioY - currentRatioY);
        float changeZ = Mathf.Abs(newRatioZ - currentRatioZ);

        if (changeX >= changeY && changeX >= changeZ)
        {
            // X is primary — scale Y and Z down to fill the remainder.
            float remainder = 1f - newRatioX;
            float otherSum = newRatioY + newRatioZ;
            if (otherSum > 0.0001f)
            {
                newRatioY = newRatioY / otherSum * remainder;
                newRatioZ = newRatioZ / otherSum * remainder;
            }
        }
        else if (changeY >= changeX && changeY >= changeZ)
        {
            float remainder = 1f - newRatioY;
            float otherSum = newRatioX + newRatioZ;
            if (otherSum > 0.0001f)
            {
                newRatioX = newRatioX / otherSum * remainder;
                newRatioZ = newRatioZ / otherSum * remainder;
            }
        }
        else
        {
            float remainder = 1f - newRatioZ;
            float otherSum = newRatioX + newRatioY;
            if (otherSum > 0.0001f)
            {
                newRatioX = newRatioX / otherSum * remainder;
                newRatioY = newRatioY / otherSum * remainder;
            }
        }

        // Derive new dimensions from lerped ratios + current volume.
        // Volume is guaranteed by construction — same formula as before.
        Debug.Log("x:" + newRatioX + ", "+ "y:" + newRatioY + ", " +"z:" + newRatioZ + ", together:" + (newRatioX+ newRatioY+newRatioZ));
        float scaleProduct = newRatioX * newRatioY * newRatioZ;
        if (scaleProduct <= 0f) return;

        float k = Mathf.Pow(currentVol / scaleProduct, 1f / 3f);

        float barX = newRatioX * k;
        float barY = newRatioY * k;
        float barZ = newRatioZ * k;

        Vector3 center = currentCenter;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i];

            // Normalized position within current bounds (-0.5 to 0.5 per axis).
            Vector3 normalized = new Vector3(
                (v.x - currentCenter.x) / current.x,
                (v.y - currentCenter.y) / current.y,
                (v.z - currentCenter.z) / current.z
            );

            // Place vertex at same normalized position within new dimensions.
            vertices[i] = new Vector3(
                center.x + normalized.x * barX,
                center.y + normalized.y * barY,
                center.z + normalized.z * barZ
            );
        }

    }

    private void SnapToBox(float volume)
    {
        // Use current bounds proportions as the starting box shape.
        Vector3 currentCenter = GetCurrentCenter();
        Vector3 current = GetCurrentDimensions();

        float dimSum = current.x + current.y + current.z;
        float rx = current.x / dimSum;
        float ry = current.y / dimSum;
        float rz = current.z / dimSum;

        float scaleProduct = rx * ry * rz;
        float k = Mathf.Pow(volume / scaleProduct, 1f / 3f);

        float barX = rx * k;
        float barY = ry * k;
        float barZ = rz * k;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i];

            Vector3 normalized = new Vector3(
                (v.x - currentCenter.x) / current.x,
                (v.y - currentCenter.y) / current.y,
                (v.z - currentCenter.z) / current.z
            );

            vertices[i] = new Vector3(
                currentCenter.x + normalized.x * barX,
                currentCenter.y + normalized.y * barY,
                currentCenter.z + normalized.z * barZ
            );
        }

        deformingMesh.vertices = vertices;
        deformingMesh.RecalculateNormals();
        deformingMesh.RecalculateBounds();

        MeshCollider mc = GetComponentInChildren<MeshCollider>();
        if (mc != null)
            mc.sharedMesh = deformingMesh;
    }
    // -------------------------------------------------------------------------
    // Volume
    // -------------------------------------------------------------------------

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

            // If object is transformed (scaled/rotated), apply it
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