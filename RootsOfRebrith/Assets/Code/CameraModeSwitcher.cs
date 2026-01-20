using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

public class CameraModeSwitcher : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private CinemachineVirtualCamera exploreCam;
    [SerializeField] private CinemachineVirtualCamera commandCam;

    [Header("Input")]
    [Tooltip("Button action (e.g. key V)")]
    [SerializeField] private InputActionReference toggleAction;

    [Header("Priorities")]
    [SerializeField] private int explorePriority = 20;
    [SerializeField] private int commandPriority = 20;
    [SerializeField] private int inactivePriority = 10;

    private bool isCommandMode = false;

    private void OnEnable()
    {
        if (toggleAction != null && toggleAction.action != null)
        {
            toggleAction.action.Enable();
            toggleAction.action.performed += OnToggle;
        }
    }

    private void OnDisable()
    {
        if (toggleAction != null && toggleAction.action != null)
        {
            toggleAction.action.performed -= OnToggle;
            toggleAction.action.Disable();
        }
    }

    private void OnToggle(InputAction.CallbackContext ctx)
    {
        isCommandMode = !isCommandMode;
        ApplyCameraState();
    }

    private void Start()
    {
        // Ensure correct initial state
        ApplyCameraState();
    }

    private void ApplyCameraState()
    {
        if (isCommandMode)
        {
            commandCam.Priority = commandPriority;
            exploreCam.Priority = inactivePriority;
        }
        else
        {
            exploreCam.Priority = explorePriority;
            commandCam.Priority = inactivePriority;
        }
    }
}