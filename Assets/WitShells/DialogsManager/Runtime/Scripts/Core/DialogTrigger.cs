using UnityEngine;
using UnityEngine.Events;

namespace WitShells.DialogsManager
{
    /// <summary>
    /// A component that triggers conversations based on collision/trigger events.
    /// Requires a Collider component on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DialogTrigger : MonoBehaviour
    {
        #region Enums

        public enum TriggerType
        {
            Manual,
            OnStart,
            OnEnable,
            OnTriggerEnter,
            OnTriggerExit,
            OnCollisionEnter,
            OnCollisionExit
        }

        #endregion

        #region Serialized Fields

        [Header("Conversation")]
        [Tooltip("The conversation to trigger.")]
        [SerializeField] private Conversation conversation;

        [Header("Trigger Settings")]
        [Tooltip("How the conversation should be triggered.")]
        [SerializeField] private TriggerType triggerType = TriggerType.OnTriggerEnter;

        [Tooltip("Tag filter for collision/trigger events. Leave empty to accept all.")]
        [SerializeField] private string tagFilter = "Player";

        [Header("Options")]
        [Tooltip("Only trigger once, then disable.")]
        [SerializeField] private bool triggerOnce = false;

        [Tooltip("Delay before starting the conversation (in seconds).")]
        [SerializeField, Min(0)] private float triggerDelay = 0f;

        [Tooltip("If true, will stop any currently playing conversation before starting.")]
        [SerializeField] private bool interruptCurrent = false;

        [Header("Events")]
        [SerializeField] private UnityEvent onTriggered = new UnityEvent();

        #endregion

        #region Properties

        /// <summary>
        /// The conversation that will be triggered.
        /// </summary>
        public Conversation Conversation
        {
            get => conversation;
            set => conversation = value;
        }

        /// <summary>
        /// Whether this trigger has been activated.
        /// </summary>
        public bool HasTriggered { get; private set; }

        /// <summary>
        /// Whether something is currently inside the trigger zone.
        /// </summary>
        public bool IsInTriggerZone { get; private set; }

        /// <summary>
        /// Unity event accessor for when dialog is triggered.
        /// </summary>
        public UnityEvent OnTriggered => onTriggered;

        #endregion

        #region Private Fields

        private bool canTrigger = true;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (triggerType == TriggerType.OnStart)
            {
                TriggerConversation();
            }
        }

        private void OnEnable()
        {
            if (triggerType == TriggerType.OnEnable && Application.isPlaying)
            {
                TriggerConversation();
            }
        }

        #endregion

        #region 3D Trigger/Collision Events

        private void OnTriggerEnter(Collider other)
        {
            if (!PassesTagFilter(other.gameObject))
                return;

            IsInTriggerZone = true;

            if (triggerType == TriggerType.OnTriggerEnter)
            {
                TriggerConversation();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!PassesTagFilter(other.gameObject))
                return;

            IsInTriggerZone = false;

            if (triggerType == TriggerType.OnTriggerExit)
            {
                TriggerConversation();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!PassesTagFilter(collision.gameObject))
                return;

            if (triggerType == TriggerType.OnCollisionEnter)
            {
                TriggerConversation();
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (!PassesTagFilter(collision.gameObject))
                return;

            if (triggerType == TriggerType.OnCollisionExit)
            {
                TriggerConversation();
            }
        }

        #endregion

        #region 2D Trigger/Collision Events

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!PassesTagFilter(other.gameObject))
                return;

            IsInTriggerZone = true;

            if (triggerType == TriggerType.OnTriggerEnter)
            {
                TriggerConversation();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!PassesTagFilter(other.gameObject))
                return;

            IsInTriggerZone = false;

            if (triggerType == TriggerType.OnTriggerExit)
            {
                TriggerConversation();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!PassesTagFilter(collision.gameObject))
                return;

            if (triggerType == TriggerType.OnCollisionEnter)
            {
                TriggerConversation();
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (!PassesTagFilter(collision.gameObject))
                return;

            if (triggerType == TriggerType.OnCollisionExit)
            {
                TriggerConversation();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually trigger the conversation.
        /// </summary>
        public void TriggerConversation()
        {
            if (!canTrigger)
                return;

            if (conversation == null)
            {
                Debug.LogWarning($"[DialogTrigger] No conversation assigned on {gameObject.name}");
                return;
            }

            if (!interruptCurrent && DialogManager.Instance != null && DialogManager.Instance.HasActiveConversation)
            {
                return;
            }

            HasTriggered = true;

            if (triggerOnce)
            {
                canTrigger = false;
            }

            if (triggerDelay > 0)
            {
                StartCoroutine(TriggerWithDelay());
            }
            else
            {
                ExecuteTrigger();
            }
        }

        /// <summary>
        /// Resets the trigger to allow it to fire again.
        /// </summary>
        public void ResetTrigger()
        {
            canTrigger = true;
            HasTriggered = false;
        }

        #endregion

        #region Private Methods

        private System.Collections.IEnumerator TriggerWithDelay()
        {
            yield return new WaitForSeconds(triggerDelay);
            ExecuteTrigger();
        }

        private void ExecuteTrigger()
        {
            if (DialogManager.Instance != null)
            {
                DialogManager.Instance.StartConversation(conversation);
            }
            onTriggered?.Invoke();
        }

        private bool PassesTagFilter(GameObject obj)
        {
            if (string.IsNullOrEmpty(tagFilter))
                return true;

            return obj.CompareTag(tagFilter);
        }

        #endregion
    }
}
