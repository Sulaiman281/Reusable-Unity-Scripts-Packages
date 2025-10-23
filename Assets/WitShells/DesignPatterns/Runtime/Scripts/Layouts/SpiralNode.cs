using UnityEngine;
using WitShells.DesignPatterns.Core;

namespace WitShells.DesignPatterns
{
    public interface ISpiralNode
    {
        ISpiralNode Next { get; set; }
        ISpiralNode Previous { get; set; }
        void Initialize(Vector3 position, int index);
        GameObject GameObject { get; }
        int Index { get; }
        Vector2Int Coordinate { get; set; }

        void UpdateCoordinate(Vector2Int coordinate);
    }

    public abstract class SpiralNode<T> : MonoBehaviour, ISpiralNode
    {
        public ISpiralNode Next { get; set; }
        public ISpiralNode Previous { get; set; }
        public GameObject GameObject => gameObject;
        public int Index { get; private set; }
        public Vector2Int Coordinate { get; set; }

        public T Data { get; set; }

        public void Initialize(Vector3 position, int index)
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            transform.localPosition = position;
            Index = index;
            Next = null;
            Previous = null;

            gameObject.name = $"SpiralNode_{index}_({position.x},{position.y},{position.z})";
        }

        public abstract void UpdateNode(T data);

        public virtual void UpdateCoordinate(Vector2Int coordinate)
        {
            Coordinate = coordinate;
        }
    }
}