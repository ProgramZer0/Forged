using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LazerScriptLast : MonoBehaviour
{
    [SerializeField] private bool hasLight = false;
    [SerializeField] private bool doorUnlocked = false;

    [SerializeField] private GameObject LockedDoor;
    [SerializeField] private GameObject UnlockedDoor;

    private LineRenderer lineRenderer;
    private Vector3 Original;
    private void Start()
    {
        lineRenderer = gameObject.GetComponentInParent<LineRenderer>();
        Original = transform.position;
        Original.y = 289.5f;
        lineRenderer.SetPosition(0, Original);
        if (hasLight)
            DrawLazer();
        LockedDoor.SetActive(true);
        UnlockedDoor.SetActive(false);
    }
    private void Update()
    {
        if(hasLight)
        {
            DrawLazer();

            if (!doorUnlocked)
            {
                unlockDoor();
            }
        } 
        else
        {
            lineRenderer.SetPosition(1, Original);
        }
    }

    private void unlockDoor()
    {
        doorUnlocked = true;
        LockedDoor.SetActive(false);
        UnlockedDoor.SetActive(true);
    }

    private void DrawLazer()
    {
        Vector3 temp = new Vector3(445.031006f, 296.583002f, 3053.68311f);
        lineRenderer.SetPosition(1, temp);
    }

    public void SetLight(bool light)
    {
        hasLight = light;
    }
}
