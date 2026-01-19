using UnityEngine;
using UnityEngine.InputSystem;
using FarmingEngine;

public class FECombatInput : MonoBehaviour
{
    private PlayerCharacterCombat combat;

    private void Awake()
    {
        combat = GetComponent<PlayerCharacterCombat>();
    }

    // Wird von PlayerInput (Behavior = Send Messages) automatisch aufgerufen
    public void OnAttack(InputValue value)
    {
        if (!value.isPressed)
            return;

        if (combat != null && combat.CanAttack())
            combat.Attack();
    }
}