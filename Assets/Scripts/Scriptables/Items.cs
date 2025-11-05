using UnityEngine;

public enum Itemtype
{
    Metals,
    Dusts,
    Bloom,
    Potions,
    Equipment,
    Tools,
    Default
}

[CreateAssetMenu(fileName = "New Item", menuName = "Assets/Item")]
public class Items : ScriptableObject
{
    public Itemtype type;
    public string itemID;
    public string itemName;
    [TextArea]
    public string itemDescription;
    public string cost;
    public Items nextItem;
    public Sprite itemSprite;
    public GameObject model;
    public bool CanGoInSmeltery;
    public bool impuritySmash;
    public int heatLevel;
    public int heatStage;
}
