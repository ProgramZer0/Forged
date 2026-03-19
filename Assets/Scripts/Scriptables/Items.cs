using UnityEngine;

public class Items : MonoBehaviour
{
    public int itemID;
    public string itemName;
    public string itemDescription;
    public string cost;

    public Itemtype type;

    [Header("Runtime Metal State")]
    public PhaseType currentPhase = PhaseType.NONE;
    public float heatTimer = 0f;
    public float condensed = 0f;

    public Color baseColor;

    public ItemData itemData;

    public void ApplyData(ItemData data)
    {
        itemData = data;
        itemID = data.itemID;
        itemName = data.itemName;
        itemDescription = data.itemDescription;
        cost = data.cost;

        type = data.type;

        // IMPORTANT: reset runtime values
        currentPhase = PhaseType.NONE;
        heatTimer = 0f;
        condensed = 0f;
        if (GetComponent<Renderer>())
            baseColor = GetComponent<Renderer>().material.color;
        else
            baseColor = Color.white;
    }
    public void ApplyJustItemData(ItemData data)
    {
        itemData = data;
        itemID = data.itemID;
        itemName = data.itemName;
        itemDescription = data.itemDescription;
        cost = data.cost;

        type = data.type;
    }

}