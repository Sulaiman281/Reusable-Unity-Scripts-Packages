namespace WitShells.ThirdPersonControl
{
    using UnityEngine;

    /// <summary>
    /// ScriptableObject containing all third-person controller settings.
    /// Create an instance via Create > WitShells > ThirdPersonControl > Settings.
    /// </summary>
    [CreateAssetMenu(fileName = "ThirdPersonSettings", menuName = "WitShells/ThirdPersonControl/Settings")]
    public class ThirdPersonSettings : ScriptableObject
    {
        [Header("Movement Settings")]
        [Tooltip("Walking speed in units per second")]
        [SerializeField] private float walkSpeed = 2f;

        [Tooltip("Running/sprinting speed in units per second")]
        [SerializeField] private float runSpeed = 5f;

        [Tooltip("Crouching speed in units per second")]
        [SerializeField] private float crouchSpeed = 1f;

        [Tooltip("Jump force applied when jumping")]
        [SerializeField] private float jumpForce = 3.5f;

        [Tooltip("Rotation smoothing factor (higher = faster rotation)")]
        [Range(0.01f, 1f)]
        [SerializeField] private float rotationSmoothness = 0.15f;

        [Header("Ground Check Settings")]
        [Tooltip("Distance to check for ground below the character")]
        [SerializeField] private float groundCheckDistance = 0.2f;

        [Tooltip("Layers considered as ground for ground checking")]
        [SerializeField] private LayerMask groundLayers = 1;

        [Header("Physics Settings")]
        [Tooltip("Maximum falling velocity")]
        [SerializeField] private float terminalVelocity = 53f;

        [Tooltip("Time before another jump can be triggered")]
        [SerializeField] private float jumpTimeout = 0.2f;

        [Tooltip("Velocity applied when grounded to keep character grounded")]
        [SerializeField] private float groundedGravity = -2f;

        [Header("Camera Settings")]
        [Tooltip("Look/camera rotation speed")]
        [SerializeField] private float lookSpeed = 2f;

        [Tooltip("Mouse/input sensitivity multiplier")]
        [SerializeField] private float lookSensitivity = 1f;

        [Tooltip("Minimum pitch angle (looking down)")]
        [SerializeField] private float minPitch = -30f;

        [Tooltip("Maximum pitch angle (looking up)")]
        [SerializeField] private float maxPitch = 70f;

        [Header("Audio Settings")]
        [Tooltip("Sound effects object for footsteps and other sounds")]
        [SerializeField] private SoundSfxObject soundEffects;

        [Tooltip("Master volume for character sounds")]
        [Range(0f, 1f)]
        [SerializeField] private float soundVolume = 1f;

        #region Properties

        // Movement
        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public float CrouchSpeed => crouchSpeed;
        public float JumpForce => jumpForce;
        public float RotationSmoothness => rotationSmoothness;

        // Ground Check
        public float GroundCheckDistance => groundCheckDistance;
        public LayerMask GroundLayers => groundLayers;

        // Physics
        public float TerminalVelocity => terminalVelocity;
        public float JumpTimeout => jumpTimeout;
        public float GroundedGravity => groundedGravity;

        // Camera
        public float LookSpeed => lookSpeed;
        public float LookSensitivity => lookSensitivity;
        public float MinPitch => minPitch;
        public float MaxPitch => maxPitch;

        // Audio
        public SoundSfxObject SoundEffects => soundEffects;
        public float SoundVolume => soundVolume;

        #endregion

        #region Static Default Instance

        private static ThirdPersonSettings _default;

        /// <summary>
        /// Returns a default settings instance. Attempts to load from Resources first.
        /// </summary>
        public static ThirdPersonSettings Default
        {
            get
            {
                if (_default == null)
                {
                    _default = Resources.Load<ThirdPersonSettings>("ThirdPersonSettings");
                    if (_default == null)
                    {
                        _default = CreateInstance<ThirdPersonSettings>();
                    }
                }
                return _default;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets the appropriate movement speed based on current state.
        /// </summary>
        public float GetMovementSpeed(bool isSprinting, bool isCrouching)
        {
            if (isSprinting) return runSpeed;
            if (isCrouching) return crouchSpeed;
            return walkSpeed;
        }

        /// <summary>
        /// Calculates the initial jump velocity based on jump force and gravity.
        /// </summary>
        public float CalculateJumpVelocity()
        {
            return Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
        }

        /// <summary>
        /// Clamps the pitch angle within the defined limits.
        /// </summary>
        public float ClampPitch(float pitch)
        {
            return Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        #endregion
    }
}
