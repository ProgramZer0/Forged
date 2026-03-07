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

    [SerializeField] private AnvilManager anvilMgr;
    [SerializeField] private CraftingRecipeManager recipeManager;
    [SerializeField] private ItemDatabase itemDatabase;

    private float hitForce = 0.03f;
    private float hitSurface = 0.2f;
    private Items itemOnAnvil;
    private Mesh workingMesh;
    private float originalHeight;

    //private GameObject currentObj;
    private SmithingMode currentSmithingMode = SmithingMode.Normal;

    public void ChangeSmithingMode(SmithingMode mode) { currentSmithingMode = mode; }
    public bool HandleShapingEditor(RaycastHit hit)
    {
        if (hit.transform != null)
        {
            if (itemOnAnvil == null)
                if (!TryToGrabItem(hit))
                    return false;

            Recipe shapingRecipe = recipeManager.FindRecipe(PhaseType.Shaping, itemOnAnvil.itemID);

            if (shapingRecipe != null)
            {
                float tempNeeded = shapingRecipe.requiredValue * 20;

                if (itemOnAnvil.heatTimer >= tempNeeded)
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

            if (itemOnAnvil.heatTimer >= tempNeeded)
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

            if (itemOnAnvil == null )
            {
                Debug.Log("hit is: " + hit.transform.gameObject);
                if (!TryToGrabItem(hit))
                    return false;
            }
            Debug.Log("all valid entering crafting");

            Recipe condensingRecipe = recipeManager.FindRecipe(PhaseType.Condensing, itemOnAnvil.itemID);
            Recipe anvilRecipe = recipeManager.FindRecipe(PhaseType.AnvilHammering, itemOnAnvil.itemID);
            Recipe shapingRecipe = recipeManager.FindRecipe(PhaseType.Shaping, itemOnAnvil.itemID);



            if (anvilRecipe != null)
            {
                Debug.Log("anvil recipe exsist ID: Input Item: " + anvilRecipe.inputItemIDs[0] +
                    ", requiredValue: " + anvilRecipe.requiredValue + ", output Item: " + anvilRecipe.outputItemID);

                Items newItem = itemDatabase.GetItemByID(anvilRecipe.outputItemID);
                Debug.Log("Item: " + newItem.name);

                if (newItem != null)
                {
                    ModelChange(newItem, hit);

                    itemOnAnvil = newItem;
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
                if (itemOnAnvil.heatTimer >= 0)
                {
                    if (condensingRecipe != null)
                    {
                        Debug.Log("condensing recipe exsist ID: Input Item: " + condensingRecipe.inputItemIDs[0] +
                    ", requiredValue: " + condensingRecipe.requiredValue + ", output Item: " + condensingRecipe.outputItemID);

                        Recipe heatingRecipe = recipeManager.FindRecipe(PhaseType.Heating, itemOnAnvil.itemID);
                        if (heatingRecipe != null)
                        {
                            float tempNeeded = heatingRecipe.requiredValue * 20;

                            if (itemOnAnvil.heatTimer >= tempNeeded)
                            {
                                CondenseItem(hit, condensingRecipe);
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


    public Item GetItemFromHit(RaycastHit hit)
    {
        Item item = hit.transform.gameObject.GetComponent<Item>();
        if (item != null)
        {
            Debug.Log("found item on main");
            return item;
        }
        else
        {
            item = hit.transform.parent.GetComponent<Item>();
            if (item != null)
            {
                Debug.Log("found item on parent");
                return item;
            }
        }
        return null;
    }
    private bool TryToGrabItem(RaycastHit hit)
    {
        //Debug.Log("trying to get item");
        Item item = hit.transform.gameObject.GetComponent<Item>();
        if (item != null)
        {
            Debug.Log("found item on main");
            itemOnAnvil = item.item;
            //currentObj = hit.collider.gameObject;
            return true;
        }
        else
        {
            Debug.Log(hit.transform.parent);

            item = hit.transform.parent.GetComponent<Item>();
            if (item != null)
            {
                Debug.Log("found item on parent");
                itemOnAnvil = item.item;
                //currentObj = hit.transform.parent.gameObject;
                return true;
            }
        }

        return false;
    }

    private void CondenseItem(RaycastHit hit, Recipe condensingRecipe)
    {
        float targetPercent = condensingRecipe.requiredValue;

        if (itemOnAnvil.condensed >= targetPercent)
            return;

        float baseStep = Force.value * 0.01f;

        // Optional: diminishing returns
        float efficiency = Mathf.Lerp(1f, 0.2f, itemOnAnvil.condensed);
        float step = baseStep * efficiency;

        float remaining = targetPercent - itemOnAnvil.condensed;
        float appliedStep = Mathf.Min(step, remaining);

        itemOnAnvil.condensed += appliedStep;

        // Calculate scale from progress instead of multiplying
        Vector3 scale;

        if (currentSmithingMode == SmithingMode.Normal)
        {
            scale = GetNormalModeScaleFromProgress(itemOnAnvil.condensed);
            hit.collider.gameObject.transform.localScale = scale;

            if (itemOnAnvil.condensed >= targetPercent)
            {
                //completed
            }
        }
        else
            GetExpertModeScaleFromProgress(hit, condensingRecipe);


    }
    private void GetExpertModeScaleFromProgress(RaycastHit hit, Recipe condensingRecipe)
    {
        workingMesh = hit.transform.GetComponent<Mesh>();
        float force = Force.value;
        float radius = 0.5f;

        if (itemOnAnvil.condensed >= condensingRecipe.requiredValue)
            return;

        // ----- Progress Gain -----
        float baseGain = force * 0.02f;

        float efficiency = Mathf.Lerp(1f, 0.2f, itemOnAnvil.condensed);
        float appliedGain = baseGain * efficiency;

        float remaining = condensingRecipe.requiredValue - itemOnAnvil.condensed;
        appliedGain = Mathf.Min(appliedGain, remaining);

        itemOnAnvil.condensed += appliedGain;

        // ----- Mesh Deformation -----
        Vector3[] vertices = workingMesh.vertices;

        Vector3 localHit = hit.collider.gameObject.transform.InverseTransformPoint(hit.point);
        Vector3 localDirection = hit.collider.gameObject.transform.InverseTransformDirection(Vector3.down);

        float maxHeight = Mathf.Lerp(
            originalHeight,
            originalHeight * 0.5f,
            itemOnAnvil.condensed
        );

        float currentHeight = workingMesh.bounds.size.y;
        float allowedCompression = currentHeight - maxHeight;

        for (int i = 0; i < vertices.Length; i++)
        {
            float distance = Vector3.Distance(vertices[i], localHit);

            if (distance < radius)
            {
                float falloff = 1f - (distance / radius);

                Vector3 move = localDirection * force * 0.01f * falloff;

                // Clamp vertical compression
                if (allowedCompression > 0)
                    vertices[i] += move;
            }
        }

        workingMesh.vertices = vertices;
        workingMesh.RecalculateNormals();
        workingMesh.RecalculateBounds();

        if (itemOnAnvil.condensed >= condensingRecipe.requiredValue)
        {
            //
        }
    }

    private Vector3 GetNormalModeScaleFromProgress(float progress)
    {
        float height = Mathf.Lerp(1f, 0.5f, progress);
        float length = Mathf.Lerp(1f, 1.8f, progress);
        float width = Mathf.Lerp(1f, 0.8f, progress);

        return new Vector3(width, height, length);
    }



    private void ModelChange(Items changeTo, RaycastHit hit)
    {
        GameObject obj = hit.transform.gameObject;

        Items changeFromItem = null;
        Item objItem = null;

        var item = hit.transform.gameObject.GetComponent<Item>();
        if (item != null)
        {
            objItem = item;
            changeFromItem = item.item;
            obj = hit.collider.gameObject;
        }
        else
        {
            item = hit.transform.gameObject.GetComponentInParent<Item>();
            if (item != null)
            {
                objItem = item;
                changeFromItem = item.item;
                obj = hit.transform.parent.gameObject;
            }
        }


        if (obj != null)
        {

            Debug.Log("Changing Models: change to " + changeTo.name);
            Debug.Log("from " + changeFromItem.name);

            switch (changeTo.type)
            {
                case Itemtype.Ore:
                    //this should never happen
                    break;
                case Itemtype.Chunk:
                    if (changeFromItem.type == Itemtype.Ore)
                    {
                        obj.GetComponent<OreSplitter>().SplitOre();
                        objItem.item = changeTo;
                    }
                    break;
                case Itemtype.Dust:

                    Destroy(obj);
                    Vector3 spawnPos = hit.transform.position;
                    GameObject o = UnityEngine.Object.Instantiate(changeTo.model, spawnPos, Quaternion.Euler(0, 0, 0));

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
                        //making bloom texture change to metal texture
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
