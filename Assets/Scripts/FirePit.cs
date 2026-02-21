using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirePit : MonoBehaviour
{
    [SerializeField] private GameObject Wood1;
    [SerializeField] private GameObject Wood2;
    [SerializeField] private GameObject Wood3;
    [SerializeField] private GameObject charWood1;
    [SerializeField] private GameObject charWood2;
    [SerializeField] private GameObject charWood3;
    [SerializeField] private GameObject WoodPlaced;
    [SerializeField] private GameObject WoodBurning;
    [SerializeField] private Items charcoalItem;
    [SerializeField] private GameObject fireReady;
    [SerializeField] private Light FireLight;
    [SerializeField] private TextMesh textTip;
    [SerializeField] private Color WoodColor;
    [SerializeField] private Color charWoodColor;

    private bool fireLit;
    private bool canAdd;
    private int logsPlaced;
    private float timePassed;
    private string textforTip;
    private float RangeofTip = 5;
    private Controls playerControls;
    
    void Start()
    {
        playerControls = FindAnyObjectByType<Controls>();
        fireReady.SetActive(false);
        logsPlaced = 0;
        timePassed = 0;
        Wood1.SetActive(false);
        Wood2.SetActive(false);
        Wood3.SetActive(false);
        WoodPlaced.SetActive(false);
        WoodBurning.SetActive(false);
        canAdd = true;
        WoodColor.a = 1;
        charWoodColor.a = 1;
        charWood1.GetComponentInChildren<Renderer>().material.color = charWoodColor;
        charWood2.GetComponentInChildren<Renderer>().material.color = charWoodColor;
        charWood3.GetComponentInChildren<Renderer>().material.color = charWoodColor;
        Wood1.GetComponentInChildren<Renderer>().material.color = WoodColor;
        Wood2.GetComponentInChildren<Renderer>().material.color = WoodColor;
        Wood3.GetComponentInChildren<Renderer>().material.color = WoodColor;
    }

    // Update is called once per frame
    void Update()
    {
        if(Vector3.Distance(gameObject.transform.position, playerControls.GetPlayerPos()) < RangeofTip)
        {
            RaycastHit hit;
            if(Physics.Raycast(gameObject.transform.position, (playerControls.GetPlayerPos() - transform.position), out hit, RangeofTip))
            {
                if(hit.collider.tag == "Player")
                {
                    //Debug.Log("logs placed colider hit player");
                    if (logsPlaced > 0)
                    {
                        //Debug.Log("logs placed :" + logsPlaced);
                        if (!fireLit)
                        {
                            //Debug.Log("fire not lit");
                            //starting fire with match 
                            textforTip = "Press r to start fire";
                            textTip.text = textforTip;
                            if (Input.GetKeyDown(KeyCode.R))
                            {
                                startFire();
                                //put animation here?
                            }
                        }
                    }
                }
            }
        }
        else
        {
            textforTip = "";
            textTip.text = textforTip;
        }
        
        switch(logsPlaced)
        {
            case 0:
                timePassed = 0;
                Wood1.SetActive(false);
                Wood2.SetActive(false);
                Wood3.SetActive(false);
                charWood1.SetActive(false);
                charWood2.SetActive(false);
                charWood3.SetActive(false);
                WoodPlaced.SetActive(false);
                WoodBurning.SetActive(false);
                canAdd = true;
                fireReady.SetActive(false);
                FireLight.intensity = 0;
                break;
            case 1:
                WoodPlaced.SetActive(true);
                Wood1.SetActive(true);
                break;
            case 2:
                WoodPlaced.SetActive(true);
                Wood2.SetActive(true);
                break;
            case 3:
                Wood3.SetActive(true);
                WoodPlaced.SetActive(true);
                canAdd = false;
                break;
            default:
                logsPlaced = 0;
                timePassed = 0;
                fireReady.SetActive(false);
                Wood1.SetActive(false);
                Wood2.SetActive(false);
                Wood3.SetActive(false);
                WoodPlaced.SetActive(false);
                WoodBurning.SetActive(false);
                canAdd = true;
                break;
        }
        
        if (fireLit)
        {
            timePassed = timePassed + Time.deltaTime;
            if (timePassed <= 10)
                FireLight.intensity = timePassed;
            if(true)
            {
                WoodColor.a = 1-((timePassed / 100f) + 0.05f);
                switch (logsPlaced)
                {
                    case 0:
                        charWood1.SetActive(false);
                        charWood2.SetActive(false);
                        charWood3.SetActive(false);
                        break;
                    case 1:
                        charWood1.SetActive(true);
                        charWood2.SetActive(false);
                        charWood3.SetActive(false);
                        break;
                    case 2:
                        charWood1.SetActive(true);
                        charWood2.SetActive(true);
                        charWood3.SetActive(false);
                        break;
                    case 3:
                        charWood1.SetActive(true);
                        charWood2.SetActive(true);                     
                        charWood3.SetActive(true);
                        break;
                    default:
                        break;
                }

            }

            var tempAlpha = (((timePassed * timePassed) / 1000f) * ((timePassed * timePassed) / 1000f)) + 0.25f;
            if (tempAlpha <= 1)
                charWoodColor.a = tempAlpha;
            //Debug.Log(tempAlpha);
            charWood1.GetComponentInChildren<Renderer>().material.color = charWoodColor;
            charWood2.GetComponentInChildren<Renderer>().material.color = charWoodColor;
            charWood3.GetComponentInChildren<Renderer>().material.color = charWoodColor;
            Wood1.GetComponentInChildren<Renderer>().material.color = WoodColor;
            Wood2.GetComponentInChildren<Renderer>().material.color = WoodColor;
            Wood3.GetComponentInChildren<Renderer>().material.color = WoodColor;

            if (timePassed >= 45)
            {
                WoodBurning.SetActive(false);
                fireLit = false;
                var tempPos = WoodBurning.transform.position;
                var tempRot = Quaternion.Euler(90, 0, 0);

                for (int i = 0; i < logsPlaced; i++)
                {
                    switch (i)
                    {
                        case 0:
                            tempPos = charWood1.transform.position;
                            tempRot = charWood1.transform.rotation;
                            break;
                        case 1:
                            tempPos = charWood2.transform.position;
                            tempRot = charWood2.transform.rotation;
                            break;
                        case 2:
                            tempPos = charWood3.transform.position;
                            tempRot = charWood3.transform.rotation;
                            break;
                        default:
                            //nothing
                            break;
                    }

                    GameObject o = UnityEngine.Object.Instantiate(charcoalItem.model, tempPos, tempRot);
                }
                fireReady.SetActive(false);
                logsPlaced = 0;
                timePassed = 0;
                WoodColor.a = 1;
                charWoodColor.a = 1;
                charWood1.GetComponentInChildren<Renderer>().material.color = WoodColor;
                charWood2.GetComponentInChildren<Renderer>().material.color = WoodColor;
                charWood3.GetComponentInChildren<Renderer>().material.color = WoodColor;
                Wood1.GetComponentInChildren<Renderer>().material.color = WoodColor;
                Wood2.GetComponentInChildren<Renderer>().material.color = WoodColor;
                Wood3.GetComponentInChildren<Renderer>().material.color = WoodColor;
            }
        }

    }

    public void addWoodenLog()
    {
        logsPlaced = logsPlaced + 1;
        if(logsPlaced >= 3)
        {
            canAdd = false;
        }
    }

    public void startFire()
    {
        if (logsPlaced > 0)
        {
            WoodBurning.SetActive(true);
            fireLit = true;
            fireReady.SetActive(true);
        }
    }

    public bool CanAddCheck()
    {
        return canAdd;
    }
}
