using System.Linq; // make sure this is at the top
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;
    [SerializeField] private string resourcesFolder;
    private Dictionary<int, Items> itemsByID = new Dictionary<int, Items>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        LoadItemsFromResources(resourcesFolder);
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
            Items item = itemsByID[id];
            Debug.Log($"Item ID: {id} | Name: {item.name}");
        }

        Debug.Log($"Total Items Loaded: {itemsByID.Count}");
        Debug.Log("===== ITEM DATABASE ID DUMP END =====");
    }


    public void LoadItemsFromResources(string folderPath)
    {
        Items[] loadedItems = Resources.LoadAll<Items>(folderPath);

        foreach (var item in loadedItems)
        {
            RegisterItem(item);
        }

        Debug.Log($"Loaded {itemsByID.Count} items.");
    }

    private void RegisterItem(Items item)
    {
        if (itemsByID.ContainsKey(item.itemID))
        {
            Debug.LogWarning($"Duplicate Item ID detected: {item.itemID}");
            return;
        }

        itemsByID[item.itemID] = item;
    }

    public Items GetItemByID(int id)
    {
        if (itemsByID.TryGetValue(id, out var item))
            return item;

        Debug.LogWarning($"Item ID not found: {id}");
        return null;
    }
}