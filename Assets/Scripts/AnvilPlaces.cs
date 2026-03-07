using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnvilPlaces : MonoBehaviour
{
    public GameObject objonAnvil = null;
    public Items emptyItem;
    [SerializeField] private WorkstationScript workstation;
    [SerializeField] private AnvilManager anvilMgr;
    private void OnTriggerEnter(Collider colid)
    {
        try
        {
            workstation.setItemOnAnvil(colid.gameObject.GetComponent<Item>().item);
            objonAnvil = colid.gameObject;
            anvilMgr.SetRotator(objonAnvil);
        }
        catch { 
        
        }
    }
    private void OnTriggerExit(Collider colid)
    {
        objonAnvil = null;
        anvilMgr.SetRotator(objonAnvil);
        workstation.setItemOnAnvil(emptyItem);
    }


    public void ReTriggerCol()
    {
        StartCoroutine(ResetCollider());
    }
    IEnumerator ResetCollider()
    {
        Collider col = GetComponent<Collider>();
        col.enabled = false;
        yield return null;
        col.enabled = true;
    }
}