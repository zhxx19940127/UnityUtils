// 自动生成，请勿手改（可重复生成覆盖）
using UnityEngine;
using UnityEngine.UI;

public class Panel : MonoBehaviour
{
    // <auto-fields>
    private TMPro.TMP_Text _txtTextTMP;
    private TMPro.TMP_Text _txtTextTMP_1;
    private UnityEngine.UI.Button _btnButton;
    private UnityEngine.UI.RawImage _imgRawImage;
    private UnityEngine.UI.Slider _sldSlider;
    private UnityEngine.UI.Toggle _toggle;
    // </auto-fields>
    // <auto-props>
    public TMPro.TMP_Text TextTMP => _txtTextTMP;
    public TMPro.TMP_Text TextTMP1 => _txtTextTMP_1;
    public UnityEngine.UI.Button Button => _btnButton;
    public UnityEngine.UI.RawImage RawImage => _imgRawImage;
    public UnityEngine.UI.Slider Slider => _sldSlider;
    public UnityEngine.UI.Toggle Gle => _toggle;
    // </auto-props>
    // <auto-assign>
    private void Awake()
    {
        var __tr0 = transform.Find("Text (TMP)");
        if (__tr0 == null) Debug.LogError("[AutoUI] 未找到路径: Text (TMP)");
        else { _txtTextTMP = __tr0.GetComponent<TMPro.TMP_Text>(); if (_txtTextTMP == null) Debug.LogError("[AutoUI] 路径 Text (TMP) 缺少组件 TMPro.TMP_Text"); }
        var __tr1 = transform.Find("Button/Text (TMP)");
        if (__tr1 == null) Debug.LogError("[AutoUI] 未找到路径: Button/Text (TMP)");
        else { _txtTextTMP_1 = __tr1.GetComponent<TMPro.TMP_Text>(); if (_txtTextTMP_1 == null) Debug.LogError("[AutoUI] 路径 Button/Text (TMP) 缺少组件 TMPro.TMP_Text"); }
        var __tr2 = transform.Find("Button");
        if (__tr2 == null) Debug.LogError("[AutoUI] 未找到路径: Button");
        else { _btnButton = __tr2.GetComponent<UnityEngine.UI.Button>(); if (_btnButton == null) Debug.LogError("[AutoUI] 路径 Button 缺少组件 UnityEngine.UI.Button"); }
        var __tr3 = transform.Find("RawImage");
        if (__tr3 == null) Debug.LogError("[AutoUI] 未找到路径: RawImage");
        else { _imgRawImage = __tr3.GetComponent<UnityEngine.UI.RawImage>(); if (_imgRawImage == null) Debug.LogError("[AutoUI] 路径 RawImage 缺少组件 UnityEngine.UI.RawImage"); }
        var __tr4 = transform.Find("Slider");
        if (__tr4 == null) Debug.LogError("[AutoUI] 未找到路径: Slider");
        else { _sldSlider = __tr4.GetComponent<UnityEngine.UI.Slider>(); if (_sldSlider == null) Debug.LogError("[AutoUI] 路径 Slider 缺少组件 UnityEngine.UI.Slider"); }
        var __tr5 = transform.Find("Toggle");
        if (__tr5 == null) Debug.LogError("[AutoUI] 未找到路径: Toggle");
        else { _toggle = __tr5.GetComponent<UnityEngine.UI.Toggle>(); if (_toggle == null) Debug.LogError("[AutoUI] 路径 Toggle 缺少组件 UnityEngine.UI.Toggle"); }
    }
    // </auto-assign>
    // <user-code>
    // 你的手写逻辑请写在这里，生成器不会修改此区域
    // </user-code>
}
