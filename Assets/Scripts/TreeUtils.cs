using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Harvestables))]
public class TreeUtils : MonoBehaviour
{
    private GameObject Generator;
    private Vector3 groundPos;
    private float timePassed = 0;
    private bool timerOn = false;
    private LayerMask TreeMask;

    private void Awake()
    {
        gameObject.transform.localScale =  new Vector3(0.02f, 0.02f, 0.02f);
        timerOn = true;
        TreeMask = FindObjectOfType<TerrainUtils>().getTreeMask();
    }

    private void Update()
    {
        if (timerOn)
        {
            //Debug.Log(timePassed);
            timePassed = Time.deltaTime + timePassed;

            if(timePassed > 20 && timePassed < 50)
            {
                gameObject.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                setGroundPos();
                gameObject.transform.position = groundPos;
            }
            if(timePassed > 50 && timePassed < 80)
            {
                gameObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                setGroundPos();
                gameObject.transform.position = groundPos;
            }
            if(timePassed > 80 && timePassed < 120)
            {
                gameObject.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                setGroundPos();
                gameObject.transform.position = groundPos;
            }
            if(timePassed > 120)
            {
                setIsReady();
                timerOn = false;
            }
        }
        else
        {
            //Debug.Log("timer off");
        }
    }

    public void setIsReady()
    {
        gameObject.transform.localScale = new Vector3(1, 1, 1);
    }

    public void setGroundPos()
    {
        Vector3 temp = transform.position;
        temp.y = temp.y + 5;

        RaycastHit hit;

        if (Physics.Raycast(temp, Vector3.down, out hit, 40, TreeMask))
        {
            groundPos = hit.point;
        }
    }

    public void SetTimer(int temp)
    {
        timePassed = temp;
    }

    public void FellTree()
    {
        //play anaimation?
        Generator.GetComponent<TreeGen>().spawnTreeWait(20, 60, transform.position);
    }

    public void setGenerator(GameObject temp)
    {
        Generator = temp;
    }
}
