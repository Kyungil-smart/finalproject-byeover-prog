//담당자: 조규민
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 벽지 색상 슬롯의 표시 상태와 클릭 이벤트 전달을 담당한다.
/// </summary>
public class HousingWallpaperSlotView : MonoBehaviour
{
    [Header("슬롯 UI")]
    [SerializeField] private Button _button;
    [SerializeField] private Image _colorImage;
    [SerializeField] private Image _statusBackground;
    [SerializeField] private TMP_Text _statusText;
    [SerializeField] private GameObject _unlockConditionRoot;
    [SerializeField] private TMP_Text _unlockConditionText;

    [Header("상태 색상")]
    [SerializeField] private Color _normalStatusColor = Color.white;
    [SerializeField] private Color _selectedStatusColor = new Color(0f, 0.85f, 0.32f, 1f);
    [SerializeField] private Color _lockedStatusColor = new Color(0.18f, 0.18f, 0.18f, 0.92f);
    [SerializeField] private Color _unlockConditionBoxColor = new Color(0f, 0f, 0f, 0.72f);

    [Header("해금 조건 표시 위치")]
    [SerializeField] private Vector2 _unlockConditionPosition = new Vector2(0f, -8f);
    [SerializeField] private Vector2 _unlockConditionSize = new Vector2(0f, 42f);
    [SerializeField] private Vector2 _unlockConditionTextPaddingMin = new Vector2(6f, 3f);
    [SerializeField] private Vector2 _unlockConditionTextPaddingMax = new Vector2(-6f, -3f);
    [SerializeField] private float _unlockConditionFontSize = 20f;

    public event Action<int> Clicked;

    private int _slotIndex;

    private void Awake()
    {
        // 기능: 슬롯 UI 참조를 찾고 버튼 클릭 이벤트를 연결한다.
        ResolveReferences();
        ValidateReferences();

        if (_button != null)
            _button.onClick.AddListener(NotifyClicked);
    }

    private void OnDestroy()
    {
        // 기능: 오브젝트 제거 시 슬롯 버튼 클릭 이벤트를 해제한다.
        if (_button != null)
            _button.onClick.RemoveListener(NotifyClicked);
    }

    private void OnValidate()
    {
        // 기능: Inspector 값 변경 시 해금 조건 UI 배치를 즉시 반영한다.
        ApplyUnlockConditionLayout();
    }

    public void SetData(int _index, Color _color, bool _isSelected)
    {
        // 기능: 슬롯 색상과 선택 표시를 현재 카테고리 데이터 기준으로 갱신한다.
        _slotIndex = _index;

        if (_colorImage != null)
            _colorImage.color = _color;

        if (_statusBackground != null)
            _statusBackground.color = _isSelected ? _selectedStatusColor : _normalStatusColor;

        if (_statusText != null)
            _statusText.text = _isSelected ? "착용중" : string.Empty;

        SetUnlockConditionVisible(false, string.Empty);
    }

    public void SetUnlockState(bool _isUnlocked, string _unlockCondition, bool _showUnlockCondition)
    {
        // 기능: 잠금 상태일 때 상태 문구와 클릭 시 노출할 해금 조건 박스를 제어한다.
        if (_button != null)
            _button.interactable = true;

        if (_statusBackground != null && !_isUnlocked)
            _statusBackground.color = _lockedStatusColor;

        if (_statusText != null && !_isUnlocked)
            _statusText.text = "잠금";

        SetUnlockConditionVisible(!_isUnlocked && _showUnlockCondition, _unlockCondition);
    }

    private void NotifyClicked()
    {
        // 기능: 버튼 클릭을 슬롯 인덱스 이벤트로 외부에 전달한다.
        Clicked?.Invoke(_slotIndex);
    }

    private void ResolveReferences()
    {
        // 기능: 슬롯 버튼, 색상 Image, 상태 Image, 텍스트 참조를 Hierarchy에서 찾는다.
        if (_button == null)
            _button = GetComponent<Button>();

        if (_colorImage == null)
        {
            Transform _color = transform.Find("Image_Color");
            if (_color != null)
                _colorImage = _color.GetComponent<Image>();
        }

        if (_statusBackground == null)
        {
            Transform _status = transform.Find("Image_Status");
            if (_status != null)
                _statusBackground = _status.GetComponent<Image>();
        }

        ResolveStatusText();
        ResolveUnlockConditionText();
    }

    private void ResolveStatusText()
    {
        // 기능: 상태 배경 안의 Text_Status를 찾고 없으면 프로토타입용 TMP를 생성한다.
        if (_statusText != null)
            return;

        if (_statusBackground == null)
            return;

        Transform _statusTextTransform = _statusBackground.transform.Find("Text_Status");
        if (_statusTextTransform != null)
        {
            _statusText = _statusTextTransform.GetComponent<TMP_Text>();
            return;
        }

        GameObject _statusTextObject = CreateTextObject("Text_Status", _statusBackground.transform, 26f);
        RectTransform _statusRect = _statusTextObject.GetComponent<RectTransform>();
        _statusRect.anchorMin = Vector2.zero;
        _statusRect.anchorMax = Vector2.one;
        _statusRect.offsetMin = new Vector2(4f, 2f);
        _statusRect.offsetMax = new Vector2(-4f, -2f);
        _statusText = _statusTextObject.GetComponent<TMP_Text>();
    }

    private void ResolveUnlockConditionText()
    {
        // 기능: 해금 조건이 필요한 슬롯에만 배치된 선택 UI를 찾아 Inspector 조절값을 적용한다.
        if (_unlockConditionRoot == null)
        {
            Transform _root = transform.Find("UnlockConditionBox");
            if (_root != null)
                _unlockConditionRoot = _root.gameObject;
        }

        if (_unlockConditionText == null && _unlockConditionRoot != null)
        {
            Transform _text = _unlockConditionRoot.transform.Find("Text_UnlockCondition");
            if (_text != null)
                _unlockConditionText = _text.GetComponent<TMP_Text>();
        }

        ApplyUnlockConditionLayout();
        SetUnlockConditionVisible(false, string.Empty);
    }

    private void ApplyUnlockConditionLayout()
    {
        // 기능: 해금 조건 박스의 위치, 크기, 패딩, 글자 크기를 Inspector 값으로 반영한다.
        if (_unlockConditionRoot != null)
        {
            RectTransform _rootRect = _unlockConditionRoot.GetComponent<RectTransform>();
            if (_rootRect != null)
            {
                _rootRect.anchorMin = new Vector2(0f, 0f);
                _rootRect.anchorMax = new Vector2(1f, 0f);
                _rootRect.pivot = new Vector2(0.5f, 1f);
                _rootRect.anchoredPosition = _unlockConditionPosition;
                _rootRect.sizeDelta = _unlockConditionSize;
            }

            Image _rootImage = _unlockConditionRoot.GetComponent<Image>();
            if (_rootImage != null)
                _rootImage.color = _unlockConditionBoxColor;
        }

        if (_unlockConditionText == null)
            return;

        RectTransform _textRect = _unlockConditionText.GetComponent<RectTransform>();
        if (_textRect != null)
        {
            _textRect.anchorMin = Vector2.zero;
            _textRect.anchorMax = Vector2.one;
            _textRect.offsetMin = _unlockConditionTextPaddingMin;
            _textRect.offsetMax = _unlockConditionTextPaddingMax;
        }

        _unlockConditionText.fontSize = _unlockConditionFontSize;
        _unlockConditionText.fontSizeMax = _unlockConditionFontSize;
    }

    private GameObject CreateTextObject(string _name, Transform _parent, float _fontSize)
    {
        // 기능: 슬롯 내부 상태 표시용 TMP 텍스트 오브젝트를 생성한다.
        GameObject _textObject = new GameObject(_name, typeof(RectTransform), typeof(TextMeshProUGUI));
        _textObject.transform.SetParent(_parent, false);

        TextMeshProUGUI _text = _textObject.GetComponent<TextMeshProUGUI>();
        _text.fontSize = _fontSize;
        _text.enableAutoSizing = true;
        _text.fontSizeMin = 12f;
        _text.fontSizeMax = _fontSize;
        _text.alignment = TextAlignmentOptions.Center;
        _text.color = Color.white;
        _text.raycastTarget = false;

        return _textObject;
    }

    private void SetUnlockConditionVisible(bool _isVisible, string _unlockCondition)
    {
        // 기능: 해금 조건 박스 표시 여부와 문구를 갱신한다.
        if (_unlockConditionRoot != null)
            _unlockConditionRoot.SetActive(_isVisible);

        if (_unlockConditionText != null)
            _unlockConditionText.text = _unlockCondition;
    }

    private void ValidateReferences()
    {
        // 기능: 슬롯 UI 필수 참조와 선택 참조의 누락 여부를 경고 로그로 표시한다.
        if (_button == null)
            Debug.LogWarning("[HousingWallpaperSlotView] 슬롯 Button이 연결되지 않았습니다.", this);

        if (_colorImage == null)
            Debug.LogWarning("[HousingWallpaperSlotView] 색상 Image가 연결되지 않았습니다.", this);

        if (_statusBackground == null)
            Debug.LogWarning("[HousingWallpaperSlotView] 상태 배경 Image가 연결되지 않았습니다.", this);

        if (_statusText == null)
            Debug.LogWarning("[HousingWallpaperSlotView] 잠금 상태 Text가 연결되지 않았습니다.", this);

        if (_unlockConditionRoot != null && _unlockConditionText == null)
            Debug.LogWarning("[HousingWallpaperSlotView] 해금 조건 Text가 연결되지 않았습니다.", this);
    }
}
