namespace WitShells.ThirdPersonControl
{
    using UnityEngine;

    public class ThirdPersonControl : MonoBehaviour
    {
        [Header("Input Settings")]
        public Vector2 direction;
        public bool jump;
        public bool crouch;
        public bool sprint;

        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private float runSpeed = 5f;
        [SerializeField] private float crouchSpeed = 1f;
        [SerializeField] private float jumpForce = 3.5f; // Lowered for less "fast" jump

        [Header("Ground Check Settings")]
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask groundLayers;

        [Header("References")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform cameraTarget; // Camera target for movement direction

        private bool _isGrounded;
        private bool _wasGrounded;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private float _jumpTimeout = 0.2f;
        private float _jumpTimeoutDelta;

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

        // Input Getter/Setter
        public Vector2 Direction
        {
            get => direction;
            set => direction = value;
        }

        public bool Jump
        {
            get => jump;
            set => jump = value;
        }

        public bool Crouch
        {
            get => crouch;
            set => crouch = value;
        }

        public bool Sprint
        {
            get => sprint;
            set => sprint = value;
        }

        public Transform CameraTarget
        {
            get => cameraTarget;
            set => cameraTarget = value;
        }

        private void Awake()
        {
            _jumpTimeoutDelta = _jumpTimeout;
        }

        private void FixedUpdate()
        {
            UpdateMovement();
            _wasGrounded = _isGrounded;
        }

        private void UpdateMovement()
        {
            GroundCheck();
            GravityUpdate();

            // Use cameraTarget's forward/right for movement direction
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

            float speed = sprint ? runSpeed : crouch ? crouchSpeed : walkSpeed;
            Vector3 moveDirection = move * speed;

            // Rotate character to face movement direction if moving
            if (move.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.15f);
            }

            // Jump logic
            if (jump && _isGrounded && _jumpTimeoutDelta <= 0.0f)
            {
                _verticalVelocity = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
                Animator.SetTrigger("Jump");
                jump = false;
            }

            moveDirection.y = _verticalVelocity;

            if (direction.magnitude > 0.01f)
            {
                Animator.SetFloat("Speed", sprint ? 1.0f : 0.5f);
            }
            else
            {
                Animator.SetFloat("Speed", 0.0f);
            }

            CharacterController.Move(moveDirection * Time.deltaTime);
        }

        private void GravityUpdate()
        {
            if (_isGrounded)
            {
                if (!_wasGrounded)
                {
                    _verticalVelocity = -2f;
                }
                if (_jumpTimeoutDelta >= 0.0f)
                    _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTimeoutDelta = _jumpTimeout;
                if (_verticalVelocity < _terminalVelocity)
                    _verticalVelocity += Physics.gravity.y * Time.deltaTime;
            }
        }

        private void GroundCheck()
        {
            _isGrounded = Physics.CheckSphere(transform.position, groundCheckDistance, groundLayers, QueryTriggerInteraction.Ignore);
            Animator.SetBool("OnGround", _isGrounded);
        }
    }
}