using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EarthPuzzle2 : MonoBehaviour
{
    [SerializeField] private GameObject code1Couchmain1;
    [SerializeField] private GameObject code1Couchmain3;
    [SerializeField] private GameObject code1Couch1;
    [SerializeField] private GameObject code1Couch3;
    [SerializeField] private GameObject code1BookshelfMain;
    [SerializeField] private GameObject code1Bookshelf5;
    [SerializeField] private GameObject code1Bookshelf2;
    [SerializeField] private GameObject code1BookshelfMainv;
    [SerializeField] private GameObject code1Bookshelf5v;
    [SerializeField] private GameObject code1Bookshelf2v;
    [SerializeField] private GameObject code1LampMain;
    [SerializeField] private GameObject code1Lamp4;
    [SerializeField] private GameObject code1Lamp6;
    [SerializeField] private GameObject code1TableMain;
    [SerializeField] private GameObject code1Table1;
    [SerializeField] private GameObject code1Table2;
    [SerializeField] private GameObject code1Table3;
    [SerializeField] private GameObject code1Table4;
    [SerializeField] private GameObject code1Table6;

    [SerializeField] private GameObject code2SinkMain;
    [SerializeField] private GameObject code2Sink4;
    [SerializeField] private GameObject code2Sink5;
    [SerializeField] private GameObject code2Sink6;
    [SerializeField] private GameObject code2FridgeMain;
    [SerializeField] private GameObject code2Fridge1;
    [SerializeField] private GameObject code2Fridge2;
    [SerializeField] private GameObject code2Fridge3;

    [SerializeField] private GameObject code3BedMain;
    [SerializeField] private GameObject code3Bed4;
    [SerializeField] private GameObject code3Bed2;
    [SerializeField] private GameObject code3LampMain;
    [SerializeField] private GameObject code3Lamp4;
    [SerializeField] private GameObject code3Lamp6;
    [SerializeField] private GameObject code3Couchmain;
    [SerializeField] private GameObject code3Couch1;
    [SerializeField] private GameObject code3Couch3;
    [SerializeField] private GameObject code3Rugmain;
    [SerializeField] private GameObject code3Rug5;
    [SerializeField] private GameObject code3Rug6;

    [SerializeField] private GameObject LockedDoor;
    [SerializeField] private GameObject UnlockedDoor;

    private int Code1Master;
    private int Code2Master;
    private int Code3Master;

    private int Code1Wheel = 5;
    private int Code2Wheel = 5;
    private int Code3Wheel = 5;
    void Start()
    {
        SetCode1();
        SetCode2();
        SetCode3();
    }

    private void SetCode1()
    {
        int code1 = UnityEngine.Random.Range(1, 7);
        SetCode(code1, 1);
        switch (code1)
        {
            case 1:
                if(UnityEngine.Random.Range(0, 2) == 0)
                {
                    code1Couch1.SetActive(true);
                    code1Couchmain1.SetActive(false);
                }
                else
                {
                    code1Table1.SetActive(true);
                    code1TableMain.SetActive(false);
                }
                break;
            case 2:
                int i = UnityEngine.Random.Range(0, 3);
                if (i == 0)
                {
                    code1Bookshelf2.SetActive(true);
                    code1BookshelfMain.SetActive(false);
                }
                else if(i == 1)
                {
                    code1Bookshelf2v.SetActive(true);
                    code1BookshelfMainv.SetActive(false);
                }
                else
                {
                    code1Table2.SetActive(true);
                    code1TableMain.SetActive(false);
                }
                break;
            case 3:
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    code1Couch3.SetActive(true);
                    code1Couchmain3.SetActive(false);
                }
                else
                {
                    code1Table3.SetActive(true);
                    code1TableMain.SetActive(false);
                }
                break;
            case 4:
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    code1Lamp4.SetActive(true);
                    code1LampMain.SetActive(false);
                }
                else
                {
                    code1Table4.SetActive(true);
                    code1TableMain.SetActive(false);
                }
                break;
            case 5:
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    code1Bookshelf5.SetActive(true);
                    code1BookshelfMain.SetActive(false);
                }
                else
                {
                    code1Bookshelf5v.SetActive(true);
                    code1BookshelfMainv.SetActive(false);
                }
                break;
            case 6:
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    code1Lamp6.SetActive(true);
                    code1LampMain.SetActive(false);
                }
                else
                {
                    code1Table6.SetActive(true);
                    code1TableMain.SetActive(false);
                }
                break;
            default:
                break;
        }
    }

    private void SetCode2()
    {
        int code2 = UnityEngine.Random.Range(1, 7);
        SetCode(code2, 2);
        switch (code2)
        {
            case 1:
                code2FridgeMain.SetActive(false);
                code2Fridge1.SetActive(true);
                break;
            case 2:
                code2FridgeMain.SetActive(false);
                code2Fridge2.SetActive(true);
                break;
            case 3:
                code2FridgeMain.SetActive(false);
                code2Fridge3.SetActive(true);
                break;
            case 4:
                code2SinkMain.SetActive(false);
                code2Sink4.SetActive(true);
                break;
            case 5:
                code2SinkMain.SetActive(false);
                code2Sink5.SetActive(true);
                break;
            case 6:
                code2SinkMain.SetActive(false);
                code2Sink6.SetActive(true);
                break;
            default:
                break;
        }
    }

    private void SetCode3()
    {
        int code3 = UnityEngine.Random.Range(1, 7);
        SetCode(code3, 3);
        switch (code3)
        {
            case 1:
                code3Couch1.SetActive(true);
                code3Couchmain.SetActive(false);
                break;
            case 2:
                code3Bed2.SetActive(true);
                code3BedMain.SetActive(false);
                break;
            case 3:
                code3Couch3.SetActive(true);
                code3Couchmain.SetActive(false);
                break;
            case 4:
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    code3Lamp4.SetActive(true);
                    code3LampMain.SetActive(false);
                }
                else
                {
                    code3Bed4.SetActive(true);
                    code3BedMain.SetActive(false);
                }
                break;
            case 5:
                code3Rug5.SetActive(true);
                code3Rugmain.SetActive(false);
                break;
            case 6:
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    code3Rug6.SetActive(true);
                    code3Rugmain.SetActive(false);
                }
                else
                {
                    code3Lamp6.SetActive(true);
                    code3LampMain.SetActive(false);
                }
                break;
            default:
                break;
        }
    }

    public void WheelUpdate()
    {
        if(Code1Wheel == Code1Master && Code2Wheel == Code2Master && Code3Wheel == Code3Master)
        {
            UnlockDoor();
        }
    }

    private void UnlockDoor()
    {
        LockedDoor.SetActive(false);
        UnlockedDoor.SetActive(true);
    }

    public void setWheelCodeDown(int number)
    {
        Debug.Log("Terminal turn up");

        if (number == 1)
        {
            Code1Wheel += 1;
            if(Code1Wheel > 6)
            {
                Code1Wheel = 1;
            }
        }
        if (number == 2)
        {
            Code2Wheel += 1;
            if (Code2Wheel > 6)
            {
                Code2Wheel = 1;
            }
        }
        if (number == 3)
        {
            Code3Wheel += 1;
            if (Code3Wheel > 6)
            {
                Code3Wheel = 1;
            }
        }

        Invoke("WheelUpdate", 2);
    }

    public void setWheelCodeUp(int number)
    {
        Debug.Log("Terminal turn down");

        if (number == 1)
        {
            Code1Wheel -= 1;
            if (Code1Wheel <= 0)
            {
                Code1Wheel = 6;
            }
        }
        if (number == 2)
        {
            Code2Wheel -= 1;
            if (Code2Wheel <= 0)
            {
                Code2Wheel = 6;
            }
        }
        if (number == 3)
        {
            Code3Wheel -= 1;
            if (Code3Wheel <= 0)
            {
                Code3Wheel = 6;
            }
        }

        WheelUpdate();
    }

    public void SetCode(int code, int number)
    {
        if(number == 1)
        {
            Code1Master = code;
        }
        if (number == 2)
        {
            Code2Master = code;
        }
        if (number == 3)
        {
            Code3Master = code;
        }
    }
}
