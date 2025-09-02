using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using WitShells.WitActor;

namespace WitShells.SimpleCarControls
{
    [RequireComponent(typeof(SimpleCarDriver), typeof(NavMeshAgent))]
    public class SimpleCarDriverAI : MonoBehaviour, IDestination
    {
        #region Serialized Fields
        [Header("AI Navigation Settings")]
        [SerializeField] private float waypointReachedDistance = 8f;
        [SerializeField] private float finalDestinationReachedDistance = 4f;

        [Header("Collision Detection")]
        [SerializeField] private Vector3 obstacleDetectionDistance = new Vector3(3, 0, 1.65f);
        [SerializeField] private Vector3 topBottomDetectorSize = new Vector3(1.5f, .5f, 1f);
        [SerializeField] private Vector3 leftRightDetectorSize = new Vector3(1f, .5f, 1f);

        [Header("Smart AI Settings")]
        [SerializeField] private float obstacleAvoidanceWeight = 1.5f;
        [SerializeField] private float reverseSteerMultiplier = 1.5f;
        [SerializeField] private float stuckDetectionTime = 2f;
        [SerializeField] private float reverseTime = 1.5f;
        [SerializeField] private float minSpeedThreshold = 2f;
        [SerializeField] private float emergencyBrakeDistance = 3f;

        [Header("Events")]
        public UnityEvent<SimpleCarDriverAI> OnDestinationReachedEvent;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private float gizmoRadius = 1f;

        [Header("AI Target Test")]
        [SerializeField] private Transform targetTransform;
        #endregion

        #region Private Fields
        [SerializeField] private NavMeshAgent agent;
        private SimpleCarDriver carDriver;
        private Vector3 currentTargetPosition;
        private int currentCornerIndex = 1;
        private bool hasReachedDestination = true;

        // Movement state tracking
        private AIMovementState movementState = new AIMovementState();
        private ObstacleDetectionData obstacleData = new ObstacleDetectionData();
        private NavigationData navigationData = new NavigationData();
        #endregion

        #region Data Structures
        [System.Serializable]
        private class AIMovementState
        {
            public float stuckTimer;
            public float reverseTimer;
            public Vector3 lastPosition;
            public Vector3 stuckPosition;
            public bool isReversing;
            public bool isStuck;
            public float lastDecisionTime;
            public float decisionCooldown = 0.5f;
            public float minReverseDistance = 3f;
        }

        private struct ObstacleDetectionData
        {
            public bool frontBlocked;
            public bool backBlocked;
            public bool leftBlocked;
            public bool rightBlocked;
            public bool hasEscapeRoute => !leftBlocked || !rightBlocked;
            public bool allSidesBlocked => frontBlocked && leftBlocked && rightBlocked;
        }

        private struct NavigationData
        {
            public Vector3 directionToTarget;
            public float angleToTarget;
            public float distanceToTarget;
            public float distanceToFinalDestination;
            public float currentSpeed;
            public float currentSpeedAbs;
            public float positionDelta;
            public bool isFacingTarget => Mathf.Abs(angleToTarget) < 90f;
            public bool recentlyReversed;
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            ConfigureNavMeshAgent();
        }

        private void Update()
        {
            if (ShouldStopMovement()) return;

            UpdateNavMeshAgent();
            UpdatePathNavigation();
            ExecuteMovementLogic();
        }
        #endregion

        #region Initialization
        private void InitializeComponents()
        {
            carDriver = GetComponent<SimpleCarDriver>();
            agent = GetComponent<NavMeshAgent>();
            agent.enabled = true;
            movementState.lastPosition = transform.position;
        }

        private void ConfigureNavMeshAgent()
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
        }
        #endregion

        #region Movement Control Flow
        private bool ShouldStopMovement()
        {
            if (hasReachedDestination)
            {
                carDriver.SetInputs(0f, 0f);
                return true;
            }
            return false;
        }

        private void UpdateNavMeshAgent()
        {
            agent.nextPosition = transform.position;
        }

        private void UpdatePathNavigation()
        {
            if (IsPathInvalid())
            {
                carDriver.SetInputs(0f, 0f);
                return;
            }

            UpdateTargetFromPath();
        }

        private void ExecuteMovementLogic()
        {
            GatherNavigationData();
            DetectObstacles();
            UpdateStuckDetection();
            CalculateAndApplyMovementInputs();
        }
        #endregion

        #region Path Navigation
        private bool IsPathInvalid()
        {
            return agent.pathPending || agent.pathStatus != NavMeshPathStatus.PathComplete;
        }

        private void UpdateTargetFromPath()
        {
            if (!IsValidPathForNavigation())
            {
                SetDestinationReached();
                return;
            }

            UpdateCurrentTargetPosition();
            CheckDestinationReached();
            AdvanceToNextWaypoint();
        }

        private bool IsValidPathForNavigation()
        {
            return agent.path?.corners != null && agent.path.corners.Length >= 2;
        }

        private void SetDestinationReached()
        {
            hasReachedDestination = true;
            carDriver.StopCompletely();
        }

        private void UpdateCurrentTargetPosition()
        {
            currentCornerIndex = Mathf.Clamp(currentCornerIndex, 0, agent.path.corners.Length - 1);
            currentTargetPosition = agent.path.corners[currentCornerIndex];
        }

        private void CheckDestinationReached()
        {
            float distanceToDestination = Vector3.Distance(transform.position, agent.destination);
            if (distanceToDestination < finalDestinationReachedDistance)
            {
                hasReachedDestination = true;
                OnDestinationReached();
                agent.ResetPath();
                carDriver.StopCompletely();
            }
        }

        private void AdvanceToNextWaypoint()
        {
            if (ShouldAdvanceToNextWaypoint())
            {
                currentCornerIndex++;
            }
        }

        private bool ShouldAdvanceToNextWaypoint()
        {
            if (currentCornerIndex >= agent.path.corners.Length - 1) return false;

            float distanceToCorner = Vector3.Distance(transform.position, agent.path.corners[currentCornerIndex]);
            return distanceToCorner < waypointReachedDistance;
        }
        #endregion

        #region Data Gathering
        private void GatherNavigationData()
        {
            navigationData.directionToTarget = (currentTargetPosition - transform.position).normalized;
            navigationData.angleToTarget = Vector3.SignedAngle(transform.forward, navigationData.directionToTarget, Vector3.up);
            navigationData.distanceToTarget = Vector3.Distance(transform.position, currentTargetPosition);
            navigationData.distanceToFinalDestination = Vector3.Distance(transform.position, agent.destination);
            navigationData.currentSpeed = carDriver.GetSpeed();
            navigationData.currentSpeedAbs = Mathf.Abs(navigationData.currentSpeed);
            navigationData.positionDelta = Vector3.Distance(transform.position, movementState.lastPosition) / Time.deltaTime;
            navigationData.recentlyReversed = (Time.time - movementState.lastDecisionTime) < movementState.decisionCooldown;
        }

        private void DetectObstacles()
        {
            CheckSurroundings(out obstacleData.frontBlocked, out obstacleData.backBlocked,
                            out obstacleData.leftBlocked, out obstacleData.rightBlocked);
        }
        #endregion

        #region Stuck Detection and Recovery
        private void UpdateStuckDetection()
        {
            if (IsCarStuck())
            {
                HandleStuckDetection();
            }
            else if (IsCarMovingWell())
            {
                ResetStuckState();
            }

            movementState.lastPosition = transform.position;
        }

        private bool IsCarStuck()
        {
            return navigationData.positionDelta < minSpeedThreshold &&
                   obstacleData.frontBlocked &&
                   !hasReachedDestination;
        }

        private void HandleStuckDetection()
        {
            movementState.stuckTimer += Time.deltaTime;

            if (ShouldInitiateReverseManeuver())
            {
                InitiateReverseManeuver();
            }
        }

        private bool ShouldInitiateReverseManeuver()
        {
            return movementState.stuckTimer > stuckDetectionTime && !movementState.isReversing;
        }

        private void InitiateReverseManeuver()
        {
            movementState.isStuck = true;
            movementState.isReversing = true;
            movementState.reverseTimer = reverseTime;
            movementState.stuckTimer = 0f;
            movementState.stuckPosition = transform.position;
            movementState.lastDecisionTime = Time.time;
        }

        private bool IsCarMovingWell()
        {
            return navigationData.positionDelta > minSpeedThreshold * 2f;
        }

        private void ResetStuckState()
        {
            movementState.stuckTimer = 0f;
            movementState.isStuck = false;
        }
        #endregion

        #region Movement Input Calculation
        private void CalculateAndApplyMovementInputs()
        {
            float forwardAmount, turnAmount;

            if (IsInReverseState())
            {
                CalculateReverseInputs(out forwardAmount, out turnAmount);
            }
            else
            {
                CalculateNormalMovementInputs(out forwardAmount, out turnAmount);
            }

            ApplyFinalSafetyChecks(ref forwardAmount, ref turnAmount);
            carDriver.SetInputs(forwardAmount, turnAmount);
        }

        private bool IsInReverseState()
        {
            return movementState.isReversing;
        }

        private void CalculateReverseInputs(out float forwardAmount, out float turnAmount)
        {
            movementState.reverseTimer -= Time.deltaTime;

            if (ShouldExitReverseState())
            {
                ExitReverseState();
                CalculateNormalMovementInputs(out forwardAmount, out turnAmount);
                return;
            }

            CalculateReverseMovement(out forwardAmount, out turnAmount);
        }

        private bool ShouldExitReverseState()
        {
            float distanceFromStuckPoint = Vector3.Distance(transform.position, movementState.stuckPosition);

            bool timerExpired = movementState.reverseTimer <= 0f;
            bool movedEnoughDistance = distanceFromStuckPoint > movementState.minReverseDistance;
            bool pathIsClear = !obstacleData.frontBlocked && Mathf.Abs(navigationData.angleToTarget) < 60f;
            bool cannotReverse = obstacleData.backBlocked;

            return (timerExpired && movedEnoughDistance) || pathIsClear || cannotReverse;
        }

        private void ExitReverseState()
        {
            movementState.isReversing = false;
            movementState.lastDecisionTime = Time.time;
        }

        private void CalculateReverseMovement(out float forwardAmount, out float turnAmount)
        {
            forwardAmount = obstacleData.backBlocked ? 0f : -0.8f;
            turnAmount = CalculateReverseSteering();
        }

        private float CalculateReverseSteering()
        {
            if (obstacleData.backBlocked) return 0f;

            float distanceFromStuckPoint = Vector3.Distance(transform.position, movementState.stuckPosition);

            if (distanceFromStuckPoint < 1f)
            {
                return 0f; // Go straight back first
            }

            float reverseSteerDirection = -Mathf.Sign(navigationData.angleToTarget);
            float steerStrength = Mathf.Clamp01(distanceFromStuckPoint / movementState.minReverseDistance);
            return reverseSteerDirection * reverseSteerMultiplier * steerStrength;
        }

        private void CalculateNormalMovementInputs(out float forwardAmount, out float turnAmount)
        {
            turnAmount = CalculateSteeringInput();
            forwardAmount = CalculateForwardInput();
        }

        private float CalculateSteeringInput()
        {
            if (obstacleData.frontBlocked)
            {
                return CalculateObstacleAvoidanceSteering();
            }

            return CalculateNormalSteering();
        }

        private float CalculateObstacleAvoidanceSteering()
        {
            if (!obstacleData.leftBlocked && !obstacleData.rightBlocked)
            {
                // Both sides clear - choose side closer to target
                return Mathf.Sign(navigationData.angleToTarget) * obstacleAvoidanceWeight;
            }

            if (!obstacleData.leftBlocked)
            {
                return -obstacleAvoidanceWeight; // Go left
            }

            if (!obstacleData.rightBlocked)
            {
                return obstacleAvoidanceWeight; // Go right
            }

            // All directions blocked
            if (!navigationData.recentlyReversed)
            {
                InitiateReverseManeuver();
            }

            return Mathf.Sin(Time.time * 3f) * 0.5f; // Gentle wiggle
        }

        private float CalculateNormalSteering()
        {
            float baseSteering = Mathf.Clamp(navigationData.angleToTarget / 45f, -1f, 1f);
            return ApplySteeringConstraints(baseSteering);
        }

        private float ApplySteeringConstraints(float steering)
        {
            if (obstacleData.leftBlocked && steering < -0.1f)
            {
                steering = Mathf.Max(steering * 0.3f, 0f);
            }

            if (obstacleData.rightBlocked && steering > 0.1f)
            {
                steering = Mathf.Min(steering * 0.3f, 0f);
            }

            return steering;
        }

        private float CalculateForwardInput()
        {
            if (ShouldEmergencyBrake())
            {
                return -1f;
            }

            if (IsApproachingDestination())
            {
                return CalculateDestinationApproachSpeed();
            }

            if (ShouldSlowForTurning())
            {
                return 0.6f;
            }

            if (obstacleData.frontBlocked)
            {
                return CalculateObstacleNavigationSpeed();
            }

            return CalculateNormalForwardSpeed();
        }

        private bool ShouldEmergencyBrake()
        {
            bool obstacleAtSpeed = obstacleData.frontBlocked &&
                                  navigationData.currentSpeedAbs > 5f &&
                                  !navigationData.recentlyReversed;

            bool destinationEmergency = navigationData.distanceToFinalDestination < emergencyBrakeDistance &&
                                       navigationData.currentSpeedAbs > 3f;

            return obstacleAtSpeed || destinationEmergency;
        }

        private bool IsApproachingDestination()
        {
            return navigationData.distanceToFinalDestination < 20f;
        }

        private float CalculateDestinationApproachSpeed()
        {
            float brakingDistance = 20f;
            float stopDistance = 6f;

            float targetSpeed = Mathf.Lerp(0f, 25f, navigationData.distanceToFinalDestination / brakingDistance);

            if (navigationData.currentSpeedAbs > targetSpeed + 5f)
            {
                // Progressive braking
                return -Mathf.InverseLerp(targetSpeed, targetSpeed + 20f, navigationData.currentSpeedAbs);
            }

            if (navigationData.distanceToFinalDestination > stopDistance)
            {
                // Gentle approach
                return Mathf.Lerp(0.1f, 0.7f, navigationData.distanceToFinalDestination / brakingDistance);
            }

            // Final approach
            return navigationData.currentSpeedAbs > 2f ? -0.2f : 0f;
        }

        private bool ShouldSlowForTurning()
        {
            return !navigationData.isFacingTarget && navigationData.distanceToTarget > 10f;
        }

        private float CalculateObstacleNavigationSpeed()
        {
            if (navigationData.recentlyReversed)
            {
                return 0.1f; // Very slow after reversing
            }

            return obstacleData.hasEscapeRoute ? 0.3f : 0f;
        }

        private float CalculateNormalForwardSpeed()
        {
            float baseSpeed = 1f;

            // Slow down for sharp turns
            if (Mathf.Abs(navigationData.angleToTarget) > 45f)
            {
                baseSpeed *= 0.7f;
            }

            // Slow down if recently reversed
            if (navigationData.recentlyReversed)
            {
                baseSpeed *= 0.5f;
            }

            return baseSpeed;
        }

        private void ApplyFinalSafetyChecks(ref float forwardAmount, ref float turnAmount)
        {
            // Force stop at destination
            if (navigationData.distanceToFinalDestination < finalDestinationReachedDistance)
            {
                if (navigationData.currentSpeedAbs > 1f)
                {
                    forwardAmount = -1f;
                    carDriver.StopCompletely();
                }
                else
                {
                    forwardAmount = 0f;
                    turnAmount = 0f;
                }
            }

            // Clamp values
            forwardAmount = Mathf.Clamp(forwardAmount, -1f, 1f);
            turnAmount = Mathf.Clamp(turnAmount, -1f, 1f);
        }
        #endregion

        #region Public Interface
        public void SetDestination(Vector3 destination)
        {
            hasReachedDestination = false;
            currentCornerIndex = 1;
            agent.SetDestination(destination);
            ResetAIState();
        }

        public void OnDestinationReached()
        {
            OnDestinationReachedEvent?.Invoke(this);
        }

        [ContextMenu("GoToTarget")]
        public void GoToTarget()
        {
            if (targetTransform != null)
            {
                SetDestination(targetTransform.position);
            }
        }

        private void ResetAIState()
        {
            movementState.isReversing = false;
            movementState.isStuck = false;
            movementState.stuckTimer = 0f;
            movementState.reverseTimer = 0f;
            movementState.lastPosition = transform.position;
        }
        #endregion

        #region Obstacle Detection
        private void CheckSurroundings(out bool top, out bool bottom, out bool left, out bool right)
        {
            Vector3 center = transform.position + new Vector3(0, 0.5f, 0);
            Quaternion rotation = transform.rotation;

            Vector3 topPos = center + transform.forward * obstacleDetectionDistance.x;
            Vector3 bottomPos = center - transform.forward * obstacleDetectionDistance.x;
            Vector3 leftPos = center - transform.right * obstacleDetectionDistance.z;
            Vector3 rightPos = center + transform.right * obstacleDetectionDistance.z;

            top = Physics.OverlapBox(topPos, topBottomDetectorSize / 2, rotation).Any(col => col.gameObject != gameObject);
            bottom = Physics.OverlapBox(bottomPos, topBottomDetectorSize / 2, rotation).Any(col => col.gameObject != gameObject);
            left = Physics.OverlapBox(leftPos, leftRightDetectorSize / 2, rotation).Any(col => col.gameObject != gameObject);
            right = Physics.OverlapBox(rightPos, leftRightDetectorSize / 2, rotation).Any(col => col.gameObject != gameObject);
        }
        #endregion

        #region Debug and Gizmos
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || agent == null) return;

            DrawPathGizmos();
            DrawObstacleDetectionGizmos();
        }

        private void DrawPathGizmos()
        {
            if (agent.path.corners.Length == 0) return;

            DrawPathCorners();
            DrawPathConnections();
            DrawCurrentTarget();
            DrawWaypointRadius();
            DrawDestinationRadius();
        }

        private void DrawPathCorners()
        {
            Gizmos.color = Color.red;
            foreach (Vector3 corner in agent.path.corners)
            {
                Gizmos.DrawWireSphere(corner, gizmoRadius);
            }
        }

        private void DrawPathConnections()
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < agent.path.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(agent.path.corners[i], agent.path.corners[i + 1]);
            }
        }

        private void DrawCurrentTarget()
        {
            if (currentCornerIndex < agent.path.corners.Length)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(currentTargetPosition, gizmoRadius * 1.2f);
            }
        }

        private void DrawWaypointRadius()
        {
            if (currentCornerIndex < agent.path.corners.Length)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
                Gizmos.DrawWireSphere(agent.path.corners[currentCornerIndex], waypointReachedDistance);
            }
        }

        private void DrawDestinationRadius()
        {
            if (!hasReachedDestination)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
                Gizmos.DrawWireSphere(agent.destination, finalDestinationReachedDistance);
            }
        }

        private void DrawObstacleDetectionGizmos()
        {
            Color hitColor = new Color(1f, 0f, 0f, 0.4f);
            Color noHitColor = new Color(0f, 1f, 0f, 0.4f);
            Vector3 center = transform.position + new Vector3(0, 0.5f, 0);

            CheckSurroundings(out bool top, out bool bottom, out bool left, out bool right);

            DrawDetectionBox(center, topBottomDetectorSize, transform.forward, obstacleDetectionDistance.x, top ? hitColor : noHitColor);
            DrawDetectionBox(center, topBottomDetectorSize, -transform.forward, obstacleDetectionDistance.x, bottom ? hitColor : noHitColor);
            DrawDetectionBox(center, leftRightDetectorSize, -transform.right, obstacleDetectionDistance.z, left ? hitColor : noHitColor);
            DrawDetectionBox(center, leftRightDetectorSize, transform.right, obstacleDetectionDistance.z, right ? hitColor : noHitColor);
        }

        private void DrawDetectionBox(Vector3 origin, Vector3 size, Vector3 direction, float distance, Color color)
        {
            Gizmos.color = color;
            Gizmos.matrix = Matrix4x4.TRS(origin, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, size);
            Gizmos.matrix = Matrix4x4.TRS(origin + direction * distance, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, size);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawLine(origin, origin + direction * distance);
        }
#endif
        #endregion
    }
}