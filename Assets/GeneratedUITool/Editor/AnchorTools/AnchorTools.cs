#region 一键设置锚点工具

using UnityEngine;
using UnityEditor;
using System;

public static class AnchorTools
{
    private static Vector2 _initPivot; // 初始的pivot点缓存

    [MenuItem("GameObject/UI Tools/Set Anchors for Selected &a", priority = 10)]
    public static void SetAnchorsForSelected()
    {
        // 获取实际在Hierarchy面板中点击选中的对象（不包括自动选中的子对象）
        var directlySelected = Selection.transforms;

        if (directlySelected.Length == 0)
        {
            return;
        }

        int processedCount = 0;
        Undo.RecordObjects(directlySelected, "Set Anchors");

        foreach (var transform in directlySelected)
        {
            if (!(transform is RectTransform rt))
            {
                Debug.LogWarning($"跳过非UI对象: {transform.name}", transform.gameObject);
                continue;
            }

            var parent = rt.parent as RectTransform;
            if (parent == null)
            {
                Debug.LogWarning($"跳过根Canvas对象: {rt.name}", rt.gameObject);
                continue;
            }

            try
            {
                ResetToCenter(rt);
                CalculateAndSetAnchors(rt, parent);
                processedCount++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"处理 {rt.name} 失败: {ex.Message}", rt.gameObject);
            }

            Debug.Log($"✅ 已完成: 成功处理 {processedCount}/{directlySelected.Length} 个选中对象");
        }

        if (processedCount < directlySelected.Length)
        {
            Debug.LogWarning("⚠ 注意：部分对象未被处理，请查看警告信息");
        }

        Selection.objects = null;
    }

    [MenuItem("CONTEXT/RectTransform/Set Anchors (Preserve Position)")]
    private static void SetAnchorsContextMenu(MenuCommand command)
    {
        var rectTransform = command.context as RectTransform;
        if (rectTransform == null) return;

        var parent = rectTransform.parent as RectTransform;
        if (parent == null)
        {
            EditorUtility.DisplayDialog("错误", "不能在Canvas上直接操作！", "确定");
            return;
        }

        Undo.RecordObject(rectTransform, "Set Anchors");
        ResetToCenter(rectTransform);
        CalculateAndSetAnchors(rectTransform, parent);
    }

    [MenuItem("CONTEXT/RectTransform/Set Anchors to Parent Center")]
    private static void SetAnchorsToParentCenter(MenuCommand command)
    {
        var rectTransform = command.context as RectTransform;
        if (rectTransform == null) return;

        var parent = rectTransform.parent as RectTransform;
        if (parent == null)
        {
            Debug.LogWarning("无法在根Canvas上设置锚点");
            return;
        }

        Undo.RecordObject(rectTransform, "Set Anchors to Parent Center");

        // 1. 保存当前尺寸和世界位置
        Vector2 originalSize = rectTransform.rect.size;
        Vector3 originalWorldPos = rectTransform.position;

        // 2. 设置锚点到父对象中心
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        // 3. 恢复尺寸（关键步骤！）
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalSize.x);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, originalSize.y);

        // 4. 恢复世界位置
        rectTransform.position = originalWorldPos;

        Debug.Log($"已将 {rectTransform.name} 的锚点设置为父对象中心 (尺寸保持: {originalSize})", rectTransform);
    }

    /// <summary>
    /// 将RectTransform重置为居中状态
    /// </summary>
    private static void ResetToCenter(RectTransform rt)
    {
        _initPivot = rt.pivot;
        var size = rt.rect.size;
        var position = rt.localPosition;

        // 计算调整后的位置
        var newPosition = new Vector3(
            position.x + (0.5f - _initPivot.x) * size.x,
            position.y + (0.5f - _initPivot.y) * size.y,
            position.z
        );

        // 重置锚点和pivot
        rt.pivot = Vector2.one * 0.5f;
        rt.anchorMin = Vector2.one * 0.5f;
        rt.anchorMax = Vector2.one * 0.5f;

        // 保持原尺寸
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);

        // 设置新位置
        rt.localPosition = newPosition;
    }

    /// <summary>
    /// 计算并设置锚点以保持当前相对位置
    /// </summary>
    private static void CalculateAndSetAnchors(RectTransform rt, RectTransform parent)
    {
        var parentSize = parent.rect.size;
        var rtSize = rt.rect.size;
        var rtPos = rt.anchoredPosition;

        // 计算边界
        float minX = (rtPos.x - rtSize.x / 2f) / parentSize.x + 0.5f;
        float minY = (rtPos.y - rtSize.y / 2f) / parentSize.y + 0.5f;
        float maxX = (rtPos.x + rtSize.x / 2f) / parentSize.x + 0.5f;
        float maxY = (rtPos.y + rtSize.y / 2f) / parentSize.y + 0.5f;

        // 设置锚点
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);

        // 重置偏移量
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // 恢复原始pivot
        rt.pivot = _initPivot;
    }
}

#endregion