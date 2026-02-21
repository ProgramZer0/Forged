using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnvilPlaces : MonoBehaviour
{
    public GameObject objonAnvil = null;
    public Items emptyItem;
    [SerializeField] private WorkstationScript workstation;
    private void OnTriggerEnter(Collider colid)
    {
        try
        {
            workstation.setItemOnAnvil(colid.gameObject.GetComponent<Item>().item);
            objonAnvil = colid.gameObject;
            workstation.setObjOnAnvil(objonAnvil);
        }
        catch { 
        
        }
    }
    private void OnTriggerExit(Collider colid)
    {
        objonAnvil = null;
        workstation.setObjOnAnvil(objonAnvil);
        workstation.setItemOnAnvil(emptyItem);
    }
}