using Unity.Netcode;
using UnityEngine;

public class CameraMovement : NetworkBehaviour {
    public static bool CanRotate { get; set; } = true;
    private float xRotation;
    private float yRotation;
    [SerializeField] private float sensitivity; // Make global for settings later
    [SerializeField] private Transform person;
    private Transform head;

    // Interaction variables
    private Camera mainCamera;
    [SerializeField] private float interactionRange = 5f;
    [SerializeField] private float rayRadius = 0.15f;
    private IInteractable currentTarget;

    [SerializeField] public static KeyCode interactKey = KeyCode.E;

    private void Awake() {
        // Attempt to find the main camera
        mainCamera = Camera.main;

        // Fallback if Camera.main is null
        if (mainCamera == null) {
            Debug.LogWarning("Main camera not found. Attempting to find a camera manually.");
            mainCamera = FindObjectOfType<Camera>();

            if (mainCamera == null) {
                Debug.LogError("No camera found in the scene. Please ensure a camera is present and tagged as 'MainCamera'.");
                return;
            }
        }

        head = transform.parent?.gameObject?.transform;
        if (head == null) {
            Debug.LogError("Head transform is missing. Ensure the script is attached to the correct GameObject.");
        }
    }

    // Start is called before the first frame update
    void Start() {
        Cursor.lockState = CursorLockMode.Locked;

        // Ensure the camera is properly configured
        if (mainCamera != null) {
            mainCamera.enabled = true;
            if (!mainCamera.GetComponent<AudioListener>().enabled) {
                mainCamera.GetComponent<AudioListener>().enabled = true;
            }
        }
    }

    // Update is called once per frame
    void Update() {
        if (!IsOwner || mainCamera == null)
            return;

        if (CanRotate && !PauseMenu.pausedClient) {
            HandleRotation();
            HandleInteraction();
        }
    }

    /// <summary>
    /// Handles player rotation based on mouse input.
    /// </summary>
    private void HandleRotation() {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;

        // Clamp vertical rotation to prevent over-rotation
        xRotation = Mathf.Clamp(xRotation, -50f, 60f);

        // Apply rotation
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        person.rotation = Quaternion.Euler(0, yRotation, 0);
    }

    /// <summary>
    /// Handles interaction logic with objects in the scene.
    /// </summary>
    private void HandleInteraction() {
        RaycastForInteractable();

        if (Input.GetKeyDown(interactKey) && currentTarget != null) {
            currentTarget.OnInteract();
        }
    }

    /// <summary>
    /// Draws Gizmos in the Scene view for debugging interaction range.
    /// </summary>
    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position + transform.forward * interactionRange, rayRadius);
    }

    /// <summary>
    /// Throws a Spherecast to detect interactable objects.
    /// </summary>
    private void RaycastForInteractable() {
        RaycastHit hitTarget;
        Vector3 start = transform.position;

        if (Physics.CapsuleCast(start, start, rayRadius, transform.forward, out hitTarget, interactionRange)) {
            IInteractable interactable = hitTarget.collider.GetComponent<IInteractable>();

            if (interactable != null && hitTarget.distance <= interactable.MaxRange) {
                if (currentTarget != interactable) {
                    currentTarget?.OnEndHover();
                    currentTarget = interactable;
                    currentTarget.OnStartHover();
                }
            } else {
                currentTarget?.OnEndHover();
                currentTarget = null;
            }
        } else {
            currentTarget?.OnEndHover();
            currentTarget = null;
        }
    }
}
