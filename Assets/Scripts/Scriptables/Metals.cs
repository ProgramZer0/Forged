using UnityEngine;

[CreateAssetMenu(fileName = "New Metal", menuName = "Assets/Items/Metal")]
public class Metals : Items
{
    public string Strength;
    public string Durability;
    public string Flashiness;
    public string HeatStage;
    public Mesh metalMesh;
    public void Awake()
    {
        type = Itemtype.Metals;
    }
}

