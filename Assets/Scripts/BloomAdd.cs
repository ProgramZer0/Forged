using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloomAdd : MonoBehaviour
{
    [SerializeField] private Bloomery bloomery;
    private void OnTriggerEnter(Collider collision)
    {
        //Debug.Log(collision.transform.GetComponent<Item>().item.name);
        try
        {
            Debug.Log(collision.transform.GetComponent<Item>().item.name);
            if (bloomery.AddItem(collision.transform.GetComponent<Item>().item))
            {
                Destroy(collision.gameObject);
            }
            else
            {
                collision.gameObject.GetComponent<Rigidbody>().AddForce(new Vector3(UnityEngine.Random.Range(0, 20), 20, UnityEngine.Random.Range(0, 20)) * 40);
            }
        }
        catch
        {
            //nothing
        }
    }
}
