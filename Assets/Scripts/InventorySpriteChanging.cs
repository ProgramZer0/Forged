using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class InventorySpriteChanging : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Sprite normal;
    [SerializeField] private Sprite normalF;
    [SerializeField] private Sprite highlighted;
    [SerializeField] private Sprite highlightedF;

    [SerializeField] private int slot;

    private bool isFull = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isFull)
        {
            gameObject.GetComponent<Image>().sprite = highlightedF;
        }
        else
        {
            gameObject.GetComponent<Image>().sprite = highlighted;
        }

        FindObjectOfType<Controls>().updateHighlighted(true, slot);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isFull)
        {
            gameObject.GetComponent<Image>().sprite = normalF;
        }
        else
        {
            gameObject.GetComponent<Image>().sprite = normal;
        }

        FindObjectOfType<Controls>().updateHighlighted(false, slot);
    }

    public void SetIsFull(bool temp, int _slot)
    {
        if(_slot == slot)
        {
            isFull = true;
            gameObject.GetComponent<Image>().sprite = normalF;
        }
    }
}
