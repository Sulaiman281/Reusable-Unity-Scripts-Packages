
namespace WitShells.SplineRuntime
{
    using UnityEngine;
    using UnityEngine.Splines;

    [RequireComponent(typeof(SplineContainer))]
    public abstract class ASplineRuntime : MonoBehaviour
    {
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

        public void UpdateSplinePositionWithChildren()
        {
            var positions = SplineUtils.ContainerChildrenToPositionList(transform, true);
            SplineContainer.Spline = SplineUtils.CreateSplineFromPositionsList(positions);
        }
    }
}