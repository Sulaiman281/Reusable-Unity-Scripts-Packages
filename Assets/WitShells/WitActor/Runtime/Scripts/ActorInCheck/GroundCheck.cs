namespace WitShells.WitActor
{
    using UnityEngine;

    [RequireComponent(typeof(Animator))]
    public class GroundCheck : MonoBehaviour
    {
        public float checkRadius = 0.2f; // Radius of the sphere check

        private Animator animator;

        void Start()
        {
            animator = GetComponent<Animator>();
        }

        void Update()
        {
            bool isGrounded = Physics.CheckSphere(transform.position, checkRadius);
            animator.SetBool("OnGround", isGrounded);
        }

#if UNITY_EDITOR

        // Optional: visualize the check in the editor
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, checkRadius);
        }
#endif

    }
}