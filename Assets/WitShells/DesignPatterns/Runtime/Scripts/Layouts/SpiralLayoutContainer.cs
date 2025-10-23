using System.Collections.Generic;
using UnityEngine;
using WitShells.DesignPatterns.Core;

namespace WitShells.DesignPatterns
{
    public enum SpiralDirection
    {
        Clockwise,
        CounterClockwise
    }

    public class SpiralLayoutContainer : MonoBehaviour
    {
        [Header("Spiral Layout Settings")]
        [SerializeField] protected SpiralDirection spiralDirection = SpiralDirection.Clockwise;
        [SerializeField] protected float spacing = 1.0f;
        [SerializeField] protected GameObject spiralNodePrefab;
        [SerializeField] protected float spiralGrowthRate = 0.1f;
        [SerializeField] protected int maxCount = 10;
        [SerializeField] protected bool canvasSpace = false;

        protected ISpiralNode head;
        protected ISpiralNode tail;
        protected List<ISpiralNode> nodes = new List<ISpiralNode>();


        private ObjectPool<GameObject> _nodePool;

        public ObjectPool<GameObject> NodePool => _nodePool ??= new ObjectPool<GameObject>(() =>
        {
            var obj = Instantiate(spiralNodePrefab, transform);
            return obj;
        });

        public ISpiralNode Head => head;
        public ISpiralNode Tail => tail;

        protected virtual void Awake()
        {
            head = null;
            tail = null;
        }

        public virtual void GenerateSpiralLayout(int count)
        {
            count = Mathf.Min(count, maxCount);

            if (count <= 0 || spiralNodePrefab == null)
            {
                return;
            }

            ClearLayout();

            CreateFirstNode();

            for (int i = 1; i < count; i++)
            {
                AddNodeToSpiral();
            }

            ConnectNodes();
        }

        public void SetHeadCoordinate(Vector2Int coordinate)
        {
            if (head != null)
            {
                head.Coordinate = coordinate;

                var node = head;
                while (node.Next != null)
                {
                    coordinate = node.Coordinate;
                    node = node.Next;
                    node.UpdateCoordinate(CalculateCoordinateOffset(node.Index) + coordinate);
                }
            }
        }

        protected virtual void CreateFirstNode()
        {
            GameObject nodeObj = NodePool.Get();
            nodeObj.transform.localPosition = Vector3.zero;

            ISpiralNode node = nodeObj.GetComponent<ISpiralNode>();
            if (node == null)
            {
                Debug.LogError("Spiral node prefab does not implement ISpiralNode interface!");
                return;
            }

            node.Initialize(Vector3.zero, 0);
            head = node;
            tail = node;
            nodes.Add(node);
        }

        protected virtual void AddNodeToSpiral()
        {
            int index = nodes.Count;
            Vector3 position = CalculateSpiralPosition(index);

            GameObject nodeObj = NodePool.Get();
            nodeObj.transform.localPosition = position;

            ISpiralNode node = nodeObj.GetComponent<ISpiralNode>();
            if (node == null)
            {
                Debug.LogError("Spiral node prefab does not implement ISpiralNode interface!");
                return;
            }

            node.Initialize(position, index);
            tail.Next = node;
            node.Previous = tail;
            tail = node;
            nodes.Add(node);
        }

        protected virtual Vector3 CalculateSpiralPosition(int index)
        {
            // First node is always at center
            if (index == 0)
                return Vector3.zero;

            // Generate spiral positions using square spiral pattern
            int layer = 1;
            int positionCount = 1; // Start at 1 since index 0 is center

            while (true)
            {
                int perimeter = layer * 8; // Number of positions in this layer

                if (positionCount + perimeter > index)
                {
                    // Our target index is in this layer
                    int posInLayer = index - positionCount;
                    int side = posInLayer / (layer * 2); // Which side of the square (0-3)
                    int posInSide = posInLayer % (layer * 2); // Position within that side

                    // Calculate integer positions first
                    int xInt = 0, yInt = 0;

                    switch (side)
                    {
                        case 0: // Right side
                            xInt = layer;
                            yInt = posInSide - layer;
                            break;
                        case 1: // Top side
                            xInt = layer - posInSide;
                            yInt = layer;
                            break;
                        case 2: // Left side
                            xInt = -layer;
                            yInt = layer - posInSide;
                            break;
                        case 3: // Bottom side
                            xInt = -layer + posInSide;
                            yInt = -layer;
                            break;
                    }

                    // Convert to float and apply spacing
                    float x = xInt * spacing;
                    float y = yInt * spacing;

                    // Adjust direction if counter-clockwise
                    if (spiralDirection == SpiralDirection.CounterClockwise)
                        y = -y;

                    // Return based on space type (Canvas vs World)
                    return canvasSpace ? new Vector3(x, y, 0) : new Vector3(x, 0, y);
                }

                positionCount += perimeter;
                layer++;
            }
        }

        protected virtual void ConnectNodes()
        {
            if (nodes.Count <= 1) return;

            // tail.Next = head;
            // head.Previous = tail;
        }

        protected virtual void ClearLayout()
        {
            foreach (var node in nodes)
            {
                node.GameObject.SetActive(false);
                NodePool.Release(node.GameObject);
            }
            nodes.Clear();
            head = null;
            tail = null;
        }

        private Vector2Int CalculateCoordinateOffset(int index)
        {
            // First node (index 0) is at center
            if (index == 0) return Vector2Int.zero;

            // Find which layer this index is in
            int layer = 1;
            int positionCount = 1;

            while (true)
            {
                int perimeter = layer * 8;

                if (positionCount + perimeter > index)
                {
                    // Our target index is in this layer
                    int posInLayer = index - positionCount;
                    int side = posInLayer / (layer * 2); // Which side of the square (0-3)
                    int posInSide = posInLayer % (layer * 2); // Position within that side

                    int x = 0, y = 0;

                    switch (side)
                    {
                        case 0: // Right side
                            x = layer;
                            y = posInSide - layer;
                            break;
                        case 1: // Top side
                            x = layer - posInSide;
                            y = layer;
                            break;
                        case 2: // Left side
                            x = -layer;
                            y = layer - posInSide;
                            break;
                        case 3: // Bottom side
                            x = -layer + posInSide;
                            y = -layer;
                            break;
                    }

                    // Adjust direction if counter-clockwise
                    if (spiralDirection == SpiralDirection.CounterClockwise)
                        y = -y;

                    return new Vector2Int(x, y);
                }

                positionCount += perimeter;
                layer++;
            }
        }

        public IEnumerable<Vector2Int> GenerateSpiral(Vector2Int center, int totalTiles)
        {
            yield return center;

            // Directions in order: Right, Up, Left, Down
            Vector2Int[] directions = new Vector2Int[]
            {
                new(1, 0),   // Right
                new(0, 1),   // Up
                new(-1, 0),  // Left
                new(0, -1)   // Down
            };

            int stepSize = 1;   // how many tiles to move in current direction
            int dirIndex = 0;   // which direction to move
            int count = 1;      // already added the center

            Vector2Int currentPos = center;

            while (count < totalTiles)
            {
                // We increase step size every two direction changes
                for (int repeat = 0; repeat < 2; repeat++)
                {
                    for (int step = 0; step < stepSize; step++)
                    {
                        currentPos += directions[dirIndex];

                        yield return currentPos;
                        count++;

                        if (count >= totalTiles)
                            yield break;
                    }

                    // Change direction clockwise
                    dirIndex = (dirIndex + 1) % 4;
                }

                stepSize++; // Expand outward
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Test Generate Spiral Layout")]
        private void TestGenerateSpiralLayout()
        {
            GenerateSpiralLayout(maxCount);
        }
#endif
    }
}