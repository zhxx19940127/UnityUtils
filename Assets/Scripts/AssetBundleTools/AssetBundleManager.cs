using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Object = UnityEngine.Object;

/// <summary>
/// AssetBundle管理器（单例模式）
/// 负责加载、缓存和卸载AssetBundle资源
/// 适配WebGL平台，使用UnityWebRequest加载StreamingAssets中的资源
/// </summary>
public class AssetBundleManager : MonoBehaviour
{
    private static AssetBundleManager _instance;

    public static AssetBundleManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("AssetBundleManager");
                _instance = go.AddComponent<AssetBundleManager>();
                DontDestroyOnLoad(go);
            }

            return _instance;
        }
    }

    /// <summary>
    /// 已加载的AssetBundle缓存
    /// </summary>
    private Dictionary<string, AssetBundle> loadedAssetBundles = new Dictionary<string, AssetBundle>();

    /// <summary>
    ///  正在加载的AssetBundle列表
    /// </summary>
    private Dictionary<string, Coroutine> loadingBundles = new Dictionary<string, Coroutine>();

    /// <summary>
    /// 等待加载完成的回调队列
    /// </summary>
    private Dictionary<string, List<Action<AssetBundle>>> pendingCallbacks =
        new Dictionary<string, List<Action<AssetBundle>>>();

    // WebGL 并发限制
    private const int MAX_CONCURRENT_REQUESTS = 4; // WebGL 限制同时请求数（避免资源竞争）
    private int currentLoadingCount = 0;
    private Queue<Action> loadQueue = new Queue<Action>();

    // 主清单
    private AssetBundleManifest manifest;

    // 配置数据
    private AssetBundleConfigData configData;

    // 是否已初始化
    private bool isInitialized = false;


    public Transform root;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);


        StartCoroutine(Initialize(t => { Debug.LogError("初始化完成了,这里执行事件 "); },
            () => { Debug.LogError("所有模型加载完成了,这里执行事件 "); }));
    }

    /// <summary>
    /// 初始化管理器，加载配置文件和主清单
    /// </summary>
    public IEnumerator Initialize(Action<AssetBundleConfigData> onInitComplete = null, Action loadAllModels = null)
    {
        if (isInitialized)
        {
            yield break;
        }

        Debug.Log("===== AssetBundle 初始化开始 =====");

        // 1. 加载配置文件
        string configPath = AssetBundleConfig.GetConfigPath();

#if UNITY_WEBGL //&& !UNITY_EDITOR
        // WebGL平台使用UnityWebRequest
        UnityWebRequest request = UnityWebRequest.Get(configPath);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            configData = JsonUtility.FromJson<AssetBundleConfigData>(json);
            Debug.Log($"配置文件加载成功，共 {configData.bundles.Length} 个模型");
        }
        else
        {
            Debug.LogError($"✗ 加载配置文件失败: {request.error}");
        }
#else
        // 编辑器模式直接读取文件
        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);
            configData = JsonUtility.FromJson<AssetBundleConfigData>(json);
            Debug.Log($"配置文件加载成功，共 {configData.bundles.Length} 个模型");
        }
        else
        {
            Debug.LogError($"✗ 配置文件不存在: {configPath}");
        }

        yield return null;
#endif

        // 2. 加载主清单
        yield return LoadManifest();

        isInitialized = true;
        onInitComplete?.Invoke(configData);
        Debug.Log("===== AssetBundle 初始化完成 =====");

        // 3. 自动加载配置文件中的所有模型（顺序加载）
        yield return LoadAllModelsFromConfig(loadAllModels);
    }

    /// <summary>
    /// 按配置文件顺序加载所有模型（一个完成后再加载下一个，失败的重试）
    /// </summary>
    private IEnumerator LoadAllModelsFromConfig(Action loadAllModels = null)
    {
        if (configData == null || configData.bundles == null || configData.bundles.Length == 0)
        {
            Debug.LogWarning("配置文件为空，跳过自动加载");
            yield break;
        }

        Debug.Log($"===== 开始顺序加载 {configData.bundles.Length} 个模型 =====");

        List<AssetBundleItemInfo> failedBundles = new List<AssetBundleItemInfo>();
        int successCount = 0;

        // 第一轮：顺序加载所有模型
        for (int i = 0; i < configData.bundles.Length; i++)
        {
            var bundleInfo = configData.bundles[i];
            StartCoroutine(LoadModelPrefab(bundleInfo.assetName, (prefab) =>
            {
                successCount++;
                var go = Instantiate(prefab, transform);
                go.transform.SetParent(root.Find(bundleInfo.assetName));
                go.transform.localPosition = Vector3.zero;
                go.transform.localEulerAngles = Vector3.zero;
            }));

            // 每个模型之间添加短暂延迟，避免并发冲突
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log($"===== 加载完成：成功 {successCount} 个，失败 {failedBundles.Count} 个 =====");
        loadAllModels?.Invoke();
    }

    /// <summary>
    /// 加载主清单包（用于依赖管理）
    /// </summary>
    private IEnumerator LoadManifest()
    {
        // 主清单包名与输出文件夹同名，统一使用 .ab 后缀
        string manifestBundleName = "AssetBundles.ab";
        string manifestPath = AssetBundleConfig.GetAssetBundlePath(manifestBundleName);
        UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(manifestPath);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            AssetBundle manifestBundle = DownloadHandlerAssetBundle.GetContent(request);

            if (manifestBundle != null)
            {
                manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

                if (manifest != null)
                {
                    string[] allBundles = manifest.GetAllAssetBundles();
                    Debug.Log($"主清单加载成功  检测到 {allBundles.Length} 个包");

                    // 立即卸载主清单 Bundle（只需要 Manifest 对象）
                    manifestBundle.Unload(false);
                }
                else
                {
                    Debug.LogWarning("主清单加载失败，将跳过依赖检查");
                    manifestBundle.Unload(true);
                }
            }
        }
        else
        {
            Debug.LogWarning($"主清单加载失败: {request.error}");
        }

        request.Dispose();
    }

    /// <summary>
    /// 异步加载AssetBundle（支持并发请求）
    /// </summary>
    public IEnumerator LoadAssetBundleAsync(string bundleName, System.Action<AssetBundle> onComplete)
    {
        // 检查是否已加载完成
        if (loadedAssetBundles.ContainsKey(bundleName))
        {
            onComplete?.Invoke(loadedAssetBundles[bundleName]);
            yield break;
        }

        // 检查是否正在加载中
        if (loadingBundles.ContainsKey(bundleName))
        {
            // 将回调加入等待队列
            if (!pendingCallbacks.ContainsKey(bundleName))
                pendingCallbacks[bundleName] = new List<System.Action<AssetBundle>>();

            pendingCallbacks[bundleName].Add(onComplete);

            // 等待加载完成
            while (loadingBundles.ContainsKey(bundleName))
            {
                yield return null;
            }

            // 加载完成后回调（注意：LoadBundleInternal 已经调用过 onComplete 了）
            // 这里不需要再次调用，因为已经在 pendingCallbacks 中处理过了
            yield break;
        }

        // WebGL 限流：检查是否超出并发限制
        if (currentLoadingCount >= MAX_CONCURRENT_REQUESTS)
        {
            Debug.Log($"[限流] {bundleName} 等待空闲槽位... (当前: {currentLoadingCount}/{MAX_CONCURRENT_REQUESTS})");

            // 加入等待队列，依赖包优先
            bool isQueued = false;
            System.Action loadAction = () =>
            {
                if (!isQueued)
                {
                    isQueued = true;
                    currentLoadingCount++;
                    Debug.LogError($"[限流] {bundleName} 开始加载 (当前并发: {currentLoadingCount})");
                    loadingBundles[bundleName] = StartCoroutine(LoadBundleInternal(bundleName, onComplete));
                }
            };

            loadQueue.Enqueue(loadAction);

            //  关键：等待加载完成
            while (!loadedAssetBundles.ContainsKey(bundleName) && !loadingBundles.ContainsKey(bundleName))
            {
                yield return null;
            }

            // 如果进入了 loadingBundles，继续等待完成
            while (loadingBundles.ContainsKey(bundleName))
            {
                yield return null;
            }

            yield break;
        }

        // 标记为正在加载
        currentLoadingCount++;
        loadingBundles[bundleName] = StartCoroutine(LoadBundleInternal(bundleName, onComplete));

        //关键：等待加载完成
        while (loadingBundles.ContainsKey(bundleName))
        {
            yield return null;
        }
    }

    /// <summary>
    /// 内部加载方法
    /// </summary>
    private IEnumerator LoadBundleInternal(string bundleName, System.Action<AssetBundle> onComplete)
    {
        string bundlePath = AssetBundleConfig.GetAssetBundlePath(bundleName);
        // WebGL平台使用UnityWebRequestAssetBundle
        UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(bundlePath);
        request.timeout = 30; // 设置30秒超时

        float startTime = Time.realtimeSinceStartup;
        yield return request.SendWebRequest();
        float loadTime = Time.realtimeSinceStartup - startTime;

        if (request.result == UnityWebRequest.Result.Success)
        {
            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
            loadedAssetBundles[bundleName] = bundle;
            onComplete?.Invoke(bundle);
        }
        else
        {
            Debug.LogError($"[{Time.frameCount}] 加载失败: {bundleName}");
            Debug.LogError($"错误类型: {request.result}");
            Debug.LogError($"错误信息: {request.error}");
            Debug.LogError($"请求URL: {request.url}");
            Debug.LogError($"HTTP状态: {request.responseCode}");
            Debug.LogError($"耗时: {loadTime:F2}秒");
            onComplete?.Invoke(null);
        }

        request.Dispose();

        // 加载完成，移除加载标记
        loadingBundles.Remove(bundleName);
        currentLoadingCount--;
        Debug.LogError($"[{Time.frameCount}] 加载完成: {bundleName} (当前并发: {currentLoadingCount})");
        // 通知所有等待的回调
        if (pendingCallbacks.ContainsKey(bundleName))
        {
            AssetBundle loadedBundle =
                loadedAssetBundles.ContainsKey(bundleName) ? loadedAssetBundles[bundleName] : null;

            foreach (var callback in pendingCallbacks[bundleName])
            {
                callback?.Invoke(loadedBundle);
            }

            pendingCallbacks.Remove(bundleName);
        }

        // WebGL: 处理等待队列中的下一个请求
        if (loadQueue.Count > 0 && currentLoadingCount < MAX_CONCURRENT_REQUESTS)
        {
            var nextLoad = loadQueue.Dequeue();
            Debug.Log($"[限流] 处理队列中的下一个请求 (剩余: {loadQueue.Count}, 当前并发: {currentLoadingCount})");
            nextLoad?.Invoke();
        }
    }

    /// <summary>
    /// 从AssetBundle加载资源
    /// </summary>
    public IEnumerator LoadAssetAsync<T>(string bundleName, string assetName, System.Action<T> onComplete)
        where T : Object
    {
        AssetBundle bundle = null;

        yield return LoadAssetBundleAsync(bundleName, (loadedBundle) => { bundle = loadedBundle; });

        if (bundle != null)
        {
            AssetBundleRequest request = bundle.LoadAssetAsync<T>(assetName);
            yield return request;

            if (request.asset != null)
            {
                Debug.Log($"资源加载成功: {assetName} from {bundleName}");
                onComplete?.Invoke(request.asset as T);
            }
            else
            {
                Debug.LogError($"从AssetBundle加载资源失败: {assetName}");
                onComplete?.Invoke(null);
            }
        }
        else
        {
            onComplete?.Invoke(null);
        }
    }

    /// <summary>
    /// 快捷方法：根据模型名称加载预制体（带依赖处理）
    /// </summary>
    public IEnumerator LoadModelPrefab(string modelName, System.Action<GameObject> onComplete)
    {
        // 清理模型名称：替换特殊字符为安全字符（与构建时保持一致）
        string safeName = modelName.ToLower()
            .Replace(".", "-") // 点号改为横杠
            .Replace(" ", "_") // 空格改为下划线
            .Replace("&", "_"); // & 符号改为下划线

        string bundleName = $"model_{safeName}.ab";


        // 1. 先加载所有依赖
        yield return LoadDependencies(bundleName);

        // 2. 加载目标包
        AssetBundle bundle = null;
        yield return LoadAssetBundleAsync(bundleName, (loadedBundle) => { bundle = loadedBundle; });

        if (bundle == null)
        {
            Debug.LogError($"[{Time.frameCount}] Bundle 加载失败: {bundleName}");
            onComplete?.Invoke(null);
            yield break;
        }


        GameObject prefab = null;

        AssetBundleRequest assetRequest = bundle.LoadAssetAsync<GameObject>(modelName);
        yield return assetRequest;

        if (assetRequest.asset != null)
        {
            prefab = assetRequest.asset as GameObject;
        }

        if (prefab != null)
        {
            onComplete?.Invoke(prefab);
        }
        else
        {
            // 列出包中所有资源名称
            string[] allAssets = bundle.GetAllAssetNames();
            Debug.LogError($"[{Time.frameCount}] 资源提取失败，包中所有资源: [{string.Join(", ", allAssets)}]");
            onComplete?.Invoke(null);
        }
    }

    /// <summary>
    /// 加载依赖包
    /// </summary>
    private IEnumerator LoadDependencies(string bundleName)
    {
        if (manifest == null)
        {
            Debug.Log($"主清单未加载，跳过依赖检查 ({bundleName})");
            yield break;
        }

        // 获取所有依赖
        string[] dependencies = manifest.GetAllDependencies(bundleName);

        if (dependencies.Length > 0)
        {
            foreach (string dependency in dependencies)
            {
                // 如果已加载则跳过
                if (loadedAssetBundles.ContainsKey(dependency))
                {
                    continue;
                }

                Debug.LogError($"加载依赖包: {dependency} for {bundleName}");
                // 加载依赖包
                yield return LoadAssetBundleAsync(dependency, null);
            }
        }
    }

    /// <summary>
    /// 卸载AssetBundle
    /// </summary>
    public void UnloadAssetBundle(string bundleName, bool unloadAllLoadedObjects = false)
    {
        if (loadedAssetBundles.ContainsKey(bundleName))
        {
            loadedAssetBundles[bundleName].Unload(unloadAllLoadedObjects);
            loadedAssetBundles.Remove(bundleName);
        }
    }

    /// <summary>
    /// 卸载所有AssetBundle
    /// </summary>
    public void UnloadAllAssetBundles(bool unloadAllLoadedObjects = false)
    {
        foreach (var kvp in loadedAssetBundles)
        {
            kvp.Value.Unload(unloadAllLoadedObjects);
        }

        loadedAssetBundles.Clear();
        Debug.Log("所有AssetBundle已卸载");
    }

    /// <summary>
    /// 获取所有可用的Bundle信息
    /// </summary>
    public AssetBundleItemInfo[] GetAllBundleInfos()
    {
        return configData?.bundles;
    }

    /// <summary>
    /// 获取已加载的AssetBundle数量
    /// </summary>
    public int GetLoadedBundleCount()
    {
        return loadedAssetBundles.Count;
    }

    private void OnDestroy()
    {
        UnloadAllAssetBundles(true);
    }
}