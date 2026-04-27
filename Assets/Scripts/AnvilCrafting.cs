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
    //[SerializeField] private LayerMask itemMask;
    [SerializeField] private Slider Force;
    [SerializeField] private float perHitHeat = 0.5f;
    [SerializeField] private AnvilManager anvilMgr;
    [SerializeField] private CraftingRecipeManager recipeManager;
    [SerializeField] private ItemDatabase itemDatabase;

    private Vector3[] targetVertices;
    private Mesh targetMesh;
    private float hitForce = 0.03f;
    private float hitSurface = 0.2f;
    private Items itemScriptOnAnvil;
    private Mesh workingMesh;
    private float originalHeight;

    //private GameObject currentObj;
    private SmithingMode currentSmithingMode = SmithingMode.Normal;


    private void CacheTargetMesh(GameObject targetPrefab)
    {
        MeshFilter mf = targetPrefab.GetComponentInChildren<MeshFilter>();
        targetMesh = mf.sharedMesh;

        targetVertices = targetMesh.vertices;
    }
    public void ChangeSmithingMode(SmithingMode mode) { currentSmithingMode = mode; }
    public bool HandleShapingEditor(RaycastHit hit)
    {
        if (hit.transform != null)
        {
            if (itemScriptOnAnvil == null)
                if (!TryToGrabItem(hit))
                    return false;

            Recipe shapingRecipe = recipeManager.FindRecipe(PhaseType.Shaping, itemScriptOnAnvil.itemID);

            if (shapingRecipe != null)
            {
                float tempNeeded = shapingRecipe.requiredValue * 20;

                if (itemScriptOnAnvil.heatTimer >= tempNeeded)
                {
                    Vector3 direction = anvilMgr.GetHitDirection(hit);
                    AnvilHitType hitType = DetermineHitType(hit);
                    EditMesh(direction, hit.point, Force.value * hitForce, hitType, hit);
                    return true;
                }
            }
        }

        return false;
    }
    
    private AnvilHitType DetermineHitType(RaycastHit hit)
    {
        //Vector3 localPoint = AnvilPos.transform.InverseTransformPoint(hit.point);
        Vector3 localPoint = transform.InverseTransformPoint(hit.point);

        if (localPoint.x > 0.4f)
            return AnvilHitType.Edge;

        if (localPoint.x < -0.4f)
            return AnvilHitType.WarpInward;

        return AnvilHitType.Main;
    }

    private bool HandleShapingEditor(Recipe shapingRecipe, RaycastHit hit)
    {
        if (shapingRecipe != null)
        {
            float tempNeeded = shapingRecipe.requiredValue * 20;

            if (itemScriptOnAnvil.heatTimer >= tempNeeded)
            {
                Vector3 direction = anvilMgr.GetHitDirection(hit);
                AnvilHitType hitType = DetermineHitType(hit);
                EditMesh(direction, hit.point, Force.value * hitForce, hitType, hit);
                return true;
            }
        }
        return false;
    }

    private void EditMesh(Vector3 direction, Vector3 worldPoint, float force, AnvilHitType hitType, RaycastHit hit)
    {
        MeshFilter mf = hit.transform.gameObject.GetComponentInChildren<MeshFilter>();
        Mesh mesh = mf.mesh;

        Vector3[] vertices = mesh.vertices;

        // Convert hit point to LOCAL SPACE

        Vector3 localHitPoint = hit.transform.InverseTransformPoint(worldPoint);

        float radius = hitSurface;

        for (int i = 0; i < vertices.Length; i++)
        {
            float distance = Vector3.Distance(vertices[i], localHitPoint);

            if (distance < radius)
            {
                float falloff = 1f - (distance / radius);

                Vector3 localDirection = hit.transform.InverseTransformDirection(direction);

                vertices[i] += localDirection * force * falloff;

                // Optional behavior per hit type
                if (hitType == AnvilHitType.Edge)
                    vertices[i] += Vector3.right * force * 0.2f * falloff;

                if (hitType == AnvilHitType.WarpInward)
                    vertices[i] += Vector3.forward * force * 0.3f * falloff;
            }
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
    public bool HandleCrafting(RaycastHit hit)
    {

        if (hit.collider != null)
        {
            Debug.Log("hit item " + hit.collider.gameObject.name);
            Debug.Log("obj is the obj on anvil:  " + hit.collider.gameObject.name);

            if (itemScriptOnAnvil == null )
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
                Debug.Log("anvil recipe exsist ID: Input Item: " + anvilRecipe.inputItemIDs[0] +
                    ", requiredValue: " + anvilRecipe.requiredValue + ", output Item: " + anvilRecipe.outputItemID);

                ItemData newItem = itemDatabase.GetItemDataById(anvilRecipe.outputItemID);
                Debug.Log("Item: " + newItem.itemName);

                if (newItem != null)
                {
                    ModelChange(newItem, hit);

                }
            }
            else if (shapingRecipe != null)
            {
                Debug.Log("shaping recipe exsist ID: Input Item: " + shapingRecipe.inputItemIDs[0] +
                    ", requiredValue: " + shapingRecipe.requiredValue + ", output Item: " + shapingRecipe.outputItemID);

                return HandleShapingEditor(shapingRecipe, hit);
            }
            else
            {
                //needs heat 
                if (itemScriptOnAnvil.heatTimer >= 0)
                {
                    if (condensingRecipe != null)
                    {
                        Debug.Log("condensing recipe exsist ID: Input Item: " + condensingRecipe.inputItemIDs[0] +
                    ", requiredValue: " + condensingRecipe.requiredValue + ", output Item: " + condensingRecipe.outputItemID);

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
            ApplyNormalCondense(hit);
        else
            ApplyExpertCondense(hit);

        if (itemScriptOnAnvil.condensed >= target)
            ModelChange(itemTo, hit);
        else
            itemScriptOnAnvil.heatTimer -= perHitHeat;
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
    }

    private void ApplyNormalCondense(RaycastHit hit)
    {
        Transform obj = hit.collider.transform;

        float progress = itemScriptOnAnvil.condensed;

        // compress vertically
        float height = Mathf.Lerp(1f, 0.5f, progress);

        // spread lengthwise
        float length = Mathf.Lerp(1f, 1.8f, progress);

        // slightly tighten width
        float width = Mathf.Lerp(1f, 0.8f, progress);

        obj.localScale = new Vector3(width, height, length);

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

                    //currentObj = o;
                    obj = o;
                    break;
                case Itemtype.Metal:
                    if (changeFromItem.type == Itemtype.Dust)
                    {
                        //compress dust to metal
                    }
                    if (changeFromItem.type == Itemtype.Bloom)
                    {
                        itemScript.ApplyJustItemData(changeTo);
                    }
                    if (changeFromItem.type == Itemtype.Ore)
                    {
                        //shape for moab stuff
                    }
                    break;
            }
        }
    }
}
