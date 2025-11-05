using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Smeltery : MonoBehaviour
{

    [SerializeField] private Button PutInFireButton;
    [SerializeField] private Items EmptyItem;
    [SerializeField] private Items CharItem;
    [SerializeField] private GameObject heatUI;
    [SerializeField] private GameObject CharcoalIndicator;
    [SerializeField] private GameObject InFireUI;
    [SerializeField] private GameObject Fire;
    [SerializeField] private Animator Tongs;
    [SerializeField] private Light heatLight;
    [SerializeField] private Color Stage0Color;
    [SerializeField] private Color Stage1Color;
    [SerializeField] private Color Stage2Color;
    [SerializeField] private Color Stage3Color;
    [SerializeField] private Color Stage4Color;

    private int teirLevel;
    private int maxTemp;
    private int increasePerSec;
    private int decreasePerSec;
    private Items smeltItem;
    private float increaseTimer = 0;
    private float decreaseTimer = 0;
    private float timerOfStage = 0;
    private float heat = 0;
    private float charcoalTime;
    private bool timerOn = false;
    private bool inFire = false;
    //private bool addtoHeat = false;


    void Start()
    {
        charcoalTime = 0;
        smeltItem = EmptyItem;
        teirLevel = 1;
        maxTemp = 1000 * teirLevel + 1000;
        heatLight.intensity = 0;

        PutInFireButton.onClick.AddListener(PutInFire);
    }

    private void Update()
    {
        //turns the thing on if theres charcoal
        if (charcoalTime > 0)
        {
            timerOn = true;
        }

        //starting timer and decreasing charcoal time and increasing heat
        if (timerOn)
        {
            increaseTimer += Time.deltaTime;
            charcoalTime -= Time.deltaTime;
            heat = ((int)increaseTimer) * increasePerSec;
            //cleanup for charcoalTime
            if (charcoalTime < 0)
                charcoalTime = 0;
        }
        else
        {
            //starting decrease timer if the charcoal ran out
            decreaseTimer += Time.deltaTime;

            //if decrease timer reached increase timer and heat is == 0 they are both set to 0 and 
            if (heat == 0)
            {
                increaseTimer = 0;
                decreaseTimer = 0;
            }
        }

        //Debug.Log(timerOn);
        //decrease heat if timer is on and there is heat in the smelter
        if (timerOn == false && heat > 0)
        {
            float increaseHeat = (int)increaseTimer * increasePerSec;
            if (increaseHeat > 2000)
                increaseHeat = 2000;
            heat = (increaseHeat) - (decreaseTimer * decreasePerSec);
        }

        if (charcoalTime == 0)
        {
            timerOn = false;
        }

        if (heat < 0)
            heat = 0;

        if (heat > maxTemp)
            heat = maxTemp;

        if(heat > 0)
        {
            heatLight.intensity = heat / 100;
        } else
        {
            heatLight.intensity = 0;
        }

        smeltItem = FindObjectOfType<WorkstationScript>().getItemOnTongs();

        if (heat >= smeltItem.heatLevel && inFire)
        {
            timerOfStage += Time.deltaTime;
        }
        else
        {
            timerOfStage -= Time.deltaTime;
            if (timerOfStage < 0)
                timerOfStage = 0;
        }

        //Debug.Log("Current Heat is " + heat);
        //Debug.Log("increaseTimer time is " + increaseTimer);
        //Debug.Log("decreaseTimer time is " + decreaseTimer);
        //Debug.Log("timerOfStage time is " + timerOfStage);
        //Debug.Log("charcoalTime time is " + charcoalTime); 

        switch (((int)timerOfStage))
        {
            case 0:
                smeltItem = FindObjectOfType<WorkstationScript>().getItemOnTongs();
                smeltItem.heatStage = 0;
                FindObjectOfType<WorkstationScript>().setItemOnTongs(smeltItem);
                heatUI.GetComponent<Image>().color = Stage0Color;
                break;
            case 10:
                smeltItem = FindObjectOfType<WorkstationScript>().getItemOnTongs();
                smeltItem.heatStage = 1;
                FindObjectOfType<WorkstationScript>().setItemOnTongs(smeltItem);
                heatUI.GetComponent<Image>().color = Stage1Color;
                break;
            case 15:
                smeltItem = FindObjectOfType<WorkstationScript>().getItemOnTongs();
                smeltItem.heatStage = 2;
                FindObjectOfType<WorkstationScript>().setItemOnTongs(smeltItem);
                heatUI.GetComponent<Image>().color = Stage2Color;
                break;
            case 20:
                smeltItem = FindObjectOfType<WorkstationScript>().getItemOnTongs();
                smeltItem.heatStage = 3;
                FindObjectOfType<WorkstationScript>().setItemOnTongs(smeltItem);
                heatUI.GetComponent<Image>().color = Stage3Color;
                break;
            case 30:
                smeltItem = FindObjectOfType<WorkstationScript>().getItemOnTongs();
                smeltItem.heatStage = 4;
                FindObjectOfType<WorkstationScript>().setItemOnTongs(smeltItem);
                heatUI.GetComponent<Image>().color = Stage4Color;
                break;
            case 50:
                FindObjectOfType<WorkstationScript>().setItemOnTongs(EmptyItem);
                break;
            default:
                break;
        }



        //t1
        //max temp of 2k 
        //increases by 1k every charcoal put in, 100 per second for 10 seconds 
        //goes down slowy only 20 every second


        //t2
        //max temp of 3k 
        //increases by 2k every charcoal put in, 200 per second for 10 seconds 
        //goes down slowy only 10 every second

        //each item has a heat level it must be above or matching for 5 seconds per stage, indicator will tell which level of heat

        //stage 1 Yellow heat: smashing impurities
        //stage 2 Orange heat: making small modifications small verticies 
        //stage 3 Red heat: making larger mods ie whole sides smash
        //stage 4 Black heat: used for smelting things in bucket(perhaps destroy things that arent in a bucket after another 5 seconds)

        //



        //Debug.Log("this is the time: " + timer);

    }

    private void FixedUpdate()
    {
        increasePerSec = 100 * teirLevel;
        decreasePerSec = 20 / teirLevel;
        maxTemp = 1000 * teirLevel + 1000;

        if (charcoalTime > 0 | heat > 0)
            CharcoalIndicator.SetActive(true);
        else
            CharcoalIndicator.SetActive(false);

        Fire.SetActive(timerOn);

        //probs should put timers in here
    }

    private bool CheckAddCharcoal(Items item)
    {
        if (item == CharItem)
        {
            charcoalTime += (60 * teirLevel);
            return true;
        }
        else
            return false;
    }

    private void PutInFire()
    {
        if (inFire)
        {
            Tongs.SetBool("FireTongs", false);
            InFireUI.SetActive(false);
            inFire = false;
        }
        else
        {
            Tongs.SetBool("FireTongs", true);
            InFireUI.SetActive(true);
            inFire = true;
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        try
        {
            Debug.Log(collision.transform.GetComponent<Item>().item.name);
            if (CheckAddCharcoal(collision.transform.GetComponent<Item>().item))
            {
                Destroy(collision.gameObject);
            }
            else
            {
                collision.gameObject.GetComponent<Rigidbody>().AddForce(new Vector3(UnityEngine.Random.Range(0, 20), 5, UnityEngine.Random.Range(0, 20)) * 10);
            }
        }
        catch
        {
            //nothing
        }
    }
        
    public void changedItemOnTongs()
    {
        timerOfStage = 0;
        inFire = false;
        InFireUI.SetActive(false);
        smeltItem = EmptyItem;
    }
    

}
