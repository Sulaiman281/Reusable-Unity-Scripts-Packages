namespace WitShells.ThirdPersonControl
{
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    /// Third-person character controller with customizable settings via ScriptableObject.
    /// Can receive input from ThirdPersonInput or be controlled directly via properties.
    /// </summary>
    [AddComponentMenu("WitShells/Third Person Control/Third Person Controller")]
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonControl : MonoBehaviour
    {
        #region Input State

        [Header("Input State (Read-Only in Inspector)")]
        [SerializeField] private Vector2 direction;
        [SerializeField] private bool jump;
        [SerializeField] private bool crouch;
        [SerializeField] private bool sprint;

        #endregion

        #region Settings

        [Header("Settings")]
        [Tooltip("ScriptableObject containing all controller settings. If null, uses defaults.")]
        [SerializeField] private ThirdPersonSettings settings;

        [Header("Settings Override (Used if Settings is null)")]
        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private float runSpeed = 5f;
        [SerializeField] private float crouchSpeed = 1f;
        [SerializeField] private float jumpForce = 3.5f;
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask groundLayers = 1;
        [Range(0.01f, 1f)]
        [SerializeField] private float rotationSmoothness = 0.15f;

        #endregion

        #region References

        [Header("References")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform cameraTarget;

        #endregion

        #region Events

        [Header("Events")]
        public UnityEvent OnJump;
        public UnityEvent OnLand;
        public UnityEvent<bool> OnGroundedChanged;
        public UnityEvent<float> OnSpeedChanged;

        #endregion

        #region Private State

        private bool _isGrounded;
        private bool _wasGrounded;
        private float _verticalVelocity;
        private float _jumpTimeoutDelta;
        private float _currentSpeed;

        #endregion

        #region Properties - Settings Access

        /// <summary>
        /// Gets the effective walk speed from settings or override.
        /// </summary>
        public float WalkSpeed => settings != null ? settings.WalkSpeed : walkSpeed;

        /// <summary>
        /// Gets the effective run speed from settings or override.
        /// </summary>
        public float RunSpeed => settings != null ? settings.RunSpeed : runSpeed;

        /// <summary>
        /// Gets the effective crouch speed from settings or override.
        /// </summary>
        public float CrouchSpeed => settings != null ? settings.CrouchSpeed : crouchSpeed;

        /// <summary>
        /// Gets the effective jump force from settings or override.
        /// </summary>
        public float JumpForce => settings != null ? settings.JumpForce : jumpForce;

        /// <summary>
        /// Gets the effective ground check distance from settings or override.
        /// </summary>
        public float GroundCheckDistance => settings != null ? settings.GroundCheckDistance : groundCheckDistance;

        /// <summary>
        /// Gets the effective ground layers from settings or override.
        /// </summary>
        public LayerMask GroundLayers => settings != null ? settings.GroundLayers : groundLayers;

        /// <summary>
        /// Gets the effective rotation smoothness from settings or override.
        /// </summary>
        public float RotationSmoothness => settings != null ? settings.RotationSmoothness : rotationSmoothness;

        /// <summary>
        /// Gets the effective terminal velocity from settings or default.
        /// </summary>
        public float TerminalVelocity => settings != null ? settings.TerminalVelocity : 53f;

        /// <summary>
        /// Gets the effective jump timeout from settings or default.
        /// </summary>
        public float JumpTimeout => settings != null ? settings.JumpTimeout : 0.2f;

        /// <summary>
        /// Gets the effective grounded gravity from settings or default.
        /// </summary>
        public float GroundedGravity => settings != null ? settings.GroundedGravity : -2f;

        #endregion

        #region Properties - Component Access

        /// <summary>
        /// The CharacterController component (auto-cached).
        /// </summary>
        public CharacterController CharacterController
        {
            get
            {
                if (characterController == null)
                {
                    characterController = GetComponent<CharacterController>();
                }
                return characterController;
            }
        }

        /// <summary>
        /// The Animator component (auto-cached from self or children).
        /// </summary>
        public Animator Animator
        {
            get
            {
                if (animator == null)
                {
                    animator = GetComponent<Animator>();
                    if (animator == null)
                    {
                        animator = gameObject.GetComponentInChildren<Animator>();
                    }
                }
                return animator;
            }
        }

        /// <summary>
        /// The settings ScriptableObject. Can be changed at runtime.
        /// </summary>
        public ThirdPersonSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        #endregion

        #region Properties - Input Access

        /// <summary>
        /// Movement direction input (X = horizontal, Y = vertical).
        /// </summary>
        public Vector2 Direction
        {
            get => direction;
            set => direction = value;
        }

        /// <summary>
        /// Jump input. Set to true to trigger a jump.
        /// </summary>
        public bool Jump
        {
            get => jump;
            set => jump = value;
        }

        /// <summary>
        /// Crouch input state.
        /// </summary>
        public bool Crouch
        {
            get => crouch;
            set => crouch = value;
        }

        /// <summary>
        /// Sprint input state.
        /// </summary>
        public bool Sprint
        {
            get => sprint;
            set => sprint = value;
        }

        /// <summary>
        /// Camera target transform for movement direction reference.
        /// </summary>
        public Transform CameraTarget
        {
            get => cameraTarget;
            set => cameraTarget = value;
        }

        #endregion

        #region Properties - State Access

        /// <summary>
        /// Whether the character is currently grounded.
        /// </summary>
        public bool IsGrounded => _isGrounded;

        /// <summary>
        /// Current movement speed.
        /// </summary>
        public float CurrentSpeed => _currentSpeed;

        /// <summary>
        /// Current vertical velocity.
        /// </summary>
        public float VerticalVelocity => _verticalVelocity;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _jumpTimeoutDelta = JumpTimeout;
        }

        private void FixedUpdate()
        {
            UpdateMovement();
            
            // Check for landing
            if (_isGrounded && !_wasGrounded)
            {
                OnLand?.Invoke();
            }
            
            // Track grounded state changes
            if (_isGrounded != _wasGrounded)
            {
                OnGroundedChanged?.Invoke(_isGrounded);
            }
            
            _wasGrounded = _isGrounded;
        }

        #endregion

        #region Movement Logic

        private void UpdateMovement()
        {
            GroundCheck();
            GravityUpdate();

            // Calculate movement direction relative to camera
            Vector3 move = CalculateMoveDirection();

            // Calculate speed based on input state
            float targetSpeed = GetMovementSpeed();
            if (Mathf.Abs(_currentSpeed - targetSpeed) > 0.01f)
            {
                _currentSpeed = targetSpeed;
                OnSpeedChanged?.Invoke(_currentSpeed);
            }

            Vector3 moveDirection = move * _currentSpeed;

            // Rotate character to face movement direction
            if (move.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSmoothness);
            }

            // Handle jump
            if (jump && _isGrounded && _jumpTimeoutDelta <= 0.0f)
            {
                _verticalVelocity = CalculateJumpVelocity();
                if (Animator != null) Animator.SetTrigger("Jump");
                OnJump?.Invoke();
                jump = false;
            }

            moveDirection.y = _verticalVelocity;

            // Update animator
            UpdateAnimator();

            // Apply movement
            CharacterController.Move(moveDirection * Time.deltaTime);
        }

        private Vector3 CalculateMoveDirection()
        {
            Vector3 move = Vector3.zero;

            if (cameraTarget != null)
            {
                Vector3 camForward = cameraTarget.forward;
                Vector3 camRight = cameraTarget.right;
                camForward.y = 0;
                camRight.y = 0;
                camForward.Normalize();
                camRight.Normalize();
                move = camForward * direction.y + camRight * direction.x;
            }
            else
            {
                move = new Vector3(direction.x, 0, direction.y);
                move = transform.TransformDirection(move);
            }

            return move;
        }

        private float GetMovementSpeed()
        {
            if (settings != null)
            {
                return settings.GetMovementSpeed(sprint, crouch);
            }
            
            if (sprint) return runSpeed;
            if (crouch) return crouchSpeed;
            return walkSpeed;
        }

        private float CalculateJumpVelocity()
        {
            if (settings != null)
            {
                return settings.CalculateJumpVelocity();
            }
            return Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
        }

        private void GravityUpdate()
        {
            if (_isGrounded)
            {
                if (!_wasGrounded)
                {
                    _verticalVelocity = GroundedGravity;
                }
                if (_jumpTimeoutDelta >= 0.0f)
                    _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;
                if (_verticalVelocity < TerminalVelocity)
                    _verticalVelocity += Physics.gravity.y * Time.deltaTime;
            }
        }

        private void GroundCheck()
        {
            _isGrounded = Physics.CheckSphere(
                transform.position, 
                GroundCheckDistance, 
                GroundLayers, 
                QueryTriggerInteraction.Ignore
            );
            
            if (Animator != null) Animator.SetBool("OnGround", _isGrounded);
        }

        private void UpdateAnimator()
        {
            if (Animator == null) return;

            if (direction.magnitude > 0.01f)
            {
                Animator.SetFloat("Speed", sprint ? 1.0f : 0.5f);
            }
            else
            {
                Animator.SetFloat("Speed", 0.0f);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Force a jump regardless of current state.
        /// </summary>
        public void ForceJump()
        {
            _verticalVelocity = CalculateJumpVelocity();
            if (Animator != null) Animator.SetTrigger("Jump");
            OnJump?.Invoke();
        }

        /// <summary>
        /// Teleport the character to a position.
        /// </summary>
        public void Teleport(Vector3 position)
        {
            CharacterController.enabled = false;
            transform.position = position;
            CharacterController.enabled = true;
        }

        /// <summary>
        /// Teleport the character to a position with rotation.
        /// </summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            CharacterController.enabled = false;
            transform.position = position;
            transform.rotation = rotation;
            CharacterController.enabled = true;
        }

        /// <summary>
        /// Reset vertical velocity (useful after teleporting).
        /// </summary>
        public void ResetVerticalVelocity()
        {
            _verticalVelocity = 0f;
        }

        /// <summary>
        /// Apply settings from a ThirdPersonSettings ScriptableObject.
        /// </summary>
        public void ApplySettings(ThirdPersonSettings newSettings)
        {
            settings = newSettings;
        }

        #endregion
    }
}