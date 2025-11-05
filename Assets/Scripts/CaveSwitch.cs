using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaveSwitch : MonoBehaviour
{
    [SerializeField] GameObject CaveSpawn;

    private void OnTriggerEnter(Collider cold)
    {
        if(cold.gameObject.tag == "Player")
        {
            cold.transform.position = CaveSpawn.transform.position;

            //if the player is carying the cart it goes with
            if (cold.gameObject)
            {

            }
        }
    }
}
