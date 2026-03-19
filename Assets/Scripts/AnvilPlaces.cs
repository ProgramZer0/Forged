using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnvilPlaces : MonoBehaviour
{
    public GameObject objonAnvil = null;
    [SerializeField] private WorkstationScript workstation;
    [SerializeField] private AnvilManager anvilMgr;
    private void OnTriggerEnter(Collider colid)
    {

        Items item = colid.gameObject.GetComponent<Items>();
        if(item != null)
        {
            workstation.setItemOnAnvil(item);
            objonAnvil = colid.gameObject;
            anvilMgr.SetRotator(objonAnvil);

            TempManager tm = colid.GetComponent<TempManager>();
            if (tm != null)
                tm.timerEnabled = false;
        }
    }
    private void OnTriggerExit(Collider colid)
    {
        objonAnvil = null;
        anvilMgr.SetRotator(objonAnvil);
        workstation.setItemOnAnvil(null);

        TempManager tm = colid.GetComponent<TempManager>();
        if (tm != null)
            tm.timerEnabled = true;
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