using UnityEngine;
using UnityEngine.UI;

public class CameraMovements : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Controls playerControls;

    [Header("Camera")]
    [SerializeField] private Slider sensitivitySlider;

    [Header("Pickup")]
    [SerializeField] private LayerMask itemPickupLayer;
    [SerializeField] private GameObject pickUpTarget;
    [SerializeField] private GameObject pickupPrompt;
    [SerializeField] private float pickupRange = 7f;
    [SerializeField] private float pickupSpeed = 100f;
    [SerializeField] private float dropGracePeriod = 3f;
    [SerializeField] private Toggle holdToPickup;

    // Camera rotation
    private float xRotation;
    private float yRotation;

    // Interaction state
    private bool isHoldingMouse;
    private bool wheelButtonPressed;
    private bool isHoldingObject;
    private Rigidbody heldObject;
    private OreSplitter heldOreSplitter;

    // Grace period
    private float gracePeriodTimer;
    private bool inGracePeriod;

    // Cached components
    private Text promptText;
    private Animator playerAnimator;

    private void Awake()
    {
        promptText = pickupPrompt.GetComponentInChildren<Text>();
        playerAnimator = playerControls.GetAnimator();
    }

    private void Update()
    {
        HandleCameraRotation();
        HandleMouseInput();
        HandleGracePeriod();
    }

    private void FixedUpdate()
    {
        if (playerControls.getHotbarSelected() != 0) return;

        HandleRaycast();
        HandleHeldObject();
    }

    private void HandleCameraRotation()
    {
        float sensitivity = sensitivitySlider.value * 20 * Time.deltaTime;
        yRotation += Input.GetAxisRaw("Mouse X") * sensitivity;
        xRotation -= Input.GetAxisRaw("Mouse Y") * sensitivity;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
    }

    private void HandleMouseInput()
    {
        if (holdToPickup.isOn)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0)) isHoldingMouse = true;
            if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                isHoldingMouse = false;
                if (isHoldingObject) HandleRelease();
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                if (isHoldingObject)
                    HandleRelease();
                else
                    isHoldingMouse = true;
            }
        }
    }

    private void HandleGracePeriod()
    {
        if (!inGracePeriod) return;

        gracePeriodTimer -= Time.deltaTime;

        if (gracePeriodTimer <= 0)
        {
            // Raycast from held object back to player
            Vector3 directionToPlayer = transform.position - heldObject.position;
            if (Physics.Raycast(heldObject.position, directionToPlayer.normalized, out RaycastHit hit, directionToPlayer.magnitude))
            {
                if (hit.transform == transform || hit.transform == playerControls.transform)
                {
                    // Player is visible, reset timer
                    gracePeriodTimer = dropGracePeriod;
                    return;
                }
            }

            inGracePeriod = false;
            if (holdToPickup && !isHoldingMouse)
                HandleRelease();
            else if (!holdToPickup)
                HandleRelease();
        }
    }


    private void HandleRaycast()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, pickupRange, itemPickupLayer))
        {
            inGracePeriod = false;
            gracePeriodTimer = dropGracePeriod;

            UpdatePickupPrompt(hit);

            if (isHoldingMouse)
                HandleInteraction(hit);
        }
        else
        {
            if (isHoldingObject && !inGracePeriod)
            {
                inGracePeriod = true;
                gracePeriodTimer = dropGracePeriod;
            }

            if (!isHoldingObject)
            {
                promptText.text = "";
                pickupPrompt.SetActive(false);
            }
        }
    }

    private void UpdatePickupPrompt(RaycastHit hit)
    {
        if (heldObject != null) return;

        pickupPrompt.SetActive(true);
        promptText.text = hit.transform.TryGetComponent(out Item item)
            ? "Hold mouse 1 to pick up " + item.item.name
            : "Hold mouse 1 to pick up " + hit.collider.gameObject.name;
    }

    private void HandleInteraction(RaycastHit hit)
    {
        if (hit.collider.gameObject.TryGetComponent(out WheelButton wheel))
        {
            if (!wheelButtonPressed)
            {
                wheelButtonPressed = true;
                wheel.Turn();
            }
        }
        else if (heldObject == null && hit.rigidbody != null)
        {
            PickUpObject(hit);
        }
    }

    private void PickUpObject(RaycastHit hit)
    {
        heldObject = hit.rigidbody;
        heldObject.useGravity = false;
        heldObject.linearDamping = 10;
        isHoldingObject = true;
        isHoldingMouse = false;

        heldOreSplitter = hit.transform.GetComponentInParent<OreSplitter>();
        if (heldOreSplitter != null)
            heldOreSplitter.CompressChunks();
    }

    private void HandleRelease()
    {
        if (heldObject == null) return;

        heldObject.useGravity = true;
        heldObject.linearDamping = 1;
        heldObject = null;
        isHoldingObject = false;
        inGracePeriod = false;
        gracePeriodTimer = 0;

        playerAnimator.SetBool("Interacting", false);
        pickupPrompt.SetActive(false);

        if (!isHoldingMouse && wheelButtonPressed)
            wheelButtonPressed = false;

        if (heldOreSplitter != null)
        {
            heldOreSplitter.DecompressChunks();
            heldOreSplitter = null;
        }
    }

    private void HandleHeldObject()
    {
        if (heldObject == null) return;

        pickupPrompt.SetActive(false);
        playerAnimator.SetBool("Interacting", true);
        heldObject.AddForce((pickUpTarget.transform.position - heldObject.position) * pickupSpeed);
    }
}
