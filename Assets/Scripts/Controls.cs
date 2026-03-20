using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Controls : MonoBehaviour
{
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckTransform;
    [SerializeField] private LayerMask groundMask;

    [Header("References")]
    [SerializeField] private GameObject playerModel;
    [SerializeField] private GameObject orin;
    [SerializeField] private Camera playerCam;

    [Header("Camera Culling")]
    [SerializeField] private LayerMask playerViewMask;
    [SerializeField] private LayerMask cutSceneViewMask;

    [Header("Weapons")]
    [SerializeField] private GameObject axe;
    [SerializeField] private GameObject pickaxe;
    [SerializeField] private GameObject sword;

    // Movement
    private float playerSpeed = 30f;
    public float WalkSpeed = 30f;
    public float SprintSpeed = 10f;
    private const float RotationSpeed = 240f;

    // Input flags — set in Update, consumed in FixedUpdate
    private bool jumpPressed;
    private bool sprintHeld;
    private bool escPressed;
    private bool attackHeld;
    private float horizontalInput;
    private float verticalInput;

    // State
    public float health;
    public float stamina;
    public bool MovementLocked { get; private set; } = true;
    private int hotbarSelected = 0;
    private bool isAttackPlaying = false;

    private Animator animate;
    private Rigidbody rb;


    private void Start()
    {
        animate = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        GatherInput();
    }

    private void FixedUpdate()
    {
        HandleEscWhileInStation();

        if (MovementLocked)
        {
            ClearMovementAnimations();
            return;
        }

        HandleSprint();
        HandleMovement();
        HandleJump();
        HandleAttack();
    }


    private void GatherInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) escPressed = true;

        if (MovementLocked) return;

        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        sprintHeld = Input.GetKey(KeyCode.LeftShift);

        if (Input.GetKeyDown(KeyCode.Space)) jumpPressed = true;

        attackHeld = Input.GetMouseButton(0);

        HandleHotbarInput();
    }

    private void HandleHotbarInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ToggleHotbar(1, sword, false);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ToggleHotbar(2, pickaxe, true);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ToggleHotbar(3, axe, true);
    }

    // Selects the slot, or deselects if already selected
    private void ToggleHotbar(int slot, GameObject weapon, bool holdingAxeAnim)
    {
        if (hotbarSelected == slot)
        {
            hotbarSelected = 0;
            weapon.SetActive(false);
            animate.SetBool("HoldingAxe", false);
        }
        else
        {
            // Deactivate everything first
            sword.SetActive(false);
            axe.SetActive(false);
            pickaxe.SetActive(false);
            animate.SetBool("HoldingAxe", holdingAxeAnim);

            weapon.SetActive(true);
            hotbarSelected = slot;
        }
    }

    private void HandleMovement()
    {
        var forward = playerCam.transform.forward;
        var right = playerCam.transform.right;

        forward.y = 0f; forward.Normalize();
        right.y = 0f; right.Normalize();

        var desiredDir = forward * verticalInput + right * horizontalInput;
        bool isMoving = desiredDir != Vector3.zero;

        animate.SetBool("isWalking", isMoving);

        if (isMoving)
        {
            transform.Translate(desiredDir * playerSpeed * Time.deltaTime, Space.World);

            Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
            playerModel.transform.rotation = Quaternion.RotateTowards(
                playerModel.transform.rotation, targetRot, RotationSpeed * Time.deltaTime);
        }
    }

    private void HandleSprint()
    {
        playerSpeed = sprintHeld ? SprintSpeed : WalkSpeed;
        animate.SetBool("isRunning", sprintHeld);
    }

    private void HandleJump()
    {
        bool onGround = Physics.OverlapSphere(
            groundCheckTransform.position, 0.1f, groundMask).Length > 0;

        animate.SetBool("isJumping", !onGround);

        if (onGround && jumpPressed)
        {
            rb.AddForce(Vector3.up * 4f, ForceMode.VelocityChange);
        }
        jumpPressed = false;
    }

    private void HandleAttack()
    {
        // Only trigger on the frame the button goes down, and avoid spam
        if (!attackHeld || isAttackPlaying) return;

        if (hotbarSelected == 2 || hotbarSelected == 3)
        {
            animate.SetBool("HitWithAxe", true);
            StartCoroutine(ClearAttackAfterDelay(0.6f));
        }
        // Slot 1 (sword) — add sword animation here
    }

    private IEnumerator ClearAttackAfterDelay(float delay)
    {
        isAttackPlaying = true;
        yield return new WaitForSeconds(delay);
        animate.SetBool("HitWithAxe", false);
        isAttackPlaying = false;
    }

    private void HandleEscWhileInStation()
    {
        if (!escPressed) return;

        var station = FindFirstObjectByType<WorkstationScript>();
        if (station != null && station.stationCheck())
        {
            SetMovementLocked(false);
            SetMainPlayerView();
            station.DisableUI();
            escPressed = false;
        }

        var cheatMenu = FindFirstObjectByType<CheatMenu>();
        if (cheatMenu != null && cheatMenu.IsConsoleUp())
        {
            cheatMenu.ToggleConsole();
            escPressed = false;
        }
    }

    public void ConsumeEscPress() => escPressed = false;

    public bool EscWasPressed() => escPressed;

    private void ClearMovementAnimations()
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

    public void SetMovementLocked(bool locked) => MovementLocked = locked;
    public int GetHotbarSelected() => hotbarSelected;
    public Animator GetAnimator() => animate;
    public Camera GetMainCamera() => playerCam;
    public Vector3 GetPlayerPosition() => transform.position;
    public GameObject GetGameObject() => gameObject;

    public void SetCutScenePlayerView() => playerCam.cullingMask = cutSceneViewMask;
    public void SetMainPlayerView() => playerCam.cullingMask = playerViewMask;

    public void UpdateHighlighted(bool value, int index)
    {
        // Hook for inventory/highlighting system
    }
}