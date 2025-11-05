using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnvilPlaces : MonoBehaviour
{
    public GameObject objonAnvil = null;
    public Items emptyItem;
    private void OnTriggerEnter(Collider colid)
    {
        try
        {
            FindObjectOfType<WorkstationScript>().setItemOnAnvil(colid.gameObject.GetComponent<Item>().item);
            objonAnvil = colid.gameObject;
            FindObjectOfType<WorkstationScript>().setObjOnAnvil(objonAnvil);
        }
        catch { 
        
        }
    }
    private void OnTriggerExit(Collider colid)
    {
        objonAnvil = null;
        FindObjectOfType<WorkstationScript>().setObjOnAnvil(objonAnvil);
        FindObjectOfType<WorkstationScript>().setItemOnAnvil(emptyItem);
    }
}
