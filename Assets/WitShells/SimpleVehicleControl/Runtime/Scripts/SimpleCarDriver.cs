using UnityEngine;

namespace WitShells.SimpleCarControls
{
    public class SimpleCarDriver : MonoBehaviour
    {

        #region Fields
        [Header("Speed Settings")]
        [SerializeField] private float speedMax = 70f;
        [SerializeField] private float speedMin = -50f;
        [SerializeField] private float acceleration = 30f;
        [SerializeField] private float brakeSpeed = 100f;
        [SerializeField] private float reverseSpeed = 30f;
        [SerializeField] private float idleSlowdown = 10f;

        [Header("Turn Settings")]
        [SerializeField] private float turnSpeedMax = 300f;
        [SerializeField] private float turnSpeedAcceleration = 300f;
        [SerializeField] private float turnIdleSlowdown = 500f;

        [Header("Runtime Values (Read Only)")]
        [SerializeField] private float speed;
        [SerializeField] private float turnSpeed;
        [SerializeField] private float forwardAmount;
        [SerializeField] private float turnAmount;

        [Header("AI Control")]
        [SerializeField] private bool isAIControlled = false; // Prevents auto-steering inversion

        private Rigidbody _rb;
        private Rigidbody carRigidbody
        {
            get
            {
                if (_rb == null)
                {
                    _rb = GetComponent<Rigidbody>();
                }
                return _rb;
            }
        }

        #endregion

        private void Update()
        {
            if (forwardAmount > 0)
            {
                // Accelerating
                speed += forwardAmount * acceleration * Time.deltaTime;
            }
            if (forwardAmount < 0)
            {
                if (speed > 0)
                {
                    // Braking
                    speed += forwardAmount * brakeSpeed * Time.deltaTime;
                }
                else
                {
                    // Reversing
                    speed += forwardAmount * reverseSpeed * Time.deltaTime;
                }
            }

            if (forwardAmount == 0)
            {
                // Not accelerating or braking
                if (speed > 0)
                {
                    speed -= idleSlowdown * Time.deltaTime;
                }
                if (speed < 0)
                {
                    speed += idleSlowdown * Time.deltaTime;
                }
            }

            speed = Mathf.Clamp(speed, speedMin, speedMax);

            carRigidbody.linearVelocity = transform.forward * speed;

            // Store original turnAmount before any modifications
            float originalTurnAmount = turnAmount;

            if (speed < 0 && !isAIControlled)
            {
                // Going backwards, invert wheels (only for manual control)
                turnAmount = turnAmount * -1f;
            }

            if (turnAmount > 0 || turnAmount < 0)
            {
                // Turning
                if ((turnSpeed > 0 && turnAmount < 0) || (turnSpeed < 0 && turnAmount > 0))
                {
                    // Changing turn direction
                    float minTurnAmount = 20f;
                    turnSpeed = turnAmount * minTurnAmount;
                }
                turnSpeed += turnAmount * turnSpeedAcceleration * Time.deltaTime;
            }
            else
            {
                // Not turning
                if (turnSpeed > 0)
                {
                    turnSpeed -= turnIdleSlowdown * Time.deltaTime;
                }
                if (turnSpeed < 0)
                {
                    turnSpeed += turnIdleSlowdown * Time.deltaTime;
                }
                if (turnSpeed > -1f && turnSpeed < +1f)
                {
                    // Stop rotating
                    turnSpeed = 0f;
                }
            }

            float speedNormalized = speed / speedMax;
            float invertSpeedNormalized = Mathf.Clamp(1 - speedNormalized, .75f, 1f);

            turnSpeed = Mathf.Clamp(turnSpeed, -turnSpeedMax, turnSpeedMax);

            carRigidbody.angularVelocity = new Vector3(0, turnSpeed * (invertSpeedNormalized * 1f) * Mathf.Deg2Rad, 0);

            if (transform.eulerAngles.x > 2 || transform.eulerAngles.x < -2 || transform.eulerAngles.z > 2 || transform.eulerAngles.z < -2)
            {
                transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // if (collision.gameObject.layer == GameHandler.SOLID_OBJECTS_LAYER) {
            speed = Mathf.Clamp(speed, 0f, 20f);
            // }
        }

        public void SetInputs(float forwardAmount, float turnAmount)
        {
            this.forwardAmount = forwardAmount;
            this.turnAmount = turnAmount;
        }

        public void SetAIControlled(bool aiControlled)
        {
            this.isAIControlled = aiControlled;
        }

        public void ClearTurnSpeed()
        {
            turnSpeed = 0f;
        }

        public float GetSpeed()
        {
            return speed;
        }

        public float GetSpeedMax()
        {
            return speedMax;
        }

        public void SetSpeedMax(float speedMax)
        {
            this.speedMax = speedMax;
        }

        public void SetTurnSpeedMax(float turnSpeedMax)
        {
            this.turnSpeedMax = turnSpeedMax;
        }

        public void SetTurnSpeedAcceleration(float turnSpeedAcceleration)
        {
            this.turnSpeedAcceleration = turnSpeedAcceleration;
        }

        public void StopCompletely()
        {
            speed = 0f;
            turnSpeed = 0f;
            forwardAmount = 0f;
            turnAmount = 0f;
        }

    }
}