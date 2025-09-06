namespace WitShells.SplineRuntime
{
    using UnityEngine;
    using UnityEngine.Splines;
    using WitShells.DesignPatterns.Core;

    [RequireComponent(typeof(SplineContainer))]
    public class SplinePathCreator : ASplineRuntime
    {
        [Header("Reference")]
        [SerializeField] private GameObject nodePrefab;

        [Header("Settings")]
        [SerializeField] private LayerMask LayerMask = ~0;
        [SerializeField] private bool updateNodes = false;
        [SerializeField] private float spacing = 1f;
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;

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

        private void Update()
        {
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
    }
}