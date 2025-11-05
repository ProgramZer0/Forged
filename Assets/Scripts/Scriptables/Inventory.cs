using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

[CreateAssetMenu(fileName = "New Inventory", menuName = "Assets/Inventory")]
public class Inventory : ScriptableObject
{
    public List<InventorySlot> Container = new List<InventorySlot>();
    public void AddItem(Items _item, int _amount)
    {
        Items empty = FindObjectOfType<Bloomery>().returnEmpty();
        //Debug.Log("is running");
        bool hasItemRoom = false;
        for (int i = 0; i < 18; i++)
        {
            //Debug.Log("add to item");
            //Debug.Log(Container[i].item);
            if (Container[i].item == _item)
            {
                //Debug.Log("add to item");
                Container[i].AddAmount(_amount);
                return;
            }
        }
        if (hasItemRoom == false)
        {
            for (int ii = 0; ii < 18; ii++)
            {
                //Debug.Log("adding new item");
                if (Container[ii].item.itemID == empty.itemID)
                {
                    Container[ii].item = _item;
                    //Debug.Log("item is " + _item.itemName);
                    Container[ii].amount = _amount;
                    //Debug.Log("item amount is " + _amount);
                    return;
                }
            }
        }
    }

}

[System.Serializable]
public class InventorySlot
{
    public Items item;
    public int amount;
    public InventorySlot(Items _item, int _amount)
    {
        item = _item;
        amount = _amount;
    }

    public void AddAmount(int value)
    {
        //Debug.Log("added");
        amount += value;
    }

    public void DropItem(Vector3 pos)
    {
        GameObject o = Object.Instantiate(item.model, pos, Quaternion.identity);
        amount--;
        //Debug.Log("did drop");
    }
}
