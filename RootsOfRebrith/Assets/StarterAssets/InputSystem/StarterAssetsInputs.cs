using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

		[Header("Farming/Gameplay Actions (New)")]
		public bool attack;
		public bool interact;
		public bool uiCancel;
		public bool pause;

		// one-frame flags (pressed-this-frame)
		private bool _attackPressed, _interactPressed, _uiCancelPressed, _pausePressed, _jumpPressed;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;
		
		[Header("Camera Preset")]
		public bool cameraPresetNext;
		

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)       => MoveInput(value.Get<Vector2>());

		public void OnLook(InputValue value)
		{
			if (cursorInputForLook)
				LookInput(value.Get<Vector2>());
		}

		public void OnJump(InputValue value)
		{
			bool pressed = value.isPressed;
			JumpInput(pressed);
			if (pressed) _jumpPressed = true;
		}

		public void OnSprint(InputValue value)     => SprintInput(value.isPressed);

		// ---- New Actions: Attack / Interact / UI-Cancel / Pause
		public void OnAttack(InputValue value)
		{
			bool pressed = value.isPressed;
			attack = pressed;
			if (pressed) _attackPressed = true;
		}

		public void OnInteract(InputValue value)
		{
			bool pressed = value.isPressed;
			interact = pressed;
			if (pressed) _interactPressed = true;
		}
		
		public void OnCameraPreset(InputValue value)
		{
			if (value.isPressed)
				cameraPresetNext = true;
		}

		public void OnUICancel(InputValue value)
		{
			bool pressed = value.isPressed;
			uiCancel = pressed;
			if (pressed) _uiCancelPressed = true;
		}

		public void OnPause(InputValue value)
		{
			bool pressed = value.isPressed;
			pause = pressed;
			if (pressed) _pausePressed = true;
		}
		
		
#endif

		public void MoveInput(Vector2 newMoveDirection) => move = newMoveDirection;
		public void LookInput(Vector2 newLookDirection) => look = newLookDirection;
		public void JumpInput(bool newJumpState)        => jump = newJumpState;
		public void SprintInput(bool newSprintState)    => sprint = newSprintState;

		// ---- Consume-APIs (exact „was pressed this frame“)
		public bool ConsumeAttackPressed()   { var b = _attackPressed;   _attackPressed   = false; return b; }
		public bool ConsumeInteractPressed() { var b = _interactPressed; _interactPressed = false; return b; }
		public bool ConsumeUICancelPressed() { var b = _uiCancelPressed; _uiCancelPressed = false; return b; }
		public bool ConsumePausePressed()    { var b = _pausePressed;    _pausePressed    = false; return b; }
		public bool ConsumeJumpPressed()     { var b = _jumpPressed;     _jumpPressed     = false; return b; }

		private void OnApplicationFocus(bool hasFocus) => SetCursorState(cursorLocked);

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
}
