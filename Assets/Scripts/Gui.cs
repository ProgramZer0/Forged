using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public enum CursorState
{
    defaultCursor,
    handCursor,
    blankCursor,
    closedHandCursor
}

public class Gui : MonoBehaviour
{
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D handCursor;
    [SerializeField] private Texture2D closedHandCursor;
    [SerializeField] private Texture2D BLANK;

    [SerializeField] private Vector2 defaultHotspot = Vector2.zero;
    [SerializeField] private Vector2 handHotspot = Vector2.zero;

    public CursorState currentState = CursorState.defaultCursor;

    public void SetDefaultCursor()
    {
        currentState = CursorState.defaultCursor;
        Cursor.SetCursor(defaultCursor, defaultHotspot, CursorMode.Auto);
    }
    public void SetDefaultCursor(Vector2 pos)
    {
        currentState = CursorState.defaultCursor;
        Cursor.SetCursor(defaultCursor, pos, CursorMode.Auto);
    }

    public void SetHandCursor()
    {
        currentState = CursorState.handCursor;
        Cursor.SetCursor(handCursor, handHotspot, CursorMode.Auto);
    }
    public void SetHandCursor(Vector2 pos)
    {
        currentState = CursorState.handCursor;
        Cursor.SetCursor(handCursor, pos, CursorMode.Auto);
    }

    public void SetBankCursor()
    {
        currentState = CursorState.blankCursor;
        Cursor.SetCursor(BLANK, handHotspot, CursorMode.Auto);
    }
    public void SetBankCursor(Vector2 pos)
    {
        currentState = CursorState.blankCursor;
        Cursor.SetCursor(BLANK, pos, CursorMode.Auto);
    }

    public void SetClosedHCursor()
    {
        currentState = CursorState.closedHandCursor;
        Cursor.SetCursor(closedHandCursor, handHotspot, CursorMode.Auto);
    }
    public void SetClosedHCursor(Vector2 pos)
    {
        currentState = CursorState.closedHandCursor;
        Cursor.SetCursor(closedHandCursor, pos, CursorMode.Auto);
    }

    /*
    [SerializeField] private Inventory tempSwap;
    [SerializeField] private Items empty;
    [SerializeField] private LayerMask playerlayer;

    [SerializeField] private GameObject MainCam; 
    [SerializeField] private GameObject ScreenPos;
    [SerializeField] private GameObject hotBar;

    //[SerializeField] private GameObject hammerObj;
    [SerializeField] private GameObject axeObj;

    Color trans;

    int current;
    int toBe;
    Inventory currentInvSwap;
    Inventory toBeInvSwap;
    int highlightedHotSlot = 15;
    private Items currentlyHolding;

    /*void Start()
    {
        currentlyHolding = empty;

        clearInventory(AnvilSlots, Anvil);
        clearInventory(SmelterySlots, Smeltery);
    }

    private void Update()
    {
        setInventory(AnvilSlots, Anvil);
        setInventory(SmelterySlots, Smeltery);

        /*Color temp2;
        if (hotBar.activeSelf)
        {
            switch (highlightedHotSlot)
            {
                case 17:
                    temp2 = inventorySlots[17].GetComponentInParent<RawImage>().color;
                    temp2.a = 1;
                    inventorySlots[17].GetComponentInParent<RawImage>().color = temp2;

                    temp2 = inventorySlots[16].GetComponentInParent<RawImage>().color;
                    temp2.a = 0;
                    inventorySlots[16].GetComponentInParent<RawImage>().color = temp2;
                    inventorySlots[15].GetComponentInParent<RawImage>().color = temp2;
                    break;
                case 16:
                    temp2 = inventorySlots[16].GetComponentInParent<RawImage>().color;
                    temp2.a = 1;
                    inventorySlots[16].GetComponentInParent<RawImage>().color = temp2;

                    temp2 = inventorySlots[17].GetComponentInParent<RawImage>().color;
                    temp2.a = 0;
                    inventorySlots[17].GetComponentInParent<RawImage>().color = temp2;
                    inventorySlots[15].GetComponentInParent<RawImage>().color = temp2;
                    break;
                case 15:
                    temp2 = inventorySlots[15].GetComponentInParent<RawImage>().color;
                    temp2.a = 1;
                    inventorySlots[15].GetComponentInParent<RawImage>().color = temp2;

                    temp2 = inventorySlots[16].GetComponentInParent<RawImage>().color;
                    temp2.a = 0;
                    inventorySlots[16].GetComponentInParent<RawImage>().color = temp2;
                    inventorySlots[17].GetComponentInParent<RawImage>().color = temp2;
                    break;
                default:
                    Debug.Log("should not see this GUIx90");
                    break;
            }
        }
       
        Animator animate = FindObjectOfType<Controls>().GetAnimator();
        
        if (inventory.Container[highlightedHotSlot].item == empty)
        {
            if(GameObject.Find("onScreen"))
                Destroy(GameObject.Find("onScreen").gameObject);
            animate.SetBool("HoldingHammer", false);
            animate.SetBool("HoldingItem", false);
            animate.SetBool("HoldingAxe", false);
            axeObj.SetActive(false);
            //hammerObj.SetActive(false);
            currentlyHolding = empty;
        }
        if (inventory.Container[highlightedHotSlot].item.itemID == "66")
        {
            if(GameObject.Find("onScreen"))
                Destroy(GameObject.Find("onScreen").gameObject);
        }

        if (inventory.Container[highlightedHotSlot].item.itemID == "65")
        {
            if (GameObject.Find("onScreen"))
                Destroy(GameObject.Find("onScreen").gameObject);
        }

        //Debug.Log(inventory.Container[highlightedHotSlot].item);
        if (inventory.Container[highlightedHotSlot].item != empty )
        {
            if (currentlyHolding == empty)
            {
                Vector3 temp3 = ScreenPos.transform.position;
                GameObject onScreen = UnityEngine.Object.Instantiate(inventory.Container[highlightedHotSlot].item.model, temp3, Quaternion.LookRotation(MainCam.transform.forward) * Quaternion.Euler(0, 90, 0));
                onScreen.transform.parent = ScreenPos.transform;
                onScreen.GetComponent<Rigidbody>().velocity = Vector3.zero;
                onScreen.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
                onScreen.GetComponent<Rigidbody>().isKinematic = true;
                onScreen.GetComponent<Collider>().enabled = false;
                onScreen.name = "onScreen";

                currentlyHolding = inventory.Container[highlightedHotSlot].item;

                if (inventory.Container[highlightedHotSlot].item.itemName.Contains("Hammer"))
                {
                    //Debug.Log("is Hammer");
                    Destroy(GameObject.Find("onScreen").gameObject);
                    //hammerObj.SetActive(true);
                    animate.SetBool("HoldingHammer", true);
                    animate.SetBool("HoldingItem", false);
                    animate.SetBool("HoldingAxe", false);
                }
                else
                {
                    if (inventory.Container[highlightedHotSlot].item.itemName.Contains("Axe"))
                    {
                        Debug.Log("is Axe");
                        Destroy(GameObject.Find("onScreen").gameObject);
                        axeObj.SetActive(true);
                        animate.SetBool("HoldingHammer", false);
                        animate.SetBool("HoldingItem", false);
                        animate.SetBool("HoldingAxe", true);
                    }
                    else {
                        axeObj.SetActive(false);
                        //hammerObj.SetActive(false);
                        animate.SetBool("HoldingHammer", false);
                        animate.SetBool("HoldingItem", true);
                        animate.SetBool("HoldingAxe", false);
                    }
                }
            }
            if(currentlyHolding != inventory.Container[highlightedHotSlot].item)
            {
                animate.SetBool("HoldingHammer", false);
                animate.SetBool("HoldingItem", false);
                animate.SetBool("HoldingAxe", false);
                axeObj.SetActive(false);
                //hammerObj.SetActive(false);
                if(GameObject.Find("onScreen"))
                    Destroy(GameObject.Find("onScreen").gameObject);

                Vector3 temp4 = ScreenPos.transform.position;
                GameObject onScreen = UnityEngine.Object.Instantiate(inventory.Container[highlightedHotSlot].item.model, temp4, Quaternion.LookRotation(MainCam.transform.forward) * Quaternion.Euler(0, 90, 0));
                onScreen.transform.parent = ScreenPos.transform;
                onScreen.GetComponent<Rigidbody>().velocity = Vector3.zero;
                onScreen.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
                onScreen.GetComponent<Rigidbody>().isKinematic = true;
                onScreen.GetComponent<Collider>().enabled = false;
                onScreen.name = "onScreen";

                currentlyHolding = inventory.Container[highlightedHotSlot].item;

                if (inventory.Container[highlightedHotSlot].item.itemName.Contains("Hammer"))
                {
                    Destroy(GameObject.Find("onScreen").gameObject);
                    //hammerObj.SetActive(true);
                    axeObj.SetActive(false);
                    animate.SetBool("HoldingHammer", true);
                    animate.SetBool("HoldingItem", false);
                    animate.SetBool("HoldingAxe", false);
                }
                else
                {
                    if (inventory.Container[highlightedHotSlot].item.itemName.Contains("Axe"))
                    {
                        Destroy(GameObject.Find("onScreen").gameObject);
                        axeObj.SetActive(true);
                        //hammerObj.SetActive(false);
                        animate.SetBool("HoldingHammer", false);
                        animate.SetBool("HoldingItem", false);
                        animate.SetBool("HoldingAxe", true);
                    }
                    else
                    {
                        axeObj.SetActive(false);
                        //hammerObj.SetActive(false);
                        animate.SetBool("HoldingHammer", false);
                        animate.SetBool("HoldingItem", true);
                        animate.SetBool("HoldingAxe", false);
                    }
                }
            }
        }
       
    } */
    /*
    public void clearInventory(List<GameObject> slots, Inventory inv)
    {
        for (int i = 0; i < inv.Container.Count; i++)
        {
            inv.Container[i].item = empty;
            if(slots[i])
                clearSlot(slots[i]);
        }
    }

    public void clearInventory(Inventory inv)
    {
        for (int i = 0; i < inv.Container.Count; i++)
        {
            inv.Container[i].item = empty;
        }
    }

    public void setInventory(List<GameObject> slots, Inventory inv)
    {
        for (int i = 0; i < inv.Container.Count; i++)
        {
            if (inv.Container[i].item != null)
            {
                if (slots[i])
                {
                    Color temp;
                    temp = slots[i].GetComponent<Image>().color;
                    temp.a = 1f;
                    slots[i].GetComponent<Image>().color = temp;

                    slots[i].GetComponent<Image>().sprite = inv.Container[i].item.itemSprite;

                }
                if (inv.Container[i].amount != 0)
                {
                    if (slots[i].GetComponentInChildren<TextMeshProUGUI>())
                        slots[i].GetComponentInChildren<TextMeshProUGUI>().text = (inv.Container[i].amount.ToString());
                }
                else
                {
                    if (slots[i].GetComponentInChildren<TextMeshProUGUI>())
                        slots[i].GetComponentInChildren<TextMeshProUGUI>().text = "";
                    inv.Container[i].item = empty;
                }
            }

            if (inv.Container[i].item == null)
            {
                clearSlot(slots[i]);
                inv.Container[i].item = empty;
            }

            if (inv.Container[i].item == empty)
            {
                clearSlot(slots[i]);
                inv.Container[i].amount = 0;
            }
        }
    }

    public void setInventory(Inventory inv)
    {
        for (int i = 0; i < inv.Container.Count; i++)
        {
            if (inv.Container[i].item != null)
            {
                if (inv.Container[i].amount == 0)
                    inv.Container[i].item = empty;
            }

            if (inv.Container[i].item == null)
                inv.Container[i].item = empty;

            if (inv.Container[i].item == empty)
                inv.Container[i].amount = 0;
        }
    }

    public void clearSlot(GameObject slotNum)
    {
        trans = slotNum.GetComponent<Image>().color;
        trans.a = 0f;
        slotNum.GetComponent<Image>().color = trans;

        if (slotNum.GetComponentInChildren<TextMeshProUGUI>())
            slotNum.GetComponentInChildren<TextMeshProUGUI>().text = null;
        slotNum.GetComponent<Image>().sprite = empty.itemSprite;

    }

    public void SetCurrent(int _current, Inventory _inv) 
    {
        current = _current;
        currentInvSwap = _inv;
    }

    public void SetToBe(int _toBe, Inventory _inv)
    {
        toBe = _toBe;
        toBeInvSwap = _inv;
    }
    public Items ReturnEmpty()
    {
        return empty;
    }
    public void swapSlots()
    {
        if (toBeInvSwap == Bloom && toBe == 1)
        {
            checkThenAdd("Crushed Charcoal");
            return;
        }

        if (toBeInvSwap == Bloom && toBe == 0)
        {
            checkThenAdd("Dust");
            return;
        }

        if(toBeInvSwap == Anvil)
        {
            if(toBeInvSwap.Container[toBe].item == empty)
            {
                toBeInvSwap.Container[toBe].item = currentInvSwap.Container[current].item;
                toBeInvSwap.Container[toBe].amount = 1;
                currentInvSwap.Container[current].amount -= 1;
            }
            return;
        }
        //Debug.Log("currently swaping " + current + " to be " + toBe);
        //Debug.Log("size of inventory is " + inventory.Container.Count);
        if(toBeInvSwap.Container[toBe].item == currentInvSwap.Container[current].item)
        {
            toBeInvSwap.Container[toBe].amount += currentInvSwap.Container[current].amount;
            currentInvSwap.Container[current].item = empty;
            currentInvSwap.Container[current].amount = 0;
        }
        else
        {
            tempSwap.Container[0].item = toBeInvSwap.Container[toBe].item;
            tempSwap.Container[0].amount = toBeInvSwap.Container[toBe].amount;
            toBeInvSwap.Container[toBe].item = currentInvSwap.Container[current].item;
            toBeInvSwap.Container[toBe].amount = currentInvSwap.Container[current].amount;
            currentInvSwap.Container[current].item = tempSwap.Container[0].item;
            currentInvSwap.Container[current].amount = tempSwap.Container[0].amount;
            tempSwap.Container[0].item = empty;
            tempSwap.Container[0].amount = 0;
        }
    }

    public void SetDragging(GameObject obj)
    {
        if (!obj.name.Contains("Anvil"))
        {
            Color temp;
            temp = obj.GetComponent<Image>().color;
            temp.a = 0.4f;
            obj.GetComponent<Image>().color = temp;
            
            if (obj.GetComponentsInChildren<Image>()[1])
                obj.GetComponentsInChildren<Image>()[1].raycastTarget = false;
        }
        obj.GetComponent<Image>().raycastTarget = false;
        obj.transform.SetAsLastSibling();
    }

    public void exitDrag(GameObject obj)
    {
        if (!obj.name.Contains("Anvil"))
        {
            Color temp;
            temp = obj.GetComponent<Image>().color;
            temp.a = 1f;
            obj.GetComponent<Image>().color = temp;
        }
        obj.GetComponent<Image>().raycastTarget = true;
    }

    public void SetHighlightedHot(int f)
    {
        highlightedHotSlot = f;
    }

    private void checkThenAdd(string checker)
    {
        if (currentInvSwap.Container[current].item.itemName.Contains(checker))
        {
            if (toBeInvSwap.Container[toBe].item.itemName.Contains(checker))
            {
                toBeInvSwap.Container[toBe].amount += currentInvSwap.Container[current].amount;
                currentInvSwap.Container[current].amount = 0;
                currentInvSwap.Container[current].item = empty;
                return;
            }

            toBeInvSwap.Container[toBe].item = currentInvSwap.Container[current].item;
            toBeInvSwap.Container[toBe].amount = currentInvSwap.Container[current].amount;
            currentInvSwap.Container[current].amount = 0;
            currentInvSwap.Container[current].item = empty;
        }
    }*/
}
