using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WitShells.DesignPatterns.Core
{
    /// <summary>
    /// Abstract MonoBehaviour base class for <b>Drop Zone</b> areas in a drag-and-drop system.
    /// Responds to Unity pointer events (<c>IDropHandler</c>, <c>IPointerEnterHandler</c>,
    /// <c>IPointerExitHandler</c>) and delegates acceptance logic to concrete subclasses.
    /// Provides visual highlight feedback when a compatible draggable hovers over the zone.
    /// </summary>
    /// <typeparam name="T">The payload type expected by this drop zone. Must match the draggable's type.</typeparam>
    /// <remarks>
    /// Concrete subclasses must implement <see cref="CanAcceptDrop"/> to filter incoming data and
    /// <see cref="HandleDrop"/> to process accepted drops (e.g. swap inventory slots, update data models).
    /// Override <see cref="PlayDropFeedback"/> to add audio or particle effects on successful drop.
    /// </remarks>
    public abstract class DropZone<T> : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
        where T : class
    {
        [SerializeField] protected bool acceptAnyType = false;
        [SerializeField] protected Color highlightColor = Color.yellow;
        [SerializeField] protected Color normalColor = Color.white;

        protected Image backgroundImage;
        protected bool isHighlighted = false;

        /// <summary>Fired when an item is successfully dropped onto this zone.</summary>
        public UnityEvent<T> OnItemDropped;

        /// <summary>Fired when a compatible draggable enters the zone's hit area.</summary>
        public UnityEvent<T> OnItemEntered;

        /// <summary>Fired when a draggable exits the zone's hit area.</summary>
        public UnityEvent<T> OnItemExited;

        /// <summary>Fired when the zone becomes highlighted.</summary>
        public UnityEvent<DropZone<T>> OnDropZoneHighlighted;

        /// <summary>Fired when the zone returns to its normal (unhighlighted) state.</summary>
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
            draggable = gameObject.GetComponent<DraggableItem<T>>();
            return draggable != null;
        }


        /// <summary>Applies highlight colour to the background image.</summary>
        protected virtual void HighlightDropZone()
        {
            if (isHighlighted) return;

            isHighlighted = true;
            if (backgroundImage != null)
                backgroundImage.color = highlightColor;

            OnDropZoneHighlighted?.Invoke(this);
        }

        /// <summary>Restores the background image to the normal colour.</summary>
        protected virtual void UnhighlightDropZone()
        {
            if (!isHighlighted) return;

            isHighlighted = false;
            if (backgroundImage != null)
                backgroundImage.color = normalColor;

            OnDropZoneUnhighlighted?.Invoke(this);
        }

        /// <summary>
        /// Override to play visual or audio feedback (e.g. particles, sounds) when a drop succeeds.
        /// </summary>
        protected virtual void PlayDropFeedback()
        {
            // Override to add audio/visual feedback
        }

        /// <summary>
        /// Determines whether this zone accepts the given data payload.
        /// Return <c>false</c> to reject the drop silently.
        /// </summary>
        /// <param name="data">The payload carried by the dragged item.</param>
        protected abstract bool CanAcceptDrop(T data);

        /// <summary>
        /// Executes the drop logic for an accepted item (e.g. update slot data, trigger game events).
        /// </summary>
        /// <param name="data">The accepted payload.</param>
        /// <param name="draggable">Reference to the <see cref="IDraggable{T}"/> that was dropped.</param>
        protected abstract void HandleDrop(T data, IDraggable<T> draggable);
    }
}