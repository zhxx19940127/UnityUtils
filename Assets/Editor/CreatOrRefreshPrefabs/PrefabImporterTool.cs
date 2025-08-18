using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace PrefabImporterTool
{
    /// <summary>
    /// 通用预制体导入工具
    /// 功能：根据数据表自动生成/更新Unity预制体
    /// </summary>
    public class UniversalPrefabImporterTool
    {
        #region 配置区

        // 资源基础路径（所有预制体将生成在此路径下）
        private static string baseResourcesPath = "Assets/Resources/";

        // 是否启用调试日志
        private static bool enableDebugLog = true;

        #endregion

        #region 核心接口

        /// <summary>
        /// 预制体处理器接口
        /// 每个数据类型需要实现对应的处理器
        /// </summary>
        public interface IPrefabProcessor
        {
            /// <summary> 获取预制体存储文件夹名称 </summary>
            string GetFolderName(object dataItem);

            /// <summary> 获取预制体文件名称 </summary>
            string GetPrefabName(object dataItem);

            /// <summary> 新建预制体时的初始化逻辑 </summary>
            void OnCreatePrefab(GameObject prefab, object dataItem);

            /// <summary> 更新已有预制体时的逻辑 </summary>
            void OnUpdatePrefab(GameObject prefab, object dataItem);
        }

        /// <summary>
        /// 处理器特性标记
        /// 用于自动关联数据类型与处理器
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public class PrefabProcessorAttribute : Attribute
        {
            /// <summary> 处理器支持的数据类型 </summary>
            public Type DataType { get; }

            public PrefabProcessorAttribute(Type dataType)
            {
                DataType = dataType;
            }
        }

        #endregion

        #region DI容器模拟

        /// <summary>
        /// 处理器容器（简化版DI容器）
        /// 负责管理所有预制体处理器
        /// </summary>
        private static class ProcessorContainer
        {
            // 处理器类型缓存（数据类型->处理器类型）
            private static readonly Dictionary<Type, Type> _processorTypes = new Dictionary<Type, Type>();

            // 处理器实例缓存（数据类型->处理器实例）
            private static readonly Dictionary<Type, IPrefabProcessor> _processorInstances =
                new Dictionary<Type, IPrefabProcessor>();

            /// <summary> 静态构造函数：自动发现所有处理器 </summary>
            static ProcessorContainer()
            {
                DiscoverProcessors();
            }

            /// <summary> 扫描程序集发现所有处理器 </summary>
            private static void DiscoverProcessors()
            {
                // 获取所有实现了IPrefabProcessor接口的类型
                var processorTypes = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.GetInterfaces().Contains(typeof(IPrefabProcessor)));

                foreach (var type in processorTypes)
                {
                    // 获取处理器上标记的PrefabProcessor特性
                    var attr = type.GetCustomAttribute<PrefabProcessorAttribute>();
                    if (attr != null)
                    {
                        _processorTypes[attr.DataType] = type;
                        Log($"发现处理器: {type.Name} -> {attr.DataType.Name}");
                    }
                }
            }

            /// <summary> 获取指定数据项对应的处理器 </summary>
            public static IPrefabProcessor GetProcessor(object dataItem)
            {
                var dataType = dataItem.GetType();

                // 先从实例缓存中查找
                if (_processorInstances.TryGetValue(dataType, out var cachedProcessor))
                {
                    return cachedProcessor;
                }

                // 再从类型缓存中查找
                if (_processorTypes.TryGetValue(dataType, out var processorType))
                {
                    // 创建处理器实例并缓存
                    var processor = (IPrefabProcessor)Activator.CreateInstance(processorType);
                    _processorInstances[dataType] = processor;
                    return processor;
                }

                throw new InvalidOperationException($"未找到{dataType.Name}对应的处理器");
            }
        }

        #endregion

        #region 主入口

        /// <summary>
        /// 菜单入口：执行预制体导入
        /// </summary>
        [MenuItem("Tools/Universal Prefab Importer/Run Import")]
        public static void RunUniversalImport()
        {
            try
            {
                // 显示进度条
                EditorUtility.DisplayProgressBar("预制体导入", "准备数据...", 0);

                // 1. 获取数据源（转换为列表以便计算总数）
                var dataItems = GetDataItems().ToList();
                int totalCount = dataItems.Count;

                // 2. 遍历处理每个数据项
                for (int i = 0; i < totalCount; i++)
                {
                    var item = dataItems[i];
                    float progress = (float)i / totalCount;

                    // 更新进度条
                    EditorUtility.DisplayProgressBar(
                        "导入进度",
                        $"正在处理 {GetItemIdentifier(item)}...",
                        progress);

                    try
                    {
                        // 处理单个数据项
                        ProcessSingleItem(item);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"处理{GetItemIdentifier(item)}失败: {ex.Message}");
                    }
                }

                // 保存资源并刷新数据库
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // 清理进度条并显示完成提示
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("完成", $"成功导入{totalCount}个预制体", "确定");
            }
            catch (Exception ex)
            {
                // 错误处理
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("错误", $"导入失败: {ex.Message}", "确定");
                Debug.LogException(ex);
            }
        }

        #endregion

        #region 核心处理逻辑

        /// <summary>
        /// 处理单个数据项
        /// </summary>
        private static void ProcessSingleItem(object dataItem)
        {
            // 1. 获取对应的处理器
            var processor = ProcessorContainer.GetProcessor(dataItem);

            // 2. 准备文件路径
            string folderName = processor.GetFolderName(dataItem);
            string prefabName = processor.GetPrefabName(dataItem);
            string folderPath = Path.Combine(baseResourcesPath, folderName);
            string prefabPath = Path.Combine(folderPath, $"{prefabName}.prefab");

            // 3. 确保目录存在（不存在则创建）
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                Log($"创建目录: {folderPath}");
            }

            // 4. 检查预制体是否存在并执行相应操作
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject)) as GameObject;

            if (prefabAsset == null)
            {
                // 创建新预制体
                CreateNewPrefab(prefabPath, dataItem, processor);
            }
            else
            {
                // 更新已有预制体
                UpdateExistingPrefab(prefabPath, dataItem, processor);
            }
        }

        /// <summary>
        /// 创建新预制体
        /// </summary>
        private static void CreateNewPrefab(string path, object dataItem, IPrefabProcessor processor)
        {
            // 创建临时游戏对象
            GameObject newObj = new GameObject(Path.GetFileNameWithoutExtension(path));

            try
            {
                // 调用处理器初始化逻辑
                processor.OnCreatePrefab(newObj, dataItem);

                // 保存为预制体
                PrefabUtility.SaveAsPrefabAsset(newObj, path);
                Log($"创建新预制体: {path}");
            }
            finally
            {
                // 确保临时对象被销毁
                GameObject.DestroyImmediate(newObj);
            }
        }

        /// <summary>
        /// 更新已有预制体
        /// </summary>
        private static void UpdateExistingPrefab(string path, object dataItem, IPrefabProcessor processor)
        {
            // 加载已有预制体并实例化
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
            GameObject instance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;

            try
            {
                // 调用处理器更新逻辑
                processor.OnUpdatePrefab(instance, dataItem);

                // 保存修改
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                Log($"更新预制体: {path}");
            }
            finally
            {
                // 确保实例被销毁
                GameObject.DestroyImmediate(instance);
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取数据项集合（需根据项目实际情况实现）
        /// </summary>
        private static IEnumerable<object> GetDataItems()
        {
            // 这里可以从CSV、JSON、数据库等数据源获取数据项
            // 示例：返回一个简单的字符串列表作为数据项
            // 注意：实际项目中需要替换为真实数据源获取逻辑

            for (int i = 0; i < 10; i++)
            {
                yield return $"DataItem_{i + 1}";
            }

            yield break;
        }

        /// <summary>
        /// 获取数据项标识（用于日志和进度显示）
        /// </summary>
        private static string GetItemIdentifier(object dataItem)
        {
            // 可根据需要实现更复杂的标识生成逻辑
            return dataItem.ToString();
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        private static void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[预制处理工具] :  {message}");
            }
        }

        #endregion
    }
}