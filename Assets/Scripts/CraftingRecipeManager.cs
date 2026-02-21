using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class CraftingRecipeManager : MonoBehaviour
{
    public static CraftingRecipeManager Instance;

    private Dictionary<PhaseType, Dictionary<int, Recipe>> singleInputRecipes
        = new Dictionary<PhaseType, Dictionary<int, Recipe>>();

    private Dictionary<PhaseType, List<Recipe>> multiInputRecipes
        = new Dictionary<PhaseType, List<Recipe>>();

    [SerializeField] private string folderDirName;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        string dirName = Path.Combine(Application.streamingAssetsPath, folderDirName);
        LoadRecipesFromFolder(dirName);
        string modDirName = Path.Combine(Application.persistentDataPath, folderDirName);
        LoadRecipesFromFolder(modDirName);
    }

    public void LoadRecipesFromFolder(string folderPath)
    {
        string[] files = Directory.GetFiles(folderPath, "*.json");

        foreach (string file in files)
        {
            string json = File.ReadAllText(file);
            CraftingRecipeCollection collection =
                JsonUtility.FromJson<CraftingRecipeCollection>(json);

            foreach (var recipe in collection.Recipes)
            {
                RegisterRecipe(recipe);
            }
        }
    }

    private void RegisterRecipe(Recipe recipe)
    {
        if (recipe.inputItemIDs.Length == 1)
        {
            if (!singleInputRecipes.ContainsKey(recipe.phaseNeeded))
                singleInputRecipes[recipe.phaseNeeded] = new Dictionary<int, Recipe>();

            int inputID = recipe.inputItemIDs[0];
            singleInputRecipes[recipe.phaseNeeded][inputID] = recipe;
        }
        else
        {
            if (!multiInputRecipes.ContainsKey(recipe.phaseNeeded))
                multiInputRecipes[recipe.phaseNeeded] = new List<Recipe>();

            multiInputRecipes[recipe.phaseNeeded].Add(recipe);
        }
    }
    public List<Recipe> FindAllMatchingRecipes(PhaseType phase, int[] providedInputIDs)
    {
        List<Recipe> matches = new List<Recipe>();

        singleInputRecipes.TryGetValue(phase, out var singleDict);
        multiInputRecipes.TryGetValue(phase, out var multiList);

        if (singleDict == null && multiList == null)
            return matches;

        HashSet<int> providedSet = new HashSet<int>(providedInputIDs);

        if (singleDict != null)
        {
            foreach (var kvp in singleDict)
            {
                if (providedSet.Contains(kvp.Key))
                    matches.Add(kvp.Value);
            }
        }

        if (multiList != null)
        {
            foreach (var recipe in multiList)
            {
                if (AreInputsSubset(recipe.inputItemIDs, providedSet))
                    matches.Add(recipe);
            }
        }

        return matches;
    }
    private bool AreInputsSubset(int[] recipeInputs, HashSet<int> providedSet)
    {
        foreach (int id in recipeInputs)
        {
            if (!providedSet.Contains(id))
                return false;
        }

        return true;
    }
    public Recipe FindRecipe(PhaseType phase, int inputItemID)
    {
        if (singleInputRecipes.TryGetValue(phase, out var phaseDict))
        {
            if (phaseDict.TryGetValue(inputItemID, out var recipe))
                return recipe;
        }

        return null;
    }

    public Recipe FindRecipe(PhaseType phase, int[] inputItemIDs)
    {
        if (!multiInputRecipes.TryGetValue(phase, out var recipes))
            return null;

        foreach (var recipe in recipes)
        {
            if (AreInputsMatching(recipe.inputItemIDs, inputItemIDs))
                return recipe;
        }

        return null;
    }

    private bool AreInputsMatching(int[] recipeInputs, int[] providedInputs)
    {
        if (recipeInputs.Length != providedInputs.Length)
            return false;

        HashSet<int> providedSet = new HashSet<int>(providedInputs);

        foreach (int id in recipeInputs)
        {
            if (!providedSet.Contains(id))
                return false;
        }

        return true;
    }
}