using UnityEngine;

[CreateAssetMenu(fileName = "New Tool", menuName = "Assets/Items/Tool")]
public class Tools : Items
{
    public string strength;
    public string durability;
    public string Cost;
    
    public void Awake()
    {
        type = Itemtype.Metals;
    }
}

