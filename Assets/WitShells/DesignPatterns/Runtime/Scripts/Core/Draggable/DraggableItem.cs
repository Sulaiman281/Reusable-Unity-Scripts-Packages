using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WitShells.DesignPatterns.Core
{
    /// <summary>
    /// Contract for a draggable UI item that carries data of type <typeparamref name="T"/>.
    /// Implement this interface (via <see cref="DraggableItem{T}"/>) to participate in
    /// drag-and-drop interactions with <see cref="DropZone{T}"/>.
    /// </summary>
    /// <typeparam name="T">The payload type associated with this draggable item.</typeparam>
    public interface IDraggable<T> where T : class
    {
        /// <summary>Returns the data payload carried by this draggable item.</summary>
        T GetData();

        /// <summary>Assigns a new data payload to this draggable item.</summary>
        void SetData(T data);

        /// <summary>Returns the <see cref="Transform"/> of the draggable GameObject.</summary>
        Transform GetTransform();

        /// <summary>Called by the drag system when a drag operation begins.</summary>
        void OnDragStarted();

        /// <summary>
        /// Called by the drag system when a drag operation ends.
        /// </summary>
        /// <param name="wasDropped"><c>true</c> if the item was dropped onto a valid <see cref="DropZone{T}"/>.</param>
        void OnDragEnded(bool wasDropped);

        /// <summary>Returns <c>true</c> if this item is allowed to swap data with <paramref name="other"/>.</summary>
        bool CanSwapWith(IDraggable<T> other);

        /// <summary>Performs the data swap between this item and <paramref name="other"/>.</summary>
        void SwapWith(IDraggable<T> other);

        /// <summary>Returns <c>true</c> if this item should snap back to its original position after an unsuccessful drop.</summary>
        bool CanReturnToOriginalPosition();
    }

    /// <summary>
    /// Abstract MonoBehaviour base class for draggable UI items in the <b>Drag-and-Drop</b> system.
    /// Handles pointer events, canvas hierarchy management (render-on-top), transparency during drag,
    /// and optional snap-back to the original position.
    /// </summary>
    /// <typeparam name="T">The payload type associated with this draggable item. Must be a reference type.</typeparam>
    /// <remarks>
    /// Requires a <c>Canvas</c> with a <c>GraphicRaycaster</c> on the UI canvas,
    /// a <c>CanvasGroup</c> component for the transparency effect,
    /// and a reference to the root <c>dragCanvas</c> for sibling-order management.
    /// </remarks>
    public abstract class DraggableItem<T> : MonoBehaviour, IDraggable<T>, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
        where T : class
    {
        [SerializeField] protected bool returnToOriginalPosition = true;
        [SerializeField] protected bool allowSwapping = true;
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected Canvas dragCanvas;
        [SerializeField] protected Image itemImage;
        [SerializeField] protected float dragAlpha = 0.6f;

        protected T data;
        protected Vector3 originalPosition;
        protected Transform originalParent;
        protected bool isDragging = false;
        protected bool wasDropped = false;
        protected int originalSiblingIndex;

        /// <summary>Invoked when a drag operation starts, passing the item's current data.</summary>
        public UnityAction<T> OnDragStart;

        /// <summary>Invoked when a drag operation ends, passing the item's current data.</summary>
        public UnityAction<T> OnDragEnd;

        /// <summary>Invoked every frame during a drag, passing the data and current world position.</summary>
        public UnityAction<T, Vector3> OnDragging;

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanStartDrag()) return;

            isDragging = true;
            wasDropped = false;
            originalPosition = transform.position;
            originalParent = transform.parent;

            originalSiblingIndex = transform.GetSiblingIndex();

            // Move to top of hierarchy for rendering
            transform.SetParent(dragCanvas.transform, true);
            transform.SetAsLastSibling();

            // Make semi-transparent
            if (canvasGroup != null)
            {
                canvasGroup.alpha = dragAlpha;
                canvasGroup.blocksRaycasts = false;
            }

            if(itemImage != null)
            {
                itemImage.raycastTarget = false;
            }

            OnDragStarted();
            OnDragStart?.Invoke(data);
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            transform.position = eventData.position;
            OnDragging?.Invoke(data, transform.position);
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            isDragging = false;

            // Restore canvas group
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            if (itemImage != null)
            {
                itemImage.raycastTarget = true;
            }

            // Handle return to original position
            if (returnToOriginalPosition && CanReturnToOriginalPosition())
            {
                ReturnToOriginalPosition();
            }

            OnDragEnded(wasDropped);
            OnDragEnd?.Invoke(data);
        }

        public virtual void OnDrop(PointerEventData eventData)
        {
            wasDropped = true;

            // Check if we dropped on a valid target
            if (eventData.pointerDrag != null)
            {
                var droppedOn = eventData.pointerDrag.GetComponent<IDraggable<T>>();
                if (droppedOn == null) return;
                if (allowSwapping)
                {
                    if (CanSwapWith(droppedOn))
                    {
                        SwapWith(droppedOn);
                    }
                }
            }
        }

        protected virtual void ReturnToOriginalPosition()
        {
            transform.position = originalPosition;
            transform.SetParent(originalParent, true);
            transform.SetSiblingIndex(originalSiblingIndex);
        }

        // IDraggable implementation
        public virtual T GetData() => data;
        public virtual void SetData(T data) => this.data = data;
        public virtual Transform GetTransform() => transform;
        public virtual void OnDragStarted() { }
        public virtual void OnDragEnded(bool wasDropped) { }
        public abstract bool CanSwapWith(IDraggable<T> other);
        public abstract void SwapWith(IDraggable<T> other);
        public abstract bool CanReturnToOriginalPosition();

        // Abstract/Virtual methods
        protected virtual bool CanStartDrag() => data != null;
    }
}