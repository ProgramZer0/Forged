using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;

    private Dictionary<int, Items> itemsByID = new Dictionary<int, Items>();
    private Dictionary<string, Items> itemsByName = new Dictionary<string, Items>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
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

        if (!itemsByName.ContainsKey(item.itemName))
            itemsByName[item.itemName] = item;
    }

    public Items GetItemByID(int id)
    {
        if (itemsByID.TryGetValue(id, out var item))
            return item;

        Debug.LogWarning($"Item ID not found: {id}");
        return null;
    }

    public Items GetItemByName(string name)
    {
        if (itemsByName.TryGetValue(name, out var item))
            return item;

        Debug.LogWarning($"Item name not found: {name}");
        return null;
    }
}