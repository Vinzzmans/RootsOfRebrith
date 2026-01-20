using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

public class CinemachineDualZoom : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private CinemachineVirtualCamera exploreCam;
    [SerializeField] private CinemachineVirtualCamera commandCam;

    [Header("Zoom Input (New Input System)")]
    [Tooltip("Bind this to an action like Mouse Scroll Y (Axis).")]
    [SerializeField] private InputActionReference zoomAction;

    [Header("Explore Zoom Limits")]
    [SerializeField] private float exploreMinDistance = 2.5f;
    [SerializeField] private float exploreMaxDistance = 6.0f;

    [Header("Command Zoom Limits")]
    [SerializeField] private float commandMinDistance = 6.0f;
    [SerializeField] private float commandMaxDistance = 14.0f;

    [Header("Zoom Feel")]
    [Tooltip("Distance change per scroll tick (in world units). Try 0.15 - 0.35.")]
    [SerializeField] private float stepPerTick = 0.25f;

    [Tooltip("Smoothing time. Lower = snappier, higher = floatier.")]
    [SerializeField] private float smoothTime = 0.08f;

    [Tooltip("Ignore tiny scroll noise.")]
    [SerializeField] private float deadzone = 0.001f;

    [Tooltip("Normalize scroll input. Many wheels report ~120 per tick.")]
    [SerializeField] private float scrollUnitsPerTick = 120f;

    private float exploreTarget;
    private float commandTarget;

    private float exploreVelocity;
    private float commandVelocity;

    private void Awake()
    {
        exploreTarget = GetCameraDistance(exploreCam, 3.5f);
        commandTarget = GetCameraDistance(commandCam, 9.0f);
    }

    private void OnEnable()
    {
        if (zoomAction != null && zoomAction.action != null)
            zoomAction.action.Enable();
    }

    private void OnDisable()
    {
        if (zoomAction != null && zoomAction.action != null)
            zoomAction.action.Disable();
    }

    private void Update()
    {
        if (zoomAction == null || zoomAction.action == null)
            return;

        float scrollY = zoomAction.action.ReadValue<float>();
        if (Mathf.Abs(scrollY) < deadzone)
            return;

        CinemachineVirtualCamera activeCam = GetActiveCamera();
        if (activeCam == null)
            return;

        // Normalize: convert raw scroll units into "ticks"
        float ticks = scrollY / Mathf.Max(1f, scrollUnitsPerTick);

        // Optional safety clamp (prevents sudden huge jumps on some devices)
        ticks = Mathf.Clamp(ticks, -3f, 3f);

        // If direction feels inverted, remove the minus here
        float delta = -ticks * stepPerTick;

        if (activeCam == exploreCam)
        {
            exploreTarget = Mathf.Clamp(exploreTarget + delta, exploreMinDistance, exploreMaxDistance);
        }
        else if (activeCam == commandCam)
        {
            commandTarget = Mathf.Clamp(commandTarget + delta, commandMinDistance, commandMaxDistance);
        }
    }

    private void LateUpdate()
    {
        if (exploreCam != null)
        {
            float current = GetCameraDistance(exploreCam, exploreTarget);
            float next = Mathf.SmoothDamp(current, exploreTarget, ref exploreVelocity, smoothTime);
            SetCameraDistance(exploreCam, next);
        }

        if (commandCam != null)
        {
            float current = GetCameraDistance(commandCam, commandTarget);
            float next = Mathf.SmoothDamp(current, commandTarget, ref commandVelocity, smoothTime);
            SetCameraDistance(commandCam, next);
        }
    }

    private CinemachineVirtualCamera GetActiveCamera()
    {
        if (exploreCam == null && commandCam == null) return null;
        if (exploreCam != null && commandCam == null) return exploreCam;
        if (commandCam != null && exploreCam == null) return commandCam;

        return commandCam.Priority > exploreCam.Priority ? commandCam : exploreCam;
    }

    private static float GetCameraDistance(CinemachineVirtualCamera cam, float fallback)
    {
        if (cam == null) return fallback;

        var follow = cam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        return follow != null ? follow.CameraDistance : fallback;
    }

    private static void SetCameraDistance(CinemachineVirtualCamera cam, float value)
    {
        if (cam == null) return;

        var follow = cam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        if (follow != null)
            follow.CameraDistance = value;
    }
}
