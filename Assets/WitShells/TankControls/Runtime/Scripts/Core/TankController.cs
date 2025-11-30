using UnityEngine;

namespace WitShells.TankControls
{
    [RequireComponent(typeof(Rigidbody))]
    public class TankController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float rotationSpeed = 180f;
        [Tooltip("Time (seconds) to reach target velocity — lower = snappier")]
        [SerializeField] private float velocitySmoothTime = 0.12f;
        [Tooltip("Extra deceleration multiplier when stopping")]
        [SerializeField] private float decelerationMultiplier = 1.8f;

        [Header("Turret Settings")]
        [SerializeField] private Transform turretTransform;
        [SerializeField] private float turretRotationSpeed = 100f;
        [SerializeField] private float turretSmoothTime = 0.06f;
        [SerializeField] private float tiltSpeed = 5f;
        [SerializeField] private float maxTiltAngle = 1f;
        [SerializeField] private float minTiltAngle = -15f;

        [Header("Input Settings")]
        [SerializeField] private Vector2 moveInput;
        [SerializeField] private float turretRotationInput;
        [SerializeField] private float turretTiltInput;

        [Header("Runtime Tuning")]
        [SerializeField] private bool useRigidbodyInterpolation = true;

        private Rigidbody _rigidbody;

        // internal smoothing state
        private Vector3 _currentVelocity = Vector3.zero; // current smoothed velocity (world space)
        private Vector3 _velocitySmoothRef = Vector3.zero;
        private float _currentTurretYawVelocity;
        private float _currentTurretPitchVelocity;
        private float _currentBodyYawVelocity;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (useRigidbodyInterpolation)
            {
                _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        private void FixedUpdate()
        {
            HandleMovement();
            HandleTurretRotation();
        }

        private void HandleMovement()
        {
            // desired movement direction in local space
            Vector3 localInput = new Vector3(moveInput.x, 0f, moveInput.y);
            float inputMag = Mathf.Clamp01(localInput.magnitude);
            Vector3 moveDirection = localInput.normalized;

            // target velocity in world space
            Vector3 targetVelocity = (transform.TransformDirection(moveDirection)) * (moveSpeed * inputMag);

            // choose smoothing time (longer when decelerating to stop)
            float smoothTime = velocitySmoothTime;
            if (_currentVelocity.sqrMagnitude > 0.001f && targetVelocity.sqrMagnitude < 0.001f)
            {
                smoothTime *= decelerationMultiplier;
            }

            // Smoothly damp current velocity towards target velocity
            _currentVelocity = Vector3.SmoothDamp(_currentVelocity, targetVelocity, ref _velocitySmoothRef, smoothTime, Mathf.Infinity, Time.fixedDeltaTime);

            // Move rigidbody by smoothed velocity
            Vector3 newPos = _rigidbody.position + _currentVelocity * Time.fixedDeltaTime;
            _rigidbody.MovePosition(newPos);

            // Smooth body rotation to face movement direction when moving
            if (moveDirection != Vector3.zero && inputMag > 0.01f)
            {
                Quaternion desired = Quaternion.LookRotation(transform.TransformDirection(moveDirection));
                float currentYaw = transform.eulerAngles.y;
                float desiredYaw = desired.eulerAngles.y;
                float smoothedYaw = Mathf.SmoothDampAngle(currentYaw, desiredYaw, ref _currentBodyYawVelocity, Mathf.Max(0.01f, smoothTime));
                Quaternion smoothedRot = Quaternion.Euler(0f, smoothedYaw, 0f);
                _rigidbody.MoveRotation(smoothedRot);
            }
        }

        private void HandleTurretRotation()
        {
            if (turretTransform == null) return;

            // turretRotationInput expected as -1..1
            float targetDelta = turretRotationInput * turretRotationSpeed;

            // compute desired yaw relative to local yaw
            float currentYaw = turretTransform.localEulerAngles.y;
            if (currentYaw > 180f) currentYaw -= 360f;

            float desiredYaw = currentYaw + targetDelta * Time.fixedDeltaTime;

            float smoothedYaw = Mathf.SmoothDampAngle(currentYaw, desiredYaw, ref _currentTurretYawVelocity, turretSmoothTime);

            Vector3 localEuler = turretTransform.localEulerAngles;
            localEuler.y = smoothedYaw < 0 ? (smoothedYaw + 360f) : smoothedYaw;
            // Handle turret pitch (X axis) using turretTiltInput and tilt speed
            float currentPitch = turretTransform.localEulerAngles.x;
            if (currentPitch > 180f) currentPitch -= 360f;

            // target pitch change per frame
            float targetPitch = currentPitch + turretTiltInput * tiltSpeed * Time.fixedDeltaTime;
            // smooth pitch
            float smoothedPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref _currentTurretPitchVelocity, turretSmoothTime);
            // clamp to allowed range
            smoothedPitch = Mathf.Clamp(smoothedPitch, minTiltAngle, maxTiltAngle);

            localEuler.x = smoothedPitch < 0 ? (smoothedPitch + 360f) : smoothedPitch;
            turretTransform.localEulerAngles = localEuler;
        }

        #region Input Methods

        public void MoveInput(Vector2 dir)
        {
            moveInput = dir;
        }


        public void RotateTurretInput(float rotationInput)
        {
            turretRotationInput = rotationInput;
        }

        public void TiltTurretInput(float tiltInput)
        {
            turretTiltInput = tiltInput;
        }

        #endregion
    }
}