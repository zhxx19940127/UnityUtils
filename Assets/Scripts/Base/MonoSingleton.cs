using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T _instance;
    private static bool _isInitialized;
    private static bool _isApplicationQuitting;

    public static T Instance
    {
        get
        {
            if (_isApplicationQuitting)
            {
                Debug.LogWarning($"[{typeof(T)}] 实例已被销毁，应用程序正在退出");
                return null;
            }

            if (!_isInitialized)
            {
                _instance = FindObjectOfType<T>();
                
                if (_instance == null)
                {
                    var singleton = new GameObject($"[{typeof(T).Name}]");
                    _instance = singleton.AddComponent<T>();
                }
                
                DontDestroyOnLoad(_instance.gameObject);
                _isInitialized = true;
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = (T)this;
        _isInitialized = true;
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _isInitialized = false;
            _instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        _isApplicationQuitting = true;
    }
}