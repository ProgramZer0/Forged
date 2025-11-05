using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Dragable : MonoBehaviour//, IDragHandler, IEndDragHandler
{
    /*[SerializeField] private int current;
    [SerializeField] private Inventory inventory;
    private Vector3 startingPoint;
    private void Start()
    {
        startingPoint = transform.localPosition;
    }
    public int GetGameObject()
    {
        return current;
    }

    public void OnDrag(PointerEventData eventData)
    {
        //Debug.Log("currently dragging " + current);
        FindObjectOfType<Gui>().SetCurrent(current, inventory);
        FindObjectOfType<Gui>().SetDragging(gameObject);
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //Debug.Log("swap int");
        FindObjectOfType<Gui>().swapSlots();
        resetPos();
        FindObjectOfType<Gui>().exitDrag(gameObject);
    }

    public void resetPos()
    {
        transform.localPosition = startingPoint;
    }*/
}
    

