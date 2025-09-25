using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace GameObjectToolkit
{
    #region Core Extensions

    /// <summary>
    /// GameObject 基础扩展方法集合
    /// </summary>
    public static class CoreExtensions
    {
        /// <summary>
        /// 在 GameObject 的所有子孙中查找指定名称的子对象（支持是否包含未激活对象）
        /// </summary>
        /// <param name="parent">父对象</param>
        /// <param name="name">要查找的子对象名称</param>
        /// <param name="includeInactive">是否包含未激活对象</param>
        /// <returns>找到的 GameObject 或 null</returns>
        public static GameObject FindChild(this GameObject parent, string name, bool includeInactive = true)
        {
            if (parent == null) return null;

            // 使用栈模拟递归遍历
            var stack = new Stack<Transform>();
            stack.Push(parent.transform);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if ((includeInactive || current.gameObject.activeSelf) && current.name == name)
                    return current.gameObject;

                foreach (Transform child in current)
                    stack.Push(child);
            }

            return null;
        }

        /// <summary>
        /// 按路径查找子对象（如 "Root/Child/GrandChild"）
        /// </summary>
        /// <param name="root">根对象</param>
        /// <param name="path">路径（用分隔符分割）</param>
        /// <param name="separator">路径分隔符，默认 '/'</param>
        /// <param name="includeInactive">是否包含未激活对象</param>
        /// <returns>找到的 GameObject 或 null</returns>
        public static GameObject FindChildByPath(this GameObject root, string path, char separator = '/',
            bool includeInactive = true)
        {
            if (root == null || string.IsNullOrEmpty(path)) return null;

            string[] names = path.Split(separator);
            GameObject current = root;

            foreach (string name in names)
            {
                current = current.FindChild(name, includeInactive); // 递归查找
                if (current == null) return null;
            }

            return current;
        }

        /// <summary>
        /// 递归查找带有指定标签的子对象
        /// </summary>
        /// <param name="parent">父对象</param>
        /// <param name="tag">标签名</param>
        /// <param name="includeInactive">是否包含未激活对象</param>
        /// <returns>找到的 GameObject 或 null</returns>
        public static GameObject FindChildWithTag(this GameObject parent, string tag, bool includeInactive = true)
        {
            foreach (Transform child in parent.transform)
            {
                if ((includeInactive || child.gameObject.activeSelf) && child.CompareTag(tag))
                    return child.gameObject;
                var result = FindChildWithTag(child.gameObject, tag, includeInactive);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// 获取或添加指定类型的组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="obj">目标对象</param>
        /// <returns>组件实例</returns>
        public static T GetOrAddComponent<T>(this GameObject obj) where T : Component
        {
            T component = obj.GetComponent<T>();
            if (component == null) component = obj.AddComponent<T>();
            return component;
        }

        /// <summary>
        /// 判断对象是否有指定类型的组件
        /// </summary>
        public static bool HasComponent<T>(this GameObject obj) where T : Component
        {
            return obj.GetComponent<T>() != null;
        }

        /// <summary>
        /// 获取组件，若未找到则报错
        /// </summary>
        public static T GetComponentRequired<T>(this GameObject obj) where T : Component
        {
            T component = obj.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError($"未找到组件: {typeof(T).Name}，对象: {obj.name}", obj);
            }

            return component;
        }

        /// <summary>
        /// 获取组件，若未找到则警告并可选自动添加
        /// </summary>
        public static T GetComponentLogIfMissing<T>(this GameObject obj, bool addIfMissing = false) where T : Component
        {
            T component = obj.GetComponent<T>();
            if (component == null)
            {
                Debug.LogWarning($"缺少组件: {typeof(T).Name}", obj);
                if (addIfMissing) component = obj.AddComponent<T>();
            }

            return component;
        }

        /// <summary>
        /// 递归设置对象及其所有子对象的激活状态
        /// </summary>
        public static void SetActiveRecursively(this GameObject obj, bool isActive)
        {
            if (obj == null) return;

            obj.SetActive(isActive);
            foreach (Transform child in obj.transform)
            {
                child.gameObject.SetActive(isActive);
            }
        }

        /// <summary>
        /// 切换对象激活状态（开/关）
        /// </summary>
        public static void ToggleActive(this GameObject obj)
        {
            if (obj != null) obj.SetActive(!obj.activeSelf);
        }
    }

    #endregion

    #region Transform & Hierarchy

    /// <summary>
    /// Transform 及层级相关扩展方法
    /// </summary>
    public static class TransformExtensions
    {
        /// <summary>
        /// 重置对象的本地位置、旋转、缩放
        /// </summary>
        public static void ResetTransform(this GameObject obj)
        {
            if (obj == null) return;

            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 计算对象朝向目标点的旋转（2D/3D通用）
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="targetPos">目标点</param>
        /// <param name="is2D">是否2D模式</param>
        /// <returns>旋转四元数</returns>
        public static Quaternion GetRotationToTarget(this GameObject obj, Vector3 targetPos, bool is2D = false)
        {
            Vector3 direction = targetPos - obj.transform.position;
            if (is2D) return Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            return Quaternion.LookRotation(direction);
        }

        /// <summary>
        /// 清空所有子对象
        /// </summary>
        public static void ClearChildren(this GameObject parent)
        {
            if (parent == null) return;

            foreach (Transform child in parent.transform)
            {
                Object.Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// 判断 child 是否为 parent 的子级
        /// </summary>
        public static bool IsChildOf(this GameObject child, GameObject parent)
        {
            if (child == null || parent == null) return false;
            return child.transform.IsChildOf(parent.transform);
        }

        /// <summary>
        /// 获取对象在屏幕上的像素坐标
        /// </summary>
        public static Vector2 GetScreenPosition(this GameObject obj, Camera camera = null)
        {
            if (obj == null) return Vector2.zero;

            if (camera == null) camera = Camera.main;
            return camera.WorldToScreenPoint(obj.transform.position);
        }

        /// <summary>
        /// 批量设置一组对象的父对象
        /// </summary>
        public static void SetParentToAll(this GameObject[] children, GameObject parent)
        {
            foreach (GameObject child in children)
            {
                if (child != null) child.transform.SetParent(parent.transform);
            }
        }

        /// <summary>
        /// 使对象始终面向摄像机（Billboard 效果）
        /// </summary>
        public static void LookAtCamera(this GameObject obj, Camera camera = null)
        {
            if (camera == null) camera = Camera.main;
            obj.transform.forward = camera.transform.forward;
        }

        /// <summary>
        /// 获取对象的完整层级路径（如 Root/Child/GrandChild）
        /// </summary>
        public static string GetHierarchyPath(this GameObject obj)
        {
            if (obj == null) return "null";
            var path = new StringBuilder(obj.name);
            var parent = obj.transform.parent;
            while (parent != null)
            {
                path.Insert(0, parent.name + "/");
                parent = parent.parent;
            }

            return path.ToString();
        }
    }

    #endregion

    #region Component Utilities

    /// <summary>
    /// 组件相关扩展方法
    /// </summary>
    public static class ComponentExtensions
    {
        /// <summary>
        /// 安全销毁指定类型的组件
        /// </summary>
        public static void SafeDestroy<T>(this GameObject obj) where T : Component
        {
            T component = obj.GetComponent<T>();
            if (component != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Object.DestroyImmediate(component);
                else
#endif
                    Object.Destroy(component);
            }
        }

        /// <summary>
        /// 移除所有子对象上的指定类型组件
        /// </summary>
        public static void RemoveComponentsInChildren<T>(this GameObject parent) where T : Component
        {
            foreach (var component in parent.GetComponentsInChildren<T>(true))
            {
                Object.Destroy(component);
            }
        }

        /// <summary>
        /// 复制组件到另一个对象（字段和属性）
        /// </summary>
        public static T CopyComponentTo<T>(this GameObject source, GameObject target) where T : Component
        {
            T original = source.GetComponent<T>();
            if (original == null) return null;

            var newComp = target.AddComponent<T>();
            var type = typeof(T);

            // 复制字段
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                field.SetValue(newComp, field.GetValue(original));
            }

            // 复制属性
            foreach (var prop in type.GetProperties(
                         BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (prop.CanWrite)
                    prop.SetValue(newComp, prop.GetValue(original));
            }

            return newComp;
        }

        /// <summary>
        /// 递归获取所有子对象上的指定类型组件
        /// </summary>
        public static List<T> GetComponentsInChildrenRecursive<T>(this GameObject parent, bool includeInactive = false)
        {
            List<T> components = new List<T>();
            if (parent == null) return components;

            // 先处理子对象
            foreach (Transform child in parent.transform)
            {
                if (includeInactive || child.gameObject.activeSelf)
                {
                    components.AddRange(child.gameObject.GetComponentsInChildrenRecursive<T>(includeInactive));
                }
            }

            // 最后处理当前对象（避免重复）
            components.AddRange(parent.GetComponents<T>());
            return components;
        }

        /// <summary>
        /// 获取所有直接子对象上的指定类型组件（不递归）
        /// </summary>
        public static List<T> GetDirectComponentsInChildren<T>(this GameObject parent) where T : Component
        {
            List<T> components = new List<T>();
            foreach (Transform child in parent.transform)
            {
                T comp = child.GetComponent<T>();
                if (comp != null) components.Add(comp);
            }

            return components;
        }
    }

    #endregion

    #region Physics & Collision

    /// <summary>
    /// 物理与碰撞相关扩展方法
    /// </summary>
    public static class PhysicsExtensions
    {
        /// <summary>
        /// 检查对象是否在地面（Raycast 检测）
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="distance">检测距离</param>
        /// <param name="groundLayer">地面层</param>
        /// <returns>是否在地面</returns>
        public static bool IsGrounded(this GameObject obj, float distance = 0.1f, LayerMask groundLayer = default)
        {
            return Physics.Raycast(obj.transform.position, Vector3.down, distance, groundLayer);
        }

        /// <summary>
        /// 获取最近的2D碰撞体（圆形检测）
        /// </summary>
        public static Collider2D FindNearestCollider2D(this GameObject obj, float radius, LayerMask targetLayer)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(obj.transform.position, radius, targetLayer);
            if (colliders.Length == 0) return null;

            Collider2D nearest = colliders[0];
            float minDistance = Vector2.Distance(obj.transform.position, nearest.transform.position);

            foreach (var collider in colliders)
            {
                float distance = Vector2.Distance(obj.transform.position, collider.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = collider;
                }
            }

            return nearest;
        }

        /// <summary>
        /// 球形检测并返回最近对象
        /// </summary>
        public static GameObject FindNearestByRadius(this GameObject origin, float radius, LayerMask layer)
        {
            Collider[] colliders = Physics.OverlapSphere(origin.transform.position, radius, layer);
            if (colliders.Length == 0) return null;

            GameObject nearest = colliders[0].gameObject;
            float minDistance = Vector3.Distance(origin.transform.position, nearest.transform.position);

            foreach (var col in colliders)
            {
                float dist = Vector3.Distance(origin.transform.position, col.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = col.gameObject;
                }
            }

            return nearest;
        }

        /// <summary>
        /// 检查对象是否在指定 LayerMask 层级
        /// </summary>
        public static bool IsInLayerMask(this GameObject obj, LayerMask layerMask)
        {
            return layerMask == (layerMask | (1 << obj.layer));
        }

        /// <summary>
        /// 批量控制物理模拟的开关（所有子对象 Rigidbody）
        /// </summary>
        public static void SetPhysicsActive(this GameObject root, bool active)
        {
            var rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rigidbodies)
            {
                rb.isKinematic = !active;
            }
        }

        /// <summary>
        /// 递归设置对象及其所有子对象的层级（Layer）
        /// </summary>
        public static void SetLayerRecursively(this GameObject obj, int layer)
        {
            if (obj == null) return;

            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                child.gameObject.SetLayerRecursively(layer);
            }
        }

        /// <summary>
        /// 检查对象是否有指定标签（支持多个标签）
        /// </summary>
        public static bool CompareTag(this GameObject obj, params string[] tags)
        {
            if (obj == null) return false;

            foreach (string tag in tags)
            {
                if (obj.CompareTag(tag)) return true;
            }

            return false;
        }
    }

    #endregion

    #region Instantiation & Pooling

    /// <summary>
    /// 实例化与对象池相关扩展方法
    /// </summary>
    public static class InstantiationExtensions
    {
        /// <summary>
        /// 安全实例化对象（自动重置 Transform）
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="parent">父对象</param>
        /// <param name="worldPositionStays">是否保持世界坐标</param>
        /// <returns>实例化对象</returns>
        public static GameObject SafeInstantiate(this GameObject prefab, Transform parent = null,
            bool worldPositionStays = false)
        {
            var instance = Object.Instantiate(prefab, parent, worldPositionStays);
            if (worldPositionStays) return instance;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = prefab.transform.localScale; // 保持原始缩放
            return instance;
        }

        /// <summary>
        /// 延迟实例化对象并回调
        /// </summary>
        /// <param name="caller">MonoBehaviour 调用者</param>
        /// <param name="prefab">预制体</param>
        /// <param name="delay">延迟秒数</param>
        /// <param name="onComplete">实例化完成回调</param>
        /// <returns>协程句柄</returns>
        public static Coroutine InstantiateDelayed(this MonoBehaviour caller, GameObject prefab, float delay,
            System.Action<GameObject> onComplete)
        {
            return caller.StartCoroutine(InstantiateDelayedCoroutine(prefab, delay, onComplete));
        }

        /// <summary>
        /// 协程：延迟实例化
        /// </summary>
        private static IEnumerator InstantiateDelayedCoroutine(GameObject prefab, float delay,
            System.Action<GameObject> onComplete)
        {
            yield return new WaitForSeconds(delay);
            GameObject instance = Object.Instantiate(prefab);
            onComplete?.Invoke(instance);
        }

        /// <summary>
        /// 生成唯一ID（可用于网络或存档）
        /// </summary>
        public static string GenerateUniqueId(this GameObject obj)
        {
            return $"{obj.name}_{obj.GetInstanceID()}_{Time.time}";
        }

        /// <summary>
        /// 获取随机子对象的位置（常用于生成点）
        /// </summary>
        public static Vector3 GetRandomChildPosition(this GameObject parent)
        {
            if (parent.transform.childCount == 0) return parent.transform.position;
            Transform randomChild = parent.transform.GetChild(Random.Range(0, parent.transform.childCount));
            return randomChild.position;
        }
    }

    #endregion

    #region Editor Tools

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器专用扩展方法
    /// </summary>
    public static class EditorExtensions
    {
        /// <summary>
        /// 快速创建空子物体并命名
        /// </summary>
        public static GameObject CreateChild(this GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            child.ResetTransform();
            UnityEditor.Undo.RegisterCreatedObjectUndo(child, "Create Child");
            return child;
        }

        /// <summary>
        /// 在 Hierarchy 中高亮显示对象
        /// </summary>
        public static void PingInEditor(this GameObject obj)
        {
            if (obj != null) UnityEditor.EditorGUIUtility.PingObject(obj);
        }

        /// <summary>
        /// 编辑器下安全添加组件（避免重复）
        /// </summary>
        public static T AddComponentIfMissing<T>(this GameObject obj) where T : Component
        {
            T component = obj.GetComponent<T>();
            if (component == null) component = obj.AddComponent<T>();
            return component;
        }

        /// <summary>
        /// 批量重命名子物体（带序号）
        /// </summary>
        public static void RenameChildrenWithIndex(this GameObject parent, string prefix = "Child_")
        {
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                parent.transform.GetChild(i).name = $"{prefix}{i}";
            }
        }

        /// <summary>
        /// 批量修改子对象名称（添加前缀/后缀）
        /// </summary>
        public static void RenameChildren(this GameObject parent, string prefix = "", string suffix = "")
        {
            if (parent == null) return;

            foreach (Transform child in parent.transform)
            {
                child.name = $"{prefix}{child.name}{suffix}";
            }
        }
    }
#endif

    #endregion

    #region Debug & Visualization

    /// <summary>
    /// 调试与可视化相关扩展方法
    /// </summary>
    public static class DebugExtensions
    {
        /// <summary>
        /// 绘制方向线（调试用）
        /// </summary>
        public static void DrawDirectionLine(this GameObject obj, Vector3 direction, Color color, float duration = 1f)
        {
            Debug.DrawRay(obj.transform.position, direction, color, duration);
        }

        /// <summary>
        /// 显示碰撞体边界（Gizmos）
        /// </summary>
        public static void DrawColliderBounds(this GameObject obj, Color color)
        {
            Collider col = obj.GetComponent<Collider>();
            if (col == null) return;

            Bounds bounds = col.bounds;
            Gizmos.color = color;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }

    #endregion

    #region Coroutine Utilities

    /// <summary>
    /// 协程相关扩展方法
    /// </summary>
    public static class CoroutineExtensions
    {
        /// <summary>
        /// 延迟执行方法（基于协程）
        /// </summary>
        /// <param name="obj">目标对象</param>
        /// <param name="action">要执行的方法</param>
        /// <param name="delay">延迟秒数</param>
        /// <param name="coroutineRunner">协程执行器</param>
        /// <returns>协程句柄</returns>
        public static Coroutine InvokeDelayed(this GameObject obj, System.Action action, float delay,
            MonoBehaviour coroutineRunner = null)
        {
            if (obj == null || action == null) return null;

            if (coroutineRunner == null) coroutineRunner = obj.GetComponent<MonoBehaviour>();
            if (coroutineRunner == null) coroutineRunner = obj.AddComponent<EmptyMonoBehaviour>();

            return coroutineRunner.StartCoroutine(InvokeDelayedCoroutine(action, delay));
        }

        /// <summary>
        /// 延迟销毁对象
        /// </summary>
        public static void DestroyDelayed(this GameObject obj, float delay)
        {
            if (obj == null) return;
            Object.Destroy(obj, delay);
        }

        /// <summary>
        /// 协程：延迟执行
        /// </summary>
        private static IEnumerator InvokeDelayedCoroutine(System.Action action, float delay)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        /// <summary>
        /// 私有协程执行器（用于无 MonoBehaviour 时）
        /// </summary>
        private class EmptyMonoBehaviour : MonoBehaviour
        {
        }
    }

    #endregion

    #region Animation & Effects

    /// <summary>
    /// 动画与特效相关扩展方法
    /// </summary>
    public static class AnimationExtensions
    {
        /// <summary>
        /// 播放 Animator 动画
        /// </summary>
        /// <param name="obj">目标对象</param>
        /// <param name="animationName">动画名称</param>
        /// <param name="speed">播放速度</param>
        public static void PlayAnimation(this GameObject obj, string animationName, float speed = 1f)
        {
            Animator animator = obj.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning($"GameObject {obj.name} does not have an Animator component.");
                return;
            }

            animator.speed = speed;
            animator.Play(animationName);
        }

        /// <summary>
        /// 停止所有 Animator 动画
        /// </summary>
        public static void StopAllAnimations(this GameObject obj)
        {
            Animator animator = obj.GetComponent<Animator>();
            if (animator != null) animator.enabled = false;
        }

        /// <summary>
        /// 批量设置所有渲染器的启用状态
        /// </summary>
        public static void SetRenderersEnabled(this GameObject parent, bool enabled)
        {
            foreach (var renderer in parent.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = enabled;
            }
        }
    }

    #endregion
}