using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controls : MonoBehaviour
{
    //fields put in the inspector
    [SerializeField] private Transform groundCheckTransform = null;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private GameObject playerModel;
    [SerializeField] private GameObject orin;

    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private Button exitPause;
    [SerializeField] private Button saveGame;
    [SerializeField] private Button loadPause;
    [SerializeField] private Button backtoMainMenu;

    [SerializeField] private GameObject mainMenu;
    [SerializeField] private Button loadGame;
    [SerializeField] private Button newGame;
    [SerializeField] private Button exitGame;

    [SerializeField] private Camera menuCam;
    [SerializeField] private Camera playerCam;
    [SerializeField] private LayerMask PlayerView;
    [SerializeField] private LayerMask CutSceneView;
    [SerializeField] private GameObject CinimaCam;

    [SerializeField] private GameObject hpandStaI;
    [SerializeField] private Text infoStat;


    [SerializeField] private GameObject axe;
    [SerializeField] private GameObject pickaxe;
    [SerializeField] private GameObject sword;
    //[SerializeField] private GameObject Crossair;

    private Animator animate;

    private bool menuIsUp;
    private bool invIsUp;
    private bool lockedMovement;
    private bool isWaiting = false;
    private bool jumpKeyWasPressed;
    private bool shiftKeyWasPressed = false;
    private bool escKeyWasPressed;
    private bool mouseclick1;
    private int hotbarSelected = 0;
    private float horizontalInput;
    private float verticalInput;
    private float playerSpeed = 30;
    private float playerRotationSpeed = 240;
    private Rigidbody rigidbodyComp;
    private List<bool> isHighlighted = new List<bool>(17);

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < 18; i++)
        {
            isHighlighted.Add(false);
        }
        playerCam.enabled = false;
        menuCam.enabled = true;

        //menu canvasus deactivated
        pauseMenu.SetActive(false);
        mainMenu.SetActive(true);

        hpandStaI.SetActive(false);

        //cursor locking
        Cursor.lockState = CursorLockMode.None;
        CinimaCam.SetActive(false);

        //set movement enabled
        lockedMovement = true;

        //listeners for the menu buttons
        exitPause.onClick.AddListener(exitPauseMenu);
        saveGame.onClick.AddListener(saveState);
        loadPause.onClick.AddListener(load);
        backtoMainMenu.onClick.AddListener(toMainMenu);

        loadGame.onClick.AddListener(load);
        newGame.onClick.AddListener(nGame);
        exitGame.onClick.AddListener(exitEverything);

        animate = GetComponentInChildren<Animator>();

        //rigidbody for the player
        rigidbodyComp = GetComponent<Rigidbody>();

    }

    private void exitEverything()
    {
        Application.Quit();
    }

    private void nGame()
    {
        startGame();
    }

    private void load()
    {
        //WIP
    }

    private void startGame()
    {
        mainMenu.SetActive(false);

        playerCam.enabled = true;
        menuCam.enabled = false;

        //cursor locking
        Cursor.lockState = CursorLockMode.Locked;
        CinimaCam.SetActive(true);


        //set movement enabled
        lockedMovement = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
            shiftKeyWasPressed = true;

        if (Input.GetKeyUp(KeyCode.LeftShift))
            shiftKeyWasPressed = false;

        //input for space bar for jumping
        if (Input.GetKeyDown(KeyCode.Space))
            jumpKeyWasPressed = true;

        //input for escape for menu
        if (Input.GetKeyDown(KeyCode.Escape))
            escKeyWasPressed = true;

        if (Input.GetKeyDown(KeyCode.Mouse0))
            mouseclick1 = true;
        if (Input.GetKeyUp(KeyCode.Mouse0))
            mouseclick1 = false;


        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            //sword
            if (hotbarSelected != 1)
            {
                pickaxe.SetActive(false);
                axe.SetActive(false);
                animate.SetBool("HoldingAxe", false);
                sword.SetActive(true);
                hotbarSelected = 1;
            }
            else
            {
                hotbarSelected = 0;
                sword.SetActive(false);
            }

        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            //pick
            if (hotbarSelected != 2)
            {
                sword.SetActive(false);
                axe.SetActive(false);
                animate.SetBool("HoldingAxe", true);
                pickaxe.SetActive(true);
                hotbarSelected = 2;
            }
            else
            {
                hotbarSelected = 0;
                pickaxe.SetActive(false);
                animate.SetBool("HoldingAxe", false);
            }

        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            //axe
            if (hotbarSelected != 3)
            {
                pickaxe.SetActive(false);
                sword.SetActive(false);
                animate.SetBool("HoldingAxe", true);
                axe.SetActive(true);
                hotbarSelected = 3;
            }
            else
            {
                hotbarSelected = 0;
                axe.SetActive(false);
                animate.SetBool("HoldingAxe", false);
            }
        }

        //input for adsw for moving in game
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
    }

    private void FixedUpdate()
    {
        //setting UI for player
        setPlayerStats(!lockedMovement);

        //for glitchy animations stop after ui is turned off
        if (lockedMovement)
        {
            animate.SetBool("isRunning", false);
            animate.SetBool("isWalking", false);
            animate.SetBool("strafeL", false);
            animate.SetBool("strafeR", false);
            animate.SetBool("HoldingItem", false);
            animate.SetBool("HoldingAxe", false);
            animate.SetBool("HoldingHammer", false);
            animate.SetBool("HitWithHammer", false);
            animate.SetBool("HitWithAxe", false);
        }

        //sprinting if shift is pressed
        setPlayerRunning(shiftKeyWasPressed);

        //appling the movement if not locked
        if (!lockedMovement)
        {
            var forward = playerCam.transform.forward;
            var right = playerCam.transform.right;

            var lookingDirection = orin.transform.rotation;

            //project forward and right vectors on the horizontal plane (y = 0)

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            //setting direction to move
            var desiredMoveDirection = forward * verticalInput + right * horizontalInput;
            
            //actual movement
            if (desiredMoveDirection != Vector3.zero)
            {
                animate.SetBool("isWalking", true);
                transform.Translate(desiredMoveDirection * playerSpeed * Time.deltaTime);
            }

            //animating
            if (desiredMoveDirection == Vector3.zero)
            {
                animate.SetBool("isWalking", false);
            }

            //turning the player based on if a w s or d was pressed
            if (horizontalInput != 0 | verticalInput != 0)
            {
                Quaternion toRotate = Quaternion.LookRotation(desiredMoveDirection, Vector3.up);
                playerModel.transform.rotation = Quaternion.RotateTowards(playerModel.transform.rotation, toRotate, playerRotationSpeed * Time.deltaTime);
            }
        }

        //using escape again while in menu to exit the menu
        if (menuIsUp)
        {
            if (escKeyWasPressed)
            {
                exitPauseMenu();
            }
        }

        if (mouseclick1)
        {

            if (hotbarSelected == 1)
            {
                //sword animations
            }
            if (hotbarSelected == 2)
            {
                animate.SetBool("HitWithAxe", true);
            }
            if (hotbarSelected == 3)
            {
                animate.SetBool("HitWithAxe", true);
            }
        }

        
        //bringing up the menu by enabling the canvas and
        //unlocking the cursor along with locking movement
        //Debug.Log(lockedMovement);
        //Debug.Log(invIsUp);
        if (escKeyWasPressed)
        {
            if (FindObjectOfType<WorkstationScript>().stationCheck())
            {
                lockedMovement = false;
                SetMainPlayerView();
                FindObjectOfType<WorkstationScript>().DisableUI();
                escKeyWasPressed = false;
            }
            
        }

        if (escKeyWasPressed)
        {
            if (!lockedMovement)
            {
                invIsUp = false;
                pauseMenu.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                CinimaCam.SetActive(false);
                lockedMovement = true;
                escKeyWasPressed = false;
                menuIsUp = true;
            }
        }


        //for allowing jumping or not
        if (!lockedMovement)
        {
            //for checking if the player is on the ground or not
            if (Physics.OverlapSphere(groundCheckTransform.position, 0.1f, playerMask).Length == 0)
            {
                animate.SetBool("isJumping", false);
                //Debug.Log("in air");
                return;
            }

            //Debug.Log("on ground");

            //adds force upward for jumps
            if (jumpKeyWasPressed)
            {
                animate.SetBool("isJumping", true);
                rigidbodyComp.AddForce(Vector3.up * 4, ForceMode.VelocityChange);
                jumpKeyWasPressed = false;

            }
        }


    }


    private void setPlayerRunning(bool temp)
    {
        if (temp)
            playerSpeed = 10;
        else
            playerSpeed = 30;

        if (!temp)
        {
            animate.SetBool("isWalking", !temp);
        }
        animate.SetBool("isRunning", temp);
    }

    private IEnumerator Wait(float v)
    {
        isWaiting = true;
        yield return new WaitForSeconds(v);
        isWaiting = false;
        animate.SetBool("HitWithAxe", false);
    }


    //returns movements used in the camera script
    public bool getMovements()
    {
        return lockedMovement;
    }

    public void setPlayerStats(bool temp)
    {
        hpandStaI.SetActive(temp);
        infoStat.gameObject.SetActive(temp);
    }

    //used for quitting the menu
    public void exitPauseMenu()
    {
        //unlocks things
        lockedMovement = false;
        pauseMenu.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        CinimaCam.SetActive(true);
        menuIsUp = false;
        escKeyWasPressed = false;
        invIsUp = false;
    }
    private void toMainMenu()
    {
        //confirming to go back then run this

        playerCam.enabled = false;
        menuCam.enabled = true;

        //menu canvasus deactivated
        pauseMenu.SetActive(false);
        mainMenu.SetActive(true);

        //cursor locking
        Cursor.lockState = CursorLockMode.None;
        CinimaCam.SetActive(false);

        //set movement enabled
        lockedMovement = true;

    }

    private void saveState()
    {
        //WIP
    }

    public bool getInventoryStat()
    {
        return invIsUp;
    }

    /*public void RoomCheck(Items itemMain, GameObject itemobj)
    {
        Items empty = FindObjectOfType<Gui>().ReturnEmpty();
        bool addeditem = false;
        //Debug.Log("RoomCheck is Running");
        for (int i = 0; i < 15; i++)
        {
            //Debug.Log(inventory.Container[i].item.itemID);
            if (inventory.Container[i].item == itemMain | inventory.Container[i].item.itemID == empty.itemID)
            {
                //Debug.Log("has item true");
                if (addeditem == false)
                {
                    inventory.AddItem(itemMain, 1);
                    Destroy(itemobj);
                    addeditem = true;
                }
            }
        } 

        if(!addeditem)
            Debug.Log("no room");
    }*/

    public void updateHighlighted(bool _bool, int i)
        {
            isHighlighted[i] = _bool;
        }

    public Vector3 GetPlayerPos()
        {
            return gameObject.transform.position;
        }


    public void setDirectonLooking(int looking)
    {
        Debug.Log(looking);
    }

    public Animator GetAnimator()
    {
        return animate;
    }

    public Camera getMainCamera()
    {
        return playerCam;
    }

    public int getHotbarSelected()
    {
        return hotbarSelected;
    }

    public void setLockedMovement(bool temp)
    {
        lockedMovement = temp;
    }
    public GameObject getGameObj()
    {
        return gameObject;
    }

    public void SetCutScenePlayerView()
    {
        playerCam.cullingMask = CutSceneView;
    }
    public void SetMainPlayerView()
    {
        playerCam.cullingMask = PlayerView;
    }
}
