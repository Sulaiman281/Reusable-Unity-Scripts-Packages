using UnityEngine;

namespace WitShells.WitActor
{
    public interface IDestination
    {
        void SetDestination(Vector3 destination);
        void OnDestinationReached();
    }
}