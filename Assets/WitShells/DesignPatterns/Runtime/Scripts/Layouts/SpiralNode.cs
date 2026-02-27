using UnityEngine;
using WitShells.DesignPatterns.Core;

namespace WitShells.DesignPatterns
{
    /// <summary>
    /// Contract for a node in a doubly-linked spiral layout list.
    /// Each node knows its neighbours, its index in the spiral, and its 2D grid coordinate.
    /// </summary>
    public interface ISpiralNode
    {
        /// <summary>The next node in the spiral chain (clockwise or counter-clockwise).</summary>
        ISpiralNode Next { get; set; }

        /// <summary>The previous node in the spiral chain.</summary>
        ISpiralNode Previous { get; set; }

        /// <summary>Initialises this node's position and index within the spiral.</summary>
        /// <param name="position">Local position assigned to this node.</param>
        /// <param name="index">Zero-based index within the spiral.</param>
        void Initialize(Vector3 position, int index);

        /// <summary>The GameObject backing this node.</summary>
        GameObject GameObject { get; }

        /// <summary>Zero-based index of this node in the spiral sequence.</summary>
        int Index { get; }

        /// <summary>2D grid coordinate of this node (used for layout-driven positioning).</summary>
        Vector2Int Coordinate { get; set; }

        /// <summary>Updates the node's logical coordinate (does not immediately change visual position).</summary>
        void UpdateCoordinate(Vector2Int coordinate);
    }

    /// <summary>
    /// Abstract MonoBehaviour implementation of <see cref="ISpiralNode"/> that carries
    /// typed data payload <typeparamref name="T"/>.
    /// Subclass this to create spiral nodes that display game-specific content
    /// (e.g. inventory items, skill icons, quest entries).
    /// </summary>
    /// <typeparam name="T">The data type displayed or managed by this node.</typeparam>
    public abstract class SpiralNode<T> : MonoBehaviour, ISpiralNode
    {
        /// <inheritdoc />
        public ISpiralNode Next { get; set; }

        /// <inheritdoc />
        public ISpiralNode Previous { get; set; }

        /// <inheritdoc />
        public GameObject GameObject => gameObject;

        /// <inheritdoc />
        public int Index { get; private set; }

        /// <inheritdoc />
        public Vector2Int Coordinate { get; set; }

        /// <summary>The data payload associated with this node.</summary>
        public T Data { get; set; }

        /// <inheritdoc />
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

        /// <summary>
        /// Called by the layout container to refresh the node's visual representation
        /// when its <paramref name="data"/> changes.
        /// </summary>
        /// <param name="data">The new data to display.</param>
        public abstract void UpdateNode(T data);

        /// <inheritdoc />
        public virtual void UpdateCoordinate(Vector2Int coordinate)
        {
            Coordinate = coordinate;
        }
    }
}