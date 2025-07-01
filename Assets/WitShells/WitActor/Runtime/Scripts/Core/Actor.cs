namespace WitShells.WitActor
{
    using UnityEngine;
    using UnityEngine.AI;

    [RequireComponent(typeof(NavMeshAgent))]
    public class Actor : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private ActorRigBody rigBody;
        [SerializeField] private Animator animator;
    }
}