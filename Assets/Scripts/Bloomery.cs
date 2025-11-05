using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Bloomery : MonoBehaviour
{
    [SerializeField] private Items EmptyItem;
    [SerializeField] private Items charcoalItem;
    [SerializeField] private GameObject heatUI;
    [SerializeField] private GameObject bloomEffects;
    [SerializeField] private GameObject bloomDripEffects;
    [SerializeField] private GameObject bloomGunk;
    [SerializeField] private GameObject spawnPos;
    [SerializeField] private GameObject bloomCharcoal1;
    [SerializeField] private GameObject bloomCharcoal2;
    [SerializeField] private GameObject bloomCharcoal3;
    [SerializeField] private Light heatLight;

    private Color fadeColor;
    private Items bloomeryItem;
    private float timer = 0;
    private bool timerOn;
    private string heatText;
    private int charcoal = 0;
    private int bloom = 0;

    void Start()
    {
        bloomDripEffects.SetActive(false);
        bloomGunk.SetActive(false);
        charcoal = 0;
        bloom = 0;
        bloomeryItem = EmptyItem;
    }

    private void Update()
    {
        if (timerOn)
        {
            bloomEffects.SetActive(true);
            timer += Time.deltaTime;
            heatLight.intensity = timer + 15;
        }
            
        else
        {
            if(timer > 0)
            {
                timer -= Time.deltaTime;
            }
            if(timer <= 0)
            {
                timer = 0;
                bloomEffects.SetActive(false);
            }
            heatLight.intensity = timer;
        }
        //Debug.Log("this is the time: " + timer);
        heatText = "Heat: " + (timer * 100f);
        //Debug.Log(heatText);
        //heatUI.GetComponent<TextMeshProUGUI>().text = heatText;

        //Charcoal.value = bloomInventory.Container[1].amount;
        //Bloom.value = bloomInventory.Container[0].amount;
        //change these to be coliders that when hit adds to one or the other unless it doesn't fit in that case call spitup
    }

    private void FixedUpdate()
    {
        //Debug.Log("there is " + charcoal+ " charcoal");
        //Debug.Log("There is " + bloom + " " + bloomeryItem.name);
        if (charcoal >= 2 && bloom >= 1)
        {
            if (!timerOn)
            {
                timerOn = true;
            }
        }
        else
            timerOn = false;
        bloomGunk.SetActive(timerOn);

        if (timerOn)
        {
            fadeColor = bloomGunk.GetComponentInChildren<Renderer>().material.color;
            var y = bloomeryItem.heatLevel * bloomeryItem.heatLevel;
            Debug.Log((((timer * timer) / y)));
            fadeColor.a = (((timer * timer) / y));

            bloomGunk.GetComponentInChildren<Renderer>().material.color = fadeColor;
            if (timer >= bloomeryItem.heatLevel)//this can probably be changed to heat required per bloom
            {
                //Debug.Log("Item " + bloomeryItem + "should be turning to " + bloomeryItem.nextItem);
                GameObject o = UnityEngine.Object.Instantiate(bloomeryItem.nextItem.model, spawnPos.transform.position, Quaternion.Euler(90, 0, 0));
                bloom -= 1;
                charcoal -= 2;
                timerOn = false;
                timer -= 10;
            } 
            if (timer >= (bloomeryItem.heatLevel) / 4)
            {
                bloomDripEffects.SetActive(true);
            }
        }
        if (!timerOn)
        {
            fadeColor = GetComponentInChildren<Renderer>().material.color;
            fadeColor.a = 0;
            bloomGunk.GetComponentInChildren<Renderer>().material.color = fadeColor;
            bloomDripEffects.SetActive(false);
        }

        if (charcoal >= 2)
        {
            bloomCharcoal1.SetActive(true);
        }
        if (charcoal >= 6)
        {
            bloomCharcoal2.SetActive(true);

        }
        if (charcoal >= 16){
            bloomCharcoal3.SetActive(true);
        }

        if (charcoal < 2)
        {
            bloomCharcoal1.SetActive(false);
        }
        if (charcoal < 6)
        {
            bloomCharcoal2.SetActive(false);
        }

        if (charcoal < 16)
        {
            bloomCharcoal3.SetActive(false);
        }
    }

    public bool CheckAndAddItem(Items item)
    {
        //Debug.Log(item.name);
        //Debug.Log(charcoalItem.name);
        if (item == charcoalItem)
        {
            if (charcoal < 20)
            {
                charcoal += 1;
                return true;
            }
            else
                return false;
        }
        else if (bloomeryItem == EmptyItem)
        {
            if (item.name.Contains("Dust"))
            {
                bloomeryItem = item;
                bloom += 1;
                return true;
            }
            else
                return false;
        }
        else if (bloomeryItem == item)
        {
            bloom += 1;
            return true;
        } 
        else
            return false;
    }

    public Items returnEmpty()
    {
        return EmptyItem;
    }
}