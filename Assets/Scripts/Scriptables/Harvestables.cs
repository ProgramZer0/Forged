using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HarvestType
{
    Default
}

[CreateAssetMenu(fileName = "New Harvest", menuName = "Assets/Harvestable")]
public class Harvestables : ScriptableObject
{
    public string harvestID; //ID use for generating
    public string harvestName; //For ease of view
    public Items dropItem; //droping item
    public int itemAmmount; //amount dropped
    public int strength;  //the required tool to mine
    public GameObject model; //required model to spawn
    //public bool CanBeHarvested = true;
}
