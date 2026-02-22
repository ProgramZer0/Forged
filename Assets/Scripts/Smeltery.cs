using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/*
 * Smeltery.cs
 * 
 * This script handles the smeltery mechanics
 *
 * Responsibilities:
 *  - Add TempManager script to items when removed from smeltery
 *  - Handle initial item color and color changes based on heat
 *  - Modify items according to recipes while in the smeltery
 *  - Visual effects of the smeltery (fire, light, UI indicators)
 *  - Handle charcoal/fuel timers and heating
 *  - Handle smeltery modes (basic / upgraded)
 *  - Update UI elements (heat, in-fire indicator, charcoal)
 *
 * Heat System:
 *  - Heat levels: 1–10 (optional, can be derived from timer/temperature)
 *  - Each level takes longer to reach depending on tier (e.g., +20 sec per level)
 *  - Certain items may be destroyed if left too long at a given heat level
 *
 * Upgraded Mode Differences:
 *  - Adds TempManager automatically and sets item's heat timer
 *  - Modifies item color and item class properties
 *  - Continues heating if charcoal is present
 *  - Maximum temperature is set internally in UI (tier 1–8)
 *  - If item stays in same stage >30 sec, enters “curing” mode
 *
 * Notes:
 *  - TempManager handles cooling and visual tint after item leaves the smeltery
 *  - Heat is continuous; tiers are mainly for gameplay scaling
 *  - Components are enabled/disabled instead of destroyed for performance
 */

public class Smeltery : MonoBehaviour
{
    [Header("UI & Effects")]
    [SerializeField] private Button PutInFireButton;
    [SerializeField] private GameObject heatUI;
    [SerializeField] private GameObject CharcoalIndicator;
    [SerializeField] private GameObject InFireUI;
    [SerializeField] private GameObject Fire;
    [SerializeField] private Animator Tongs;
    [SerializeField] private Light heatLight;
    private Renderer rend;
    private Color baseColor;

    [Header("Tier Settings")]
    [SerializeField] private int tierLevel = 1; // 1–8
    private float maxTemp;
    private float heatIncreaseRate;
    private float heatDecreaseRate;

    [Header("Smeltery State")]
    public Items currentItem;
    private GameObject currentObj;
    [SerializeField] private Items EmptyItem;
    [SerializeField] private Items CharItem;
    private float heat = 0f;
    private float charcoalFuel = 0f;
    private bool inFire = false;

    [Header("Timers")]
    private float timeAtStage = 0f;

    [Header("References")]
    [SerializeField] private WorkstationScript workstationScript;
    [SerializeField] private CraftingRecipeManager recipeManager;
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Curing settings")]
    private float heatCheckTimer = 0f;          // counts up to 30 seconds
    private float heatAtCheckStart = 0f;        // stores heat when the 30-sec window started
    private const float curingDelay = 30f;      // must be stable for 30 seconds
    private const float heatStableThreshold = 20f; // max allowed heat change to start curing


    [Header("Optimization")]
    private Recipe currentHeatingRecipe;
    private int requiredHeatLevel;
    private Coroutine heatingCheckCoroutine;
    private Coroutine curingCheckCoroutine;

    private void Start()
    {
        SetTierValues();
        currentItem = EmptyItem;

        PutInFireButton.onClick.AddListener(ToggleFire);
        UpdateUI();
    }

    private void Update()
    {
        HandleFuel();
        HandleHeat();
        HandleItemHeating();

        // Check if item is in smeltery and not empty
        if (currentItem != null && currentItem != EmptyItem)
        {
            HandleCuringTimer();
        }
        UpdateVisuals();
        CheckHeatingRecipes();
        CheckCuringRecipes();
        //HandleItemOnTongs();
    }
    [ContextMenu("add fuel")] public void AddTestCharcoal() { AddCharcoal(100); }

    #region Core Methods
    public void SetTier(int teir) { tierLevel = teir; }
    private void SetTierValues()
    {
        //heatIncreaseRate = 1.0f * tierLevel;
        heatIncreaseRate = 1f;
        heatDecreaseRate = 0.5f / tierLevel;
        maxTemp = 100f + 20f * tierLevel;
    }

    private void HandleFuel()
    {
        if (charcoalFuel > 0)
        {
            charcoalFuel -= Time.deltaTime;
            charcoalFuel = Mathf.Max(charcoalFuel, 0);
        }
    }

    private void HandleHeat()
    {
        if (charcoalFuel > 0)
        {
            heat += heatIncreaseRate * Time.deltaTime;
        }
        else
        {
            heat -= heatDecreaseRate * Time.deltaTime;
        }

        heat = Mathf.Clamp(heat, 0, maxTemp);

        // Timer for stage (optional curing logic)
        if (heat > 0 && currentItem != EmptyItem)
            timeAtStage += Time.deltaTime;
        else
            timeAtStage = 0f;
    }

    private void UpdateVisuals()
    {
        // Fire light intensity
        if (heatLight != null)
        {
            // Exponential scaling: tweak the exponent to taste
            float normalizedHeat = heat / maxTemp; // 0–1
            heatLight.intensity = Mathf.Pow(normalizedHeat, 0.5f) * 10f; // sqrt curve, ramps faster early
        }

        // Fire visual
        if (Fire != null)
            Fire.SetActive(charcoalFuel > 0);

        // Charcoal indicator
        if (CharcoalIndicator != null)
            CharcoalIndicator.SetActive(charcoalFuel > 0);

        // In-fire UI
        if (InFireUI != null)
            InFireUI.SetActive(inFire);

        //COLOR visual
        if (currentObj != null)
        {
            ModifyColor();
        }
    }

    public void HandleItemOnTongs()
    {
        if (workstationScript == null) return;

        currentItem = workstationScript.getItemOnTongs();
        currentObj = workstationScript.getObjOnTongs();

        if (currentObj == null) return;

        rend = currentObj.GetComponentInChildren<Renderer>();
        if (rend == null) return;

        if (currentItem.baseColor == default)
        {
            baseColor = rend.material.color;
            currentItem.baseColor = baseColor;
        }
        else
        {
            baseColor = currentItem.baseColor;
        }
    }
    private void HandleCuringTimer()
    {
        if (currentItem == null || currentItem == EmptyItem) return;

        // If heat changed more than threshold, reset the stable timer
        if (Mathf.Abs(currentItem.heatTimer - heatAtCheckStart) > heatStableThreshold)
        {
            heatCheckTimer = 0f;
            heatAtCheckStart = currentItem.heatTimer; // restart from new heat
            timeAtStage = 0f;                         // reset curing timer
            return;
        }

        // Increment stable timer
        heatCheckTimer += Time.deltaTime;

        // Only increment stage timer if stable for full curingDelay
        if (heatCheckTimer >= curingDelay)
        {
            timeAtStage += Time.deltaTime;
        }
    }
    private void HandleItemHeating()
    {
        if (currentObj == null || currentItem == EmptyItem) return;

        float heatDiff = heat - currentItem.heatTimer;

        // Only transfer if item is cooler than smeltery
        if (heatDiff > 0.01f)
        {
            // Realistic transfer: proportional to difference
            float transferRate = 0.2f; // fraction per second
            currentItem.heatTimer += heatDiff * transferRate * Time.deltaTime;

            // Cutoff: if very close to smeltery heat, snap to it
            if (Mathf.Abs(heat - currentItem.heatTimer) < 0.5f)
                currentItem.heatTimer = heat;
        }

        float cutoff = heat * 0.95f; 
        if (currentItem.heatTimer > cutoff)
            currentItem.heatTimer = cutoff;
    }
    private int GetCurrentItemHeatLevel()
    {
        if (currentItem == null) return 0;
        return Mathf.FloorToInt(currentItem.heatTimer / 20f);
    }

    #endregion

    #region Public Methods

    public void AddCharcoal(float amount)
    {
        charcoalFuel += amount;
    }
    private void StartCuringCheck()
    {
        if (currentItem == null || currentItem == EmptyItem) return;

        Recipe curingRecipe = recipeManager.FindRecipe(PhaseType.Curing, currentItem.itemID);
        if (curingRecipe == null) return;

        // Stop any existing coroutine
        if (curingCheckCoroutine != null)
            StopCoroutine(curingCheckCoroutine);

        curingCheckCoroutine = StartCoroutine(CuringCheckRoutine(curingRecipe));
    }
    public void PutItemInSmeltery(Items item)
    {
        if (item == null) return;

        currentItem = item;

        StartCuringCheck();
        StartHeatingCheck();
    }

    private void StartHeatingCheck()
    {
        // Find the heating recipe once
        currentHeatingRecipe = recipeManager.FindRecipe(PhaseType.Heating, currentItem.itemID);

        if (currentHeatingRecipe != null)
        {
            requiredHeatLevel = Mathf.FloorToInt(currentHeatingRecipe.requiredValue);

            // Stop old coroutine if running
            if (heatingCheckCoroutine != null)
                StopCoroutine(heatingCheckCoroutine);

            // Start coroutine to check when item could reach heat
            heatingCheckCoroutine = StartCoroutine(HeatingCheckRoutine());
        }
    }

    public void RemoveItemFromSmeltery()
    {
        if (currentObj == null || currentItem == EmptyItem || currentItem == null) return;

        // Add TempManager to item if not present
        try
        {

            TempManager tm  = currentObj.GetComponent<TempManager>();
            if (tm == null)
                tm = currentObj.AddComponent<TempManager>();

            tm.currentHeat = currentItem.heatTimer;
            tm.enabled = true;
        }
        catch
        {
            //error
            return;
        }

        
        // Reset smeltery state
        currentItem = EmptyItem;
        currentObj = null;
        heat = 0f;
        timeAtStage = 0f;
        inFire = false;
    }

    public void ToggleFire()
    {
        inFire = !inFire;

        if (Tongs != null)
            Tongs.SetBool("FireTongs", inFire);
    }

    #endregion

    #region Trigger / Charcoal Pickup

    private void OnTriggerEnter(Collider other)
    {
        Item itemComponent = other.GetComponent<Item>();
        if (itemComponent == null) return;

        Items pickedItem = itemComponent.item;

        if (pickedItem == CharItem)
        {
            AddCharcoal(60f * tierLevel);
            Destroy(other.gameObject);
        }
        else
        {
            // Nudge non-charcoal items
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(new Vector3(Random.Range(0, 20), 5, Random.Range(0, 20)) * 10);
            }
        }
    }

    private void ModifyColor()
    {
        if (rend == null) return;

        float max = 200f;
        float heatPercent = Mathf.Clamp01(currentItem.heatTimer / max);

        Color heatTint = new Color(1f, 0.35f, 0.05f);

        Color finalColor = Color.Lerp(baseColor, heatTint, heatPercent * 0.5f);

        rend.material.color = finalColor;
    }

    #endregion

    #region Crafting section

    private void CheckHeatingRecipes()
    {
        if (currentItem == null || currentItem == EmptyItem) return;

        // Find the first recipe for this item in the Heating phase
        Recipe heatingRecipe = recipeManager.FindRecipe(PhaseType.Heating, currentItem.itemID);
        if (heatingRecipe == null) return;

        int heatlvl = GetCurrentItemHeatLevel();
        if (heatingRecipe.outputItemID == currentItem.itemID)
        {
            currentItem.currentPhase = PhaseType.Heating;
            return;
        }

        // Check if the item reached the required heat
        if (heatlvl >= heatingRecipe.requiredValue)
        {
            Items newItem = itemDatabase.GetItemByID(heatingRecipe.outputItemID);
            if (newItem != null)
            {
                float oldHeat = currentItem.heatTimer;
                ModelChange(newItem);
                currentItem = newItem;
                currentItem.heatTimer = oldHeat;
                HandleItemOnTongs();
            }
        }
    }

    private void ModelChange(Items changeTo)
    {
        //will cover this later when i do visual edits to all the items.
    }

    private void CheckCuringRecipes()
    {
        if (currentItem == null || currentItem == EmptyItem) return;

        // Only check curing if the item has been at the current stage long enough
        Recipe curingRecipe = recipeManager.FindRecipe(PhaseType.Curing, currentItem.itemID);
        if (curingRecipe == null) return;


        int heatlvl = GetCurrentItemHeatLevel();

        int timeInlvl = heatlvl * 15;

        // Check if the item has been in the smeltery long enough to cure
        if (heatlvl == curingRecipe.requiredValue) 
        {
            if (timeAtStage >= timeInlvl)
            {
                Items newItem = itemDatabase.GetItemByID(curingRecipe.outputItemID);
                if (newItem != null)
                {
                    float oldHeat = currentItem.heatTimer;
                    ModelChange(newItem);
                    currentItem = newItem;
                    currentItem.heatTimer = oldHeat;
                    HandleItemOnTongs();
                }
            }
        }
    }

    private IEnumerator HeatingCheckRoutine()
    {
        if (currentItem == null || currentHeatingRecipe == null) yield break;

        // Convert heat level to required heat
        float targetHeat = requiredHeatLevel * 20f;

        // Wait until item reaches target heat
        while (currentItem != null && currentItem.heatTimer < targetHeat)
        {
            float heatDiff = targetHeat - currentItem.heatTimer;

            // Estimate time to reach target heat
            float estimatedTime = heatDiff / (heatIncreaseRate * 0.2f); // 0.2f = transferRate in HandleItemHeating
            yield return new WaitForSeconds(Mathf.Max(estimatedTime, 0.1f));
        }

        // Item reached target heat — apply recipe
        if (currentItem != null)
        {
            Items newItem = itemDatabase.GetItemByID(currentHeatingRecipe.outputItemID);
            if (newItem != null)
            {
                float oldHeat = currentItem.heatTimer;
                ModelChange(newItem);
                currentItem = newItem;
                currentItem.heatTimer = oldHeat;
                HandleItemOnTongs();
            }
        }
    }
    private IEnumerator CuringCheckRoutine(Recipe curingRecipe)
    {
        if (currentItem == null || curingRecipe == null) yield break;

        int requiredHeatLevel = Mathf.FloorToInt(curingRecipe.requiredValue);

        while (currentItem != null)
        {
            int heatLevel = GetCurrentItemHeatLevel();

            if (heatLevel >= requiredHeatLevel)
            {
                // Estimate time to stable heat
                float heatDiff = Mathf.Abs(currentItem.heatTimer - heatAtCheckStart);
                float timeToStableHeat = Mathf.Max(0f, (heatDiff > heatStableThreshold) ? (heatDiff - heatStableThreshold) / (heatIncreaseRate * 0.2f) : 0f);

                // Time left to reach curing delay
                float remainingCuringTime = Mathf.Max(curingDelay - heatCheckTimer, 0f);

                float waitTime = Mathf.Max(timeToStableHeat, 0f) + remainingCuringTime;

                yield return new WaitForSeconds(Mathf.Max(waitTime, 0.1f)); // wait at least 0.1s

                // After wait, check if item is still valid
                if (currentItem == null) yield break;

                // If still meets condition, apply recipe
                if (timeAtStage >= requiredHeatLevel * 15f) // or your timeInLevel formula
                {
                    Items newItem = itemDatabase.GetItemByID(curingRecipe.outputItemID);
                    if (newItem != null)
                    {
                        float oldHeat = currentItem.heatTimer;
                        ModelChange(newItem);
                        currentItem = newItem;
                        currentItem.heatTimer = oldHeat;
                        HandleItemOnTongs();
                    }
                    yield break; // cured, stop coroutine
                }
            }

            // If not ready, loop again
            yield return new WaitForSeconds(1f);
        }
    }

    #endregion

    #region Optional UI Update

    private void UpdateUI()
    {
        if (heatUI != null)
            heatUI.SetActive(heat > 0);
        if (CharcoalIndicator != null)
            CharcoalIndicator.SetActive(charcoalFuel > 0);
        if (InFireUI != null)
            InFireUI.SetActive(inFire);
    }

    #endregion
}