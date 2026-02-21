using UnityEngine;

public class SmithingCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform virtualCamera; 
    [SerializeField] private Transform pivotTarget;    

    [Header("Orbit Settings")]
    [SerializeField] private float yaw = 45f;
    [SerializeField] private float pitch = 25f;
    [SerializeField] private float keyboardOrbitSpeed = 120f;
    [SerializeField] private float mouseSensitivity = 3f;

    [Header("Zoom Settings")]
    [SerializeField] private float distance = 3f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minDistance = 1.5f;
    [SerializeField] private float maxDistance = 6f;

    [Header("Pitch Limits")]
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 75f;

    private float defaultYaw;
    private float defaultPitch;
    private float defaultDistance;

    private void Start()
    {
        StoreDefaults();
        UpdateCameraPosition();
    }

    private void Update()
    {
        HandleKeyboardOrbit();
        HandleMouseOrbit();
        HandleZoom();
        UpdateCameraPosition();
    }

    private void HandleKeyboardOrbit()
    {
        if (!Input.GetMouseButton(1)) // Only when NOT holding right mouse
        {
            float input = Input.GetAxis("Horizontal"); // A/D
            yaw += input * keyboardOrbitSpeed * Time.deltaTime;
        }
    }

    private void HandleMouseOrbit()
    {
        if (Input.GetMouseButton(1))
        {
            if(Cursor.lockState != CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.Locked;
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yaw += mouseX * mouseSensitivity;
            pitch -= mouseY * mouseSensitivity;

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
        else
        {
            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;

        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0f)
        {
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }

    private void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 direction = rotation * Vector3.forward;
        Vector3 desiredPosition = pivotTarget.position - direction * distance;

        virtualCamera.position = desiredPosition;
        virtualCamera.LookAt(pivotTarget.position);
    }

    public void StoreDefaults()
    {
        defaultYaw = yaw;
        defaultPitch = pitch;
        defaultDistance = distance;
    }

    public void ResetCamera()
    {
        yaw = defaultYaw;
        pitch = defaultPitch;
        distance = defaultDistance;
        UpdateCameraPosition();
    }

    public void SetPivot(Vector3 point) { pivotTarget.position = point; }
}