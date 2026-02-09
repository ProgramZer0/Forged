using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorkstationScript : MonoBehaviour
{
    [SerializeField] private GameObject PlayerVCam;
    [SerializeField] private GameObject AnvilVCam;
    [SerializeField] private GameObject SmeltVCam;

    [SerializeField] private Button SmeltB;
    [SerializeField] private Button AnvilB;

    [SerializeField] private Button AnvilHammer;
    [SerializeField] private Button TongButton;
    [SerializeField] private Button AnvilHit;

    [SerializeField] private Items Emtpy;

    [SerializeField] private GameObject WorkstationUI;
    [SerializeField] private GameObject AnvilUI;
    [SerializeField] private GameObject SmeltUI;

    [SerializeField] private GameObject displayObj;

    [SerializeField] private GameObject AnvilPos;

    [SerializeField] private GameObject Hammer;
    [SerializeField] private GameObject Tongs;
    [SerializeField] private GameObject TongPlacement;

    [SerializeField] private Controls playerController;
    [SerializeField] private Smeltery smelteryScript;
    [SerializeField] private AnvilPlaces anvilPlace;

    [SerializeField] private LayerMask itemMask;

    private TextMesh display;
    private float rangeInteraction = 5f;
    private float hitForce = 0.03f;
    private float hitSurface = 0.2f;
    private bool inStation = false;
    private bool hammerActive = false;
    private bool tongsActive = false;
    private Items itemOnAnvil;
    private Items itemOnTongs;
    private GameObject objOnAnvil;
    private GameObject objOnTongs;
    private Vector3[] vertix;
    private Vector3 tempVertex;
    void Start()
    {
        itemOnTongs = Emtpy;
        itemOnAnvil = Emtpy;
        AnvilVCam.SetActive(false);
        SmeltVCam.SetActive(false);
        WorkstationUI.SetActive(false);

        SmeltB.onClick.AddListener(ShowSmeltery);
        AnvilB.onClick.AddListener(ShowAnvil);
        AnvilHammer.onClick.AddListener(UsingHammer);
        AnvilHit.onClick.AddListener(hitAnvil);
        TongButton.onClick.AddListener(UsingTongs);


        display = displayObj.GetComponentInChildren<TextMesh>();
    }

    void Update()
    {
        if (Vector3.Distance(gameObject.transform.position, playerController.GetPlayerPos()) < rangeInteraction)
        {
            RaycastHit hit;
            if (Physics.Raycast(gameObject.transform.position, (playerController.GetPlayerPos() - transform.position), out hit, rangeInteraction))
            {
                //Debug.Log("hit " + hit.collider.gameObject.name);
                if (hit.collider.tag == "Player")
                {
                    //Debug.Log("is in station " + inStation);
                    //display tool tip if close enough to player and player can see object
                    if (!inStation)
                        display.text = "Press f to use anvil and smelter";

                    if (Input.GetKeyDown(KeyCode.F))
                    {
                        playerController.setLockedMovement(true);
                        playerController.SetCutScenePlayerView();
                        inStation = true;
                        Hammer.SetActive(true);
                        Tongs.SetActive(true);
                        WorkstationUI.SetActive(true);
                        PlayerVCam.SetActive(false);
                        ShowAnvil();
                        Cursor.lockState = CursorLockMode.None;
                    }
                }
            }
        }
        else
        {
            display.text = "";
        }
        if (inStation)
        {
            display.text = "";
        } 
    }

    private void ShowSmeltery()
    {
        Hammer.SetActive(false);
        Tongs.GetComponent<Animator>().SetBool("InSmeltery", true);

        AnvilVCam.SetActive(false);
        SmeltVCam.SetActive(true);

        AnvilUI.SetActive(false);
        SmeltUI.SetActive(true);
    }

    private void ShowAnvil()
    {
        Hammer.SetActive(true);
        Tongs.SetActive(true);

        Tongs.GetComponent<Animator>().SetBool("InSmeltery", false);
        Tongs.GetComponent<Animator>().SetBool("FireTongs", false);

        AnvilVCam.SetActive(true);
        SmeltVCam.SetActive(false);

        AnvilUI.SetActive(true);
        SmeltUI.SetActive(false);
    }

    public bool stationCheck()
    {
        return inStation;
    }

    public void DisableUI()
    {
        Cursor.lockState = CursorLockMode.Locked;
        WorkstationUI.SetActive(false);

        Hammer.SetActive(false);
        Tongs.SetActive(false);
        Tongs.GetComponent<Animator>().SetBool("FireTongs", false);
        Tongs.GetComponent<Animator>().SetBool("InSmeltery", false);

        PlayerVCam.SetActive(true);
        AnvilVCam.SetActive(false);
        SmeltVCam.SetActive(false);
        inStation = false;
    }

    private void UsingHammer()
    {
        tongsActive = false;

        if (hammerActive)
        {
            hammerActive = false;
            //Debug.Log("the hammer is not active ");
        }
        else
        {
            hammerActive = true;
            //Debug.Log("the hammer is active ");
        }              
    }
    private void UsingTongs()
    {
        hammerActive = false;

        if (tongsActive)
        {
            tongsActive = false;
            //Debug.Log("the hammer is not active ");
        }
        else
        {
            tongsActive = true;
            //Debug.Log("the hammer is active ");
        }
    }

    public void hitAnvil()
    {
        if (hammerActive)
        {
            Hammer.GetComponent<Animator>().SetBool("HammerHit", true);
            if (itemOnAnvil.name.Contains("Ore") | itemOnAnvil.itemID == "68")
            {
                changeItem(itemOnAnvil.nextItem);
            }

            if (itemOnAnvil.heatStage >= 1 && itemOnAnvil.impuritySmash)
            {
                changeItem(itemOnAnvil.nextItem);
            }

            if (itemOnAnvil.type == Itemtype.Metals && itemOnAnvil.heatStage == 2)
            {
                Debug.Log("hit vertex");
                ChangingMesh(hitSurface, hitForce);
            }
            if (itemOnAnvil.type == Itemtype.Metals && itemOnAnvil.heatStage == 3)
            {
                ChangingMesh(hitSurface + 5, hitForce);
                Debug.Log("hit all");
            }
            StartCoroutine(letGrab());
        }
        if (tongsActive)
        {
            putItemOnAndOffTongs();
            StartCoroutine(letGrab());
        }
    }

    private void putItemOnAndOffTongs()
    {
        if (itemOnTongs == Emtpy)
        {
            if (itemOnAnvil.CanGoInSmeltery)
            {

                Tongs.GetComponent<Animator>().SetBool("TongGrab", true);
                StartCoroutine(TongPlaceDelay());
            }
        }
        else
        {
            if(itemOnAnvil == Emtpy)
            {
                Tongs.GetComponent<Animator>().SetBool("TongGrab", true);
                Destroy(objOnTongs);
                objOnTongs = null;

                smelteryScript.changedItemOnTongs();
                GameObject o = UnityEngine.Object.Instantiate(itemOnTongs.model, AnvilPos.transform.position, Quaternion.Euler(0, 0, 0));
                itemOnAnvil = itemOnTongs;
                itemOnTongs = Emtpy;

            }
        }
    }

    private void changeItem(Items nextItem)
    {
        Destroy(anvilPlace.objonAnvil);
        GameObject o = UnityEngine.Object.Instantiate(nextItem.model, AnvilPos.transform.position, Quaternion.Euler(0, 0, 0));
    }

    public void setItemOnAnvil(Items item)
    {
        itemOnAnvil = item;
    }
    public void setItemOnTongs(Items item)
    {
        itemOnTongs = item;
    }

    public Items getItemOnTongs()
    {
        return itemOnTongs;
    }
    private IEnumerator letGrab()
    {
        yield return new WaitForSeconds(.5f);
        Tongs.GetComponent<Animator>().SetBool("TongGrab", false);
        Hammer.GetComponent<Animator>().SetBool("HammerHit", false);
    }
    private IEnumerator TongPlaceDelay()
    {
        yield return new WaitForSeconds(.5f);
        GameObject objs = UnityEngine.Object.Instantiate(itemOnAnvil.model, TongPlacement.transform.position, Quaternion.Euler(0, 0, 0));
        objs.GetComponent<Rigidbody>().useGravity = false;
        objs.GetComponent<Collider>().enabled = false;
        objs.transform.SetParent(TongPlacement.transform);
        objOnTongs = objs;

        smelteryScript.changedItemOnTongs();
        Destroy(anvilPlace.objonAnvil);
        itemOnTongs = itemOnAnvil;
        itemOnAnvil = Emtpy;
    }

    private void ChangingMesh(float surfaceAreaHit, float force)
    {
        Mesh tempMesh = objOnAnvil.GetComponentInChildren<MeshFilter>().mesh;
        vertix = tempMesh.vertices;

        Debug.Log("obj on anvil is " + objOnAnvil);

        var v3 = Input.mousePosition;
        v3.z = 1;
        v3 = Camera.main.ScreenToWorldPoint(v3);
        //Debug.Log("hitting point " + v3);

        RaycastHit hit;
        Debug.Log(tempMesh.vertices);
        Debug.DrawRay(v3, AnvilVCam.transform.forward, Color.red, 20f, false);
        if (Physics.Raycast(v3, AnvilVCam.transform.forward, out hit, rangeInteraction, itemMask))
        {
            Debug.Log(hit.collider.gameObject);

            Debug.Log("hit point " + hit.point);
            try
            {
                if (hit.rigidbody.GetComponent<Item>())
                {
                    Debug.Log("Hit item on anvil");
                    Debug.Log("numbers ");
                    for (int i = 0; i < vertix.Length; i++)
                    {
                        tempVertex = vertix[i];
                        tempVertex.x = hit.transform.position.x - tempVertex.x;
                        tempVertex.y = hit.transform.position.y - tempVertex.y;
                        tempVertex.z = hit.transform.position.z - tempVertex.z;

                        if (Vector3.Distance(hit.point, tempVertex) < surfaceAreaHit)
                        {
                            
                            Debug.Log(i);

                            if (tempVertex.y == hit.point.y)
                            {
                                //Debug.Log(i);
                                vertix[i].y = vertix[i].y - force;
                            }
                        }
                    }
                    tempMesh.RecalculateBounds();
                    tempMesh.RecalculateNormals();
                    objOnAnvil.GetComponentInChildren<MeshFilter>().mesh = tempMesh;
                }
            }
            catch
            {
                //nothing 
            }
        }
    }

    public void setObjOnAnvil(GameObject temp)
    {
        objOnAnvil = temp;
    }
}