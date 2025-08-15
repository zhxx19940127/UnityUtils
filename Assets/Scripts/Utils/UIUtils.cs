namespace GameObjectToolkit
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// UI 工具类
    /// 提供全面的 UI 控件操作和辅助功能
    /// </summary>
    public static class UIUtils
    {
        #region UI 元素控制

        /// <summary>
        /// 设置 UI 元素是否可见（保留布局空间）
        /// </summary>
        /// <param name="element">UI 元素</param>
        /// <param name="visible">是否可见</param>
        public static void SetVisible(this Graphic element, bool visible)
        {
            if (element != null)
            {
                element.color = visible
                    ? new Color(element.color.r, element.color.g, element.color.b, 1f)
                    : new Color(element.color.r, element.color.g, element.color.b, 0f);
            }
        }

        /// <summary>
        /// 设置 UI 元素是否激活（不保留布局空间）
        /// </summary>
        public static void SetActive(this GameObject element, bool active)
        {
            if (element != null && element.activeSelf != active)
            {
                element.SetActive(active);
            }
        }

        /// <summary>
        /// 设置按钮交互状态
        /// </summary>
        public static void SetInteractable(this Button button, bool interactable, float disabledAlpha = 0.5f)
        {
            if (button != null)
            {
                button.interactable = interactable;

                // 调整透明度
                Graphic targetGraphic = button.targetGraphic;
                if (targetGraphic != null)
                {
                    Color color = targetGraphic.color;
                    targetGraphic.color = new Color(
                        color.r,
                        color.g,
                        color.b,
                        interactable ? 1f : disabledAlpha
                    );
                }
            }
        }

        /// <summary>
        /// 设置文本内容（安全版本）
        /// </summary>
        public static void SetText(this Text text, string content)
        {
            if (text != null)
            {
                text.text = content ?? string.Empty;
            }
        }

        /// <summary>
        /// 设置文本内容带格式（安全版本）
        /// </summary>
        public static void SetTextFormat(this Text text, string format, params object[] args)
        {
            if (text != null)
            {
                text.text = string.Format(format ?? string.Empty, args);
            }
        }

        /// <summary>
        /// 设置图片精灵（安全版本）
        /// </summary>
        public static void SetSprite(this Image image, Sprite sprite)
        {
            if (image != null)
            {
                image.sprite = sprite;
                image.enabled = sprite != null;
            }
        }

        #endregion

        #region UI 动画效果

        /// <summary>
        /// 渐显/渐隐动画
        /// </summary>
        /// <param name="element">UI 元素</param>
        /// <param name="targetAlpha">目标透明度</param>
        /// <param name="duration">持续时间</param>
        /// <param name="onComplete">完成回调</param>
        public static IEnumerator Fade(this Graphic element, float targetAlpha, float duration,
            System.Action onComplete = null)
        {
            if (element == null) yield break;

            float startAlpha = element.color.a;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

                Color color = element.color;
                element.color = new Color(color.r, color.g, color.b, alpha);

                yield return null;
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// 缩放动画
        /// </summary>
        public static IEnumerator Scale(this RectTransform transform, Vector3 targetScale, float duration,
            System.Action onComplete = null)
        {
            if (transform == null) yield break;

            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);

                yield return null;
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// 移动动画
        /// </summary>
        public static IEnumerator Move(this RectTransform transform, Vector2 targetPosition, float duration,
            System.Action onComplete = null)
        {
            if (transform == null) yield break;

            Vector2 startPosition = transform.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);

                yield return null;
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// 颜色过渡动画
        /// </summary>
        public static IEnumerator ColorTransition(this Graphic element, Color targetColor, float duration,
            System.Action onComplete = null)
        {
            if (element == null) yield break;

            Color startColor = element.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                element.color = Color.Lerp(startColor, targetColor, t);

                yield return null;
            }

            onComplete?.Invoke();
        }

        #endregion

        #region 布局辅助

        /// <summary>
        /// 设置 UI 元素锚点（不改变位置）
        /// </summary>
        public static void SetAnchor(this RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rectTransform == null) return;

            Vector2 pivot = rectTransform.pivot;
            Vector2 sizeDelta = rectTransform.sizeDelta;
            Vector2 anchoredPosition = rectTransform.anchoredPosition;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;

            rectTransform.pivot = pivot;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchoredPosition = anchoredPosition;
        }

        /// <summary>
        /// 设置 UI 元素填充父容器
        /// </summary>
        public static void SetFillParent(this RectTransform rectTransform)
        {
            if (rectTransform == null) return;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 设置 UI 元素大小
        /// </summary>
        public static void SetSize(this RectTransform rectTransform, Vector2 size)
        {
            if (rectTransform == null) return;

            Vector2 pivot = rectTransform.pivot;
            Vector2 anchoredPosition = rectTransform.anchoredPosition;

            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);

            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
        }

        /// <summary>
        /// 获取 UI 元素在世界空间中的矩形
        /// </summary>
        public static Rect GetWorldRect(this RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            Vector2 min = corners[0];
            Vector2 max = corners[2];

            return new Rect(min, max - min);
        }

        #endregion

        #region 事件管理

        /// <summary>
        /// 添加点击事件监听（自动移除重复监听）
        /// </summary>
        public static void AddClickListener(this Button button, UnityEngine.Events.UnityAction callback)
        {
            if (button == null) return;

            button.onClick.RemoveListener(callback);
            button.onClick.AddListener(callback);
        }

        /// <summary>
        /// 添加拖拽事件监听
        /// </summary>
        public static void AddDragListener(this GameObject gameObject,
            UnityEngine.Events.UnityAction<PointerEventData> onBeginDrag = null,
            UnityEngine.Events.UnityAction<PointerEventData> onDrag = null,
            UnityEngine.Events.UnityAction<PointerEventData> onEndDrag = null)
        {
            if (gameObject == null) return;

            EventTrigger trigger = gameObject.GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();
            trigger.triggers ??= new List<EventTrigger.Entry>();

            if (onBeginDrag != null)
            {
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
                entry.callback.AddListener((data) => onBeginDrag((PointerEventData)data));
                trigger.triggers.Add(entry);
            }

            if (onDrag != null)
            {
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
                entry.callback.AddListener((data) => onDrag((PointerEventData)data));
                trigger.triggers.Add(entry);
            }

            if (onEndDrag != null)
            {
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
                entry.callback.AddListener((data) => onEndDrag((PointerEventData)data));
                trigger.triggers.Add(entry);
            }
        }

        /// <summary>
        /// 添加悬停事件监听
        /// </summary>
        public static void AddHoverListener(this GameObject gameObject,
            UnityEngine.Events.UnityAction onPointerEnter,
            UnityEngine.Events.UnityAction onPointerExit)
        {
            if (gameObject == null) return;

            EventTrigger trigger = gameObject.GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();
            trigger.triggers ??= new List<EventTrigger.Entry>();

            if (onPointerEnter != null)
            {
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                entry.callback.AddListener((data) => onPointerEnter());
                trigger.triggers.Add(entry);
            }

            if (onPointerExit != null)
            {
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                entry.callback.AddListener((data) => onPointerExit());
                trigger.triggers.Add(entry);
            }
        }

        #endregion

        #region 高级功能

        /// <summary>
        /// 查找所有子物体中的指定类型组件（包括非激活物体）
        /// </summary>
        public static List<T> FindComponentsInChildren<T>(this GameObject parent, bool includeInactive = true)
            where T : Component
        {
            List<T> components = new List<T>();
            if (parent != null)
            {
                components.AddRange(parent.GetComponentsInChildren<T>(includeInactive));
            }

            return components;
        }

        /// <summary>
        /// 查找所有子物体中的指定名称对象（包括非激活物体）
        /// </summary>
        public static GameObject FindChildByName(this GameObject parent, string name, bool includeInactive = true)
        {
            if (parent == null) return null;

            Transform[] children = parent.GetComponentsInChildren<Transform>(includeInactive);
            return children.FirstOrDefault(child => child.name == name)?.gameObject;
        }

        /// <summary>
        /// 创建 UI 元素的克隆
        /// </summary>
        public static T CloneUIElement<T>(this T original, Transform parent = null) where T : Component
        {
            if (original == null) return null;

            GameObject clone = Object.Instantiate(original.gameObject, parent ?? original.transform.parent);
            clone.transform.localPosition = original.transform.localPosition;
            clone.transform.localRotation = original.transform.localRotation;
            clone.transform.localScale = original.transform.localScale;

            return clone.GetComponent<T>();
        }

        /// <summary>
        /// 重置 UI 元素的变换
        /// </summary>
        public static void ResetTransform(this RectTransform rectTransform)
        {
            if (rectTransform == null) return;

            rectTransform.localPosition = Vector3.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }

        /// <summary>
        /// 将世界坐标转换为 UI 本地坐标
        /// </summary>
        public static Vector2 WorldToUISpace(this Canvas canvas, Vector3 worldPos)
        {
            if (canvas == null) return Vector2.zero;

            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                screenPos,
                canvas.worldCamera,
                out Vector2 localPos
            );

            return localPos;
        }

        #endregion

        #region 实用扩展

        /// <summary>
        /// 设置下拉菜单选项
        /// </summary>
        public static void SetOptions(this Dropdown dropdown, List<string> options, int defaultIndex = 0)
        {
            if (dropdown == null) return;

            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdown.value = Mathf.Clamp(defaultIndex, 0, options.Count - 1);
        }

        /// <summary>
        /// 设置滚动视图内容位置（标准化）
        /// </summary>
        public static void SetNormalizedPosition(this ScrollRect scrollRect, float horizontal, float vertical)
        {
            if (scrollRect == null) return;

            scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(horizontal);
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(vertical);
        }

        /// <summary>
        /// 设置滑动条值（带动画）
        /// </summary>
        public static IEnumerator SetSliderValueAnimated(this Slider slider, float targetValue, float duration)
        {
            if (slider == null) yield break;

            float startValue = slider.value;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                slider.value = Mathf.Lerp(startValue, targetValue, t);
                yield return null;
            }
        }

        /// <summary>
        /// 设置输入框文本（安全版本）
        /// </summary>
        public static void SetInputText(this InputField inputField, string text)
        {
            if (inputField != null)
            {
                inputField.text = text ?? string.Empty;
            }
        }

        #endregion
    }
}