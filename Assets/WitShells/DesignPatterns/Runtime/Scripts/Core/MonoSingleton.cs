namespace WitShells.DesignPatterns.Core
{

    using UnityEngine;

    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        [Header("MonoSingleton")]
        public bool IsPersistent = true;

        private static T _instance;
        private static object _lock = new object();

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

        protected virtual void OnDestroy()
        {
            if (_instance != null && _instance == this)
            {
                _instance = null;
            }
        }
    }
}