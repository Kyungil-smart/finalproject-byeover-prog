// 생성자 : 김영찬
// 버튼을 누르면 인첸트 교체 후보가 Before창 테이블에 표기되도록 연결하는 스크립트
// ToDo : UI 참조만 걸려있음으로 스크립트는 인첸트 담당자가 작성해야됨

// 2차 수정자 : 조규민
// 수정 내용 : 보유 인챈트 목록에서 선택된 인챈트 데이터를 Presenter로 전달하고 정보 테이블 미연결 시 NullReference 방지
//           선택 버튼 참조가 비어 있으면 같은 오브젝트의 Button을 사용하도록 방어
//           스킬·스탯 보유 인챈트의 레벨 TMP를 자동 복구하고 Lv.n 형식으로 표시

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnchantChangeSelectButtonUI : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private Image _skillImage;
    [Tooltip("보유 인챈트의 현재 레벨을 표시할 텍스트")]
    [SerializeField] private TextMeshProUGUI _skillLevelText;
    [SerializeField] private Button _selectButton;
    [Tooltip("정보를 넘겨줄 정보테이블 UI")]
    [SerializeField] private EnchantChangeInfoTableUI _infoTableUI;

    [Header("레벨 텍스트 그림자")]
    [SerializeField] private bool _useLevelTextShadow = true;
    [SerializeField] private Color _levelTextShadowColor = new Color(0f, 0f, 0f, 0.65f);
    [SerializeField] private Vector2 _levelTextShadowDistance = new Vector2(2f, -2f);

    [Header("아이콘 테두리")]
    [SerializeField] private bool _useIconOutline = true;
    [SerializeField] private Color _iconOutlineColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f);
    [SerializeField] private float _iconOutlineThickness = 4f;
    
    private EnchantDisplayData _enchantDisplayData;

    public event Action<int> OnEnchantSelected;
    public event Action<EnchantDisplayData> OnEnchantDisplaySelected;

    public EnchantChangeInfoTableUI InfoTableUI => _infoTableUI;
    public EnchantDisplayData EnchantDisplayData => _enchantDisplayData;

    private void Awake()
    {
        if (_enchantDisplayData == null)
        {
            ClearInfo();
        }
    }

    private void OnEnable()
    {
        if (_enchantDisplayData == null)
        {
            ClearInfo();
        }

        Button _button = GetSelectButton();
        if(_button != null)
        {
            _button.onClick.AddListener(OnSelectButtonClick);
        }
    }

    private void OnDisable()
    {
        Button _button = GetSelectButton();
        if(_button != null)
        {
            _button.onClick.RemoveListener(OnSelectButtonClick);
        }
    }

    public void SetInfo(EnchantDisplayData _enchantDisplayData)
    {
        this._enchantDisplayData = _enchantDisplayData;
        _skillImage = ResolveIconImage();

        if (this._enchantDisplayData == null)
        {
            ClearInfo();
            return;
        }

        SetLevelText(_enchantDisplayData.Level);

        if (_skillImage != null)
        {
            if (!_skillImage.gameObject.activeSelf)
            {
                _skillImage.gameObject.SetActive(true);
            }

            _skillImage.enabled = true;
            // 추가: 조규민 - 보유 인챈트 선택 버튼에도 같은 ImageKey 기반 아이콘을 표시한다.
            EnchantIconLoader.ApplyIcon(_skillImage, _enchantDisplayData.ImageKey);
        }

        SetSelected(false);
    }

    public void ClearInfo()
    {
        _enchantDisplayData = null;
        _skillImage = ResolveIconImage();

        TextMeshProUGUI _levelText = GetLevelText();
        if (_levelText != null)
        {
            _levelText.text = string.Empty;
            _levelText.gameObject.SetActive(false);
        }

        if (_skillImage != null)
        {
            _skillImage.sprite = null;
            _skillImage.enabled = false;
            _skillImage.gameObject.SetActive(false);
        }

        SetSelected(false);
    }

    public void SetSelected(bool _isSelected)
    {
        ApplyButtonOutline(_enchantDisplayData != null && _isSelected);
    }

    private void OnSelectButtonClick()
    {
        if(_enchantDisplayData == null) return;
        if (_infoTableUI != null)
        {
            _infoTableUI.SetInfo(_enchantDisplayData);
        }

        OnEnchantSelected?.Invoke(_enchantDisplayData.EnchantId);
        OnEnchantDisplaySelected?.Invoke(_enchantDisplayData);
    }

    private Button GetSelectButton()
    {
        if (_selectButton != null)
        {
            return _selectButton;
        }

        _selectButton = GetComponent<Button>();
        return _selectButton;
    }

    private void SetLevelText(int _level)
    {
        TextMeshProUGUI _levelText = GetLevelText();
        if (_levelText == null)
        {
            Debug.LogWarning("[EnchantChangeSelectButtonUI] 레벨 Text (TMP) 참조를 찾을 수 없습니다.", this);
            return;
        }

        if (!_levelText.gameObject.activeSelf)
        {
            _levelText.gameObject.SetActive(true);
        }

        _levelText.enabled = true;
        _levelText.canvasRenderer.SetAlpha(1f);
        _levelText.SetText("Lv.{0}", Mathf.Max(1, _level));
        ApplyLevelTextShadow(_levelText);
    }

    private TextMeshProUGUI GetLevelText()
    {
        if (_skillLevelText != null)
        {
            return _skillLevelText;
        }

        _skillLevelText = GetComponentInChildren<TextMeshProUGUI>(true);
        return _skillLevelText;
    }

    private void ApplyLevelTextShadow(TextMeshProUGUI _levelText)
    {
        if (!_useLevelTextShadow || _levelText == null)
        {
            return;
        }

        Shadow _shadow = _levelText.GetComponent<Shadow>();
        if (_shadow == null)
        {
            _shadow = _levelText.gameObject.AddComponent<Shadow>();
        }

        _shadow.effectColor = _levelTextShadowColor;
        _shadow.effectDistance = _levelTextShadowDistance;
        _shadow.useGraphicAlpha = true;
    }

    private void ApplyButtonOutline(bool _isEnabled)
    {
        if (!_useIconOutline || !_isEnabled)
        {
            SetButtonOutlineEnabled(false);
            return;
        }

        RectTransform _borderRoot = GetOrCreateButtonBorderRoot();
        if (_borderRoot == null)
        {
            return;
        }

        _borderRoot.gameObject.SetActive(true);
        ApplyIconBorderLine(_borderRoot, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, _iconOutlineThickness));
        ApplyIconBorderLine(_borderRoot, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, _iconOutlineThickness));
        ApplyIconBorderLine(_borderRoot, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(_iconOutlineThickness, 0f));
        ApplyIconBorderLine(_borderRoot, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(_iconOutlineThickness, 0f));
    }

    private void SetButtonOutlineEnabled(bool _isEnabled)
    {
        Transform _borderRoot = transform.Find("SelectedEnchantButtonBorder");
        if (_borderRoot != null)
        {
            _borderRoot.gameObject.SetActive(_isEnabled);
        }
    }

    private RectTransform GetOrCreateButtonBorderRoot()
    {
        Transform _existingRoot = transform.Find("SelectedEnchantButtonBorder");
        if (_existingRoot != null)
        {
            return _existingRoot as RectTransform;
        }

        GameObject _borderObject = new GameObject("SelectedEnchantButtonBorder", typeof(RectTransform));
        RectTransform _borderTransform = _borderObject.GetComponent<RectTransform>();
        _borderTransform.SetParent(transform, false);
        _borderTransform.anchorMin = Vector2.zero;
        _borderTransform.anchorMax = Vector2.one;
        _borderTransform.offsetMin = Vector2.zero;
        _borderTransform.offsetMax = Vector2.zero;
        _borderTransform.SetAsLastSibling();
        return _borderTransform;
    }

    private void ApplyIconBorderLine(RectTransform _borderRoot, string _lineName, Vector2 _anchorMin, Vector2 _anchorMax, Vector2 _pivot, Vector2 _anchoredPosition, Vector2 _sizeDelta)
    {
        Image _lineImage = GetOrCreateBorderLine(_borderRoot, _lineName);
        RectTransform _lineTransform = _lineImage.rectTransform;
        _lineTransform.anchorMin = _anchorMin;
        _lineTransform.anchorMax = _anchorMax;
        _lineTransform.pivot = _pivot;
        _lineTransform.anchoredPosition = _anchoredPosition;
        _lineTransform.sizeDelta = _sizeDelta;
        _lineImage.color = _iconOutlineColor;
        _lineImage.raycastTarget = false;
        _lineImage.enabled = true;
    }

    private Image GetOrCreateBorderLine(RectTransform _borderRoot, string _lineName)
    {
        Transform _existingLine = _borderRoot.Find(_lineName);
        if (_existingLine != null)
        {
            Image _existingImage = _existingLine.GetComponent<Image>();
            if (_existingImage != null)
            {
                return _existingImage;
            }
        }

        GameObject _lineObject = new GameObject(_lineName, typeof(RectTransform), typeof(Image));
        RectTransform _lineTransform = _lineObject.GetComponent<RectTransform>();
        _lineTransform.SetParent(_borderRoot, false);
        return _lineObject.GetComponent<Image>();
    }

    private Image ResolveIconImage()
    {
        Image _childImage = FindDirectChildImage("Image");
        if (_childImage != null)
        {
            return _childImage;
        }

        if (_skillImage != null && _skillImage.GetComponent<Button>() == null)
        {
            return _skillImage;
        }

        Image[] _images = GetComponentsInChildren<Image>(true);
        for (int _index = 0; _index < _images.Length; _index++)
        {
            Image _image = _images[_index];
            if (_image != null && _image.gameObject != gameObject)
            {
                return _image;
            }
        }

        return _skillImage;
    }

    private Image FindDirectChildImage(string _objectName)
    {
        for (int _index = 0; _index < transform.childCount; _index++)
        {
            Transform _child = transform.GetChild(_index);
            if (_child == null || _child.name != _objectName)
            {
                continue;
            }

            Image _image = _child.GetComponent<Image>();
            if (_image != null)
            {
                return _image;
            }
        }

        return null;
    }
}
