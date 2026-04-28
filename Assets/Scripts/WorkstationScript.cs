using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum SmithingMode
{
    Normal,
    Expert
}

public class WorkstationScript : MonoBehaviour
{
    [SerializeField] private GameObject PlayerVCam;
    [SerializeField] private GameObject mainVCam;
    [SerializeField] private GameObject anvilVCam;
    [SerializeField] private GameObject SmeltVCam;
    [SerializeField] private Camera mainCam;
    [SerializeField] private LayerMask itemAndAnvilMask;

    [SerializeField] private Button SmeltB;
    [SerializeField] private Button AnvilB;
    [SerializeField] private Button EditB;
    [SerializeField] private Button TongButton;

    [SerializeField] private float minClickWaitTime = 0.2f;

    [SerializeField] private GameObject WorkstationUI;
    [SerializeField] private GameObject WorkstationButtons;
    [SerializeField] private GameObject AnvilPos;

    [SerializeField] private GameObject AnvilUI;
    [SerializeField] private GameObject AnvilEditUI;
    [SerializeField] private GameObject SmeltUI;

    [SerializeField] private GameObject displayObj;

    [SerializeField] private GameObject Hammer;
    [SerializeField] private GameObject Tongs;
    [SerializeField] private GameObject TongPlacement;

    [SerializeField] private CraftingRecipeManager recipeManager;
    [SerializeField] private Controls playerController;
    [SerializeField] private Smeltery smelteryScript;
    [SerializeField] private AnvilPlaces anvilPlace;
    [SerializeField] private AnvilManager anvilManger;
    [SerializeField] private AnvilCrafting anvilCrafter;
    [SerializeField] private GameObject itemDefaultParents;
    [SerializeField] private float rangeInteraction = 6f;

    [SerializeField] private Gui GUI;
    //[SerializeField] private SmithingCameraController EditCameraController;

    public SmithingMode currentSmithingMode;
    private TextMesh display;
    private bool LMBPressed = false;
    private bool usingTongs = false;
    public bool inStation = false;
    private bool isClicking = false;
    private Items itemOnAnvil;
    private Items itemOnTongs;
    private GameObject objOnTongs;


    void Start()
    {
        itemOnTongs = null;
        itemOnAnvil = null;
        mainVCam.SetActive(false);
        anvilVCam.SetActive(false);
        SmeltVCam.SetActive(false);
        WorkstationUI.SetActive(false);
        SmeltB.onClick.AddListener(ShowSmeltery);
        AnvilB.onClick.AddListener(ShowMain);
        EditB.onClick.AddListener(ShowAnvilEdit);

        TongButton.onClick.AddListener(UsingTongs);

        display = displayObj.GetComponentInChildren<TextMesh>();
        currentSmithingMode = SmithingMode.Normal;
        anvilCrafter.ChangeSmithingMode(currentSmithingMode);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
            LMBPressed = true;
        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            LMBPressed = false;
        }

        if (LMBPressed)
        {
            if(!isClicking)
                if (usingTongs)
                {
                    UseTongs();
                    StartCoroutine(letGrab());
                    StartCoroutine(clickWait());
                    usingTongs = false;
                }
        }

        if (Vector3.Distance(AnvilPos.transform.position, playerController.GetPlayerPosition()) < rangeInteraction)
        {
            RaycastHit hit;
            if (Physics.Raycast(AnvilPos.transform.position, (playerController.GetPlayerPosition() - AnvilPos.transform.position), out hit, rangeInteraction))
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
                        playerController.SetMovementLocked(true);
                        playerController.SetCutScenePlayerView();
                        inStation = true;
                        Hammer.SetActive(true);
                        Tongs.SetActive(true);
                        WorkstationUI.SetActive(true);
                        PlayerVCam.SetActive(false);
                        ShowMain();
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
    private void UseTongs()
    {

        Ray ray = mainCam.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, rangeInteraction, itemAndAnvilMask))
        {
            var itemScript = anvilCrafter.GetItemScriptFromHit(hit);
            if(itemScript != null && itemOnTongs == null)
            {
                itemOnAnvil = itemScript;
                if (itemOnAnvil.type != Itemtype.Chunk || itemOnAnvil.type != Itemtype.Dust)
                    PutItemOnTongs(hit);
                return;
            }
            if (itemOnTongs != null)
                TakeItemOffTongs(hit);
        }
    }

    private IEnumerator clickWait()
    {
        isClicking = true;
        yield return new WaitForSeconds(minClickWaitTime);
        isClicking = false;
    }
    private void ShowSmeltery()
    {
        Hammer.SetActive(false);
        Tongs.GetComponent<Animator>().SetBool("InSmeltery", true);
        anvilManger.SetAnvilViewType(AnvilMode.None);

        mainVCam.SetActive(false);
        SmeltVCam.SetActive(true);
        anvilVCam.SetActive(false);

        AnvilUI.SetActive(false);
        AnvilEditUI.SetActive(false);
        SmeltUI.SetActive(true);
    }

    public void ShowMain()
    {
        anvilManger.HideHammer();
        WorkstationButtons.SetActive(true);
        Hammer.SetActive(true);
        Tongs.SetActive(true);
        anvilManger.SetAnvilViewType(AnvilMode.None);

        Tongs.GetComponent<Animator>().SetBool("InSmeltery", false);
        Tongs.GetComponent<Animator>().SetBool("FireTongs", false);

        mainVCam.SetActive(true);
        anvilVCam.SetActive(false);
        SmeltVCam.SetActive(false);

        AnvilEditUI.SetActive(false);
        AnvilUI.SetActive(true);
        SmeltUI.SetActive(false);
    }

   

    private void ShowAnvilEdit()
    {
        WorkstationButtons.SetActive(false);
        anvilManger.SetAnvilViewType(AnvilMode.view);

        Hammer.SetActive(false);
        Tongs.SetActive(false);

        anvilVCam.SetActive(true);
        mainVCam.SetActive(false);
        SmeltVCam.SetActive(false);

        AnvilEditUI.SetActive(true);
        AnvilUI.SetActive(false);
        SmeltUI.SetActive(false);
    }

    public bool stationCheck()
    {
        return inStation;
    }

    public void DisableUI()
    {
        Cursor.lockState = CursorLockMode.Locked;
        anvilManger.SetAnvilViewType(AnvilMode.None);
        WorkstationUI.SetActive(false);

        Hammer.SetActive(false);
        Tongs.SetActive(false);
        Tongs.GetComponent<Animator>().SetBool("FireTongs", false);
        Tongs.GetComponent<Animator>().SetBool("InSmeltery", false);

        PlayerVCam.SetActive(true);
        mainVCam.SetActive(false);
        anvilVCam.SetActive(false);
        SmeltVCam.SetActive(false);
        playerController.SetMovementLocked(false);
        inStation = false;
    }
    private void UsingTongs()
    {
        usingTongs = !usingTongs;
    }

    private void TakeItemOffTongs(RaycastHit hit)
    {
        Debug.Log("take off tongs");
        Tongs.GetComponent<Animator>().SetBool("TongGrab", true);
        objOnTongs.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        
        objOnTongs.GetComponent<Collider>().enabled = true;
        objOnTongs.transform.position = hit.point + new Vector3(0, 0, 0.1f);
        Debug.Log("pos is + " + objOnTongs.transform.position);

        // In Normal mode, snap Y up if this item has a condensing recipe
        if (currentSmithingMode == SmithingMode.Normal)
        {
            Items itemScript = objOnTongs.GetComponent<Items>();
            if (itemScript != null)
            {
                Recipe condensingRecipe = recipeManager.FindRecipe(PhaseType.Condensing, itemScript.itemID);
                if (condensingRecipe != null)
                {
                    Vector3 currentForward = objOnTongs.transform.forward;
                    currentForward.y = 0f;
                    if (currentForward.sqrMagnitude < 0.001f)
                        currentForward = Vector3.forward;
                    objOnTongs.transform.rotation = Quaternion.LookRotation(currentForward.normalized, Vector3.up);
                }
            }
        }

        objOnTongs.GetComponent<Rigidbody>().useGravity = true;
        objOnTongs.transform.SetParent(itemDefaultParents.transform);
        objOnTongs = null;
        itemOnTongs = null;
        smelteryScript.HandleItemOnTongs();
    }
    private void PutItemOnTongs(RaycastHit hit)
    {
        GameObject obj = hit.transform.gameObject;
        if (itemOnTongs == null)
        {
            Tongs.GetComponent<Animator>().SetBool("TongGrab", true);
            StartCoroutine(TongPlaceDelay(obj));
        }
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
    public GameObject getObjOnTongs()
    {
        return objOnTongs;
    }
    private IEnumerator letGrab()
    {
        yield return new WaitForSeconds(.5f);
        Tongs.GetComponent<Animator>().SetBool("TongGrab", false);
        Hammer.GetComponent<Animator>().SetBool("HammerHit", false);
    }
    private IEnumerator TongPlaceDelay(GameObject obj)
    {
        yield return new WaitForSeconds(.5f);
        obj.GetComponent<Rigidbody>().useGravity = false;
        obj.GetComponent<Collider>().enabled = false;
        obj.transform.SetParent(TongPlacement.transform);
        obj.transform.position = TongPlacement.transform.position;

        // In Normal mode, snap Y up if this item has a condensing recipe
        if (currentSmithingMode == SmithingMode.Normal)
        {
            Items itemScript = obj.GetComponent<Items>();
            if (itemScript != null)
            {
                Recipe condensingRecipe = recipeManager.FindRecipe(PhaseType.Condensing, itemScript.itemID);
                if (condensingRecipe != null)
                {
                    Vector3 currentForward = obj.transform.forward;
                    currentForward.y = 0f;
                    if (currentForward.sqrMagnitude < 0.001f)
                        currentForward = Vector3.forward;
                    obj.transform.rotation = Quaternion.LookRotation(currentForward.normalized, Vector3.up);
                }
            }
        }

        objOnTongs = obj;
        obj.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;

        itemOnTongs = itemOnAnvil;
        itemOnAnvil = null;
        smelteryScript.HandleItemOnTongs();
    }

}