using UnityEngine;


//Item Type determines:
/*  If an item gets destroyed or not and at what temp
 *  it also determines if it can be consumed, put into hand, also some other scripts 
 *  
 */
public enum Itemtype
{
    Dust,
    Bloom,
    Chunk,
    Metal,
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
