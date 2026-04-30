using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class AnvilManager : MonoBehaviour
{
    [Header("UI Assignments")]
    [SerializeField] private Button ChangeSide;
    [SerializeField] private Button ChangeAxisB;
    [SerializeField] private Button ResetButton;
    [SerializeField] private Button EditModeSwitch;
    [SerializeField] private Button MovePivotPoint;
    [SerializeField] private Button ExitEditMode;

    [Header("Hammer Settings")]
    [SerializeField] private GameObject hammerOBJ;
    [SerializeField] private GameObject hammerSwingPoint;
    [SerializeField] private Vector3 hammerFlatRotationValue;
    [SerializeField] private Vector3 hammerPeenRotationValue;

    [Header("Other Settings")]
    [SerializeField] private float minClickWaitTime = 0.2f;
    [SerializeField] private float rangeInteraction = 4f;
    [SerializeField] private LayerMask itemMask;

    [Header("Helper Sliders")]
    [SerializeField] private Slider zHelperSlider;
    [SerializeField] private Slider yHelperSlider;
    [SerializeField] private Slider xHelperSlider;
    [SerializeField] private Toggle LockZToggle;
    [SerializeField] private Toggle LockYToggle;
    [SerializeField] private Toggle LockXToggle;
    [SerializeField] private Toggle SliderToggle;


    [Header("Refrences")]
    [SerializeField] private TextMeshProUGUI AnvilEditModeText;
    [SerializeField] private WorkstationScript WS;
    [SerializeField] private SmithingCameraController EditCameraController;
    [SerializeField] private AnvilCrafting crafter;
    [SerializeField] private AnvilPlaces anvilPlace;
    [SerializeField] private Camera mainCam;
    [SerializeField] private Gui GUI;

    private GameObject currentRotating;
    private AnvilMode currentAnvilMode;
    private Vector3 lastMousePos;
    private Vector3 dragOffset;
    private Vector3 currentRotateAxis = Vector3.up; // current axis for RotateSide
    private Vector3[] localAxes = new Vector3[3];
    private Plane dragPlane;
    private int currentAxisIndex = 0;
    private bool shiftPressed = false;
    private bool LMBPressed = false;
    private bool isClicking = false;
    private bool isMoving = false;
    private bool isDragging = false;
    private char lockedAxis = '\0';
    private Vector3 ratios = new Vector3(0.33f, 0.33f, 0.34f);

    public bool sliderOn = false;

    void Start()
    {
        currentAnvilMode = AnvilMode.None;
        ChangeSide.onClick.AddListener(RotateSide);
        ChangeAxisB.onClick.AddListener(ChangeAxis);
        ResetButton.onClick.AddListener(ResetObj);
        EditModeSwitch.onClick.AddListener(SwitchEditMode);
        MovePivotPoint.onClick.AddListener(SetMovingBool);
        ExitEditMode.onClick.AddListener(WS.ShowMain);
        SliderToggle.onValueChanged.AddListener(value => OffOnSlider(value));
        zHelperSlider.onValueChanged.AddListener(value => ZXYSliderChanged('z', value));
        xHelperSlider.onValueChanged.AddListener(value => ZXYSliderChanged('x', value));
        yHelperSlider.onValueChanged.AddListener(value => ZXYSliderChanged('y', value));
        LockZToggle.onValueChanged.AddListener(value => LockSlider('z', value));
        LockXToggle.onValueChanged.AddListener(value => LockSlider('x', value));
        LockYToggle.onValueChanged.AddListener(value => LockSlider('y', value));


    }

    private void Update()
    {
        if (currentAnvilMode == AnvilMode.None)
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

    #region Setters
    private void SetMovingBool() { isMoving = true; }
    public void SetRotator(GameObject obj)
    {
        if (obj != currentRotating)
            currentAxisIndex = 0;
        currentRotating = obj;
    }
    public void SetAnvilViewType(AnvilMode mode) { currentAnvilMode = mode; }
    public void SetGravity(bool value, GameObject obj)
    {
        if (obj == null) return;

        obj.GetComponent<Rigidbody>().useGravity = value;
    }

    #endregion

    #region Handle Mouse clicks and movement
    private void ZXYSliderChanged(char axis, float value)
    {
        value = Mathf.Clamp01(value);

        ratios = SolveRatios(ratios, axis, value);

        xHelperSlider.value = GetAxis(ratios, 'x');
        yHelperSlider.value = GetAxis(ratios, 'y');
        zHelperSlider.value = GetAxis(ratios, 'z');

    }
    private Vector3 SolveRatios(Vector3 r, char axis, float newValue)
    {
        newValue = Mathf.Clamp01(newValue);

        if (lockedAxis == axis)
            return r;

        float old = GetAxis(r, axis);
        float delta = newValue - old;

        r = SetAxis(r, axis, newValue);

        if (lockedAxis == '\0')
        {
            // split evenly
            char a1, a2;
            GetOtherAxes(axis, out a1, out a2);

            r = AddToAxis(r, a1, -delta * 0.5f);
            r = AddToAxis(r, a2, -delta * 0.5f);
        }
        else
        {
            char free = GetFreeAxis(axis, lockedAxis);

            float remaining = -delta;

            float freeVal = GetAxis(r, free);
            float newFree = Mathf.Clamp01(freeVal + remaining);

            float actualDelta = newFree - freeVal;

            r = SetAxis(r, free, newFree);

            // if we couldn't absorb full delta  adjust main axis back
            float leftover = remaining - actualDelta;
            r = AddToAxis(r, axis, -leftover);
        }

        return NormalizeToOne(r);
    }

    private void LockSlider(char axis, bool isOn)
    {
        if (!isOn)
        {
            if (lockedAxis == axis)
                lockedAxis = '\0';
            return;
        }

        lockedAxis = axis;

        LockXToggle.isOn = axis == 'x';
        LockYToggle.isOn = axis == 'y';
        LockZToggle.isOn = axis == 'z';
    }

    private void OffOnSlider(bool val)
    {
        sliderOn = val;
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
            //Vector3 targetPos = hit.point + AnvilPos.transform.up * 0.05f;
            Vector3 targetPos = hit.point + transform.up * 0.05f;

            hammerOBJ.transform.position = Vector3.Lerp(
                hammerOBJ.transform.position,
                targetPos,
                Time.deltaTime * 15f
            );

            //Vector3 normal = AnvilPos.transform.up;
            Vector3 normal = transform.up;


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
        if (isMoving)
        {
            MovePivot();
            isMoving = false;
            return;
        }

        Ray ray = mainCam.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        bool hitRay = false;
        if (Physics.Raycast(ray, out hit, rangeInteraction, itemMask))
        {
            currentRotating = hit.transform.gameObject;
            hitRay = true;
            Debug.Log("hit something");
        }
        else
            anvilPlace.ReTriggerCol();

        if (currentRotating == null)
            return;

        if (currentAnvilMode != AnvilMode.view)
        {
            if (!hitRay) return;

            if (currentAnvilMode == AnvilMode.Flat)
            {
                StartCoroutine(clickWait());
                if (crafter.HandleCrafting(hit))
                    StartCoroutine(SwingHammerAnimation(true));
            }
            else if (currentAnvilMode == AnvilMode.Peen)
            {
                StartCoroutine(clickWait());
                if (crafter.HandleCrafting(hit))
                    StartCoroutine(SwingHammerAnimation(false));
            }
        }
        else
        {
            if (GUI.currentState != CursorState.closedHandCursor)
                GUI.SetClosedHCursor(Input.mousePosition);
            HandlePointAndDrag();
        }
    }
    public Vector3 GetHitDirection(RaycastHit hit)
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

    private void HandlePointAndDrag()
    {
        if (currentRotating == null)
            return;

        // Start drag
        if (!isDragging)
        {
            isDragging = true;
            lastMousePos = Input.mousePosition;

            // Plane aligned with Anvil's up
            //was anvilpos.transform
            dragPlane = new Plane(transform.up,
                currentRotating.transform.position + transform.up * 0.01f
            );

            // Compute initial offset
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                dragOffset = currentRotating.transform.position - hitPoint;
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
                currentRotating.transform.position = Vector3.Lerp(
                    currentRotating.transform.position,
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
            currentRotating.transform.Rotate(topAxis, mouseDelta.x * rotateSpeed, Space.World);

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

    #endregion

    #region Rotaing
    // Call this on "Change Axis" button
    public void ChangeAxis()
    {
        currentAxisIndex = (currentAxisIndex + 1) % 3;
    }

    public void RotateSide()
    {
        if (currentRotating == null) return;

        Vector3 axis = currentAxisIndex switch
        {
            0 => Vector3.up,
            1 => Vector3.right,
            2 => Vector3.forward,
            _ => Vector3.up
        };

        Quaternion rotation = Quaternion.AngleAxis(90f, axis);
        currentRotating.transform.rotation = rotation * currentRotating.transform.rotation;
    }

    public void ResetObj()
    {
        if (currentRotating == null) return;

        currentRotating.transform.rotation = Quaternion.identity;
        currentRotateAxis = Vector3.up;
    }

    #endregion

    #region helpers
    private Vector3 GetCurrentTopAxis()
    {
        if (currentRotating == null)
            return Vector3.up;

        // Optional: snap to nearest world axis for stability
        Vector3 up = currentRotating.transform.up;
        Vector3 right = currentRotating.transform.right;
        Vector3 forward = currentRotating.transform.forward;

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

    public void HideHammer()
    {
        hammerOBJ.SetActive(false);
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

    public Vector3 GetHammerRight() { return hammerOBJ.transform.right; }

    public AnvilMode GetCurrentAnvilMode() { return currentAnvilMode; }
    public float GetXSliderHelper() { return xHelperSlider.value; }
    public float GetZSliderHelper() { return zHelperSlider.value; }
    public float GetYSliderHelper() { return yHelperSlider.value; }

    private float GetAxis(Vector3 v, char a)
    {
        return a switch
        {
            'x' => v.x,
            'y' => v.y,
            'z' => v.z,
            _ => 0f
        };
    }

    private Vector3 SetAxis(Vector3 v, char a, float value)
    {
        switch (a)
        {
            case 'x': v.x = value; break;
            case 'y': v.y = value; break;
            case 'z': v.z = value; break;
        }
        return v;
    }
    private Vector3 AddToAxis(Vector3 v, char a, float delta)
    {
        float value = GetAxis(v, a) + delta;
        value = Mathf.Clamp01(value);
        return SetAxis(v, a, value);
    }
    private void GetOtherAxes(char changed, out char a1, out char a2)
    {
        if (changed == 'x') { a1 = 'y'; a2 = 'z'; }
        else if (changed == 'y') { a1 = 'x'; a2 = 'z'; }
        else { a1 = 'x'; a2 = 'y'; }
    }
    private char GetFreeAxis(char changed, char locked)
    {
        if (changed != 'x' && locked != 'x') return 'x';
        if (changed != 'y' && locked != 'y') return 'y';
        return 'z';
    }
    private Vector3 NormalizeToOne(Vector3 v)
    {
        float sum = v.x + v.y + v.z;

        if (sum <= 0.0001f)
            return new Vector3(0.33f, 0.33f, 0.34f);

        return v / sum;
    }
    #endregion
}
