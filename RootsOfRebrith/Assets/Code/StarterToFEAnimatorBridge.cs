using UnityEngine;
using StarterAssets;
using FarmingEngine;
using UnityEngine.InputSystem;


/// <summary>
/// Brücke zwischen Starter Assets (Bewegung/Input) und Farming Engine (Zustände/Events),
/// ersetzt PlayerCharacterAnim: setzt alle Animator-Parameter und reagiert auf FE-Events.
/// </summary>
[DefaultExecutionOrder(100)] // nach TPC updaten
public class StarterToFEAnimatorBridge : MonoBehaviour
{
    [Header("Refs (auto)")]
    public Animator animator;                 // Animator am Character
    public ThirdPersonController tpc;         // Starter Assets TPC
    public StarterAssetsInputs inputs;        // Starter Assets Inputs
    public PlayerCharacter character;         // Farming Engine PlayerCharacter

    [Header("Optional FE-Module (auto)")]
    public PlayerCharacterCombat combat;
    public PlayerCharacterCraft crafting;
    public PlayerCharacterRide ride;
    public PlayerCharacterSwim swim;

    [Header("Animator Parameter (wie in PlayerCharacterAnim)")]
    public string move_anim   = "Move";
    public string move_side_x = "MoveX";
    public string move_side_z = "MoveZ";

    public string attack_anim  = "Attack";
    public string attack_speed = "AttackSpeed";

    public string take_anim   = "Take";
    public string craft_anim  = "Craft";
    public string build_anim  = "Build";
    public string use_anim    = "Use";       // (FE nutzt aktuell kein Event dafür)
    public string damaged_anim = "Damaged";
    public string death_anim   = "Death";
    public string sleep_anim   = "Sleep";
    public string fish_anim    = "Fish";
    public string dig_anim     = "Dig";
    public string water_anim   = "Water";
    public string hoe_anim     = "Hoe";
    public string ride_anim    = "Ride";
    public string swim_anim    = "Swim";
    public string climb_anim   = "Climb";

    [Header("Tuning")]
    [Tooltip("Ab wann Move=true gesetzt wird (Stick/WASD Magnitude).")]
    public float moveDeadzone = 0.1f;
    [Tooltip("Glättung von MoveX/MoveZ in Sekunden.")]
    public float dampTime = 0.08f;
    [Tooltip("Kamera-relative Eingaben in Welt-Richtung umrechnen.")]
    public bool useCameraRelative = true;

    private Camera _cam;
    private float _xVel, _zVel; // SmoothDamp-Helper

    void Reset()
    {
        animator  = GetComponentInChildren<Animator>();
        tpc       = GetComponent<ThirdPersonController>();
        inputs    = GetComponent<StarterAssetsInputs>();
        character = GetComponent<PlayerCharacter>();

        combat    = GetComponent<PlayerCharacterCombat>();
        crafting  = GetComponent<PlayerCharacterCraft>();
        ride      = GetComponent<PlayerCharacterRide>();
        swim      = GetComponent<PlayerCharacterSwim>();
    }

    void Awake()
    {
        if (!animator)  animator  = GetComponentInChildren<Animator>();
        if (!tpc)       tpc       = GetComponent<ThirdPersonController>();
        if (!inputs)    inputs    = GetComponent<StarterAssetsInputs>();
        if (!character) character = GetComponent<PlayerCharacter>();

        if (!combat)    combat    = GetComponent<PlayerCharacterCombat>();
        if (!crafting)  crafting  = GetComponent<PlayerCharacterCraft>();
        if (!ride)      ride      = GetComponent<PlayerCharacterRide>();
        if (!swim)      swim      = GetComponent<PlayerCharacterSwim>();

        _cam = Camera.main;
    }

    void OnEnable()
    {
        // FE-Events abonnieren (entspricht PlayerCharacterAnim.Start)
        if (character != null)
        {
            if (character.Inventory != null)
            {
                character.Inventory.onTakeItem += OnTakeItem;
                character.Inventory.onDropItem += OnDropItem;
            }
            if (crafting != null)
            {
                crafting.onCraft += OnCraft;
                crafting.onBuild += OnBuild;
            }
            if (combat != null)
            {
                combat.onAttack   += OnFEAttack;
                combat.onAttackHit += OnAttackHit;
                combat.onDamaged  += OnDamaged;
                combat.onDeath    += OnDeath;
            }

            character.onTriggerAnim += OnTriggerAnim;

            if (character.Jumping)
                character.Jumping.onJump += OnJump;
        }
    }

    void OnDisable()
    {
        // Events sauber abmelden
        if (character != null)
        {
            if (character.Inventory != null)
            {
                character.Inventory.onTakeItem -= OnTakeItem;
                character.Inventory.onDropItem -= OnDropItem;
            }
            if (crafting != null)
            {
                crafting.onCraft -= OnCraft;
                crafting.onBuild -= OnBuild;
            }
            if (combat != null)
            {
                combat.onAttack    -= OnFEAttack;
                combat.onAttackHit -= OnAttackHit;
                combat.onDamaged   -= OnDamaged;
                combat.onDeath     -= OnDeath;
            }

            character.onTriggerAnim -= OnTriggerAnim;

            if (character.Jumping)
                character.Jumping.onJump -= OnJump;
        }
    }

    void Update()
    {
        if (!animator || !tpc || !inputs || character == null)
            return;

        // --- Pausenlogik wie in PlayerCharacterAnim ---
        bool player_paused   = TheGame.Get().IsPausedByPlayer();
        bool gameplay_paused = TheGame.Get().IsPausedByScript();
        animator.enabled = !player_paused;
        if (!animator.enabled) return;

        // --- Bewegungsparameter (ersetzt Move/Side-Logik aus PlayerCharacterAnim) ---
        // Quelle: StarterAssetsInputs / Kamera
        Vector2 mv = inputs.move; // (x: rechts/links, y: vor/zurück)
        Vector3 worldMove = new Vector3(mv.x, 0f, mv.y);

        if (useCameraRelative && _cam != null)
        {
            Vector3 fwd = _cam.transform.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = _cam.transform.right; right.y = 0f; right.Normalize();
            worldMove = right * mv.x + fwd * mv.y;
        }

        // Move Bool: entweder Eingabe über Schwelle ODER echte Geschwindigkeit (falls z.B. Ausgleiten)
        float inputMag = mv.magnitude;
        bool isMoving = (!gameplay_paused && (inputMag > moveDeadzone || GetHorizontalSpeed() > 0.05f));

        SetAnimBool(move_anim, isMoving);

        // MoveX/MoveZ relativ zur Blickrichtung der Figur (wie in PlayerCharacterAnim)
        // Wir nehmen die Richtung relativ zu character forward:
        Vector3 moveNorm = worldMove.sqrMagnitude > 0.0001f ? worldMove.normalized : Vector3.zero;
        float mangle = (moveNorm == Vector3.zero) ? 0f : Vector3.SignedAngle(character.GetFacing(), moveNorm, Vector3.up);
        Vector3 side = new Vector3(Mathf.Sin(mangle * Mathf.Deg2Rad), 0f, Mathf.Cos(mangle * Mathf.Deg2Rad)) * Mathf.Clamp01(inputMag);

        // Glätten:
        float targetX = side.x;
        float targetZ = side.z;
        float curX = animator.GetFloat(move_side_x);
        float curZ = animator.GetFloat(move_side_z);
        float newX = Mathf.SmoothDamp(curX, targetX, ref _xVel, dampTime);
        float newZ = Mathf.SmoothDamp(curZ, targetZ, ref _zVel, dampTime);
        SetAnimFloat(move_side_x, newX);
        SetAnimFloat(move_side_z, newZ);

        // --- Zustandsbooleans wie in PlayerCharacterAnim ---
        SetAnimBool(craft_anim, !gameplay_paused && character.Crafting != null && character.Crafting.IsCrafting());
        SetAnimBool(sleep_anim, character.IsSleeping());
        SetAnimBool(fish_anim,  character.IsFishing());
        SetAnimBool(ride_anim,  ride != null && ride.IsRiding());
        SetAnimBool(swim_anim,  swim != null && swim.IsSwimming());
        SetAnimBool(climb_anim, character.IsClimbing());
        // (dig_anim, water_anim, hoe_anim werden über Trigger via character.TriggerAnim gesetzt)
    }

    // -------- Animator Hilfen --------
    private void SetAnimBool(string id, bool value)
    {
        if (!string.IsNullOrEmpty(id))
            animator.SetBool(id, value);
    }
    private void SetAnimFloat(string id, float value)
    {
        if (!string.IsNullOrEmpty(id))
            animator.SetFloat(id, value);
    }
    private void SetAnimTrigger(string id)
    {
        if (!string.IsNullOrEmpty(id))
            animator.SetTrigger(id);
    }

    private float GetHorizontalSpeed()
    {
        // Robust: wenn der TPC den CharacterController nutzt, nimm dessen Velocity;
        // alternativ grob aus Transform-Bewegung (nicht ideal, aber failsafe).
        var cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            Vector3 v = cc.velocity; v.y = 0f;
            return v.magnitude;
        }
        return 0f;
    }

    // -------- FE-Event-Callbacks (entspricht PlayerCharacterAnim) --------
    private void OnTakeItem(Item item)
    {
        SetAnimTrigger(take_anim);
    }

    private void OnDropItem(Item item)
    {
        // kein Standard-Trigger in PlayerCharacterAnim
    }

    private void OnCraft(CraftData cdata)
    {
        // PlayerCharacterAnim hatte hier keinen Trigger – optional könntest du craft_anim triggern.
        // Wir lassen es wie im Original leer, Craft-Bool läuft über Update.
    }

    private void OnBuild(Buildable buildable)
    {
        SetAnimTrigger(build_anim);
    }

    private void OnJump()
    {
        // PlayerCharacterAnim: "Add jump animation here" – leer gelassen
    }

    private void OnDamaged()
    {
        SetAnimTrigger(damaged_anim);
    }

    private void OnDeath()
    {
        SetAnimTrigger(death_anim);
    }

    private void OnFEAttack(Destructible target, bool ranged)
    {
        // Gleiche Logik wie PlayerCharacterAnim: Attack-Speed setzen + evtl. Ausrüstungs-Override für Anim-Namen
        string anim = attack_anim;
        float anim_speed = combat != null ? combat.GetAttackAnimSpeed() : 1f;

        // Override über EquipItem (wie im Original)
        EquipItem equip = character.Inventory != null ? character.Inventory.GetEquippedWeaponMesh() : null;
        if (equip != null)
        {
            if (!ranged && !string.IsNullOrEmpty(equip.attack_melee_anim))
                anim = equip.attack_melee_anim;
            if (ranged && !string.IsNullOrEmpty(equip.attack_ranged_anim))
                anim = equip.attack_ranged_anim;
        }

        SetAnimFloat(attack_speed, anim_speed);
        SetAnimTrigger(anim);
    }

    private void OnAttackHit(Destructible target)
    {
        // PlayerCharacterAnim: leer
    }

    private void OnTriggerAnim(string anim, float duration)
    {
        // Wird z.B. von Hoe, Dig, Water, Use usw. genutzt:
        // PlayerCharacterHoe: character.TriggerAnim(character.Animation ? character.Animation.hoe_anim : "", pos);
        SetAnimTrigger(anim);
    }
}
