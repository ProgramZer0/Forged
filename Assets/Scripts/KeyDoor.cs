using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyDoor : MonoBehaviour
{
    [SerializeField] private int keyID;
    [SerializeField] private int DoorNumber;
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.gameObject);
        if(collision.gameObject.GetComponent<Items>())
        {
            if (collision.gameObject.GetComponent<Items>().itemID == keyID)
            {
                Debug.Log("Door " + DoorNumber + " has key");
                FindFirstObjectByType<EarthPuzzle1>().setKeys(DoorNumber);
                Destroy(collision.gameObject);
            }
        }
    }
}
