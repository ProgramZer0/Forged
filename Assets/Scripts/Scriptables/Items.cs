using UnityEngine;

public enum Itemtype
{
    Metals,
    Dusts,
    Bloom,
    Potions,
    Equipment,
    Tools,
    Default,
    Ore
}

[CreateAssetMenu(fileName = "New Item", menuName = "Assets/Item")]
public class Items : ScriptableObject
{
    public Itemtype type;
    public int itemID;
    public string itemName;

    [TextArea]
    public string itemDescription;
    public string cost;
    public Sprite itemSprite;
    public GameObject model;
    public PhaseType currentPhase = PhaseType.NONE;
    public float heatTimer = 0f;
    public float condensed = 0f;
    public Color baseColor; 
    
}
