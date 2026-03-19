using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
public enum Itemtype { Dust, Bloom, Chunk, Metal, Potions, Equipment, Tools, Default, Ore }
public enum MetalType { Tin, Copper, Bronze, Iron, Steel, Silver, Nickel, Titanium, Oppa, Nameless, Moabilimite, Santillum, Gold, Electrum, Lithium, Poillum, NONE }

[System.Serializable]
public class ItemData
{
    public int itemID;
    public string itemName;
    public string itemDescription;
    public string cost;
    public string prefabPath;
    public Itemtype type;
}

[System.Serializable]
public class ItemCollection
{
    public ItemData[] items;
}

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;

    [SerializeField] private string folderDirName;

    private Dictionary<int, ItemData> itemsByID = new();
    private Dictionary<int, GameObject> prefabByID = new();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        string dir = Path.Combine(Application.streamingAssetsPath, folderDirName);
        LoadItemsFromJSON(dir);
    }

    public void LoadItemsFromJSON(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"Item folder not found: {folderPath}");
            return;
        }

        string[] files = Directory.GetFiles(folderPath, "*.json");

        foreach (string file in files)
        {
            string json = File.ReadAllText(file);

            ItemCollection collection =
                JsonUtility.FromJson<ItemCollection>(json);

            foreach (var item in collection.items)
            {
                RegisterItem(item);
            }
        }

        Debug.Log($"Loaded {itemsByID.Count} items.");
    }

    private void RegisterItem(ItemData item)
    {
        if (itemsByID.ContainsKey(item.itemID))
        {
            Debug.LogWarning($"Duplicate Item ID detected: {item.itemID}");
            return;
        }

        itemsByID[item.itemID] = item;

        GameObject prefab = Resources.Load<GameObject>(item.prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"Prefab not found at path: {item.prefabPath}");
            return;
        }

        prefabByID[item.itemID] = prefab;
    }

    public ItemData GetItemDataById(int id)
    {
        if (itemsByID.TryGetValue(id, out var item))
            return item;

        Debug.LogWarning($"Item ID not found: {id}");
        return null;
    }

    public GameObject GetPrefab(int id)
    {
        if (prefabByID.TryGetValue(id, out var prefab))
            return prefab;

        Debug.LogWarning($"Prefab not found for item ID: {id}");
        return null;
    }

    public GameObject SpawnItem(int id, Vector3 position, Quaternion rot)
    {
        if (!prefabByID.TryGetValue(id, out var prefab))
        {
            Debug.LogError($"No prefab for item ID: {id}");
            return null;
        }

        GameObject obj = Instantiate(prefab, position, rot);

        Items item = obj.GetComponent<Items>();
        if (item != null)
        {
            item.ApplyData(itemsByID[id]);
        }
        else
        {
            Debug.LogWarning($"Spawned prefab missing Items component: {prefab.name}");
        }

        return obj;
    }

    [ContextMenu("Debug Print All Item IDs (Sorted)")]
    public void DebugPrintAllItemIDsSorted()
    {
        Debug.Log("===== ITEM DATABASE ID DUMP (SORTED) START =====");

        if (itemsByID.Count == 0)
        {
            Debug.LogWarning("Item database is empty.");
            return;
        }

        var sortedIDs = itemsByID.Keys.OrderBy(id => id);

        foreach (int id in sortedIDs)
        {
            var item = itemsByID[id];
            Debug.Log($"ID: {id} | Name: {item.itemName} | Prefab: {item.prefabPath}");
        }

        Debug.Log($"Total Items Loaded: {itemsByID.Count}");
        Debug.Log("===== ITEM DATABASE ID DUMP END =====");
    }
}