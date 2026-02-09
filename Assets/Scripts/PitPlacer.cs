using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PitPlacer : MonoBehaviour
{ 
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.transform.GetComponent<Item>().item.name.Contains("Wood"))
        {
            if (FindFirstObjectByType<FirePit>().CanAddCheck())
            {
                FindFirstObjectByType<FirePit>().addWoodenLog();
                Destroy(collision.gameObject);
            }
            else
            {
                if (collision.gameObject.GetComponent<Rigidbody>())
                {
                    collision.gameObject.GetComponent<Rigidbody>().AddForce(new Vector3(UnityEngine.Random.Range(0, 20), 20, UnityEngine.Random.Range(0, 20)) * 40);
                }
            }
        }
        else if (collision.GetComponent<Collider>().tag == "Player" | collision.GetComponent<Collider>().tag == "Terrain" | collision.transform.GetComponent<Item>().item.name.Contains("Charcoal"))
        {
            //nothing
        }
        else
        {
            if (collision.gameObject.GetComponent<Rigidbody>())
            {
                collision.gameObject.GetComponent<Rigidbody>().AddForce(new Vector3(UnityEngine.Random.Range(0, 20), 20, UnityEngine.Random.Range(0, 20)) * 40);
            }
        }
    }
}