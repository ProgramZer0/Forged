using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColiderFix : MonoBehaviour
{
    private GameObject rigds;

    private void OnCollisionEnter(Collision collision)
    {
        rigds = FindObjectOfType<Controls>().getGameObj();

        //Debug.Log(collision.gameObject.name);
        if (collision.gameObject.name == rigds.name)
        {
            transform.GetComponentInChildren<Rigidbody>().isKinematic = true;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        rigds = FindObjectOfType<Controls>().getGameObj();

        //Debug.Log(collision.gameObject.name);
        if (collision.gameObject.name == rigds.name)
        {
            transform.GetComponentInChildren<Rigidbody>().isKinematic = true;
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        rigds = FindObjectOfType<Controls>().getGameObj();

        //Debug.Log(collision.gameObject.name);
        if (collision.gameObject.name == rigds.name)
        {
            transform.GetComponentInChildren<Rigidbody>().isKinematic = false;
        }
    }
}
