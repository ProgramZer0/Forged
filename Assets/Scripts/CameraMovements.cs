using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraMovements : MonoBehaviour
{
    [SerializeField] private Slider sense;
    [SerializeField] private GameObject playerlocation;
    [SerializeField] private LayerMask ItemPickupLayer;
    [SerializeField] private GameObject PickUpObject;
    [SerializeField] private GameObject pickupPrompt;

    private bool lockedMovement;

    float xRotation;
    float yRotation;
    private bool mouseclick1 = false;
    private bool Buttonhit = false;
    private float pickupRange = 7f;
    private float speed = 100f;

    private Rigidbody heldObject;

    private void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sense.value * 20;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sense.value * 20;
        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        playerlocation.transform.rotation = Quaternion.Euler(0, yRotation, 0);

        if (Input.GetKeyDown(KeyCode.Mouse0))
            mouseclick1 = true;
        if (Input.GetKeyUp(KeyCode.Mouse0))
            mouseclick1 = false;
    }

    private void FixedUpdate()
    {
        if (FindObjectOfType<Controls>().getHotbarSelected() == 0)
        {

            RaycastHit hit;
            
            Debug.DrawRay(transform.position, transform.forward, Color.green, 2, true);
            if (Physics.Raycast(transform.position, transform.forward, out hit, pickupRange, ItemPickupLayer))
            {
                //Debug.Log(hit.collider.gameObject);

                if (hit.transform.TryGetComponent(out Item hit2)){
                    pickupPrompt.SetActive(true);
                    pickupPrompt.GetComponentInChildren<Text>().text = "Hold mouse 1 to pick up " + hit2.name;
                }
            }
            if (mouseclick1)
            {
                //Debug.Log(hit.collider.gameObject);

                try
                {
                    if (hit.collider.gameObject.name.Contains("WheelButton"))
                    {
                        if (!Buttonhit)
                        {
                            Buttonhit = true;

                            Debug.Log("Is a Wheel");

                            hit.collider.gameObject.GetComponent<WheelButton>().Turn();
                        }
                    }
                    else if (heldObject == null)
                    {
                        heldObject = hit.rigidbody;

                        heldObject.useGravity = false;
                        heldObject.drag = 10;
                    }
                }
                catch 
                { 
                    //nothin
                }
            }
            else {
                if (Buttonhit)
                {
                    Buttonhit = false;
                }
                try
                {
                    FindObjectOfType<Controls>().GetAnimator().SetBool("Interacting", false);
                    heldObject.drag = 1;
                    heldObject.useGravity = true;
                }
                catch {
                    //nothing
                }
                heldObject = null;
            }

            if (heldObject)
            {
                pickupPrompt.SetActive(false);
                FindObjectOfType<Controls>().GetAnimator().SetBool("Interacting", true);
                heldObject.AddForce((PickUpObject.transform.position - heldObject.position) * speed);
            }
            else
            {
                pickupPrompt.GetComponentInChildren<Text>().text = "";
                pickupPrompt.SetActive(false);
            }
        }
    }
}
