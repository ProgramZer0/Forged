using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum AnvilMode
{
    view,
    Flat,
    Peen,
    None
}

public enum AnvilHitType
{
    WarpInward,
    Indent,
    Main,
    Edge
}
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

    [SerializeField] private Button SmeltB;
    [SerializeField] private Button AnvilB;
    [SerializeField] private Button EditB;

    [SerializeField] private Button ChangeSide;
    [SerializeField] private Button ChangeAxisB;
    [SerializeField] private Button ResetButton;
    [SerializeField] private Button EditModeSwitch;
    [SerializeField] private Button MovePivotPoint;
    [SerializeField] private Button ExitEditMode;
    [SerializeField] private Slider Force;

    [SerializeField] private Button TongButton;

    [SerializeField] private Items Emtpy;

    [SerializeField] private GameObject WorkstationUI;
    [SerializeField] private GameObject WorkstationButtons;

    [SerializeField] private GameObject AnvilUI;
    [SerializeField] private GameObject AnvilEditUI;
    [SerializeField] private GameObject SmeltUI;

    [SerializeField] private TextMeshProUGUI AnvilEditModeText;

    [SerializeField] private GameObject displayObj;

    [SerializeField] private GameObject AnvilPos;

    [SerializeField] private GameObject Hammer;
    [SerializeField] private GameObject Tongs;
    [SerializeField] private GameObject TongPlacement;

    [SerializeField] private Controls playerController;
    [SerializeField] private Smeltery smelteryScript;
    [SerializeField] private AnvilPlaces anvilPlace;

    [SerializeField] private LayerMask itemMask;

    [SerializeField] private GameObject hammerOBJ;
    [SerializeField] private GameObject hammerSwingPoint;

    [SerializeField] private Gui GUI;
    [SerializeField] private SmithingCameraController EditCameraController;
    [SerializeField] private CraftingRecipeManager recipeManager;
    [SerializeField] private ItemDatabase itemDatabase;

    [SerializeField] private Vector3 hammerFlatRotationValue;
    [SerializeField] private Vector3 hammerPeenRotationValue;
    [SerializeField] private float minClickWaitTime = 0.05f;
    [SerializeField] private float rangeInteraction = 6f;

    public SmithingMode currentSmithingMode;
    public AnvilMode currentAnvilMode;
    private Vector3 lastMousePos;
    private bool isDragging = false;
    private TextMesh display;
    
    private float hitForce = 0.03f;
    private float hitSurface = 0.2f;
    private bool inStation = false;
    //private bool hammerActive = false;
    //private bool tongsActive = false;
    private bool shiftPressed = false;
    private bool LMBPressed = false;
    private bool isClicking = false;
    private bool isMoving = false;
    private Items itemOnAnvil;
    private Items itemOnTongs;
    private GameObject objOnAnvil;
    private GameObject objOnTongs;
    private Vector3[] vertix;
    private Vector3 tempVertex;
    private Vector3 dragOffset;
    private Plane dragPlane;
    private Vector3 dragVelocity;
    private int verticalRotationIndex = 0; // 0–3
    private Vector3 currentRotateAxis = Vector3.up; // current axis for RotateSide
    private int flipIndex = 0; // 0–3 (if needed)
    private Vector3 currentTopAxis = Vector3.up;
    private Vector3[] localAxes = new Vector3[3];
    private int currentAxisIndex = 0;
    private Recipe currentRecipe;
    private Mesh workingMesh;
    private float originalHeight;

    void Start()
    {
        itemOnTongs = Emtpy;
        itemOnAnvil = Emtpy;
        mainVCam.SetActive(false);
        anvilVCam.SetActive(false);
        SmeltVCam.SetActive(false);
        WorkstationUI.SetActive(false);
        currentAnvilMode = AnvilMode.None;
        SmeltB.onClick.AddListener(ShowSmeltery);
        AnvilB.onClick.AddListener(ShowMain);
        EditB.onClick.AddListener(ShowAnvilEdit);
        ChangeSide.onClick.AddListener(RotateSide);
        ChangeAxisB.onClick.AddListener(ChangeAxis);
        ResetButton.onClick.AddListener(ResetObj);
        EditModeSwitch.onClick.AddListener(SwitchEditMode);
        MovePivotPoint.onClick.AddListener(SetMovingBool);
        ExitEditMode.onClick.AddListener(ShowMain);
        TongButton.onClick.AddListener(UsingTongs);

        InitializeAxes();
        display = displayObj.GetComponentInChildren<TextMesh>();
    }

    void Update()
    {
        if(currentAnvilMode == AnvilMode.None)
        {
            AnvilEditModeText.text = "";
        }
        else
        {
            AnvilEditModeText.text = currentAnvilMode.ToString();
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
            LMBPressed = true;
        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            LMBPressed = false;
            isDragging = false;   
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
            shiftPressed = true;
        if (Input.GetKeyUp(KeyCode.LeftShift))
            shiftPressed = false;

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

        if (LMBPressed)
        {
            if (!isClicking)
                HandleClicking();
        }

        if (currentAnvilMode == AnvilMode.view)
        {
            //SetGravity(false);
            ShowHand();
        }
        else if (currentAnvilMode == AnvilMode.Flat)
        {
            //SetGravity(true);
            ShowFlat();
        }
        else if (currentAnvilMode == AnvilMode.Peen)
        {
            //SetGravity(true);
            ShowPeen();
        }
    }

    private void SetMovingBool()
    {
        isMoving = true;
    }

    public void SetGravity(bool value)
    {
        if (objOnAnvil == null) return;

        objOnAnvil.GetComponent<Rigidbody>().useGravity = value;
    }

    private void MovePivot()
    {
        Camera cam = mainCam.GetComponent<Camera>();
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, rangeInteraction))
        {
            EditCameraController.SetPivot(hit.point);
        }
    }
    private void ShowHand()
    {
        hammerOBJ.SetActive(false);
        if (GUI.currentState != CursorState.handCursor)
            GUI.SetHandCursor(Input.mousePosition);
    }
    private void ShowFlat()
    {
        hammerOBJ.SetActive(true);
        if (GUI.currentState != CursorState.blankCursor)
            GUI.SetBankCursor(Input.mousePosition);

        Camera cam = mainCam.GetComponent<Camera>();
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, rangeInteraction))
        {
            // Hover slightly off surface
            Vector3 targetPos = hit.point + hit.normal * 0.05f;

            hammerOBJ.transform.position = Vector3.Lerp(
                hammerOBJ.transform.position,
                targetPos,
                Time.deltaTime * 15f
            );

            // Rotate hammer to strike INTO the surface
            Vector3 forwardOnSurface = Vector3.ProjectOnPlane(mainCam.transform.forward, hit.normal);

            if (forwardOnSurface.sqrMagnitude < 0.001f)
            {
                forwardOnSurface = Vector3.Cross(hit.normal, mainCam.transform.right);
            }

            Quaternion surfaceRot = Quaternion.LookRotation(-hit.normal, forwardOnSurface);

            Quaternion baseOffset = Quaternion.Euler(hammerFlatRotationValue);

            Quaternion finalRot = surfaceRot * baseOffset;

            hammerOBJ.transform.rotation = Quaternion.Slerp(
                hammerOBJ.transform.rotation,
                finalRot,
                Time.deltaTime * 15f
            );
        }
    }

    private void ShowPeen()
    {
        hammerOBJ.SetActive(true);
        if (GUI.currentState != CursorState.blankCursor)
            GUI.SetBankCursor(Input.mousePosition);

        Camera cam = mainCam.GetComponent<Camera>();
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, rangeInteraction))
        {
            Vector3 targetPos = hit.point + AnvilPos.transform.up * 0.05f;

            hammerOBJ.transform.position = Vector3.Lerp(
                hammerOBJ.transform.position,
                targetPos,
                Time.deltaTime * 15f
            );

            Vector3 normal = AnvilPos.transform.up;

            Vector3 forwardOnSurface = Vector3.ProjectOnPlane(mainCam.transform.forward, normal);

            if (forwardOnSurface.sqrMagnitude < 0.001f)
            {
                forwardOnSurface = Vector3.Cross(normal, mainCam.transform.right);
            }

            Quaternion surfaceRot = Quaternion.LookRotation(-normal, forwardOnSurface);

            Quaternion baseOffset = Quaternion.Euler(hammerPeenRotationValue);

            Quaternion finalRot = surfaceRot * baseOffset;

            hammerOBJ.transform.rotation = Quaternion.Slerp(
                hammerOBJ.transform.rotation,
                finalRot,
                Time.deltaTime * 15f
            );
        }
    }

    private IEnumerator clickWait()
    {
        isClicking = true;
        yield return new WaitForSeconds(minClickWaitTime);
        isClicking = false;
    }

    private void HandleClicking()
    {
        if(isMoving)
        {
            MovePivot();
            isMoving = false;
            return;
        }

        if (currentAnvilMode == AnvilMode.view)
        {
            if (GUI.currentState != CursorState.closedHandCursor)
                GUI.SetClosedHCursor(Input.mousePosition);
            HandlePointAndDrag();
        }
        else if (currentAnvilMode == AnvilMode.Flat)
        {
            StartCoroutine(clickWait());
            if(HandleCrafting())
                StartCoroutine(SwingHammerAnimation(true));
            
        }
        else if (currentAnvilMode == AnvilMode.Peen)
        {
            StartCoroutine(clickWait());
            if (HandleShapingEditor())
                StartCoroutine(SwingHammerAnimation(false));
        }
    }



    private IEnumerator SwingHammerAnimation(bool IsFlatSide)
    {
        // Record current rotation (hover rotation)
        Quaternion startRot = hammerSwingPoint.transform.rotation;

        // Determine swing rotation offset
        Quaternion swingRot;
        if (IsFlatSide)
        {
            // Slight rotation forward for flat strike
            swingRot = startRot * Quaternion.Euler(0, 0f, -30f);
        }
        else
        {
            // Slight rotation downward/right for peen strike
            swingRot = startRot * Quaternion.Euler(0, 0f, 30);
        }

        // Swing towards strike
        float t = 0f;
        float swingDuration = 0.1f; // seconds
        while (t < 1f)
        {
            t += Time.deltaTime / swingDuration;
            hammerSwingPoint.transform.rotation = Quaternion.Slerp(startRot, swingRot, t);
            yield return null;
        }

        // Hold impact for a frame
        yield return new WaitForSeconds(0.05f);

        // Return to hover rotation (current ShowFlat/ShowPeen rotation)
        t = 0f;

        Quaternion endRot = hammerSwingPoint.transform.localRotation;
        Quaternion targetRot = Quaternion.identity;

        while (t < 1f)
        {
            t += Time.deltaTime / swingDuration;
            hammerSwingPoint.transform.localRotation = Quaternion.Slerp(endRot, targetRot, t);
            yield return null;
        }

        hammerSwingPoint.transform.localRotation = Quaternion.identity;
    }

    private bool HandleShapingEditor()
    {
        if (objOnAnvil == null) return false;

        Ray ray = mainCam.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rangeInteraction, itemMask))
        {
            if (hit.collider.gameObject == objOnAnvil)
            {
                Recipe shapingRecipe = recipeManager.FindRecipe(PhaseType.Shaping, itemOnAnvil.itemID);
                if (shapingRecipe != null)
                {
                    float tempNeeded = shapingRecipe.requiredValue * 20;

                    if (itemOnAnvil.heatTimer >= tempNeeded)
                    {
                        Vector3 direction = GetHitDirection(hit);
                        AnvilHitType hitType = DetermineHitType(hit);
                        EditMesh(direction, hit.point, Force.value * hitForce, hitType);
                        return true;
                    }
                }
            }
        }
        
        return false;
    }

    private bool HandleShapingEditor(Recipe shapingRecipe, RaycastHit hit)
    {
        if (shapingRecipe != null)
        {
            float tempNeeded = shapingRecipe.requiredValue * 20;

            if (itemOnAnvil.heatTimer >= tempNeeded)
            {
                Vector3 direction = GetHitDirection(hit);
                AnvilHitType hitType = DetermineHitType(hit);
                EditMesh(direction, hit.point, Force.value * hitForce, hitType);
                return true;
            }
        }
        return false;
    }

    private bool HandleCrafting()
    {
        if (objOnAnvil == null) return false;

        Ray ray = mainCam.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rangeInteraction, itemMask))
        {
            if (hit.collider.gameObject == objOnAnvil)
            {
                Recipe condensingRecipe = recipeManager.FindRecipe(PhaseType.Condensing, itemOnAnvil.itemID);
                Recipe anvilRecipe = recipeManager.FindRecipe(PhaseType.AnvilHammering, itemOnAnvil.itemID);
                Recipe shapingRecipe = recipeManager.FindRecipe(PhaseType.Shaping, itemOnAnvil.itemID);

                if(anvilRecipe != null)
                {
                    Items newItem = itemDatabase.GetItemByID(anvilRecipe.outputItemID);
                    if (newItem != null)
                    {
                        ModelChange(newItem);
                        itemOnAnvil = newItem;
                    }
                }
                else if (shapingRecipe != null)
                {
                    return HandleShapingEditor(shapingRecipe, hit);
                }
                else
                {
                    //needs heat 
                    if (itemOnAnvil.heatTimer >= 0)
                    {
                        if (condensingRecipe != null)
                        {
                            Recipe heatingRecipe = recipeManager.FindRecipe(PhaseType.Heating, itemOnAnvil.itemID);
                            if (heatingRecipe != null)
                            {
                                float tempNeeded = heatingRecipe.requiredValue * 20;

                                if (itemOnAnvil.heatTimer >= tempNeeded)
                                {
                                    CondenseItem(hit, condensingRecipe);
                                    return true;
                                }
                                // not high enough heat
                            }
                            // if nothing else is found 
                        }
                    }
                    //no heat at all
                }
            }
        }
        return false;
    }

    private void CondenseItem(RaycastHit hit, Recipe condensingRecipe)
    {
        float targetPercent = condensingRecipe.requiredValue;

        if (itemOnAnvil.condensed >= targetPercent)
            return;

        float baseStep = Force.value * 0.01f;

        // Optional: diminishing returns
        float efficiency = Mathf.Lerp(1f, 0.2f, itemOnAnvil.condensed);
        float step = baseStep * efficiency;

        float remaining = targetPercent - itemOnAnvil.condensed;
        float appliedStep = Mathf.Min(step, remaining);

        itemOnAnvil.condensed += appliedStep;

        // Calculate scale from progress instead of multiplying
        Vector3 scale;

        if (currentSmithingMode == SmithingMode.Normal)
        {
            scale = GetNormalModeScaleFromProgress(itemOnAnvil.condensed);
            objOnAnvil.transform.localScale = scale;

            if (itemOnAnvil.condensed >= targetPercent)
            {
                //completed
            }
        }
        else
            GetExpertModeScaleFromProgress(hit, condensingRecipe);

        
    }

    private void GetExpertModeScaleFromProgress(RaycastHit hit, Recipe condensingRecipe)
    {
        float force = Force.value;
        float radius = 0.5f;

        if (itemOnAnvil.condensed >= condensingRecipe.requiredValue)
            return;

        // ----- Progress Gain -----
        float baseGain = force * 0.02f;

        float efficiency = Mathf.Lerp(1f, 0.2f, itemOnAnvil.condensed);
        float appliedGain = baseGain * efficiency;

        float remaining = condensingRecipe.requiredValue - itemOnAnvil.condensed;
        appliedGain = Mathf.Min(appliedGain, remaining);

        itemOnAnvil.condensed += appliedGain;

        // ----- Mesh Deformation -----
        Vector3[] vertices = workingMesh.vertices;

        Vector3 localHit = objOnAnvil.transform.InverseTransformPoint(hit.point);
        Vector3 localDirection = objOnAnvil.transform.InverseTransformDirection(Vector3.down);

        float maxHeight = Mathf.Lerp(
            originalHeight,
            originalHeight * 0.5f,
            itemOnAnvil.condensed
        );

        float currentHeight = workingMesh.bounds.size.y;
        float allowedCompression = currentHeight - maxHeight;

        for (int i = 0; i < vertices.Length; i++)
        {
            float distance = Vector3.Distance(vertices[i], localHit);

            if (distance < radius)
            {
                float falloff = 1f - (distance / radius);

                Vector3 move = localDirection * force * 0.01f * falloff;

                // Clamp vertical compression
                if (allowedCompression > 0)
                    vertices[i] += move;
            }
        }

        workingMesh.vertices = vertices;
        workingMesh.RecalculateNormals();
        workingMesh.RecalculateBounds();

        if (itemOnAnvil.condensed >= condensingRecipe.requiredValue)
        {
            //
        }
    }

    private Vector3 GetNormalModeScaleFromProgress(float progress)
    {
        float height = Mathf.Lerp(1f, 0.5f, progress);
        float length = Mathf.Lerp(1f, 1.8f, progress);
        float width = Mathf.Lerp(1f, 0.8f, progress);

        return new Vector3(width, height, length);
    }

    private void ModelChange(Items changeTo)
    {
        //will cover this later when i do visual edits to all the items.
    }

    private Vector3 GetHitDirection(RaycastHit hit)
    {
        if (currentAnvilMode == AnvilMode.Flat)
        {
            return -hit.normal; // push inward
        }
        else if (currentAnvilMode == AnvilMode.Peen)
        {
            return -hit.normal * 1.5f; // sharper push
        }

        return Vector3.down;
    }

    private AnvilHitType DetermineHitType(RaycastHit hit)
    {
        Vector3 localPoint = AnvilPos.transform.InverseTransformPoint(hit.point);

        if (localPoint.x > 0.4f)
            return AnvilHitType.Edge;

        if (localPoint.x < -0.4f)
            return AnvilHitType.WarpInward;

        return AnvilHitType.Main;
    }

    

    private void EditMesh(Vector3 direction, Vector3 worldPoint, float force, AnvilHitType hitType)
    {
        MeshFilter mf = objOnAnvil.GetComponentInChildren<MeshFilter>();
        Mesh mesh = mf.mesh;

        Vector3[] vertices = mesh.vertices;

        // Convert hit point to LOCAL SPACE

        Vector3 localHitPoint = objOnAnvil.transform.InverseTransformPoint(worldPoint);

        float radius = hitSurface;

        for (int i = 0; i < vertices.Length; i++)
        {
            float distance = Vector3.Distance(vertices[i], localHitPoint);

            if (distance < radius)
            {
                float falloff = 1f - (distance / radius);

                Vector3 localDirection = objOnAnvil.transform.InverseTransformDirection(direction);

                vertices[i] += localDirection * force * falloff;

                // Optional behavior per hit type
                if (hitType == AnvilHitType.Edge)
                    vertices[i] += Vector3.right * force * 0.2f * falloff;

                if (hitType == AnvilHitType.WarpInward)
                    vertices[i] += Vector3.forward * force * 0.3f * falloff;
            }
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
    private void HandlePointAndDrag()
    {
        if (objOnAnvil == null) return;


        // Start drag
        if (!isDragging)
        {
            isDragging = true;
            lastMousePos = Input.mousePosition;

            // Plane aligned with Anvil's up
            dragPlane = new Plane(
                AnvilPos.transform.up,
                objOnAnvil.transform.position + AnvilPos.transform.up * 0.01f
            );

            // Compute initial offset
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                dragOffset = objOnAnvil.transform.position - hitPoint;
            }

        }

        if (shiftPressed)
        {
            // MOVE mode: smooth position update
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 targetPos = hitPoint + dragOffset;

                // Smoothly move toward target
                float moveSpeed = 20f; // tweak for responsiveness
                objOnAnvil.transform.position = Vector3.Lerp(
                    objOnAnvil.transform.position,
                    targetPos,
                    moveSpeed * Time.deltaTime
                );
            }
        }
        else
        {

            // Determine the current top face axis
            Vector3 topAxis = GetCurrentTopAxis(); // returns Vector3.up, Vector3.right, etc. depending on side

            Vector3 mouseDelta = Input.mousePosition - lastMousePos;
            float rotateSpeed = 0.3f;

            // Rotate around the top face normal (locked axis)
            objOnAnvil.transform.Rotate(topAxis, mouseDelta.x * rotateSpeed, Space.World);

            lastMousePos = Input.mousePosition;


            /*
            Vector3 mouseDelta = Input.mousePosition - lastMousePos;
            float rotateSpeed = 0.2f;

            // Always use camera axes
            Vector3 camRight = mainCam.transform.right;
            Vector3 camUp = mainCam.transform.up;

            // Build rotations
            Quaternion rotX = Quaternion.AngleAxis(-mouseDelta.x * rotateSpeed, camUp);
            Quaternion rotY = Quaternion.AngleAxis(mouseDelta.y * rotateSpeed, camRight);

            // Apply
            objOnAnvil.transform.rotation = rotX * rotY * objOnAnvil.transform.rotation;*/
        }

        lastMousePos = Input.mousePosition;
    }

    private Vector3 GetCurrentTopAxis()
    {
        if (objOnAnvil == null)
            return Vector3.up;

        // Optional: snap to nearest world axis for stability
        Vector3 up = objOnAnvil.transform.up;
        Vector3 right = objOnAnvil.transform.right;
        Vector3 forward = objOnAnvil.transform.forward;

        // Compare which axis is closest to world up
        float upDot = Mathf.Abs(Vector3.Dot(up, Vector3.up));
        float rightDot = Mathf.Abs(Vector3.Dot(right, Vector3.up));
        float forwardDot = Mathf.Abs(Vector3.Dot(forward, Vector3.up));

        if (upDot > rightDot && upDot > forwardDot)
            return up;
        else if (rightDot > forwardDot)
            return right;
        else
            return forward;
    }

    private void InitializeAxes()
    {
        if (objOnAnvil == null) return;

        localAxes[0] = objOnAnvil.transform.up;
        localAxes[1] = objOnAnvil.transform.right;
        localAxes[2] = objOnAnvil.transform.forward;

        currentAxisIndex = 0;
        currentRotateAxis = localAxes[currentAxisIndex];
    }

    // Call this on "Change Axis" button
    public void ChangeAxis()
    {
        if (objOnAnvil == null) return;
        currentAxisIndex = (currentAxisIndex + 1) % localAxes.Length;
        currentRotateAxis = localAxes[currentAxisIndex];
    }

    // Call this on "Rotate Side" button
    public void RotateSide()
    {

        if (objOnAnvil == null) return;

        Quaternion rotation = Quaternion.AngleAxis(90f, currentRotateAxis);
        objOnAnvil.transform.rotation = rotation * objOnAnvil.transform.rotation;
    }

    public void ResetObj()
    {
        if (objOnAnvil == null) return;

        objOnAnvil.transform.rotation = Quaternion.identity;
        currentRotateAxis = Vector3.up;
    }

    private void ShowSmeltery()
    {
        Hammer.SetActive(false);
        Tongs.GetComponent<Animator>().SetBool("InSmeltery", true);
        currentAnvilMode = AnvilMode.None;

        mainVCam.SetActive(false);
        SmeltVCam.SetActive(true);
        anvilVCam.SetActive(false);

        AnvilUI.SetActive(false);
        AnvilEditUI.SetActive(false);
        SmeltUI.SetActive(true);
    }

    private void ShowMain()
    {
        WorkstationButtons.SetActive(true);
        Hammer.SetActive(true);
        Tongs.SetActive(true);
        currentAnvilMode = AnvilMode.None;

        Tongs.GetComponent<Animator>().SetBool("InSmeltery", false);
        Tongs.GetComponent<Animator>().SetBool("FireTongs", false);

        mainVCam.SetActive(true);
        anvilVCam.SetActive(false);
        SmeltVCam.SetActive(false);

        AnvilEditUI.SetActive(false);
        AnvilUI.SetActive(true);
        SmeltUI.SetActive(false);
    }

    private void SwitchEditMode()
    {
        switch (currentAnvilMode)
        {
            case AnvilMode.view:
                currentAnvilMode = AnvilMode.Flat;
                break;
            case AnvilMode.Flat:
                currentAnvilMode = AnvilMode.Peen;
                break;
            case AnvilMode.Peen:
                currentAnvilMode = AnvilMode.view;
                break;
        }
    }

    private void ShowAnvilEdit()
    {
        WorkstationButtons.SetActive(false);
        currentAnvilMode = AnvilMode.view;

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
        currentAnvilMode = AnvilMode.None;
        WorkstationUI.SetActive(false);

        Hammer.SetActive(false);
        Tongs.SetActive(false);
        Tongs.GetComponent<Animator>().SetBool("FireTongs", false);
        Tongs.GetComponent<Animator>().SetBool("InSmeltery", false);

        PlayerVCam.SetActive(true);
        mainVCam.SetActive(false);
        anvilVCam.SetActive(false);
        SmeltVCam.SetActive(false);
        inStation = false;
    }
    private void UsingTongs()
    {
        putItemOnAndOffTongs();
        StartCoroutine(letGrab());
    }

    private void putItemOnAndOffTongs()
    {
        if (itemOnTongs == Emtpy)
        {
            Tongs.GetComponent<Animator>().SetBool("TongGrab", true);
            StartCoroutine(TongPlaceDelay());
        }
        else
        {
            Tongs.GetComponent<Animator>().SetBool("TongGrab", true);
            Destroy(objOnTongs);
            objOnTongs = null;

            smelteryScript.HandleItemOnTongs();
            GameObject o = UnityEngine.Object.Instantiate(itemOnTongs.model, AnvilPos.transform.position, Quaternion.Euler(0, 0, 0));
            itemOnAnvil = itemOnTongs;
            itemOnTongs = Emtpy;
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
    private IEnumerator TongPlaceDelay()
    {
        yield return new WaitForSeconds(.5f);
        GameObject objs = UnityEngine.Object.Instantiate(itemOnAnvil.model, TongPlacement.transform.position, Quaternion.Euler(0, 0, 0));
        objs.GetComponent<Rigidbody>().useGravity = false;
        objs.GetComponent<Collider>().enabled = false;
        objs.transform.SetParent(TongPlacement.transform);
        objOnTongs = objs;

        smelteryScript.HandleItemOnTongs();
        Destroy(anvilPlace.objonAnvil);
        itemOnTongs = itemOnAnvil;
        itemOnAnvil = Emtpy;
    }
    public void setObjOnAnvil(GameObject temp)
    {
        objOnAnvil = temp;
        InitializeAxes();
    }
}