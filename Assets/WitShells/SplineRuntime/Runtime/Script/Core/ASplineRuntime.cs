
namespace WitShells.SplineRuntime
{
    using UnityEngine;
    using UnityEngine.Splines;

    [RequireComponent(typeof(SplineContainer))]
    public abstract class ASplineRuntime : MonoBehaviour
    {
        [Header("Update Position Settings")]
        [SerializeField] private float updateInterval = 0.1f;
        [SerializeField] private bool updateWithChildren = true;

        private float _lastUpdateTime = 0f;

        protected SplineContainer splineContainer;

        public SplineContainer SplineContainer
        {
            get
            {
                if (splineContainer == null)
                    splineContainer = GetComponent<SplineContainer>();
                return splineContainer;
            }
        }

        public virtual void Update()
        {
            if (updateWithChildren)
            {
                if (Time.time - _lastUpdateTime > updateInterval)
                {
                    UpdateSplinePositionWithChildren();
                    _lastUpdateTime = Time.time;
                }
            }
        }


        public void UpdateSplinePositionWithChildren()
        {
            var positions = SplineUtils.ContainerChildrenToPositionList(transform, true);
            SplineContainer.Spline = SplineUtils.CreateSplineFromPositionsList(positions);
        }
    }
}