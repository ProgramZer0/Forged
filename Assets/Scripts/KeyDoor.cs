using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyDoor : MonoBehaviour
{
    [SerializeField] private Items key;
    [SerializeField] private int DoorNumber;
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.gameObject);
        if(collision.gameObject.GetComponent<Item>().item == key)
        {
            Debug.Log("Door " + DoorNumber + " has key");
            FindObjectOfType<EarthPuzzle1>().setKeys(DoorNumber);
            Destroy(collision.gameObject);
        }
    }
}
