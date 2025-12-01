using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WitShells.TankControls
{
    [RequireComponent(typeof(Rigidbody))]
    public class TankController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float rotationSpeed = 1200f;
        [Tooltip("How quickly the tank reaches target forward speed (higher = snappier)")]
        [SerializeField] private float acceleration = 8f;
        [Tooltip("Smoothing time for body yaw when turning")]
        [SerializeField] private float turnSmoothTime = 0.12f;
        [Tooltip("Minimum absolute forward speed required before body can yaw")]
        [SerializeField] private float minSpeedForTurning = 0.3f;
        [Tooltip("Time (seconds) to reach target velocity — lower = snappier")]
        [SerializeField] private float velocitySmoothTime = 0.12f;
        [Tooltip("Extra deceleration multiplier when stopping")]
        [SerializeField] private float decelerationMultiplier = 1.8f;

        [Header("Turret Settings")]
        [SerializeField] private Transform turretTransform;
        [SerializeField] private float turretRotationSpeed = 200f;
        [SerializeField] private float turretSmoothTime = 0.06f;
        [SerializeField] private float tiltSpeed = 200f;
        [SerializeField] private float maxTiltAngle = 1f;
        [SerializeField] private float minTiltAngle = -15f;

        [Header("Input Settings")]
        [SerializeField] private Vector2 moveInput;
        [SerializeField] private float turretRotationInput;
        [SerializeField] private float turretTiltInput;

        [Header("Input References")]
        [SerializeField] private Transform centerOfMass;
        [SerializeField] private Transform cameraTarget;

        public Vector2 lookInput;

        [Header("Look Settings")]
        [SerializeField] private float lookSpeed = 2.0f;
        [SerializeField] private float sensitivity = 1.0f;

        [Header("Pitch Clamp")]
        [SerializeField] private float minPitch = -30f; // Down
        [SerializeField] private float maxPitch = 70f;  // Up

        // Optional Input System references — assign these in the inspector to have the
        // controller read input directly from Unity's Input System actions.
        public InputActionReference moveAction;
        public InputActionReference turretRotateAction;
        public InputActionReference turretTiltAction;
        public InputActionReference lookAction;
        [Tooltip("When true the assigned InputActionReferences will be used; otherwise input must be provided via the public input methods.")]
        [SerializeField] private bool useInputReferences = true;

        [Header("Runtime Tuning")]
        [SerializeField] private bool useRigidbodyInterpolation = true;

        [Header("Stability")]
        [Tooltip("Strength of the upright correction torque (higher = stronger correction)")]
        [SerializeField] private float balanceStrength = 10f;
        [Tooltip("Damping applied to angular velocity on pitch/roll axes")]
        [SerializeField] private float balanceDamping = 2f;
        [Tooltip("If true and a `centerOfMass` transform is assigned, set the Rigidbody.centerOfMass at Awake using that transform.")]
        [SerializeField] private bool autoSetCenterOfMass = true;

        private Rigidbody _rigidbody;

        // internal smoothing state
        private Vector3 _currentVelocity = Vector3.zero; // current smoothed velocity (world space)
        private Vector3 _velocitySmoothRef = Vector3.zero;
        private float _currentForwardSpeed = 0f;
        private float _speedSmoothRef = 0f;
        private float _currentTurretYawVelocity;
        private float _currentTurretPitchVelocity;
        private float _currentBodyYawVelocity;

        private float _currentYaw;
        private float _currentPitch;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (useRigidbodyInterpolation)
            {
                _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            }

            // Optionally set the Rigidbody center of mass from the provided transform to help stability
            if (autoSetCenterOfMass && centerOfMass != null)
            {
                _rigidbody.centerOfMass = _rigidbody.transform.InverseTransformPoint(centerOfMass.position);
            }

            if (cameraTarget != null)
            {
                Vector3 angles = cameraTarget.eulerAngles;
                _currentYaw = angles.y;
                _currentPitch = angles.x;
            }
        }

        private void OnEnable()
        {
            if (!useInputReferences) return;
            if (moveAction != null && moveAction.action != null)
            {
                try
                {
                    moveAction.action.performed += OnMovePerformed;
                    moveAction.action.canceled += OnMoveCanceled;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to subscribe moveAction callbacks: {ex.Message}");
                }
            }

            if (turretRotateAction != null && turretRotateAction.action != null)
            {
                try
                {
                    turretRotateAction.action.performed += OnTurretRotatePerformed;
                    turretRotateAction.action.canceled += OnTurretRotateCanceled;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to subscribe turretRotateAction callbacks: {ex.Message}");
                }
            }

            if (turretTiltAction != null && turretTiltAction.action != null)
            {
                try
                {
                    turretTiltAction.action.performed += OnTurretTiltPerformed;
                    turretTiltAction.action.canceled += OnTurretTiltCanceled;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to subscribe turretTiltAction callbacks: {ex.Message}");
                }
            }

            if (lookAction != null && lookAction.action != null)
            {
                try
                {
                    lookAction.action.performed += OnLookPerformed;
                    lookAction.action.canceled += OnLookCanceled;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to subscribe lookAction callbacks: {ex.Message}");
                }
            }

            // Enable assigned actions on next frame to avoid mutating action maps during input processing
            _enableActionsCoroutine = StartCoroutine(EnableAssignedActionsNextFrame());
        }

        private void OnDisable()
        {
            if (!useInputReferences) return;
            // stop any pending enable coroutine
            if (_enableActionsCoroutine != null)
            {
                StopCoroutine(_enableActionsCoroutine);
                _enableActionsCoroutine = null;
            }
            if (moveAction != null && moveAction.action != null)
            {
                try
                {
                    moveAction.action.performed -= OnMovePerformed;
                    moveAction.action.canceled -= OnMoveCanceled;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to unsubscribe moveAction callbacks: {ex.Message}");
                }
                try
                {
                    if (moveAction.action.enabled)
                        moveAction.action.Disable();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to disable moveAction: {ex.Message}");
                }
            }

            if (turretRotateAction != null && turretRotateAction.action != null)
            {
                try
                {
                    turretRotateAction.action.performed -= OnTurretRotatePerformed;
                    turretRotateAction.action.canceled -= OnTurretRotateCanceled;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to unsubscribe turretRotateAction callbacks: {ex.Message}");
                }
                try
                {
                    if (turretRotateAction.action.enabled)
                        turretRotateAction.action.Disable();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to disable turretRotateAction: {ex.Message}");
                }
            }

            if (turretTiltAction != null && turretTiltAction.action != null)
            {
                try
                {
                    turretTiltAction.action.performed -= OnTurretTiltPerformed;
                    turretTiltAction.action.canceled -= OnTurretTiltCanceled;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to unsubscribe turretTiltAction callbacks: {ex.Message}");
                }
                try
                {
                    if (turretTiltAction.action.enabled)
                        turretTiltAction.action.Disable();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to disable turretTiltAction: {ex.Message}");
                }
            }

            if (lookAction != null && lookAction.action != null)
            {
                try
                {
                    lookAction.action.performed -= OnLookPerformed;
                    lookAction.action.canceled -= OnLookCanceled;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to unsubscribe lookAction callbacks: {ex.Message}");
                }
                try
                {
                    if (lookAction.action.enabled)
                        lookAction.action.Disable();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to disable lookAction: {ex.Message}");
                }
            }
        }

        private Coroutine _enableActionsCoroutine;

        private IEnumerator EnableAssignedActionsNextFrame()
        {
            // wait a single frame so enabling happens outside the current input update
            yield return null;

            if (!useInputReferences)
                yield break;

            if (moveAction != null && moveAction.action != null)
            {
                try
                {
                    if (!moveAction.action.enabled)
                        moveAction.action.Enable();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to enable moveAction: {ex.Message}");
                }
            }

            if (turretRotateAction != null && turretRotateAction.action != null)
            {
                try
                {
                    if (!turretRotateAction.action.enabled)
                        turretRotateAction.action.Enable();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to enable turretRotateAction: {ex.Message}");
                }
            }

            if (turretTiltAction != null && turretTiltAction.action != null)
            {
                try
                {
                    if (!turretTiltAction.action.enabled)
                        turretTiltAction.action.Enable();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to enable turretTiltAction: {ex.Message}");
                }
            }

            // Enable look action as well (if assigned) to receive look events
            if (lookAction != null && lookAction.action != null)
            {
                try
                {
                    if (!lookAction.action.enabled)
                        lookAction.action.Enable();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to enable lookAction: {ex.Message}");
                }
            }

            _enableActionsCoroutine = null;
        }

        private void FixedUpdate()
        {
            HandleMovement();
            HandleTurretRotation();
            StabilizeUpright();
        }

        private void LateUpdate()
        {
            if (cameraTarget == null) return;

            // Accumulate yaw and pitch based on look input
            _currentYaw += lookInput.x * lookSpeed * sensitivity * Time.deltaTime;
            _currentPitch -= lookInput.y * lookSpeed * sensitivity * Time.deltaTime; // Invert Y for typical controls

            // Clamp pitch
            _currentPitch = Mathf.Clamp(_currentPitch, minPitch, maxPitch);

            // Apply rotation (pitch X, yaw Y, roll Z=0)
            cameraTarget.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
        }

        private void StabilizeUpright()
        {
            // Compute a corrective torque to align the vehicle's up vector with world up.
            // Use cross product between current up and desired up to get axis of correction.
            Vector3 up = transform.up;
            Vector3 error = Vector3.Cross(up, Vector3.up);

            // Corrective torque proportional to angular error
            Vector3 correctiveTorque = error * balanceStrength;

            // Dampen existing angular velocity on pitch and roll axes (x and z)
            Vector3 angVel = _rigidbody.angularVelocity;
            Vector3 angVelPitchRoll = new Vector3(angVel.x, 0f, angVel.z);
            Vector3 dampingTorque = -angVelPitchRoll * balanceDamping;

            // Apply as acceleration to avoid depending on mass
            _rigidbody.AddTorque(correctiveTorque + dampingTorque, ForceMode.Acceleration);
        }

        private void HandleMovement()
        {
            // Arcade-style tank: forward/back controls linear speed, horizontal controls yaw.
            // Rotation occurs only when tank is moving above a small threshold to avoid
            // instant pivoting when stationary.

            // Target forward speed (can be negative for reverse)
            float targetSpeed = moveInput.y * moveSpeed;

            // Smoothly approach target speed using acceleration as a smoothing factor
            float smoothTimeSpeed = Mathf.Max(0.01f, 1f / Mathf.Max(0.0001f, acceleration));
            _currentForwardSpeed = Mathf.SmoothDamp(_currentForwardSpeed, targetSpeed, ref _speedSmoothRef, smoothTimeSpeed, Mathf.Infinity, Time.fixedDeltaTime);

            // Move rigidbody forward by the smoothed forward speed
            Vector3 newPos = _rigidbody.position + transform.forward * (_currentForwardSpeed * Time.fixedDeltaTime);
            _rigidbody.MovePosition(newPos);

            // Determine if we should allow yaw (only when moving fast enough)
            float absForward = Mathf.Abs(_currentForwardSpeed);
            if (absForward >= minSpeedForTurning)
            {
                // Scale turn responsiveness based on forward speed (so turning at low speed is slower)
                float speedFactor = Mathf.Clamp01(absForward / moveSpeed);
                float yawInput = moveInput.x;
                float yawDelta = yawInput * rotationSpeed * speedFactor * Time.fixedDeltaTime;

                float currentYaw = transform.eulerAngles.y;
                float desiredYaw = currentYaw + yawDelta;
                float smoothedYaw = Mathf.SmoothDampAngle(currentYaw, desiredYaw, ref _currentBodyYawVelocity, turnSmoothTime);
                Quaternion smoothedRot = Quaternion.Euler(0f, smoothedYaw, 0f);
                _rigidbody.MoveRotation(smoothedRot);
            }
            else
            {
                // When nearly stopped, gradually damp any residual yaw velocity
                _currentBodyYawVelocity = Mathf.Lerp(_currentBodyYawVelocity, 0f, Time.fixedDeltaTime * 8f);
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

        public void SetEnableInputReferences(bool enable)
        {
            useInputReferences = enable;
        }

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

        private void OnMovePerformed(InputAction.CallbackContext ctx)
        {
            MoveInput(ctx.ReadValue<Vector2>());
        }

        private void OnMoveCanceled(InputAction.CallbackContext ctx)
        {
            MoveInput(Vector2.zero);
        }

        private void OnTurretRotatePerformed(InputAction.CallbackContext ctx)
        {
            RotateTurretInput(ctx.ReadValue<float>());
        }

        private void OnTurretRotateCanceled(InputAction.CallbackContext ctx)
        {
            RotateTurretInput(0f);
        }

        private void OnTurretTiltPerformed(InputAction.CallbackContext ctx)
        {
            TiltTurretInput(ctx.ReadValue<float>());
        }

        private void OnTurretTiltCanceled(InputAction.CallbackContext ctx)
        {
            TiltTurretInput(0f);
        }

        private void OnLookPerformed(InputAction.CallbackContext ctx)
        {
            try
            {
                lookInput = ctx.ReadValue<Vector2>();
            }
            catch (Exception)
            {
                lookInput = Vector2.zero;
            }
        }

        private void OnLookCanceled(InputAction.CallbackContext ctx)
        {
            lookInput = Vector2.zero;
        }

        #endregion
    }
}