namespace WitShells.SplineRuntime
{
    using UnityEngine;
    using UnityEngine.Splines;
    using WitShells.DesignPatterns.Core;

    [RequireComponent(typeof(SplineContainer))]
    public class SplinePathCreator : ASplineRuntime
    {
        [Header("Reference")]
        public GameObject nodePrefab;

        [Header("Settings")]
        public LayerMask LayerMask = ~0;
        public bool updateNodes = false;
        public float spacing = 1f;
        public Vector3 rotationOffset = Vector3.zero;

        [Header("Runtime")]
        [SerializeField] private GameObject spawnObject;

        private ObjectPool<GameObject> nodePool;

        public ObjectPool<GameObject> NodePool
        {
            get
            {
                if (nodePool == null)
                    nodePool = new ObjectPool<GameObject>(() => Instantiate(nodePrefab));
                return nodePool;
            }
        }

        public GameObject SpawnObject
        {
            get
            {
                if (spawnObject == null)
                    spawnObject = new GameObject("SpawnContainer");
                return spawnObject;
            }
        }

        public override void Update()
        {
            base.Update();
            if (SplineContainer == null) return;
            if (SplineContainer.Spline == null) return;
            if (SplineContainer.Spline.Count == 0) return;

            if (updateNodes)
                UpdateSplinePositions();
        }

        private void UpdateSplinePositions()
        {
            UpdateSplinePositionWithChildren();
            SplineUtils
                .SpawnObjectsAlongSplineAdvanced(splineContainer, NodePool, SpawnObject.transform, LayerMask,
                     true, true, 0, spacing, rotationOffset);
        }

        public void ClearNodes()
        {
            nodePool = null;
        }
    }
}