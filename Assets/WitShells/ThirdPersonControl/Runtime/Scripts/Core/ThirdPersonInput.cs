namespace WitShells.ThirdPersonControl
{
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.Events;

    /// <summary>
    /// Handles player input using Unity's New Input System PlayerInput component.
    /// Requires PlayerInput component with "Move", "Look", "Jump", "Sprint", and "Crouch" actions.
    /// </summary>
    [AddComponentMenu("WitShells/Third Person Control/Third Person Input")]
    [RequireComponent(typeof(PlayerInput))]
    public class ThirdPersonInput : MonoBehaviour
    {
        #region Settings

        [Header("Input Settings")]
        [Tooltip("Whether to use analog movement values or normalize them")]
        [SerializeField] private bool analogMovement = true;

        [Tooltip("Invert the Y axis for look input")]
        [SerializeField] private bool invertLookY = false;

        [Tooltip("Invert the X axis for look input")]
        [SerializeField] private bool invertLookX = false;

        [Tooltip("Deadzone for movement input")]
        [Range(0f, 0.5f)]
        [SerializeField] private float movementDeadzone = 0.1f;

        [Tooltip("Deadzone for look input")]
        [Range(0f, 0.5f)]
        [SerializeField] private float lookDeadzone = 0.01f;

        [Header("Cursor Settings")]
        [Tooltip("Lock and hide cursor on start")]
        [SerializeField] private bool lockCursor = true;

        #endregion

        #region Events

        [Header("Events")]
        public UnityEvent<Vector2> OnMoveInput;
        public UnityEvent<Vector2> OnLookInput;
        public UnityEvent OnJumpInput;
        public UnityEvent<bool> OnSprintInput;
        public UnityEvent<bool> OnCrouchInput;

        #endregion

        #region Input State

        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _jumpInput;
        private bool _sprintInput;
        private bool _crouchInput;

        /// <summary>Current movement input vector.</summary>
        public Vector2 MoveInput => _moveInput;

        /// <summary>Current look/camera input vector.</summary>
        public Vector2 LookInput => _lookInput;

        /// <summary>True when jump was pressed this frame.</summary>
        public bool JumpInput => _jumpInput;

        /// <summary>True while sprint is held.</summary>
        public bool SprintInput => _sprintInput;

        /// <summary>True while crouch is held.</summary>
        public bool CrouchInput => _crouchInput;

        #endregion

#if UNITY_EDITOR
        #region Debug (Editor Only)

        [Space(10)]
        [Header("Debug Input (Read Only)")]
        [SerializeField] private Vector2 _debugMoveInput;
        [SerializeField] private Vector2 _debugLookInput;
        [SerializeField] private bool _debugJumpInput;
        [SerializeField] private bool _debugSprintInput;
        [SerializeField] private bool _debugCrouchInput;

        #endregion
#endif

        #region References

        [Header("Target")]
        [Tooltip("The ThirdPersonControl component to send input to")]
        [SerializeField] private ThirdPersonControl targetController;

        [Tooltip("The CinemachineCamLookInput component to send look input to")]
        [SerializeField] private CinemachineCamLookInput cameraController;

        private PlayerInput _playerInput;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
        }

        private void OnEnable()
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Update()
        {
            ProcessInput();
            ApplyInputToTargets();

            // Reset one-shot inputs
            _jumpInput = false;
        }

        #endregion

        #region Input Processing

        private void ProcessInput()
        {
            if (_playerInput == null) return;

            // Move input
            Vector2 rawMove = _playerInput.actions["Move"].ReadValue<Vector2>();
            _moveInput = ApplyDeadzone(rawMove, movementDeadzone);
            if (!analogMovement && _moveInput.sqrMagnitude > 0)
            {
                _moveInput = _moveInput.normalized;
            }

            // Look input
            Vector2 rawLook = _playerInput.actions["Look"].ReadValue<Vector2>();
            _lookInput = ApplyDeadzone(rawLook, lookDeadzone);
            if (invertLookX) _lookInput.x *= -1f;
            if (invertLookY) _lookInput.y *= -1f;
        }

        private void ApplyInputToTargets()
        {
#if UNITY_EDITOR
            // Update debug values in editor
            _debugMoveInput = _moveInput;
            _debugLookInput = _lookInput;
            _debugJumpInput = _jumpInput;
            _debugSprintInput = _sprintInput;
            _debugCrouchInput = _crouchInput;
#endif

            // Apply to ThirdPersonControl
            if (targetController != null)
            {
                targetController.Direction = _moveInput;
                targetController.Sprint = _sprintInput;
                targetController.Crouch = _crouchInput;

                if (_jumpInput)
                {
                    targetController.Jump = true;
                }
            }

            // Apply to camera controller
            if (cameraController != null)
            {
                cameraController.LookInput = _lookInput;
            }

            // Invoke events
            OnMoveInput?.Invoke(_moveInput);
            OnLookInput?.Invoke(_lookInput);
        }

        private Vector2 ApplyDeadzone(Vector2 input, float deadzone)
        {
            return input.magnitude < deadzone ? Vector2.zero : input;
        }

        #endregion

        #region PlayerInput Callbacks (Assign in PlayerInput Events or use SendMessages)

        /// <summary>Called by PlayerInput when Jump action is performed.</summary>
        public void OnJump(InputValue value)
        {
            _jumpInput = value.isPressed;
            if (_jumpInput) OnJumpInput?.Invoke();
        }

        /// <summary>Called by PlayerInput when Sprint action is performed.</summary>
        public void OnSprint(InputValue value)
        {
            _sprintInput = value.isPressed;
            OnSprintInput?.Invoke(_sprintInput);
        }

        /// <summary>Called by PlayerInput when Crouch action is performed.</summary>
        public void OnCrouch(InputValue value)
        {
            _crouchInput = value.isPressed;
            OnCrouchInput?.Invoke(_crouchInput);
        }

        #endregion

        #region Public Methods

        /// <summary>Register a ThirdPersonControl component to receive input.</summary>
        public void RegisterController(ThirdPersonControl controller)
        {
            targetController = controller;
        }

        /// <summary>Unregister the current ThirdPersonControl component.</summary>
        public void UnregisterController()
        {
            targetController = null;
        }

        /// <summary>Register a CinemachineCamLookInput component to receive look input.</summary>
        public void RegisterCameraController(CinemachineCamLookInput camera)
        {
            cameraController = camera;
        }

        /// <summary>Unregister the current camera controller.</summary>
        public void UnregisterCameraController()
        {
            cameraController = null;
        }

        /// <summary>Locks or unlocks the cursor.</summary>
        public void SetCursorLock(bool locked)
        {
            lockCursor = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        /// <summary>Toggle cursor lock state.</summary>
        public void ToggleCursorLock()
        {
            SetCursorLock(!lockCursor);
        }

        #endregion
    }
}
