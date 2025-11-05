using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EarthPuzzle1 : MonoBehaviour
{
    [SerializeField] private GameObject lockedDoor;
    [SerializeField] private GameObject UnlockedDoor;
    [SerializeField] private GameObject key1;
    [SerializeField] private GameObject key2;
    [SerializeField] private GameObject key3;

    private bool key1In = false;
    private bool key2In = false;
    private bool key3In = false;

    private void Start()
    {
        key1In = false;
        key2In = false;
        key3In = false;
        lockedDoor.SetActive(true);
        UnlockedDoor.SetActive(false);
    }
    private void FixedUpdate()
    {
        key1.SetActive(key1In);
        key2.SetActive(key2In);
        key3.SetActive(key3In);

        if(key3In && key2In && key1In)
        {
            lockedDoor.SetActive(false);
            UnlockedDoor.SetActive(true);
        }
    }

    public void setKeys(int door)
    {
        switch (door)
        {
            case 1:
                key1In = true;
                break;
            case 2:
                key2In = true;
                break;
            case 3:
                key3In = true;
                break;
            default:
                break;
        }    
    }
}
