using UnityEngine;
using UnityEngine.Events;
using WitShells.DesignPatterns;

namespace WitShells.ShootingSystem
{
    [RequireComponent(typeof(LineRenderer))]
    public class Trajectory : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Transform launchPoint;
        [SerializeField] private GameObject targetIndicatorPrefab;

        [Header("Trajectory Settings")]
        [SerializeField] private int segmentCount = 30;


        [Header("Input Settings")]
        [SerializeField] private float range = 10f;
        [SerializeField] private float launchForce = 15f;

        [Header("Turret Settings")]
        [SerializeField] private float minTiltAngle = -15f;
        [SerializeField] private float maxTiltAngle = 1f;
        [SerializeField] private float tiltOffset = 0f;

        [Header("Launch Offset")]
        [Tooltip("Local-space offset applied to the projectile spawn position and trajectory origin.")]
        [SerializeField] private Vector3 launchPositionOffset = Vector3.zero;

        [Header("Events")]
        public UnityEvent<Transform> OnProjectileLaunched;


        public void SetupTrajectory(float force, float maxRange)
        {
            launchForce = force;
            range = maxRange;
        }

        private GameObject _indicatorInstance;

        private void Awake()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            if (lineRenderer != null)
                lineRenderer.positionCount = 0;
        }

        private void OnEnable()
        {
            if (lineRenderer != null)
                lineRenderer.enabled = true;
        }

        private void OnDisable()
        {
            if (lineRenderer != null)
                lineRenderer.enabled = false;
            HideIndicator();
        }

        private void Update()
        {
            if (lineRenderer == null || launchPoint == null) return;
            if (!lineRenderer.enabled) return;

            RenderTrajectory();
        }

        /// <summary>
        /// Render trajectory points based on the launch point's orientation and configured force.
        /// Performs segment-by-segment raycasts to detect collisions and places the indicator on hit.
        /// </summary>
        private void RenderTrajectory()
        {
            int segments = Mathf.Max(2, segmentCount);

            Vector3[] points = new Vector3[segments];

            // starting position (apply local offset)
            Vector3 startPos = launchPoint.position + launchPoint.TransformDirection(launchPositionOffset);

            // initial velocity (world space) along launchPoint.forward
            Vector3 initialVelocity = launchPoint.forward * launchForce;

            // compute horizontal speed to estimate time-to-range
            Vector3 horizVel = new Vector3(initialVelocity.x, 0f, initialVelocity.z);
            float horizSpeed = horizVel.magnitude;

            float maxTime;
            if (horizSpeed > 0.001f)
            {
                maxTime = range / horizSpeed;
            }
            else
            {
                // fallback: treat forward speed as magnitude
                maxTime = range / Mathf.Max(0.001f, initialVelocity.magnitude);
            }

            Vector3 hitPoint = Vector3.zero;
            Vector3 hitNormal = Vector3.up;
            bool hit = false;
            int usedSegments = segments;

            Vector3 prev = startPos;

            for (int i = 0; i < segments; i++)
            {
                float t = (segments == 1) ? 0f : (i / (float)(segments - 1)) * maxTime;
                Vector3 pos = startPos + initialVelocity * t + 0.5f * Physics.gravity * (t * t);
                points[i] = pos;

                if (i > 0 && !hit)
                {
                    Vector3 dir = pos - prev;
                    float dist = dir.magnitude;
                    if (dist > 0.0001f)
                    {
                        if (Physics.Raycast(prev, dir.normalized, out RaycastHit rhit, dist))
                        {
                            hit = true;
                            hitPoint = rhit.point;
                            hitNormal = rhit.normal;
                            // include the hit point as the last visible point
                            points[i] = hitPoint;
                            usedSegments = i + 1;
                        }
                    }
                }

                prev = pos;
                if (hit) break;
            }


            // update line renderer
            lineRenderer.positionCount = usedSegments;
            for (int i = 0; i < usedSegments; i++)
                lineRenderer.SetPosition(i, points[i]);

            Vector3 indicatorPos = points[usedSegments - 1];
            Vector3 indicatorNormal = hit ? hitNormal : Vector3.up;
            ShowIndicator(indicatorPos, indicatorNormal);
        }

        private void ShowIndicator(Vector3 position, Vector3 normal)
        {
            if (targetIndicatorPrefab == null) return;

            if (_indicatorInstance == null)
            {
                _indicatorInstance = Instantiate(targetIndicatorPrefab, position, Quaternion.identity, null);
            }
            else
            {
                _indicatorInstance.SetActive(true);
                _indicatorInstance.transform.position = position;
            }

            // align indicator to surface normal if possible
            if (_indicatorInstance != null)
            {
                if (normal != Vector3.zero)
                    _indicatorInstance.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(launchPoint.forward, normal), normal);
            }
        }

        private void HideIndicator()
        {
            if (_indicatorInstance != null)
                _indicatorInstance.SetActive(false);
        }

        /// <summary>
        /// Manually show the trajectory (enables the line renderer and indicator).
        /// </summary>
        public void Show()
        {
            if (lineRenderer != null)
                lineRenderer.enabled = true;
            if (_indicatorInstance != null)
                _indicatorInstance.SetActive(true);
        }

        /// <summary>
        /// Manually hide the trajectory (disables the line renderer and hides the indicator).
        /// </summary>
        public void Hide()
        {
            if (lineRenderer != null)
                lineRenderer.enabled = false;
            HideIndicator();
        }

        /// <summary>
        /// Instantly set the turret (launch point) local X rotation (pitch) to the given degrees clamped between <see cref="minTiltAngle"/> and <see cref="maxTiltAngle"/>.
        /// Useful for snapping the aim to a known tilt value.
        /// </summary>
        /// <param name="pitchDegrees">Target local X rotation in degrees (signed -180..180 preferred).</param>
        public void SetTurretTiltInstant(float pitchDegrees)
        {
            if (launchPoint == null) return;

            float clamped = Mathf.Clamp(pitchDegrees, minTiltAngle, maxTiltAngle);

            // Normalize to 0-360 for Euler storage, but keep clamped sign
            float store = clamped < 0f ? (clamped + 360f) : clamped;

            Vector3 e = launchPoint.localEulerAngles;
            e.x = store;
            launchPoint.localEulerAngles = e;
        }

        /// <summary>
        /// Launches the provided Rigidbody as a projectile from the launch point using the configured <see cref="launchForce"/>.
        /// The Rigidbody will have its position/rotation set to the launch point and its velocity assigned so physics takes over.
        /// </summary>
        /// <param name="projectile">A Rigidbody instance (can be a pooled object). Must not be null.</param>
        public void ShootProjectile(Rigidbody projectile)
        {
            if (projectile == null || launchPoint == null) return;

            // place and orient projectile at the launch point (apply local offset)
            Vector3 spawnPos = launchPoint.position + launchPoint.TransformDirection(launchPositionOffset);
            projectile.position = spawnPos;
            projectile.rotation = launchPoint.rotation;

            // ensure physics will simulate
            projectile.isKinematic = false;
            projectile.useGravity = true;

            // give immediate velocity along the launch forward vector
            Vector3 initialVelocity = launchPoint.forward * launchForce;
            // apply as velocity change so mass differences won't affect initial speed
            projectile.AddForce(initialVelocity, ForceMode.VelocityChange);
            OnProjectileLaunched?.Invoke(projectile.transform);
        }

        /// <summary>
        /// Convenience overload: instantiate a prefab that has a Rigidbody and launch it.
        /// Returns the spawned Rigidbody or null if spawn/rigidbody not available.
        /// </summary>
        public Rigidbody ShootProjectile(GameObject projectilePrefab)
        {
            if (projectilePrefab == null || launchPoint == null) return null;

            Vector3 spawnPos = launchPoint.position + launchPoint.TransformDirection(launchPositionOffset);
            GameObject go = Instantiate(projectilePrefab, spawnPos, launchPoint.rotation);
            if (go.TryGetComponent<Rigidbody>(out var rb))
            {
                ShootProjectile(rb);
                return rb;
            }
            Destroy(go);
            WitLogger.LogWarning($"Projectile prefab {projectilePrefab.name} does not have a Rigidbody component.");
            return null;
        }


#if UNITY_EDITOR

        [Header("Test Projectile")]
        [SerializeField] private GameObject testProjectilePrefab;

        [ContextMenu("Test Shoot Projectile")]
        private void TestShootProjectile()
        {
            if (testProjectilePrefab != null)
            {
                ShootProjectile(testProjectilePrefab);
            }
        }
#endif

    }
}