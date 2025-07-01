namespace WitShells.ThirdPersonControl
{
    using Unity.Cinemachine;
    using UnityEngine;

    public class CinemachineCamLookInput : MonoBehaviour
    {
        [Header("Input Settings")]
        public Vector2 lookInput;

        [Header("Look Settings")]
        [SerializeField] private float lookSpeed = 2.0f;
        [SerializeField] private float sensitivity = 1.0f;

        [Header("Pitch Clamp")]
        [SerializeField] private float minPitch = -30f; // Down
        [SerializeField] private float maxPitch = 70f;  // Up

        [Header("References")]
        [SerializeField] private CinemachineCamera cinemachineCamera;


        public Vector2 LookInput
        {
            get => lookInput;
            set { lookInput = value; }
        }


        public CinemachineCamera CinemachineCamera
        {
            get
            {
                if (cinemachineCamera == null)
                    cinemachineCamera = GetComponent<CinemachineCamera>();
                return cinemachineCamera;
            }
        }

        public Transform Target => CinemachineCamera.Target.TrackingTarget;

        private float _currentYaw;
        private float _currentPitch;

        private void Start()
        {
            if (Target != null)
            {
                Vector3 angles = Target.eulerAngles;
                _currentYaw = angles.y;
                _currentPitch = angles.x;
            }
        }

        private void LateUpdate()
        {
            if (Target == null) return;

            // Accumulate yaw and pitch based on look input
            _currentYaw += lookInput.x * lookSpeed * sensitivity * Time.deltaTime;
            _currentPitch -= lookInput.y * lookSpeed * sensitivity * Time.deltaTime; // Invert Y for typical controls

            // Clamp pitch
            _currentPitch = Mathf.Clamp(_currentPitch, minPitch, maxPitch);

            // Apply rotation (pitch X, yaw Y, roll Z=0)
            Target.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
        }

    }
}