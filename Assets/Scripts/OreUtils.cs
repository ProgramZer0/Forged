using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OreUtils : MonoBehaviour
{
    private GameObject Generator;
    public void FellOre()
    {
        //play anaimation?
        Generator.GetComponent<TreeGen>().spawnTreeWait(120, 300, transform.position);
    }

    public void setGenerator(GameObject temp)
    {
        Generator = temp;
    }
}
