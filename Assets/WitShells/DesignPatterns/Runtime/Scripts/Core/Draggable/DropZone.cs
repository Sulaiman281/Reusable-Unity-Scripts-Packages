using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WitShells.DesignPatterns.Core
{
    // Abstract generic class for handling drop zones
    public abstract class DropZone<T> : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
        where T : class
    {
        [SerializeField] protected bool acceptAnyType = false;
        [SerializeField] protected Color highlightColor = Color.yellow;
        [SerializeField] protected Color normalColor = Color.white;

        protected Image backgroundImage;
        protected bool isHighlighted = false;

        // Events
        public UnityEvent<T> OnItemDropped;
        public UnityEvent<T> OnItemEntered;
        public UnityEvent<T> OnItemExited;
        public UnityEvent<DropZone<T>> OnDropZoneHighlighted;
        public UnityEvent<DropZone<T>> OnDropZoneUnhighlighted;

        protected virtual void Awake()
        {
            backgroundImage = GetComponent<Image>();
            if (backgroundImage != null)
                normalColor = backgroundImage.color;
        }

        public virtual void OnDrop(PointerEventData eventData)
        {
            var draggedObject = eventData.pointerDrag;
            if (draggedObject == null) return;

            if (!IsDraggableObject(draggedObject, out var draggable)) return;

            var data = draggable.GetData();
            if (data == null && !acceptAnyType) return;

            if (CanAcceptDrop(data))
            {
                HandleDrop(data, draggable);
                OnItemDropped?.Invoke(data);
                PlayDropFeedback();
            }

            UnhighlightDropZone();
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;

            if (!IsDraggableObject(eventData.pointerDrag, out var draggable)) return;

            var data = draggable.GetData();
            if (CanAcceptDrop(data))
            {
                HighlightDropZone();
                OnItemEntered?.Invoke(data);
            }
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;

            if (!IsDraggableObject(eventData.pointerDrag, out var draggable)) return;

            var data = draggable.GetData();
            UnhighlightDropZone();
            OnItemExited?.Invoke(data);
        }

        private bool IsDraggableObject(GameObject gameObject, out IDraggable<T> draggable)
        {
            draggable = gameObject.GetComponent<MonoBehaviour>() as IDraggable<T>;
            return draggable != null;
        }


        protected virtual void HighlightDropZone()
        {
            if (isHighlighted) return;

            isHighlighted = true;
            if (backgroundImage != null)
                backgroundImage.color = highlightColor;

            OnDropZoneHighlighted?.Invoke(this);
        }

        protected virtual void UnhighlightDropZone()
        {
            if (!isHighlighted) return;

            isHighlighted = false;
            if (backgroundImage != null)
                backgroundImage.color = normalColor;

            OnDropZoneUnhighlighted?.Invoke(this);
        }

        protected virtual void PlayDropFeedback()
        {
            // Override to add audio/visual feedback
        }

        // Abstract methods to be implemented by concrete classes
        protected abstract bool CanAcceptDrop(T data);
        protected abstract void HandleDrop(T data, IDraggable<T> draggable);
    }
}