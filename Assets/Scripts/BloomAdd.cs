using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloomAdd : MonoBehaviour
{
    [SerializeField] private Bloomery bloomery;
    private void OnTriggerEnter(Collider collision)
    {
        var itemScript = collision.transform.GetComponent<Items>();
        if (itemScript)
            if (bloomery.AddItem(itemScript.itemData))
        {
            Destroy(collision.gameObject);
        }
        else
        {
            collision.gameObject.GetComponent<Rigidbody>().AddForce(new Vector3(UnityEngine.Random.Range(0, 20), 20, UnityEngine.Random.Range(0, 20)) * 40);
        }
    }
}
