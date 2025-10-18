using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    public enum FreelookMode
    {
        Hold = 0,
        Toggle = 10,
        Always = 20,
        Never = 30,
    }

    /// <summary>
    /// Adapter/Fassade für die alte FarmingEngine-Kamera.
    /// - Lässt bestehenden Code weiterhin über TheCamera.cs arbeiten.
    /// - Wenn move_enabled = false, bewegt NICHT die echte Kamera-Transform;
    ///   stattdessen werden nur Target-/Facing-Werte aktualisiert, sodass
    ///   Cinemachine/StarterAssets die eigentliche Kamerabewegung übernehmen können.
    /// </summary>
    public class TheCamera : MonoBehaviour
    {
        private static TheCamera _instance;

        [Header("Follow")]
        public GameObject follow_target;              // Wird automatisch gesucht, falls leer
        public Vector3 follow_offset = new Vector3(0f, 3f, -5f);
        public Vector3 custom_offset = Vector3.zero;

        [Header("Movement")]
        public bool move_enabled = false;             // Wenn false: Kamera NICHT selbst bewegen (Cinemachine übernimmt)
        public float rotate_speed = 120f;
        public float zoom_speed = 2f;
        public float zoom_in_max = 0f;
        public float zoom_out_max = 1f;

        [Header("Smoothing")]
        public bool smooth_camera = false;
        public float smooth_speed = 10f;
        public float smooth_rotate_speed = 90f;

        [Header("Mobile Only")]
        public float rotate_speed_touch = 10f;
        public float zoom_speed_touch = 1f;

        [Header("Third Person Only")]
        public FreelookMode freelook_mode = FreelookMode.Never;
        public float freelook_speed_x = 150f;
        public float freelook_speed_y = 150f;
        public float freelook_max_up = 0.8f;     // clamp anhand forward.y
        public float freelook_max_down = 0.8f;

        // Runtime
        private Camera cam;
        private Transform target_transform;      // Virtuelles Ziel (wird immer aktualisiert)
        private Transform cam_target_transform;  // Kind von target_transform für Offset/Zoom
        private Vector3 current_vel = Vector3.zero;
        private float current_zoom = 0f;
        private bool is_locked = false;          // Cursor Lock

        // Shake
        private float shake_timer = 0f;
        private float shake_intensity = 1f;
        private Vector3 shake_vector;

        // Input Cache
        private float add_rotate = 0f;

        protected virtual void Awake()
        {
            _instance = this;

            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;

            // Hilfsobjekte für Target/Offset
            GameObject camTarget = new GameObject("CameraTarget");
            target_transform = camTarget.transform;
            target_transform.position = transform.position - follow_offset;

            GameObject camTargetCam = new GameObject("CameraTargetCam");
            cam_target_transform = camTargetCam.transform;
            cam_target_transform.SetParent(target_transform);
            cam_target_transform.localPosition = follow_offset;
            cam_target_transform.localRotation = transform.localRotation;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        protected virtual void LateUpdate()
        {
            // Ziel suchen, wenn leer
            if (follow_target == null)
            {
                // 1) FarmingEngine PlayerCharacter
                var player = PlayerCharacter.GetFirst();
                if (player != null)
                    follow_target = player.gameObject;

                // 2) StarterAssets ThirdPersonController
                if (follow_target == null)
                {
                    var tpc = FindAnyObjectByType<StarterAssets.ThirdPersonController>();
                    if (tpc != null) follow_target = tpc.gameObject;
                }

                if (follow_target == null)
                    return;
            }

            // Optional eigene Input-Lese-Logik, nur wenn TheCamera die Kamera steuert
            if (move_enabled)
                UpdateControls();

            // Target aktualisieren (immer), aber Kamera nur bewegen, wenn move_enabled
            bool free = IsFreelook();
            if (free)
            {
                if (move_enabled) UpdateFreeCamera_MoveAndTarget();
                else UpdateFreeCamera_TargetOnly();
            }
            else
            {
                if (move_enabled) UpdateCamera_MoveAndTarget();
                else UpdateCamera_TargetOnly();
            }

            // UI-Entriegelung
            if (is_locked && TheUI.Get() && TheUI.Get().IsBlockingPanelOpened())
                ToggleLock();

            // Shake nur anwenden, wenn wir die Kamera selbst bewegen
            if (shake_timer > 0f && move_enabled)
            {
                shake_timer -= Time.deltaTime;
                shake_vector = new Vector3(Mathf.Cos(shake_timer * Mathf.PI * 8f) * 0.02f, Mathf.Sin(shake_timer * Mathf.PI * 7f) * 0.02f, 0f);
                transform.position += shake_vector * shake_intensity;
            }
        }

        // -------- Target-Update (keine echte Kamerabewegung) --------

        protected void UpdateCamera_TargetOnly()
        {
            float rotY = target_transform.rotation.eulerAngles.y + add_rotate * Time.deltaTime;
            Quaternion targ_rot = Quaternion.Euler(target_transform.rotation.eulerAngles.x, rotY, 0f);

            if (smooth_camera)
            {
                target_transform.position = Vector3.SmoothDamp(target_transform.position, follow_target.transform.position, ref current_vel, 1f / Mathf.Max(0.0001f, smooth_speed));
                target_transform.rotation = Quaternion.Slerp(target_transform.rotation, targ_rot, smooth_rotate_speed * Time.deltaTime);
            }
            else
            {
                target_transform.position = follow_target.transform.position;
                target_transform.rotation = targ_rot;
            }

            Vector3 targ_zoom = (follow_offset + custom_offset) * (1f - current_zoom);
            cam_target_transform.localPosition = Vector3.Lerp(cam_target_transform.localPosition, targ_zoom, 10f * Time.deltaTime);
        }

        protected void UpdateFreeCamera_TargetOnly()
        {
            // Maus/Gamepad Delta nur verwenden, wenn gelocked oder Gamepad
            Vector2 mouse_delta = Vector2.zero;
            var controls = PlayerControls.Get();
            var mouse = PlayerControlsMouse.Get();
            if (mouse != null && is_locked) mouse_delta += mouse.GetMouseDelta();
            if (controls != null && controls.IsGamePad()) mouse_delta += controls.GetFreelook();

            Quaternion rot_backup = target_transform.rotation;
            Quaternion targ_rot = target_transform.rotation;
            targ_rot = Quaternion.AngleAxis(freelook_speed_y * -mouse_delta.y * 0.5f, target_transform.right) * targ_rot;
            targ_rot = Quaternion.Euler(0f, freelook_speed_x * mouse_delta.x, 0) * targ_rot;
            targ_rot.eulerAngles = new Vector3(targ_rot.eulerAngles.x, targ_rot.eulerAngles.y, 0f);

            if (smooth_camera)
            {
                target_transform.position = Vector3.SmoothDamp(target_transform.position, follow_target.transform.position, ref current_vel, 1f / Mathf.Max(0.0001f, smooth_speed));
                target_transform.rotation = Quaternion.Slerp(target_transform.rotation, targ_rot, smooth_rotate_speed * Time.deltaTime);
            }
            else
            {
                target_transform.position = follow_target.transform.position;
                target_transform.rotation = targ_rot;
            }

            Vector3 targ_zoom = (follow_offset + custom_offset) * (1f - current_zoom);
            cam_target_transform.localPosition = Vector3.Lerp(cam_target_transform.localPosition, targ_zoom, 10f * Time.deltaTime);

            if (cam_target_transform.forward.y > freelook_max_up || cam_target_transform.forward.y < -freelook_max_down)
                target_transform.rotation = rot_backup;
        }

        // -------- Volle Bewegung (legacy), falls move_enabled = true --------

        protected void UpdateCamera_MoveAndTarget()
        {
            UpdateCamera_TargetOnly(); // Target updaten
            transform.position = cam_target_transform.position;
            transform.rotation = cam_target_transform.rotation;
        }

        protected void UpdateFreeCamera_MoveAndTarget()
        {
            UpdateFreeCamera_TargetOnly(); // Target updaten
            transform.position = cam_target_transform.position;
            transform.rotation = cam_target_transform.rotation;
        }

        protected virtual void UpdateControls()
        {
            // Sehr konservative Default-Implementierung (angepasst an das alte Verhalten)
            // Scroll = Zoom
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                current_zoom = Mathf.Clamp01(current_zoom + (-scroll) * zoom_speed * 0.05f);
                current_zoom = Mathf.Clamp(current_zoom, zoom_in_max, zoom_out_max);
            }

            // Rechte Maustaste halten = Lock & drehen
            if (Input.GetMouseButtonDown(1))
                SetLockMode(true);
            if (Input.GetMouseButtonUp(1) && freelook_mode != FreelookMode.Always)
                SetLockMode(false);

            // Tastatur Q/E als Beispielrotation (falls gewünscht)
            add_rotate = 0f;
            if (Input.GetKey(KeyCode.Q)) add_rotate -= rotate_speed;
            if (Input.GetKey(KeyCode.E)) add_rotate += rotate_speed;
        }

        public bool IsFreelook()
        {
            if (freelook_mode == FreelookMode.Always) return true;
            if (freelook_mode == FreelookMode.Never) return false;
            if (freelook_mode == FreelookMode.Hold) return is_locked;
            if (freelook_mode == FreelookMode.Toggle) return is_locked;
            return false;
        }

        public virtual void ToggleLock()
        {
            SetLockMode(!is_locked);
        }

        public virtual void SetLockMode(bool locked)
        {
            // Wenn wir die Kamera nicht selber bewegen, Maus-Cursor in Ruhe lassen
            if (!move_enabled)
            {
                is_locked = false;
                return;
            }

            if (is_locked != locked)
            {
                is_locked = locked;
                Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !locked;
            }
        }

        public void Shake(float duration, float intensity = 1f)
        {
            shake_timer = duration;
            shake_intensity = intensity;
        }

        // ---------- Public API, von anderen Skripten genutzt ----------

        public Vector3 GetTargetPos()
        {
            return target_transform != null ? target_transform.position : transform.position;
        }

        /// <summary>
        /// Erwartet von TheRender.cs – liefert TargetPos plus Blickrichtungs-Offset.
        /// </summary>
        public Vector3 GetTargetPosOffsetFace(float offset)
        {
            Vector3 forward = GetFacingRotation() * Vector3.forward;
            forward.y = 0f;
            return GetTargetPos() + forward.normalized * offset;
        }

        /// <summary>
        /// Erwartet von TheGame.cs – „springt“ die Kamera an ein Ziel (für Scene-Start etc.).
        /// Im Adapter-Modus (move_enabled=false) wird nur das Target versetzt.
        /// </summary>
        public void MoveToTarget(Vector3 pos)
        {
            if (target_transform != null)
                target_transform.position = pos;

            if (move_enabled)
            {
                // Wenn wir die Kamera selbst steuern, auch die echte Kamera bewegen
                transform.position = pos + (follow_offset + custom_offset) * (1f - current_zoom);
            }
            // Wenn Cinemachine steuert: kein direktes Kamerabewegen nötig
        }

        public Quaternion GetFacingRotation()
        {
            // Nimmt die tatsächliche Kamera-Forward (Main Camera), damit Movement relativ zur echten Blickrichtung bleibt
            Camera c = GetCam();
            if (c != null)
            {
                Vector3 fwd = c.transform.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
                return Quaternion.LookRotation(fwd.normalized, Vector3.up);
            }
            // Fallback
            Vector3 ff = transform.forward; ff.y = 0f;
            if (ff.sqrMagnitude < 0.0001f) ff = Vector3.forward;
            return Quaternion.LookRotation(ff.normalized, Vector3.up);
        }

        public Camera GetCam()
        {
            if (cam == null) cam = Camera.main;
            return cam;
        }

        public static Camera GetCamera()
        {
            return _instance != null ? _instance.GetCam() : Camera.main;
        }

        /// <summary>
        /// Erwartet von PlayerControlsMouse – prüft, ob Screen-Koordinate im Kamera-PixelRect liegt.
        /// </summary>
        public bool IsInside(Vector2 screenPos)
        {
            Camera c = GetCam();
            if (c == null) return true; // failsafe
            return c.pixelRect.Contains(screenPos);
        }

        public static TheCamera Get()
        {
            return _instance;
        }
        
        // --- Back-compat helpers for Farming Engine ---

        /// <summary>Quaternion-Rotation relativ zur aktuellen Kamera-Ausrichtung.</summary>
        public Quaternion GetRotation()
        {
            return GetFacingRotation(); // nutzt echte Kamera (Camera.main)
        }

        /// <summary>Vorwärts-Vektor (y=0) in Blickrichtung – für z.B. LookRotation in FX.</summary>
        public Vector3 GetFacingFront()
        {
            var q = GetFacingRotation();
            Vector3 f = q * Vector3.forward;
            f.y = 0f;
            return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
        }

        /// <summary>Rohes Kamera-Forward (nicht abgeflacht), z.B. für Aim/Raycast.</summary>
        public Vector3 GetFacingDir()
        {
            Camera c = GetCam();
            Vector3 f = c != null ? c.transform.forward : transform.forward;
            return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
        }

        /// <summary>Alias für altes API. Entspricht unserem IsFreelook().</summary>
        public bool IsFreeRotation()
        {
            return IsFreelook();
        }
        
        // Wird von Schwimm-/Kletter-Scripten genutzt, um die Kameraposition leicht zu verschieben
        public void SetOffset(Vector3 offset)
        {
            custom_offset = offset;
        }

        /// <summary>Parameterloses Shake für altes API.</summary>
        public void Shake()
        {
            Shake(0.1f, 1f); // kurzer Standard-Impuls
        }

    }
}
