using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace WitShells.DesignPatterns.Core
{
    public interface IDraggable<T> where T : class
    {
        T GetData();
        void SetData(T data);
        Transform GetTransform();
        void OnDragStarted();
        void OnDragEnded(bool wasDropped);
        bool CanSwapWith(IDraggable<T> other);
        void SwapWith(IDraggable<T> other);
    }

    public abstract class DraggableItem<T> : MonoBehaviour, IDraggable<T>, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
        where T : class
    {
        [SerializeField] protected bool returnToOriginalPosition = true;
        [SerializeField] protected bool allowSwapping = true;
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected Canvas dragCanvas;
        [SerializeField] protected float dragAlpha = 0.6f;

        protected T data;
        protected Vector3 originalPosition;
        protected Transform originalParent;
        protected bool isDragging = false;
        protected bool wasDropped = false;

        // Events
        public UnityAction<T> OnDragStart;
        public UnityAction<T> OnDragEnd;
        public UnityAction<T, Vector3> OnDragging;

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanStartDrag()) return;

            isDragging = true;
            wasDropped = false;
            originalPosition = transform.position;
            originalParent = transform.parent;

            // Move to top of hierarchy for rendering
            transform.SetParent(dragCanvas.transform, true);
            transform.SetAsLastSibling();

            // Make semi-transparent
            if (canvasGroup != null)
            {
                canvasGroup.alpha = dragAlpha;
                canvasGroup.blocksRaycasts = false;
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

            // Handle return to original position
            if (returnToOriginalPosition)
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
        }

        // IDraggable implementation
        public virtual T GetData() => data;
        public virtual void SetData(T data) => this.data = data;
        public virtual Transform GetTransform() => transform;
        public virtual void OnDragStarted() { }
        public virtual void OnDragEnded(bool wasDropped) { }
        public abstract bool CanSwapWith(IDraggable<T> other);
        public abstract void SwapWith(IDraggable<T> other);

        // Abstract/Virtual methods
        protected virtual bool CanStartDrag() => data != null;
    }
}