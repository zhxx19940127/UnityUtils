// 自动生成，请勿手改（可重复生成覆盖）
using UnityEngine;
using UnityEngine.UI;

namespace test
{
public class Panel : UnityEngine.MonoBehaviour
{
    // <auto-fields>
    private TMPro.TMP_Text _txtTextTMP;
    private TMPro.TMP_Text _txtTextTMP_1;
    private UnityEngine.UI.Button _btnButton;
    private UnityEngine.UI.RawImage _imgRawImage;
    private UnityEngine.UI.Slider _sldSlider;
    private UnityEngine.UI.Text _txtText;
    private UnityEngine.UI.Text _txtText11;
    private UnityEngine.UI.Toggle _toggle;
    // </auto-fields>
    // <auto-props>
    public TMPro.TMP_Text TextTMP => _txtTextTMP;
    public TMPro.TMP_Text TextTMP1 => _txtTextTMP_1;
    public UnityEngine.UI.Button Button => _btnButton;
    public UnityEngine.UI.RawImage RawImage => _imgRawImage;
    public UnityEngine.UI.Slider Slider => _sldSlider;
    public UnityEngine.UI.Text Text => _txtText;
    public UnityEngine.UI.Text Text11 => _txtText11;
    public UnityEngine.UI.Toggle Gle => _toggle;
    // </auto-props>
    // <auto-assign>
    public void InitRefs()
    {
        var __tr0 = transform.Find("Text (TMP)");
        if (__tr0 == null) Debug.LogError("[AutoUI] 未找到路径: Text (TMP)");
        else {
            var __comps = __tr0.GetComponents<TMPro.TMP_Text>();
            _txtTextTMP = (__comps != null && __comps.Length > 0) ? __comps[0] : null;
            if (_txtTextTMP == null) Debug.LogError("[AutoUI] 路径 Text (TMP) 未找到索引 0 的组件 TMPro.TMP_Text");
        }
        var __tr1 = transform.Find("Button/Text (TMP)");
        if (__tr1 == null) Debug.LogError("[AutoUI] 未找到路径: Button/Text (TMP)");
        else {
            var __comps = __tr1.GetComponents<TMPro.TMP_Text>();
            _txtTextTMP_1 = (__comps != null && __comps.Length > 0) ? __comps[0] : null;
            if (_txtTextTMP_1 == null) Debug.LogError("[AutoUI] 路径 Button/Text (TMP) 未找到索引 0 的组件 TMPro.TMP_Text");
        }
        var __tr2 = transform.Find("Button");
        if (__tr2 == null) Debug.LogError("[AutoUI] 未找到路径: Button");
        else {
            var __comps = __tr2.GetComponents<UnityEngine.UI.Button>();
            _btnButton = (__comps != null && __comps.Length > 0) ? __comps[0] : null;
            if (_btnButton == null) Debug.LogError("[AutoUI] 路径 Button 未找到索引 0 的组件 UnityEngine.UI.Button");
        }
        var __tr3 = transform.Find("RawImage");
        if (__tr3 == null) Debug.LogError("[AutoUI] 未找到路径: RawImage");
        else {
            var __comps = __tr3.GetComponents<UnityEngine.UI.RawImage>();
            _imgRawImage = (__comps != null && __comps.Length > 0) ? __comps[0] : null;
            if (_imgRawImage == null) Debug.LogError("[AutoUI] 路径 RawImage 未找到索引 0 的组件 UnityEngine.UI.RawImage");
        }
        var __tr4 = transform.Find("Slider");
        if (__tr4 == null) Debug.LogError("[AutoUI] 未找到路径: Slider");
        else {
            var __comps = __tr4.GetComponents<UnityEngine.UI.Slider>();
            _sldSlider = (__comps != null && __comps.Length > 0) ? __comps[0] : null;
            if (_sldSlider == null) Debug.LogError("[AutoUI] 路径 Slider 未找到索引 0 的组件 UnityEngine.UI.Slider");
        }
        var __tr5 = transform.Find("Text");
        if (__tr5 == null) Debug.LogError("[AutoUI] 未找到路径: Text");
        else {
            var __comps = __tr5.GetComponents<UnityEngine.UI.Text>();
            _txtText = (__comps != null && __comps.Length > 0) ? __comps[0] : null;
            if (_txtText == null) Debug.LogError("[AutoUI] 路径 Text 未找到索引 0 的组件 UnityEngine.UI.Text");
        }
        var __tr6 = transform.Find("Text_1");
        if (__tr6 == null) Debug.LogError("[AutoUI] 未找到路径: Text_1");
        else {
            var __comps = __tr6.GetComponents<UnityEngine.UI.Text>();
            _txtText11 = (__comps != null && __comps.Length > 0) ? __comps[0] : null;
            if (_txtText11 == null) Debug.LogError("[AutoUI] 路径 Text_1 未找到索引 0 的组件 UnityEngine.UI.Text");
        }
        var __tr7 = transform.Find("Toggle");
        if (__tr7 == null) Debug.LogError("[AutoUI] 未找到路径: Toggle");
        else {
            var __comps = __tr7.GetComponents<UnityEngine.UI.Toggle>();
            _toggle = (__comps != null && __comps.Length > 0) ? __comps[0] : null;
            if (_toggle == null) Debug.LogError("[AutoUI] 路径 Toggle 未找到索引 0 的组件 UnityEngine.UI.Toggle");
        }
    }
    // </auto-assign>
    // <user-code>
    // 你的手写逻辑请写在这里，生成器不会修改此区域
    // </user-code>
}
}
