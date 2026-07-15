// 생성자 : 김영찬
// 설명   : 개별 인챈트 카드 프리팹의 비주얼 제어 및 클릭 이벤트 포워딩

// 수정자 : 조규민
// 수정 내용 : 카드별 리롤 버튼과 남은 리롤 횟수 표시 연결, Viewport Mask 외부 오버레이 배치 지원,
//             리롤 버튼 Sprite 원본 색상 적용, 숫자 제거 및 버튼 중앙 리롤 아이콘 추가

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnchantCardUI : MonoBehaviour
{
    [Header("UI 컴포넌트 참조")]
    [SerializeField] private TextMeshProUGUI _typeText;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private Image _iconImage;
    
    [Header("버튼 세팅")]
    [SerializeField] private Button _selectButton;
    [SerializeField] private Button _rerollButton;

    [Header("리롤 버튼 배치")]
    [Tooltip("카드 하단 중앙 기준 리롤 버튼 위치입니다. Y가 낮을수록 아래로 내려갑니다.")]
    [SerializeField] private Vector2 _rerollAnchoredPosition = new Vector2(0f, 125f);
    [SerializeField] private Vector2 _rerollSize = new Vector2(225f, 125f);
    [Tooltip("런타임에 생성되는 리롤 버튼에 표시할 이미지입니다.")]
    [SerializeField] private Sprite _rerollButtonSprite;

    [Header("리롤 아이콘 배치")]
    [Tooltip("Button & LowerBGLayer 하위 RerollIconImage에 표시할 이미지입니다.")]
    [SerializeField] private Sprite _rerollIconSprite;
    [SerializeField] private Vector2 _rerollIconSize = new Vector2(64f, 64f);

    private RectTransform _rerollRoot;
    private RectTransform _rerollOverlay;

    // 카드가 클릭되었을 때 이 카드가 몇 번째 인덱스(순서)인지 부모 뷰에 알리기 위한 이벤트
    public Action OnCardClicked;
    public Action OnRerollClicked;

    private void Awake()
    {
        if (_selectButton == null)
        {
            _selectButton = GetComponent<Button>();
        }

        EnsureRerollUI();
    }

    private void OnEnable()
    {
        if (_selectButton != null)
        {
            _selectButton.onClick.AddListener(OnSelectButtonClick);
        }

        if (_rerollButton != null)
        {
            _rerollButton.onClick.AddListener(OnRerollButtonClick);
        }
    }

    private void OnDisable()
    {
        if (_selectButton != null)
        {
            _selectButton.onClick.RemoveListener(OnSelectButtonClick);
        }

        if (_rerollButton != null)
        {
            _rerollButton.onClick.RemoveListener(OnRerollButtonClick);
        }
    }

    /// <summary>
    /// 실제 데이터 구조를 받아 UI 텍스트를 갱신
    /// </summary>
    public void Setup(EnchantDisplayData data)
    {
        if (data == null) return;

        // 인챈트 타입 라벨 반영 (Presenter가 stat-type 기반으로 채움)
        if (_typeText != null)
            _typeText.text = EnchantGroupIDToEnchantGroupTypeMapper.GetLabelText(data.TypeLabel);
        
        // 인챈트 이름 반영 (예: "공격력 증가", "체인 라이트닝" 등)
        if (_nameText != null) 
            _nameText.text = EnchantColorMapper.SetColorHexCodeText(data.Name, data.ElementalType);
        
        // 인챈트 상세 설명 반영
        if (_descriptionText != null)
        {
            _descriptionText.text = data.Description;
        }

        // 추가: 조규민 - 인챈트 선택 카드의 ImageKey를 Resources/EnchantIcons 경로의 Sprite로 표시한다.
        EnchantIconLoader.ApplyIcon(_iconImage, data.ImageKey);
    }

    private string RemoveDescriptionTags(string _description)
    {
        if (string.IsNullOrWhiteSpace(_description))
        {
            return _description;
        }

        string[] _lines = _description.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        List<string> _descriptionLines = new List<string>();
        for (int _index = 0; _index < _lines.Length; _index++)
        {
            string _lineWithoutTags = Regex.Replace(_lines[_index], @"#[^\s#]+", string.Empty);
            _lineWithoutTags = Regex.Replace(_lineWithoutTags, @"[ \t]{2,}", " ").Trim();
            if (string.IsNullOrWhiteSpace(_lineWithoutTags))
            {
                continue;
            }

            _descriptionLines.Add(_lineWithoutTags);
        }

        return string.Join("\n", _descriptionLines);
    }

    // visible = 리롤 기능 자체가 켜졌는지(전역). interactable = 이 카드가 지금 리롤 가능한지(잔여/사용 여부).
    // 버튼 노출은 기능 활성 여부로만 결정해 항상 보이게 하고, 사용된 카드는 비활성(회색)으로 둔다.
    public void SetRerollState(bool visible, bool interactable)
    {
        EnsureRerollUI();

        if (_rerollRoot != null)
        {
            _rerollRoot.gameObject.SetActive(visible);
        }

        if (_rerollButton != null)
        {
            _rerollButton.interactable = interactable;
        }
    }

    public void AttachRerollOverlay(RectTransform overlay)
    {
        if (overlay == null) return;

        EnsureRerollUI();
        _rerollOverlay = overlay;
        _rerollRoot.SetParent(_rerollOverlay, false);
        _rerollRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _rerollRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _rerollRoot.pivot = new Vector2(0.5f, 0.5f);
        _rerollRoot.sizeDelta = _rerollSize;
        _rerollRoot.SetAsLastSibling();
        RefreshRerollPosition();
    }

    public void RefreshRerollPosition()
    {
        if (_rerollRoot == null || _rerollOverlay == null) return;

        RectTransform cardRect = transform as RectTransform;
        if (cardRect == null) return;

        Vector3 cardBottomCenter = cardRect.TransformPoint(new Vector3(0f, cardRect.rect.yMin, 0f));
        Vector3 overlayPosition = _rerollOverlay.InverseTransformPoint(cardBottomCenter);
        Vector3 overlayOffset = _rerollOverlay.InverseTransformVector(cardRect.TransformVector(_rerollAnchoredPosition));
        _rerollRoot.anchoredPosition = (Vector2)(overlayPosition + overlayOffset);
    }

    public void SetSelectInteractable(bool interactable)
    {
        if (_selectButton != null)
        {
            _selectButton.interactable = interactable;
        }
    }

    private void OnSelectButtonClick()
    {
        OnCardClicked?.Invoke();
    }

    private void OnRerollButtonClick()
    {
        OnRerollClicked?.Invoke();
    }

    private void OnDestroy()
    {
        if (_rerollRoot != null && _rerollRoot.parent != transform)
        {
            Destroy(_rerollRoot.gameObject);
        }
    }

    private void EnsureRerollUI()
    {
        if (_rerollRoot == null)
        {
            _rerollRoot = ResolveOrCreateRect(transform, "RerollBoundary");
        }

        bool isCardChild = _rerollRoot.parent == transform;
        _rerollRoot.pivot = new Vector2(0.5f, 0.5f);
        if (isCardChild)
        {
            _rerollRoot.anchorMin = new Vector2(0.5f, 0f);
            _rerollRoot.anchorMax = new Vector2(0.5f, 0f);
            _rerollRoot.anchoredPosition = _rerollAnchoredPosition;
        }
        _rerollRoot.sizeDelta = _rerollSize;
        _rerollRoot.SetAsLastSibling();

        RectTransform buttonRect = ResolveOrCreateRect(_rerollRoot, "Button & LowerBGLayer");
        buttonRect.anchorMin = Vector2.zero;
        buttonRect.anchorMax = Vector2.one;
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = Vector2.zero;

        Image buttonImage = buttonRect.GetComponent<Image>();
        if (buttonImage == null)
        {
            buttonImage = buttonRect.gameObject.AddComponent<Image>();
        }

        if (_rerollButtonSprite != null)
        {
            buttonImage.sprite = _rerollButtonSprite;
        }

        buttonImage.color = Color.white;
        buttonImage.raycastTarget = true;

        if (_rerollButton == null)
        {
            _rerollButton = buttonRect.GetComponent<Button>();
            if (_rerollButton == null)
            {
                _rerollButton = buttonRect.gameObject.AddComponent<Button>();
            }
        }

        _rerollButton.targetGraphic = buttonImage;

        RectTransform iconRect = ResolveOrCreateRect(buttonRect, "RerollIconImage");
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = _rerollIconSize;

        Image rerollIconImage = iconRect.GetComponent<Image>();
        if (rerollIconImage == null)
        {
            rerollIconImage = iconRect.gameObject.AddComponent<Image>();
        }

        if (_rerollIconSprite != null)
        {
            rerollIconImage.sprite = _rerollIconSprite;
        }

        rerollIconImage.color = Color.white;
        rerollIconImage.preserveAspect = true;
        rerollIconImage.raycastTarget = false;
        rerollIconImage.enabled = rerollIconImage.sprite != null;

        Transform countText = _rerollRoot.Find("RerollCountText (TMP)");
        if (countText != null)
        {
            countText.gameObject.SetActive(false);
        }

        _rerollRoot.gameObject.SetActive(false);
    }

    private RectTransform ResolveOrCreateRect(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
        {
            return existingRect;
        }

        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.layer = gameObject.layer;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

}
