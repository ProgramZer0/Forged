using System.Collections;
using UnityEngine;

/*
 * Bloomery.cs
 * 
 * This script handles the Bloomery mechanics
 *
 * Responsibilities:
 *  - Modify items according to recipes while in the bloomery
 *  - Visual effects of the bloomery (fire, light, text, )
 *  - Handle charcoal/fuel timers and heating
 *  - Handle smeltery modes (basic / upgraded)
 *  - Update UI elements (heat, in-fire indicator, charcoal)
 *
 * Ratio System:
 *  - The amount of charcoal needed per item is how long it burns
 *  - It has to burn that much charcoal to be crafted 
 *  - 15s per charcoal
 *  
 * Upgraded Mode Differences:
 *  - Subtract teir level per second per charcoal and allow a higher (ratio)
 */

public class Bloomery : MonoBehaviour
{
    [Header("Items")]
    [SerializeField] private Items EmptyItem;
    [SerializeField] private Items charcoalItem;
    [SerializeField] private float defaultTimePerCharcoal = 16f;
    [SerializeField] private float defaultMaxRatio = 0.25F;

    [Header("Visuals")]
    [SerializeField] private GameObject bloomEffects;
    [SerializeField] private GameObject bloomDripEffects;
    [SerializeField] private GameObject bloomGunk;
    [SerializeField] private GameObject spawnPos;
    [SerializeField] private GameObject bloomCharcoal1;
    [SerializeField] private GameObject bloomCharcoal2;
    [SerializeField] private GameObject bloomCharcoal3;
    [SerializeField] private Light heatLight;

    [Header("References")]
    [SerializeField] private WorkstationScript workstationScript;
    [SerializeField] private CraftingRecipeManager recipeManager;
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Teir")]
    [SerializeField] private int teir = 1;

    private Items currentItem = null;
    private int currentItemCount = 0;
    private int currentCharcoal = 0;
    private float charcoalNeededPerItem = 1;
    private int charcoalUsed = 0;
    private Recipe currentRecipe;
    private bool blooming = false;
    private float bloomTimer = 0f;
    private float charocalTimer = 0f;
    private float timePerCharcoal = 0;
    private float maxRatio = 0;

    void Start()
    {
        bloomDripEffects.SetActive(false);
        bloomGunk.SetActive(false);
        heatLight.intensity = 0;
        bloomEffects.SetActive(false);
        HandleTeir();
    }

    private void HandleTeir()
    {
        timePerCharcoal = defaultTimePerCharcoal - teir;
        maxRatio = defaultMaxRatio / teir;
    }

    public void SetTeir(int t)
    {
        teir = t;
        HandleTeir();
    }

    void Update()
    {
        if (blooming)
        {
            HandleBlooming();
        }
    }
    
    public bool AddItem(Items item)
    {
        if (item == charcoalItem)
        {
            currentCharcoal++;
            UpdateCharcoalVisuals();
            TryStartBlooming();
            return true;
        }

        if (currentItem == null)
        {
            Recipe bloomRecipe = recipeManager.FindRecipe(PhaseType.Bloomery, item.itemID);

            if (bloomRecipe == null) return false;
            if (bloomRecipe.requiredValue > maxRatio) return false;

            currentRecipe = bloomRecipe;

            currentItem = item;
            currentItemCount = 1;
            TryStartBlooming();
            return true;
        }

        if (currentItem == item)
        {
            currentItemCount++;
            TryStartBlooming();
            return true;
        }

        // if it's a different item type, reject it
        return false;
    }

    private void TryStartBlooming()
    {
        if (blooming) return;
        if (currentItem == null) return;
        if (currentRecipe == null) return;

        charcoalNeededPerItem = Mathf.CeilToInt(1f / currentRecipe.requiredValue);

        if (currentItemCount > 0 && currentCharcoal >= charcoalNeededPerItem)
        {
            blooming = true;
            bloomTimer = 0f;
            charocalTimer = 0f;
            bloomEffects.SetActive(true);
            bloomGunk.SetActive(true);
        }
    }

    private void HandleBlooming()
    {
        bloomTimer += Time.deltaTime;
        charocalTimer += Time.deltaTime;

        // Exponential scaling: tweak the exponent to taste
        float normalizedHeat = bloomTimer / 100f; // 0–1
        heatLight.intensity = Mathf.Pow(normalizedHeat, 0.5f) * 10f; // sqrt curve, ramps faster early
        
        // optional: show drips after some time
        if (bloomTimer >= (timePerCharcoal/2))
            bloomDripEffects.SetActive(true);


        // complete bloom after reaching item's heat requirement
        if (charocalTimer >= timePerCharcoal)
        {
            charcoalUsed++;
            currentCharcoal--;
            charocalTimer = 0;


            //if we've used enough charcoal and an item we create an item 
            if (charcoalUsed >= charcoalNeededPerItem && currentItemCount > 0)
            {
                currentItemCount--;
                charcoalUsed = 0;
                Items nextItem = itemDatabase.GetItemByID(currentRecipe.outputItemID);

                // spawn the final item
                Instantiate(nextItem.model, spawnPos.transform.position, Quaternion.Euler(90, 0, 0));

                //Not enough to continue
                if (currentCharcoal < charcoalNeededPerItem || currentItemCount <= 0)
                {
                    bloomTimer = 0;
                    blooming = false;
                    StartCoroutine(UpdateVisualTurnOff());
                }
            }
        }
    }
    private void ResetBloom()
    {
        if (currentItemCount <= 0)
        {
            currentRecipe = null;
            currentItem = null;
            charcoalNeededPerItem = 1;
        }
        if (currentCharcoal <= 0)
        {
            charcoalUsed = 0;
            currentCharcoal = 0;
        }
        HandleTeir();
    }

    private IEnumerator UpdateVisualTurnOff()
    {
        UpdateCharcoalVisuals();
        bloomDripEffects.SetActive(false);
        bloomGunk.SetActive(false);
        ResetBloom();
        
        
        float normalizedHeat = bloomTimer / 100f; // 0–1
        heatLight.intensity = Mathf.Pow(normalizedHeat, 0.5f) * 10f;
        yield return new WaitForSeconds(0.2f);
        normalizedHeat = normalizedHeat * 0.8f;
        heatLight.intensity = Mathf.Pow(normalizedHeat, 0.5f) * 10f;
        yield return new WaitForSeconds(0.2f);
        normalizedHeat = normalizedHeat * 0.6f;
        heatLight.intensity = Mathf.Pow(normalizedHeat, 0.5f) * 10f;
        yield return new WaitForSeconds(0.2f);
        normalizedHeat = normalizedHeat * 0.5f;
        heatLight.intensity = Mathf.Pow(normalizedHeat, 0.5f) * 10f;
        yield return new WaitForSeconds(0.2f);

        heatLight.intensity = 0;
        bloomEffects.SetActive(false);
    }

    private void UpdateCharcoalVisuals()
    {
        bloomCharcoal1.SetActive(currentCharcoal >= 2);
        bloomCharcoal2.SetActive(currentCharcoal >= 6);
        bloomCharcoal3.SetActive(currentCharcoal >= 16);
    }

    public Items ReturnEmpty() => EmptyItem;
}