using UnityEngine;

namespace PrefabImporterTool
{
    /// <summary>
    /// 武器数据处理器示例
    /// </summary>
    [UniversalPrefabImporterTool.PrefabProcessor(typeof(string))]
    public class WeaponProcessor : UniversalPrefabImporterTool.IPrefabProcessor
    {
        /// <summary> 获取武器预制体存储路径 </summary>
        public string GetFolderName(object dataItem)
        {
            var data = (string)dataItem;
            // 按武器稀有度分级存储
            return $"Weapons";
        }

        /// <summary> 获取武器预制体名称 </summary>
        public string GetPrefabName(object dataItem)
        {
            // 使用武器ID作为预制体名称
            return ((string)dataItem);
        }

        /// <summary> 初始化新武器预制体 </summary>
        public void OnCreatePrefab(GameObject prefab, object dataItem)
        {
            var data = (string)dataItem;

            // 添加武器组件
            // 添加碰撞体
            // 添加基础3D模型


            var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.transform.SetParent(prefab.transform);
        }

        /// <summary> 更新已有武器预制体 </summary>
        public void OnUpdatePrefab(GameObject prefab, object dataItem)
        {
        }
    }
}