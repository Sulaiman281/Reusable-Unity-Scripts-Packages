namespace WitShells.WitActor
{
    using UnityEngine;

    public class ActorRigBody : MonoBehaviour
    {
        [Header("Look At Settings")]
        public Transform targetLookAt;
        public float minLookAtDistance = 0.5f;
        public float maxLookAtDistance = 10f;

        [Header("Feet Place Settings")]
        [SerializeField] private Vector3 feetOffSet = new Vector3(0, .1f, 0);

        [SerializeField] private IKRigReferences rigRefs;

#if UNITY_EDITOR

        void OnValidate()
        {
            rigRefs = GetComponentInChildren<IKRigReferences>();
        }
#endif

        void Update()
        {
            UpdateLookAtAim();
        }

        public void LateUpdate()
        {
            KeepFeetOnGround(rigRefs.LeftFeet, rigRefs.LeftLegTarget);
            KeepFeetOnGround(rigRefs.RightFeet, rigRefs.RightLegTarget);
        }

        private void UpdateLookAtAim()
        {
            if (rigRefs == null || rigRefs.Head == null || targetLookAt == null)
                return;

            var distance = Vector3DistanceEY(transform.position, targetLookAt.position);
            if (distance > minLookAtDistance && distance < maxLookAtDistance)
            {
                rigRefs.HeadWeight = Mathf.Clamp01(1 - (distance - minLookAtDistance) / (maxLookAtDistance - minLookAtDistance));
                rigRefs.Head.transform.position = targetLookAt.position;
            }
            else
            {
                rigRefs.HeadWeight = 0f; // Disable look at if out of range
            }
        }

        public float Vector3DistanceEY(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        public void KeepFeetOnGround(Transform source, Transform target)
        {
            Ray ray = new Ray(source.position + Vector3.up * .5f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 2f))
            {
                target.position = hit.point + feetOffSet;
            }
        }
    }
}