using UnityEngine;
using UnityEngine.InputSystem;
using FarmingEngine;

public class InteractInputBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCharacter playerCharacter;

    [Header("Input")]
    [Tooltip("Bind to your New Input System action (Keyboard/E).")]
    [SerializeField] private InputActionReference interactAction;

    private void Reset()
    {
        playerCharacter = GetComponent<PlayerCharacter>();
    }
    
    private void Start()
    {
        Debug.Log("[InteractBridge] Start");
    }
    
    private void Update()
    {
        if (interactAction == null || interactAction.action == null)
            return;

        if (interactAction.action.WasPressedThisFrame())
        {
            Debug.Log("[InteractBridge] E pressed");

            Debug.Log("[InteractBridge] playerCharacter ref = " +
                      (playerCharacter != null ? playerCharacter.name : "NULL"));

            if (playerCharacter != null)
            {
                Debug.Log("[InteractBridge] Calling InteractWithCrosshair()");
                playerCharacter.InteractWithCrosshair();
                Debug.Log("[InteractBridge] Call finished");
            }
            else
            {
                Debug.LogWarning("[InteractBridge] playerCharacter is NULL -> not calling");
            }
        }
    }



    private void OnEnable()
    {
        Debug.Log("[InteractBridge] OnEnable");

        if (playerCharacter == null)
            playerCharacter = GetComponent<PlayerCharacter>();

        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.Enable();
            interactAction.action.performed += OnInteractPerformed;
        }
    }

    private void OnDisable()
    {
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.performed -= OnInteractPerformed;
            interactAction.action.Disable();
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (playerCharacter == null)
            return;

        // Guardrails similar to what PlayerCharacter already checks internally
        if (TheGame.Get().IsPaused())
            return;

        if (!playerCharacter.IsControlsEnabled() || playerCharacter.IsBusy())
            return;

        playerCharacter.InteractWithCrosshair();
    }
}
