namespace WitShells.DesignPatterns.Core
{

    using UnityEngine;

    /// <summary>
    /// Thread-safe <b>MonoBehaviour Singleton</b> base class.
    /// Subclass this to create a manager or service that must have exactly one instance
    /// in the scene. The instance is auto-located via <c>FindFirstObjectByType</c> and
    /// optionally kept alive across scene loads via <c>DontDestroyOnLoad</c>.
    /// </summary>
    /// <typeparam name="T">The concrete MonoBehaviour type that should be a singleton.</typeparam>
    /// <remarks>
    /// <b>Usage:</b> <c>public class AudioManager : MonoSingleton&lt;AudioManager&gt; { }</c><br/>
    /// Access the instance anywhere via <c>AudioManager.Instance</c>.<br/>
    /// Set <see cref="IsPersistent"/> to <c>false</c> in the Inspector if the singleton
    /// should be destroyed when loading a new scene.
    /// </remarks>
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        /// <summary>
        /// When <c>true</c> (default), the singleton's GameObject survives scene loads via
        /// <c>DontDestroyOnLoad</c>. Set to <c>false</c> for scene-specific managers.
        /// </summary>
        [Header("MonoSingleton")]
        public bool IsPersistent = true;

        private static T _instance;
        private static object _lock = new object();

        /// <summary>
        /// Gets the single instance of <typeparamref name="T"/>.
        /// Lazily locates the instance in the scene if it has not been found yet.
        /// Logs an error if more than one instance exists.
        /// </summary>
        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindFirstObjectByType<T>(FindObjectsInactive.Include);

                        if (FindObjectsByType<T>(FindObjectsSortMode.None).Length > 1)
                        {
                            Debug.LogError("[MonoSingleton] Something went really wrong " +
                                " - there should never be more than 1 singleton! Reopening the scene might fix it.");
                            return _instance;
                        }
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Unity lifecycle: assigns the singleton instance and optionally marks it persistent.
        /// Destroys duplicate GameObjects and warns in the console if more than one exists.
        /// </summary>
        public virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;

                if (IsPersistent)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (_instance != this)
            {
                Debug.LogWarning("[MonoSingleton] Instance '" + typeof(T) +
                    "' already exists. Destroying duplicate instance.");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Unity lifecycle: clears the static instance reference when this object is destroyed.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_instance != null && _instance == this)
            {
                _instance = null;
            }
        }
    }
}