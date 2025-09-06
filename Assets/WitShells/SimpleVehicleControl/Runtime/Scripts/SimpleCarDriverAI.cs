using System.Linq;
using System.Collections.Generic;
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
        [SerializeField] private float pathRecalculationTime = 1f;
        [SerializeField] private float checkpointSkipAngleThreshold = 120f;
        [SerializeField] private float checkpointSkipProximityThreshold = 15f;

        [Header("Collision Detection")]
        [SerializeField] private Vector3 obstacleDetectionDistance = new Vector3(3, 0, 1.65f);
        [SerializeField] private Vector3 topBottomDetectorSize = new Vector3(1.5f, .5f, 1f);
        [SerializeField] private Vector3 leftRightDetectorSize = new Vector3(1f, .5f, 1f);
        [SerializeField] private float trafficDetectionRadius = 15f;
        [SerializeField] private LayerMask vehicleLayerMask;

        [Header("Traffic Management")]
        [SerializeField] private float carAvoidanceDistance = 5f;
        [SerializeField] private float slowForVehicleDistance = 10f;
        [SerializeField] private float vehicleDetectionAngle = 60f;
        [SerializeField] private bool enableDynamicPathfinding = true;
        [SerializeField] private float vehicleAvoidanceWeight = 2.5f;
        [SerializeField] private bool enableCheckpointSkipping = true;
        [SerializeField] private float minimumDistanceToResetPath = 20f;

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
        [SerializeField] private bool showTrafficGizmos = true;

        [Header("AI Target Test")]
        [SerializeField] private Transform targetTransform;
        #endregion

        #region Private Fields
        [SerializeField] private NavMeshAgent agent;
        private SimpleCarDriver carDriver;
        private Vector3 currentTargetPosition;
        private int currentCornerIndex = 1;
        private bool hasReachedDestination = true;
        private float pathRecalculationTimer = 0f;
        private Vector3 lastDestination;
        private Vector3 originalDestination;
        private SimpleCarDriverAI[] nearbyVehicles = new SimpleCarDriverAI[0];
        private bool isAvoidingOtherVehicle = false;
        private Vector3 vehicleAvoidanceDirection = Vector3.zero;
        private float nearbyTrafficDensity = 0f;

        // Movement state tracking
        private AIMovementState movementState = new AIMovementState();
        private ObstacleDetectionData obstacleData = new ObstacleDetectionData();
        private NavigationData navigationData = new NavigationData();
        private TrafficData trafficData = new TrafficData();
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
            public bool allowCheckpointSkip = false;
            public Vector3 skippedCheckpointPos;
            public bool hasSkippedCheckpoint = false;
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
            public bool shouldSkipCurrentWaypoint;
        }

        private struct TrafficData
        {
            public SimpleCarDriverAI closestVehicle;
            public float closestVehicleDistance;
            public bool isVehicleAhead;
            public bool isVehicleOnSide;
            public Vector3 avoidanceDirection;
            public bool needsToGiveWay;
            public int vehicleCount;
            public bool hasVehicleInPath;
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
            DetectTraffic();
            UpdatePathNavigation();
            ExecuteMovementLogic();
            UpdatePathRecalculation();
        }
        #endregion

        #region Initialization
        private void InitializeComponents()
        {
            carDriver = GetComponent<SimpleCarDriver>();
            agent = GetComponent<NavMeshAgent>();
            agent.enabled = true;
            movementState.lastPosition = transform.position;

            // Set vehicle layer if not set
            if (vehicleLayerMask == 0)
            {
                vehicleLayerMask = LayerMask.GetMask("Default");
            }
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

        private void UpdatePathRecalculation()
        {
            if (!enableDynamicPathfinding) return;

            pathRecalculationTimer -= Time.deltaTime;
            if (pathRecalculationTimer <= 0)
            {
                pathRecalculationTimer = pathRecalculationTime;

                // Only recalculate if there's significant traffic or we're stuck
                if ((trafficData.vehicleCount > 0 && trafficData.hasVehicleInPath) ||
                    movementState.isStuck)
                {
                    RecalculatePath();
                }

                // If we've deviated too far from our original destination, reset the path
                if (movementState.hasSkippedCheckpoint &&
                    Vector3.Distance(transform.position, movementState.skippedCheckpointPos) > minimumDistanceToResetPath)
                {
                    ResetToOriginalDestination();
                }
            }
        }

        private void RecalculatePath()
        {
            if (agent.isPathStale || agent.pathPending) return;

            // Random slight offset to avoid all cars making the same decision
            Vector3 randomOffset = new Vector3(
                Random.Range(-2f, 2f),
                0,
                Random.Range(-2f, 2f)
            );

            Vector3 targetPos = agent.destination + randomOffset;

            // Store the last destination to detect changes
            lastDestination = agent.destination;

            // Try to find a new path
            agent.SetDestination(targetPos);
        }

        private void ResetToOriginalDestination()
        {
            if (originalDestination != Vector3.zero &&
                Vector3.Distance(agent.destination, originalDestination) > 1f)
            {
                agent.SetDestination(originalDestination);
                movementState.hasSkippedCheckpoint = false;
            }
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

        #region Traffic Detection and Management
        private void DetectTraffic()
        {
            // Find nearby vehicles
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, trafficDetectionRadius, vehicleLayerMask);

            List<SimpleCarDriverAI> vehicles = new List<SimpleCarDriverAI>();
            SimpleCarDriverAI closestVehicle = null;
            float closestDistance = float.MaxValue;
            bool vehicleAhead = false;
            bool vehicleOnSide = false;
            Vector3 avoidDir = Vector3.zero;
            bool giveWay = false;
            bool vehicleInPath = false;

            foreach (var collider in hitColliders)
            {
                // Skip this vehicle
                if (collider.gameObject == gameObject) continue;

                // Check if it's a vehicle
                SimpleCarDriverAI otherVehicle = collider.GetComponent<SimpleCarDriverAI>();
                if (otherVehicle == null) continue;

                vehicles.Add(otherVehicle);

                Vector3 dirToVehicle = otherVehicle.transform.position - transform.position;
                float distToVehicle = dirToVehicle.magnitude;

                // Track closest vehicle
                if (distToVehicle < closestDistance)
                {
                    closestVehicle = otherVehicle;
                    closestDistance = distToVehicle;
                }

                // Check if vehicle is ahead
                float angleToVehicle = Vector3.Angle(transform.forward, dirToVehicle);
                if (angleToVehicle < vehicleDetectionAngle)
                {
                    vehicleAhead = true;

                    // Calculate avoidance direction (perpendicular to direction to other vehicle)
                    Vector3 perp = Vector3.Cross(dirToVehicle.normalized, Vector3.up);

                    // Choose the perpendicular direction that's closer to our target
                    float leftDot = Vector3.Dot(perp, navigationData.directionToTarget);
                    float rightDot = Vector3.Dot(-perp, navigationData.directionToTarget);

                    Vector3 bestPerp = (leftDot > rightDot) ? perp : -perp;
                    avoidDir += bestPerp;

                    // Check if vehicle is directly in our path
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position, transform.forward, out hit,
                                       carAvoidanceDistance * 2, vehicleLayerMask))
                    {
                        if (hit.collider.gameObject == otherVehicle.gameObject)
                        {
                            vehicleInPath = true;
                        }
                    }
                }

                // Check if vehicle is on side (for intersection priority)
                if (angleToVehicle > 45f && angleToVehicle < 135f && distToVehicle < carAvoidanceDistance * 1.5f)
                {
                    vehicleOnSide = true;

                    // Give way to vehicles coming from the right (can be customized based on your traffic rules)
                    float rightSidedness = Vector3.Dot(transform.right, dirToVehicle.normalized);
                    if (rightSidedness > 0.5f)
                    {
                        giveWay = true;
                    }
                }
            }

            // Normalize avoidance direction
            if (avoidDir.magnitude > 0.01f)
            {
                avoidDir.Normalize();
            }

            // Update traffic data
            trafficData.closestVehicle = closestVehicle;
            trafficData.closestVehicleDistance = closestDistance;
            trafficData.isVehicleAhead = vehicleAhead;
            trafficData.isVehicleOnSide = vehicleOnSide;
            trafficData.avoidanceDirection = avoidDir;
            trafficData.needsToGiveWay = giveWay;
            trafficData.vehicleCount = vehicles.Count;
            trafficData.hasVehicleInPath = vehicleInPath;

            // Store for other methods to use
            nearbyVehicles = vehicles.ToArray();
            nearbyTrafficDensity = Mathf.Clamp01((float)vehicles.Count / 5f); // Normalize density

            // Set flag for vehicle avoidance
            isAvoidingOtherVehicle = vehicleAhead && closestDistance < carAvoidanceDistance;
            vehicleAvoidanceDirection = avoidDir;
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

            // Check if we should skip waypoints (like when dragged by another car)
            if (enableCheckpointSkipping && ShouldSkipCurrentWaypoint())
            {
                SkipToNextRelevantWaypoint();
            }
            else
            {
                AdvanceToNextWaypoint();
            }
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
                if (agent.enabled)
                {
                    agent.ResetPath();
                }
                carDriver.StopCompletely();
            }
        }

        private bool ShouldSkipCurrentWaypoint()
        {
            // Skip if we're far ahead of current checkpoint
            if (currentCornerIndex < agent.path.corners.Length - 1)
            {
                Vector3 currentWaypoint = agent.path.corners[currentCornerIndex];
                Vector3 nextWaypoint = agent.path.corners[currentCornerIndex + 1];

                // Calculate angle between current-to-waypoint and current-to-next
                Vector3 toCurrentWaypoint = currentWaypoint - transform.position;
                Vector3 toNextWaypoint = nextWaypoint - transform.position;

                float angleBetween = Vector3.Angle(toCurrentWaypoint, toNextWaypoint);

                // If we're going in opposite direction to reach current waypoint (likely we've been pushed past it)
                if (angleBetween > checkpointSkipAngleThreshold &&
                    toCurrentWaypoint.magnitude > checkpointSkipProximityThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        private void SkipToNextRelevantWaypoint()
        {
            if (!movementState.hasSkippedCheckpoint)
            {
                // Record the skipped checkpoint to potentially return to it later
                movementState.skippedCheckpointPos = agent.path.corners[currentCornerIndex];
                movementState.hasSkippedCheckpoint = true;
            }

            // Find the first waypoint ahead of us
            for (int i = currentCornerIndex; i < agent.path.corners.Length; i++)
            {
                Vector3 waypointDir = agent.path.corners[i] - transform.position;
                float angleToWaypoint = Vector3.Angle(transform.forward, waypointDir);

                // If this waypoint is ahead of us, use it
                if (angleToWaypoint < 90f)
                {
                    currentCornerIndex = i;
                    return;
                }
            }

            // If we didn't find any, just advance by one
            currentCornerIndex++;
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
                   (obstacleData.frontBlocked || trafficData.isVehicleAhead && trafficData.closestVehicleDistance < carAvoidanceDistance * 0.5f);
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
            float steering = 0f;

            // Handle vehicle avoidance steering
            if (isAvoidingOtherVehicle)
            {
                float avoidanceStrength = 1f - Mathf.Clamp01(trafficData.closestVehicleDistance / carAvoidanceDistance);
                steering = Vector3.Dot(vehicleAvoidanceDirection, transform.right) * vehicleAvoidanceWeight * avoidanceStrength;

                // Apply constraints based on obstacles
                if ((steering < 0 && obstacleData.leftBlocked) || (steering > 0 && obstacleData.rightBlocked))
                {
                    steering *= 0.2f; // Reduce steering if obstacle in that direction
                }

                return Mathf.Clamp(steering, -1f, 1f);
            }

            // Normal navigation logic
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
            // Priority 1: Emergency braking
            if (ShouldEmergencyBrake())
            {
                return -1f;
            }

            // Priority 2: Traffic rules
            if (trafficData.needsToGiveWay && trafficData.closestVehicleDistance < slowForVehicleDistance)
            {
                return Mathf.Lerp(-0.5f, 0.1f, trafficData.closestVehicleDistance / slowForVehicleDistance);
            }

            // Priority 3: Vehicle ahead
            if (trafficData.isVehicleAhead && trafficData.closestVehicleDistance < slowForVehicleDistance)
            {
                // Progressive slowing based on distance
                float speedFactor = Mathf.Clamp01(trafficData.closestVehicleDistance / slowForVehicleDistance);

                // Stop completely if very close
                if (trafficData.closestVehicleDistance < carAvoidanceDistance * 0.5f)
                {
                    return -0.5f;
                }

                // Slow down proportionally to distance
                return Mathf.Lerp(0.1f, 0.7f, speedFactor);
            }

            // Priority 4: Destination approach
            if (IsApproachingDestination())
            {
                return CalculateDestinationApproachSpeed();
            }

            // Priority 5: Turning behavior
            if (ShouldSlowForTurning())
            {
                return 0.6f;
            }

            // Priority 6: Obstacle handling
            if (obstacleData.frontBlocked)
            {
                return CalculateObstacleNavigationSpeed();
            }

            // Normal driving
            return CalculateNormalForwardSpeed();
        }

        private bool ShouldEmergencyBrake()
        {
            bool obstacleAtSpeed = obstacleData.frontBlocked &&
                                  navigationData.currentSpeedAbs > 5f &&
                                  !navigationData.recentlyReversed;

            bool destinationEmergency = navigationData.distanceToFinalDestination < emergencyBrakeDistance &&
                                       navigationData.currentSpeedAbs > 3f;

            bool vehicleEmergency = trafficData.isVehicleAhead &&
                                   trafficData.closestVehicleDistance < carAvoidanceDistance * 0.3f &&
                                   navigationData.currentSpeedAbs > 3f;

            return obstacleAtSpeed || destinationEmergency || vehicleEmergency;
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

            // Slow down in dense traffic
            baseSpeed *= Mathf.Lerp(1f, 0.7f, nearbyTrafficDensity);

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
            agent.enabled = true;
            agent.SetDestination(destination);
            originalDestination = destination; // Store original destination
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
            movementState.hasSkippedCheckpoint = false;
            pathRecalculationTimer = 0f;
        }

        public void Stop()
        {
            carDriver.StopCompletely();
            agent.enabled = false;
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

            if (showTrafficGizmos)
            {
                DrawTrafficGizmos();
            }
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

            // Calculate positions for each detection box
            Vector3 topPos = center + transform.forward * obstacleDetectionDistance.x;
            Vector3 bottomPos = center - transform.forward * obstacleDetectionDistance.x;
            Vector3 leftPos = center - transform.right * obstacleDetectionDistance.z;
            Vector3 rightPos = center + transform.right * obstacleDetectionDistance.z;

            // Draw the four detection boxes at their correct positions
            Gizmos.color = top ? hitColor : noHitColor;
            Gizmos.matrix = Matrix4x4.TRS(topPos, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, topBottomDetectorSize);

            Gizmos.color = bottom ? hitColor : noHitColor;
            Gizmos.matrix = Matrix4x4.TRS(bottomPos, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, topBottomDetectorSize);

            Gizmos.color = left ? hitColor : noHitColor;
            Gizmos.matrix = Matrix4x4.TRS(leftPos, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, leftRightDetectorSize);

            Gizmos.color = right ? hitColor : noHitColor;
            Gizmos.matrix = Matrix4x4.TRS(rightPos, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, leftRightDetectorSize);

            // Reset matrix
            Gizmos.matrix = Matrix4x4.identity;

            // Draw lines from center to each detection box (optional)
            Gizmos.color = Color.white;
            Gizmos.DrawLine(center, topPos);
            Gizmos.DrawLine(center, bottomPos);
            Gizmos.DrawLine(center, leftPos);
            Gizmos.DrawLine(center, rightPos);
        }

        private void DrawTrafficGizmos()
        {
            // Draw traffic detection radius
            Gizmos.color = new Color(0.3f, 0.3f, 1f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, trafficDetectionRadius);

            // Draw vehicle avoidance area
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, carAvoidanceDistance);

            // Draw slow down area
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, slowForVehicleDistance);

            // Draw detected vehicles
            if (nearbyVehicles != null)
            {
                foreach (var vehicle in nearbyVehicles)
                {
                    if (vehicle == null) continue;

                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(transform.position + Vector3.up * 0.5f,
                                   vehicle.transform.position + Vector3.up * 0.5f);
                }
            }

            // Draw avoidance direction
            if (isAvoidingOtherVehicle)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(transform.position + Vector3.up, vehicleAvoidanceDirection * 5f);
            }

            // Draw skipped checkpoint
            if (movementState.hasSkippedCheckpoint)
            {
                Gizmos.color = new Color(1f, 0.5f, 1f, 0.8f);
                Gizmos.DrawWireSphere(movementState.skippedCheckpointPos, 2f);
                Gizmos.DrawLine(transform.position, movementState.skippedCheckpointPos);
            }
        }
#endif
        #endregion
    }
}