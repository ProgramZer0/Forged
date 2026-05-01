using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum AnvilHitType
{
    WarpInward,
    Indent,
    Main,
    Edge
}

public class AnvilCrafting : MonoBehaviour
{
    [SerializeField] private GameObject AnvilPos;
    [SerializeField] private Slider Force;
    [SerializeField] private float perHitHeat = 0.5f;
    [SerializeField] private AnvilManager anvilMgr;
    [SerializeField] private CraftingRecipeManager recipeManager;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private Collider anvilCollider;

    private Vector3[] targetVertices;
    private Mesh targetMesh;
    private float hitForce = 0.03f;
    private float hitSurface = 0.2f;
    private Items itemScriptOnAnvil;
    private Mesh workingMesh;
    private float originalHeight;

    private SmithingMode currentSmithingMode = SmithingMode.Normal;

    private void CacheTargetMesh(GameObject targetPrefab)
    {
        MeshFilter mf = targetPrefab.GetComponentInChildren<MeshFilter>();
        targetMesh = mf.sharedMesh;
        targetVertices = targetMesh.vertices;
    }

    public void ChangeSmithingMode(SmithingMode mode) { currentSmithingMode = mode; }

    public bool HandleShapingEditor(Recipe shapingRecipe, RaycastHit hit)
    {
        Debug.Log("shaping editor");
        if (hit.transform == null) return false;

        if (itemScriptOnAnvil == null)
            if (!TryToGrabItem(hit))
                return false;

        if (shapingRecipe == null) return false;

        float tempNeeded = shapingRecipe.requiredValue * 20;
        if (itemScriptOnAnvil.heatTimer < tempNeeded) return false;

        // Add ShapeManager if not already present
        ShapeManager sm = itemScriptOnAnvil.GetComponent<ShapeManager>();
        if (sm == null)
            sm = itemScriptOnAnvil.gameObject.AddComponent<ShapeManager>();

        sm.anvilCollider = anvilCollider;

        sm.OnHammerHit(
            hit,
            Force.value,
            anvilMgr.GetCurrentAnvilMode(),
            currentSmithingMode,
            true,
            anvilMgr.GetHammerRight(),
            anvilCollider
        );

        return true;
    }

    public bool HandleCrafting(RaycastHit hit)
    {
        if (hit.collider != null)
        {
            Debug.Log("hit item " + hit.collider.gameObject.name);

            TryToGrabItem(hit);
            if (itemScriptOnAnvil == null)
            {
                Debug.Log("hit is: " + hit.transform.gameObject);
                if (!TryToGrabItem(hit))
                    return false;
            }
            Debug.Log("all valid entering crafting");

            Recipe condensingRecipe = recipeManager.FindRecipe(PhaseType.Condensing, itemScriptOnAnvil.itemID);
            Recipe anvilRecipe = recipeManager.FindRecipe(PhaseType.AnvilHammering, itemScriptOnAnvil.itemID);
            Recipe shapingRecipe = recipeManager.FindRecipe(PhaseType.Shaping, itemScriptOnAnvil.itemID);

            if (anvilRecipe != null)
            {
                Debug.Log("anvil recipe exists");
                ItemData newItem = itemDatabase.GetItemDataById(anvilRecipe.outputItemID);
                if (newItem != null)
                    ModelChange(newItem, hit);
            }
            else if (shapingRecipe != null)
            {
                return HandleShapingEditor(shapingRecipe, hit);
            }
            else
            {
                if (anvilMgr.GetCurrentAnvilMode() != AnvilMode.Flat)
                    return false;
                //needs heat 
                if (itemScriptOnAnvil.heatTimer >= 0)
                {
                    if (condensingRecipe != null)
                    {
                        Debug.Log("condensing recipe exists");

                        Recipe heatingRecipe = recipeManager.FindRecipe(PhaseType.Heating, itemScriptOnAnvil.itemID);
                        if (heatingRecipe != null)
                        {
                            float tempNeeded = heatingRecipe.requiredValue * 20;
                            ItemData newItem = itemDatabase.GetItemDataById(condensingRecipe.outputItemID);

                            if (itemScriptOnAnvil.heatTimer >= tempNeeded)
                            {
                                CondenseItem(hit, condensingRecipe, newItem);
                                return true;
                            }
                            // not high enough heat
                        }
                        // if nothing else is found 
                    }
                }
                //no heat at all
            }
        }
        return false;
    }

    public Items GetItemScriptFromHit(RaycastHit hit)
    {
        Items itemScript = hit.transform.gameObject.GetComponent<Items>();
        if (itemScript != null)
        {
            Debug.Log("found item on main");
            return itemScript;
        }
        else
        {
            itemScript = hit.transform.parent.GetComponent<Items>();
            if (itemScript != null)
            {
                Debug.Log("found item on parent");
                return itemScript;
            }
        }
        return null;
    }

    private bool TryToGrabItem(RaycastHit hit)
    {
        itemScriptOnAnvil = null;
        Items item = hit.transform.GetComponent<Items>();

        if (item == null)
            item = hit.transform.GetComponentInParent<Items>();

        if (item == null)
            return false;

        itemScriptOnAnvil = item;

        MeshFilter mf = item.GetComponentInChildren<MeshFilter>();

        if (mf != null)
        {
            workingMesh = Instantiate(mf.mesh);
            mf.mesh = workingMesh;
            originalHeight = workingMesh.bounds.size.y;
        }

        return true;
    }

    private void CondenseItem(RaycastHit hit, Recipe condensingRecipe, ItemData itemTo)
    {
        float target = condensingRecipe.requiredValue;

        if (itemScriptOnAnvil.condensed >= target)
            return;

        float baseStep = Force.value * 0.01f;
        float efficiency = Mathf.Lerp(1f, 0.2f, itemScriptOnAnvil.condensed);
        float step = baseStep * efficiency;
        float remaining = target - itemScriptOnAnvil.condensed;
        float appliedStep = Mathf.Min(step, remaining);

        itemScriptOnAnvil.condensed += appliedStep;

        if (currentSmithingMode == SmithingMode.Normal)
            ApplyNormalCondense(hit, condensingRecipe);
        else
            ApplyExpertCondense(hit);

        if (itemScriptOnAnvil.condensed >= target)
        {
            // Hard snap to target on final hit to eliminate any remaining vertex drift
            MeshFilter mf = hit.collider.transform.GetComponentInChildren<MeshFilter>();
            if (mf != null)
            {
                CondensingData condensingData = itemScriptOnAnvil.gameObject.GetComponent<CondensingData>();
                if (condensingData != null)
                {
                    Mesh mesh = mf.mesh;
                    mesh.vertices = condensingData.targetVertices;
                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();

                    MeshCollider mc = hit.collider.transform.GetComponentInChildren<MeshCollider>();
                    if (mc != null)
                        mc.sharedMesh = mesh;
                }
            }

            ModelChange(itemTo, hit);
        }
        else
        {
            itemScriptOnAnvil.heatTimer -= perHitHeat;
        }
    }

    /// <summary>
    /// Generates target bar vertices whose total volume equals
    /// originalVolume * volumeRatio (e.g. 0.7 = 70% of original).
    /// Shape is a flat elongated ingot. Called only once per item
    /// and cached on a CondensingData component.
    /// </summary>
    private Vector3[] GenerateBarTargetVertices(Mesh mesh, float volumeRatio, Transform transform)
    {
        Bounds b = mesh.bounds;
        float originalVolume = CalculateMeshVolume(mesh, transform);
        float targetVolume = originalVolume * volumeRatio;

        Vector3 ratio = new Vector3(2f, 1f, 5f); // X:Y:Z shape of rod

        float k = Mathf.Pow(targetVolume / (ratio.x * ratio.y * ratio.z), 1f / 3f);

        float barX = ratio.x * k;
        float barY = ratio.y * k;
        float barZ = ratio.z * k;

        Vector3 center = b.center;
        Vector3 half = new Vector3(barX * 0.5f, barY * 0.5f, barZ * 0.5f);

        Vector3[] meshVerts = mesh.vertices;
        Vector3[] targets = new Vector3[meshVerts.Length];

        for (int i = 0; i < meshVerts.Length; i++)
        {
            Vector3 dir = meshVerts[i] - center;

            // Hard snap each vertex to the nearest face of the bar cuboid
            // based on which axis it's dominant on
            float absX = Mathf.Abs(dir.x);
            float absY = Mathf.Abs(dir.y);
            float absZ = Mathf.Abs(dir.z);

            targets[i] = new Vector3(
                Mathf.Sign(dir.x == 0 ? 1 : dir.x) * half.x,
                Mathf.Sign(dir.y == 0 ? 1 : dir.y) * half.y,
                Mathf.Sign(dir.z == 0 ? 1 : dir.z) * half.z
            ) + center;
        }

        return targets;
    }

    private Mesh GenerateCleanBarMesh(Mesh currentMesh, int vertexCount)
    {
        Bounds b = currentMesh.bounds;
        Vector3 center = b.center;
        Vector3 half = b.extents;

        Mesh cleanMesh = new Mesh();

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector3> normals = new List<Vector3>();

        int vertsPerFace = vertexCount / 6;
        int gridSize = Mathf.Max(2, Mathf.RoundToInt(Mathf.Sqrt(vertsPerFace)));

        // (normal, uAxis, vAxis, faceCenter)
        (Vector3 normal, Vector3 uAxis, Vector3 vAxis, Vector3 faceCenter)[] faces =
        {
            (Vector3.up,      Vector3.right,   Vector3.forward, center + Vector3.up      * half.y),
            (Vector3.down,    Vector3.right,   Vector3.forward, center + Vector3.down    * half.y),
            (Vector3.right,   Vector3.forward, Vector3.up,      center + Vector3.right   * half.x),
            (Vector3.left,    Vector3.forward, Vector3.up,      center + Vector3.left    * half.x),
            (Vector3.back,    Vector3.right,   Vector3.up,      center + Vector3.back    * half.z),
            (Vector3.forward, Vector3.right,   Vector3.up,      center + Vector3.forward * half.z),
        };

        for (int f = 0; f < faces.Length; f++)
        {
            var face = faces[f];
            int baseIndex = verts.Count;

            Vector3 uExtent = Vector3.Scale(face.uAxis, half);
            Vector3 vExtent = Vector3.Scale(face.vAxis, half);

            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    float u = Mathf.Lerp(-1f, 1f, (float)col / (gridSize - 1));
                    float v = Mathf.Lerp(-1f, 1f, (float)row / (gridSize - 1));

                    Vector3 vert = face.faceCenter + u * uExtent + v * vExtent;

                    // Subtle bevel — nudge corners slightly inward
                    float bevel = 0.015f;
                    vert = Vector3.Lerp(vert, center, bevel * (Mathf.Abs(u) + Mathf.Abs(v)));

                    verts.Add(vert);
                    normals.Add(face.normal);
                }
            }

            for (int row = 0; row < gridSize - 1; row++)
            {
                for (int col = 0; col < gridSize - 1; col++)
                {
                    int i = baseIndex + row * gridSize + col;

                    // Even faces (up, right, forward) — one winding
                    // Odd faces (down, left, back) — opposite winding
                    if (f % 2 == 0)
                    {
                        tris.Add(i);
                        tris.Add(i + gridSize);
                        tris.Add(i + 1);
                        tris.Add(i + 1);
                        tris.Add(i + gridSize);
                        tris.Add(i + gridSize + 1);
                    }
                    else
                    {
                        tris.Add(i);
                        tris.Add(i + 1);
                        tris.Add(i + gridSize);
                        tris.Add(i + 1);
                        tris.Add(i + gridSize + 1);
                        tris.Add(i + gridSize);
                    }
                }
            }
        }

        cleanMesh.vertices = verts.ToArray();
        cleanMesh.triangles = tris.ToArray();
        cleanMesh.normals = normals.ToArray();
        cleanMesh.RecalculateBounds();

        return cleanMesh;
    }

    private void ApplyNormalCondense(RaycastHit hit, Recipe condensingRecipe)
    {
        MeshFilter mf = hit.collider.transform.GetComponentInChildren<MeshFilter>();
        if (mf == null) return;

        Mesh mesh = mf.mesh;
        GameObject itemObj = itemScriptOnAnvil.gameObject;

        // Use cached CondensingData if it exists, otherwise generate and attach it now
        CondensingData condensingData = itemObj.GetComponent<CondensingData>();
        if (condensingData == null)
        {
            condensingData = itemObj.AddComponent<CondensingData>();
            condensingData.targetVertices = GenerateBarTargetVertices(mesh, condensingRecipe.requiredValue, hit.collider.transform);
            Debug.Log("Generated and cached condensing target bar mesh.");
        }

        Vector3[] condensingTargetVertices = condensingData.targetVertices;

        if (condensingTargetVertices.Length != mesh.vertices.Length)
        {
            Debug.LogWarning("Vertex count mismatch between mesh and condensing target.");
            return;
        }

        Vector3[] vertices = mesh.vertices;

        // Lerp t is driven by progress mesh reaches target exactly when condensing completes
        float raw = Mathf.Clamp01(itemScriptOnAnvil.condensed / condensingRecipe.requiredValue);
        float progress;
        Debug.Log("raw is " + raw);

        if (raw < 0.1f)
            progress = Mathf.SmoothStep(0f, 1f, Mathf.Pow(raw, 2f));
        else if (raw < 0.4f)
            progress = Mathf.SmoothStep(0f, 1f, Mathf.Pow(raw, 2.5f));
        else if (raw < 0.9f)
            progress = Mathf.SmoothStep(0f, 1f, Mathf.Pow(raw, 8f));
        else
        {
            float phase3End = Mathf.SmoothStep(0f, 1f, Mathf.Pow(0.9f, 8f));
            progress = Mathf.Lerp(phase3End, 1f, Mathf.SmoothStep(0f, 1f, (raw - 0.9f) / 0.1f));
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = Vector3.Lerp(vertices[i], condensingTargetVertices[i], progress);
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshCollider mc = hit.collider.transform.GetComponent<MeshCollider>();
        if (mc != null)
            mc.sharedMesh = mesh;

        ApplyNormalMaterial(hit.collider.gameObject, progress);
    }

    private void ApplyNormalMaterial(GameObject obj, float progress)
    {
        Renderer r = obj.GetComponentInChildren<Renderer>();

        if (r == null)
            return;

        Color startColor = new Color(0.35f, 0.32f, 0.30f); // dull ore
        Color metalColor = new Color(0.85f, 0.55f, 0.25f); // refined metal

        r.material.color = Color.Lerp(startColor, metalColor, progress);

        if (r.material.HasProperty("_Metallic"))
            r.material.SetFloat("_Metallic", Mathf.Lerp(0f, 0.8f, progress));

        if (r.material.HasProperty("_Smoothness"))
            r.material.SetFloat("_Smoothness", Mathf.Lerp(0.1f, 0.6f, progress));
    }

    private void ApplyExpertCondense(RaycastHit hit)
    {
        MeshFilter mf = hit.transform.GetComponent<MeshFilter>();
        Mesh mesh = mf.mesh;

        Vector3[] vertices = mesh.vertices;
        Vector3 localHit = hit.transform.InverseTransformPoint(hit.point);
        Vector3 localDirection = hit.transform.InverseTransformDirection(Vector3.down);

        float radius = 0.5f;
        float force = Force.value;

        float maxHeight = Mathf.Lerp(originalHeight, originalHeight * 0.5f, itemScriptOnAnvil.condensed);
        float currentHeight = mesh.bounds.size.y;
        float allowedCompression = currentHeight - maxHeight;

        for (int i = 0; i < vertices.Length; i++)
        {
            float distance = Vector3.Distance(vertices[i], localHit);

            if (distance < radius)
            {
                float falloff = 1f - (distance / radius);
                Vector3 move = localDirection * force * 0.01f * falloff;

                if (allowedCompression > 0)
                    vertices[i] += move;
            }
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshCollider mc = hit.transform.GetComponent<MeshCollider>();
        if (mc != null)
            mc.sharedMesh = mesh;
    }

    private void ModelChange(ItemData changeTo, RaycastHit hit)
    {
        GameObject obj = hit.transform.gameObject;

        ItemData changeFromItem = null;
        Items itemScript = null;

        var item = hit.transform.gameObject.GetComponent<Items>();
        if (item != null)
        {
            itemScript = item;
            changeFromItem = item.itemData;
            obj = hit.collider.gameObject;
        }
        else
        {
            item = hit.transform.gameObject.GetComponentInParent<Items>();
            if (item != null)
            {
                itemScript = item;
                changeFromItem = item.itemData;
                obj = hit.transform.parent.gameObject;
            }
        }

        if (obj != null)
        {
            Debug.Log("Changing Models: change to " + changeTo.itemName);
            Debug.Log("from " + changeFromItem.itemName);

            switch (changeTo.type)
            {
                case Itemtype.Ore:
                    //this should never happen
                    break;
                case Itemtype.Chunk:
                    if (changeFromItem.type == Itemtype.Ore)
                    {
                        obj.GetComponent<OreSplitter>().SplitOre();
                        itemScript.ApplyJustItemData(changeTo);
                    }
                    break;
                case Itemtype.Dust:
                    Destroy(obj);
                    Vector3 spawnPos = hit.transform.position;
                    GameObject o = itemDatabase.SpawnItem(changeTo.itemID, spawnPos, Quaternion.Euler(0, 0, 0));
                    obj = o;
                    break;
                case Itemtype.Metal:
                    if (changeFromItem.type == Itemtype.Dust)
                    {
                        // compress dust to metal
                    }
                    if (changeFromItem.type == Itemtype.Bloom)
                    {
                        MeshFilter mf = obj.GetComponentInChildren<MeshFilter>();
                        if (mf != null)
                        {
                            Mesh cleanMesh = GenerateCleanBarMesh(mf.mesh, 800);
                            mf.mesh = cleanMesh;

                            MeshCollider mc = obj.GetComponentInChildren<MeshCollider>();
                            if (mc != null)
                                mc.sharedMesh = cleanMesh;
                        }

                        itemScript.ApplyJustItemData(changeTo);
                    }
                    if (changeFromItem.type == Itemtype.Ore)
                    {
                        // shape for moab stuff
                    }
                    break;
            }
        }
    }

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
}