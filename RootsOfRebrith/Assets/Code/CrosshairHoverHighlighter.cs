using UnityEngine;
using FarmingEngine;

/// <summary>
/// Highlights (Selectable.SetHover) ONLY the object under the crosshair,
/// and ONLY if the player is actually in interaction range.
/// 
/// - Ray distance is derived from the PlayerCharacter interact range (single source of truth).
/// - Uses QueryTriggerInteraction.Collide by default (pickups often use trigger colliders).
/// - Uses Raycast (can be upgraded to SphereCast later if needed).
/// </summary>
public class CrosshairHoverHighlighter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCharacter player;

    [Header("Raycast")]
    [Tooltip("Optional: restrict what can be highlighted (e.g., Interactable layer). Leave as Everything if unsure.")]
    [SerializeField] private LayerMask interactMask = ~0;

    [Tooltip("Extra distance added on top of the player's interact range to compensate for camera offset.")]
    [SerializeField] private float distancePadding = 1.5f;

    [Tooltip("Allow raycast to hit trigger colliders (recommended for pickups).")]
    [SerializeField] private bool hitTriggers = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool debugDrawRay = false;

    private Selectable _current;

    private void Reset()
    {
        player = GetComponentInParent<PlayerCharacter>();
    }

    private void Awake()
    {
        if (player == null)
            player = GetComponentInParent<PlayerCharacter>();
    }

    private void OnDisable()
    {
        // Ensure outline is cleared when disabling
        if (_current != null)
        {
            _current.SetHover(false);
            _current = null;
        }
    }

    private void Update()
    {
        if (player == null)
            return;

        // Don't highlight while paused/busy to avoid confusing feedback
        if (TheGame.Get().IsPaused() || player.IsBusy() || !player.IsControlsEnabled())
        {
            ClearCurrent();
            return;
        }

        Selectable next = GetSelectableUnderCrosshair(player, out RaycastHit hit);

        // Gate by actual interaction range (same rule Interact uses)
        if (next != null && !next.IsInUseRange(player))
        {
            if (debugLogs)
                Debug.Log($"[CrosshairHover] Hit {next.name} but out of use range.");
            next = null;
        }

        if (next == _current)
            return;

        if (_current != null)
            _current.SetHover(false);

        _current = next;

        if (_current != null)
            _current.SetHover(true);

        if (debugLogs)
        {
            string hitName = hit.collider != null ? hit.collider.name : "none";
            Debug.Log($"[CrosshairHover] Current={( _current != null ? _current.name : "NULL")} | RayHit={hitName}");
        }
    }

    private void ClearCurrent()
    {
        if (_current != null)
        {
            _current.SetHover(false);
            _current = null;
        }
    }

    private Selectable GetSelectableUnderCrosshair(PlayerCharacter pc, out RaycastHit hit)
    {
        hit = default;

        Camera cam = Camera.main;
        if (cam == null)
        {
            if (debugLogs)
                Debug.LogWarning("[CrosshairHover] Camera.main is NULL. Tag your gameplay camera as MainCamera.");
            return null;
        }

        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray ray = cam.ScreenPointToRay(screenCenter);

        // Single source of truth: player interact range (+ small padding for camera offset)
        float rayDistance = Mathf.Max(pc.interact_range, 0.1f) + distancePadding;

        QueryTriggerInteraction qti = hitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

        if (debugDrawRay)
            Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.green, 0f);

        if (Physics.Raycast(ray, out hit, rayDistance, interactMask, qti))
        {
            // Selectable might be on parent, not necessarily on the collider object
            return hit.collider.GetComponentInParent<Selectable>();
        }

        return null;
    }
}
