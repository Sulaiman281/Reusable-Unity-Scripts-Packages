namespace WitShells.ThirdPersonControl
{
    using Unity.Cinemachine;
    using UnityEngine;

    /// <summary>
    /// Handles camera look input for Cinemachine third-person camera.
    /// Can use settings from ThirdPersonSettings ScriptableObject or local overrides.
    /// </summary>
    [AddComponentMenu("WitShells/Third Person Control/Cinemachine Camera Look Input")]
    public class CinemachineCamLookInput : MonoBehaviour
    {
        #region Input

        [Header("Input")]
        [Tooltip("Current look input vector (set by ThirdPersonInput or manually)")]
        public Vector2 lookInput;

        #endregion

        #region Settings

        [Header("Settings")]
        [Tooltip("ScriptableObject containing camera settings. If null, uses local overrides.")]
        [SerializeField] private ThirdPersonSettings settings;

        [Header("Settings Override (Used if Settings is null)")]
        [SerializeField] private float lookSpeed = 2.0f;
        [SerializeField] private float sensitivity = 1.0f;
        [SerializeField] private float minPitch = -30f;
        [SerializeField] private float maxPitch = 70f;

        #endregion

        #region References

        [Header("References")]
        [SerializeField] private CinemachineCamera cinemachineCamera;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the effective look speed from settings or override.
        /// </summary>
        public float LookSpeed => settings != null ? settings.LookSpeed : lookSpeed;

        /// <summary>
        /// Gets the effective sensitivity from settings or override.
        /// </summary>
        public float Sensitivity => settings != null ? settings.LookSensitivity : sensitivity;

        /// <summary>
        /// Gets the effective minimum pitch from settings or override.
        /// </summary>
        public float MinPitch => settings != null ? settings.MinPitch : minPitch;

        /// <summary>
        /// Gets the effective maximum pitch from settings or override.
        /// </summary>
        public float MaxPitch => settings != null ? settings.MaxPitch : maxPitch;

        /// <summary>
        /// Look input vector (X = horizontal, Y = vertical).
        /// </summary>
        public Vector2 LookInput
        {
            get => lookInput;
            set => lookInput = value;
        }

        /// <summary>
        /// The Cinemachine camera component (auto-cached).
        /// </summary>
        public CinemachineCamera CinemachineCamera
        {
            get
            {
                if (cinemachineCamera == null)
                    cinemachineCamera = GetComponent<CinemachineCamera>();
                return cinemachineCamera;
            }
        }

        /// <summary>
        /// The camera's tracking target transform.
        /// </summary>
        public Transform Target => CinemachineCamera?.Target.TrackingTarget;

        /// <summary>
        /// Current yaw angle (horizontal rotation).
        /// </summary>
        public float CurrentYaw => _currentYaw;

        /// <summary>
        /// Current pitch angle (vertical rotation).
        /// </summary>
        public float CurrentPitch => _currentPitch;

        /// <summary>
        /// The settings ScriptableObject. Can be changed at runtime.
        /// </summary>
        public ThirdPersonSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        #endregion

        #region Private State

        private float _currentYaw;
        private float _currentPitch;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            InitializeAngles();
        }

        private void LateUpdate()
        {
            if (Target == null) return;

            // Mouse delta is already a per-frame value, don't multiply by deltaTime
            // The input is only non-zero when mouse is moving (this is normal for delta)
            float yawDelta = lookInput.x * LookSpeed * Sensitivity * 0.1f;
            float pitchDelta = lookInput.y * LookSpeed * Sensitivity * 0.1f;

            // Accumulate yaw and pitch
            _currentYaw += yawDelta;
            _currentPitch -= pitchDelta;

            // Clamp pitch
            _currentPitch = ClampPitch(_currentPitch);

            // Apply rotation
            Target.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the camera's tracking target.
        /// </summary>
        public void SetTarget(Transform target)
        {
            if (CinemachineCamera != null)
            {
                CinemachineCamera.Target.TrackingTarget = target;
                InitializeAngles();
            }
        }

        /// <summary>
        /// Resets the camera rotation to look at the target's forward direction.
        /// </summary>
        public void ResetRotation()
        {
            if (Target != null)
            {
                _currentYaw = Target.eulerAngles.y;
                _currentPitch = 0f;
            }
        }

        /// <summary>
        /// Sets the camera angles directly.
        /// </summary>
        public void SetAngles(float yaw, float pitch)
        {
            _currentYaw = yaw;
            _currentPitch = ClampPitch(pitch);
        }

        /// <summary>
        /// Apply settings from a ThirdPersonSettings ScriptableObject.
        /// </summary>
        public void ApplySettings(ThirdPersonSettings newSettings)
        {
            settings = newSettings;
        }

        #endregion

        #region Private Methods

        private void InitializeAngles()
        {
            if (Target != null)
            {
                Vector3 angles = Target.eulerAngles;
                _currentYaw = angles.y;
                _currentPitch = angles.x;
                
                // Normalize pitch to -180 to 180 range
                if (_currentPitch > 180f)
                    _currentPitch -= 360f;
            }
        }

        private float ClampPitch(float pitch)
        {
            if (settings != null)
            {
                return settings.ClampPitch(pitch);
            }
            return Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        #endregion
    }
}