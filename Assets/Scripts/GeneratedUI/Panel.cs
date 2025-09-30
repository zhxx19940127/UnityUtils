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
        _txtTextTMP = transform.Find("Text (TMP)").GetComponent<TMPro.TMP_Text>();
        _txtTextTMP_1 = transform.Find("Button/Text (TMP)").GetComponent<TMPro.TMP_Text>();
        _btnButton = transform.Find("Button").GetComponent<UnityEngine.UI.Button>();
        _imgRawImage = transform.Find("RawImage").GetComponent<UnityEngine.UI.RawImage>();
        _sldSlider = transform.Find("Slider").GetComponent<UnityEngine.UI.Slider>();
        _toggle = transform.Find("Toggle").GetComponent<UnityEngine.UI.Toggle>();
    }
    // </auto-assign>


    // <user-code>
    // 你的手写逻辑请写在这里，生成器不会修改此区域
    private void Start()
    {
        // 示例：按钮点击事件
        _btnButton.onClick.AddListener(null);
    }
    // </user-code>
}