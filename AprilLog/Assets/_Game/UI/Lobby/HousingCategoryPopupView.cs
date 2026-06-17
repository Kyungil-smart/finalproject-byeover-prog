//담당자: 조규민
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하우징 카테고리 팝업 UI 표시와 벽지 슬롯 선택 이벤트 전달을 담당한다.
/// </summary>
public class HousingCategoryPopupView : MonoBehaviour
{
    private const int DefaultSlotCount = 12;
    private const int SurfaceSlotCount = 20;
    private const int LockedSlotStartIndex = 8;
    private const int PurchaseSlotStartIndex = 12;
    private const int UnlockConditionStartChapter = 2;
    private const int NoFocusedLockedSlotIndex = -1;

    [Header("팝업 UI")]
    [SerializeField] private GameObject _popupRoot;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private Button _closeButton;

    [Header("색상 슬롯 UI")]
    [SerializeField] private GameObject _wallpaperContentRoot;
    [SerializeField] private HousingWallpaperSlotView[] _wallpaperSlots;

    [Header("바닥/벽지 탭 UI")]
    [SerializeField] private GameObject _surfaceTabRoot;
    [SerializeField] private Button _floorTabButton;
    [SerializeField] private Button _wallpaperTabButton;
    [SerializeField] private Image _floorTabBackground;
    [SerializeField] private Image _wallpaperTabBackground;
    [SerializeField] private Color _surfaceTabNormalColor = new Color(0.12f, 0.13f, 0.13f, 1f);
    [SerializeField] private Color _surfaceTabSelectedColor = new Color(0f, 0.95f, 0.45f, 1f);

    [Header("해금 진행도")]
    [SerializeField] private PlayerProgressModel _progressModel;

    [Header("구매 재화")]
    [SerializeField] private CurrencyModel _currencyModel;
    [SerializeField] private HousingWallpaperPurchaseModel _purchaseModel;

    public event Action<int, Color> ColorSelected;
    public event Action FloorTabClicked;
    public event Action WallpaperTabClicked;

    private Color[] _currentColors;
    private int _selectedIndex;
    private int _focusedLockedSlotIndex = NoFocusedLockedSlotIndex;
    private int _currentSlotCount = DefaultSlotCount;

    private void Awake()
    {
        // 기능: 팝업 시작 시 슬롯, 진행도 모델, 닫기 버튼 참조를 준비한다.
        ResolveColorSlots();
        ResolveSurfaceTabs();
        ResolveProgressModel();
        ResolveCurrencyModel();
        ResolvePurchaseModel();
        ValidateReferences();

        if (_closeButton != null)
            _closeButton.onClick.AddListener(Hide);
    }

    private void OnEnable()
    {
        // 기능: 팝업 활성화 시 진행도와 슬롯 클릭 이벤트를 연결하고 초기 상태를 숨김으로 맞춘다.
        ResolveColorSlots(true);
        ResolveSurfaceTabs();
        ResolveProgressModel();
        ResolveCurrencyModel();
        ResolvePurchaseModel();
        BindProgressModel();
        BindCurrencyModel();
        BindPurchaseModel();
        BindSurfaceTabs();
        BindColorSlots();
        Hide();
    }

    private void OnDisable()
    {
        // 기능: 팝업 비활성화 시 진행도와 슬롯 클릭 이벤트 연결을 해제한다.
        UnbindProgressModel();
        UnbindCurrencyModel();
        UnbindPurchaseModel();
        UnbindSurfaceTabs();
        UnbindColorSlots();
    }

    private void OnDestroy()
    {
        // 기능: 오브젝트 제거 시 닫기 버튼 이벤트를 해제한다.
        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(Hide);
    }

    public void ShowColorOptions(string _title, Color[] _colors, int _selectedColorIndex)
    {
        // 기능: 카테고리 제목, 색상 목록, 선택 인덱스를 받아 색상 슬롯 팝업을 연다.
        SetTitle(_title);
        SetSurfaceTabsActive(false);
        SetColorContentActive(true);

        _currentColors = _colors;
        _currentSlotCount = Mathf.Min(DefaultSlotCount, GetColorCount());
        _selectedIndex = Mathf.Clamp(_selectedColorIndex, 0, GetColorCount() - 1);
        _focusedLockedSlotIndex = NoFocusedLockedSlotIndex;

        RefreshColorSlots();
        ShowRoot();
        Canvas.ForceUpdateCanvases();
    }

    public void ShowSurfaceOptions(string _title, Color[] _colors, int _selectedColorIndex, bool _isFloorSelected)
    {
        // 추가:조규민 기능 설명: 벽지 카테고리 팝업에서 바닥/벽지 하위 탭과 색상 슬롯을 함께 표시한다.
        SetTitle(_title);
        SetSurfaceTabsActive(true);
        SetSurfaceTabSelected(_isFloorSelected);
        SetColorContentActive(true);

        _currentColors = _colors;
        _currentSlotCount = Mathf.Min(SurfaceSlotCount, GetColorCount());
        _selectedIndex = Mathf.Clamp(_selectedColorIndex, 0, GetColorCount() - 1);
        _focusedLockedSlotIndex = NoFocusedLockedSlotIndex;

        RefreshColorSlots();
        ShowRoot();
        Canvas.ForceUpdateCanvases();
    }

    public void ShowWallpaper(string _title, Color[] _wallpaperColors, int _selectedIndex)
    {
        // 기능: 기존 벽지 전용 호출부가 공통 색상 팝업 흐름을 재사용하도록 연결한다.
        ShowColorOptions(_title, _wallpaperColors, _selectedIndex);
    }

    public void ShowEmpty(string _title)
    {
        // 기능: 색상 목록이 없는 카테고리 팝업은 제목만 표시한다.
        SetTitle(_title);
        SetSurfaceTabsActive(false);
        SetColorContentActive(false);
        ShowRoot();
    }

    public void Hide()
    {
        // 기능: 팝업 루트 오브젝트를 비활성화한다.
        if (_popupRoot != null)
            _popupRoot.SetActive(false);
    }

    private void ResolveColorSlots(bool _forceRefresh = false)
    {
        // 기능: Hierarchy에 배치된 슬롯 View 배열을 Content 하위에서 수집한다.
        if (!_forceRefresh && _wallpaperSlots != null && _wallpaperSlots.Length >= SurfaceSlotCount)
            return;

        if (_wallpaperContentRoot == null)
            return;

        _wallpaperSlots = _wallpaperContentRoot.GetComponentsInChildren<HousingWallpaperSlotView>(true);
        EnsureSurfaceSlotObjects();
        _wallpaperSlots = _wallpaperContentRoot.GetComponentsInChildren<HousingWallpaperSlotView>(true);
    }

    private void EnsureSurfaceSlotObjects()
    {
        // 추가: 조규민 - 씬에 12개만 배치되어 있어도 벽지/바닥 추가 슬롯 13~20을 런타임에 보강한다.
        if (_wallpaperSlots == null || _wallpaperSlots.Length == 0)
            return;

        if (_wallpaperSlots.Length >= SurfaceSlotCount)
            return;

        Transform _parent = _wallpaperContentRoot.transform;
        HousingWallpaperSlotView _template = _wallpaperSlots[_wallpaperSlots.Length - 1];
        for (int _index = _wallpaperSlots.Length; _index < SurfaceSlotCount; _index++)
        {
            HousingWallpaperSlotView _createdSlot = Instantiate(_template, _parent);
            _createdSlot.name = "Slot_Wallpaper_" + (_index + 1);
        }
    }

    private void ResolveSurfaceTabs()
    {
        // 추가:조규민 기능 설명: Hierarchy에 배치된 팝업 왼쪽 바닥/벽지 탭 UI를 찾는다.
        if (_surfaceTabRoot == null)
        {
            Transform _tabRoot = FindChildByName(transform, "SurfaceTabRoot");
            if (_tabRoot != null)
                _surfaceTabRoot = _tabRoot.gameObject;
        }

        if (_floorTabButton == null)
            _floorTabButton = FindButtonInRoot(_surfaceTabRoot, "Btn_SurfaceFloor");

        if (_wallpaperTabButton == null)
            _wallpaperTabButton = FindButtonInRoot(_surfaceTabRoot, "Btn_SurfaceWallpaper");

        if (_floorTabBackground == null && _floorTabButton != null)
            _floorTabBackground = _floorTabButton.GetComponent<Image>();

        if (_wallpaperTabBackground == null && _wallpaperTabButton != null)
            _wallpaperTabBackground = _wallpaperTabButton.GetComponent<Image>();
    }

    private static Button FindButtonInRoot(GameObject _root, string _buttonName)
    {
        // 추가:조규민 기능 설명: 지정된 탭 루트 하위에서 이름이 일치하는 Button을 찾는다.
        if (_root == null)
            return null;

        Button[] _buttons = _root.GetComponentsInChildren<Button>(true);
        for (int _index = 0; _index < _buttons.Length; _index++)
        {
            Button _button = _buttons[_index];
            if (_button != null && _button.name == _buttonName)
                return _button;
        }

        return null;
    }

    private void ResolveProgressModel()
    {
        // 기능: Inspector 미연결 시 씬에서 PlayerProgressModel을 찾아 해금 기준으로 사용한다.
        if (_progressModel != null)
            return;

        _progressModel = FindFirstObjectByType<PlayerProgressModel>(FindObjectsInactive.Include);
    }

    private void ResolveCurrencyModel()
    {
        // 추가: 조규민 - Inspector 미연결 시 씬의 재화 모델을 찾아 구매 가능 여부에 사용한다.
        if (_currencyModel != null)
            return;

        _currencyModel = FindFirstObjectByType<CurrencyModel>(FindObjectsInactive.Include);
    }

    private void ResolvePurchaseModel()
    {
        // 추가: 조규민 - 구매 상태 모델이 없으면 팝업 오브젝트에 붙여 현재 세션 구매 상태를 관리한다.
        if (_purchaseModel == null)
            _purchaseModel = GetComponent<HousingWallpaperPurchaseModel>();

        if (_purchaseModel == null)
            _purchaseModel = gameObject.AddComponent<HousingWallpaperPurchaseModel>();

        _purchaseModel.Initialize();
    }

    private void BindProgressModel()
    {
        // 기능: 스테이지 진행도 변경 이벤트를 슬롯 해금 갱신에 연결한다.
        if (_progressModel == null)
            return;

        _progressModel.OnProgressUpdated -= HandleProgressUpdated;
        _progressModel.OnProgressUpdated += HandleProgressUpdated;
    }

    private void UnbindProgressModel()
    {
        // 기능: 진행도 변경 이벤트 연결을 해제해 중복 호출을 방지한다.
        if (_progressModel == null)
            return;

        _progressModel.OnProgressUpdated -= HandleProgressUpdated;
    }

    private void BindCurrencyModel()
    {
        // 추가: 조규민 - 재화 변화에 따라 구매 가능/불가 표시를 즉시 갱신한다.
        if (_currencyModel == null)
            return;

        _currencyModel.OnCurrencyChanged -= HandleCurrencyChanged;
        _currencyModel.OnCurrencyChanged += HandleCurrencyChanged;
    }

    private void UnbindCurrencyModel()
    {
        if (_currencyModel == null)
            return;

        _currencyModel.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    private void BindPurchaseModel()
    {
        // 추가: 조규민 - 구매 완료 시 슬롯 잠금 상태를 즉시 갱신한다.
        if (_purchaseModel == null)
            return;

        _purchaseModel.PurchaseStateChanged -= HandlePurchaseStateChanged;
        _purchaseModel.PurchaseStateChanged += HandlePurchaseStateChanged;
    }

    private void UnbindPurchaseModel()
    {
        if (_purchaseModel == null)
            return;

        _purchaseModel.PurchaseStateChanged -= HandlePurchaseStateChanged;
    }

    private void BindColorSlots()
    {
        // 기능: 각 색상 슬롯의 클릭 이벤트를 팝업 선택 처리로 연결한다.
        ResolveColorSlots();

        if (_wallpaperSlots == null)
            return;

        for (int _index = 0; _index < _wallpaperSlots.Length; _index++)
        {
            HousingWallpaperSlotView _slot = _wallpaperSlots[_index];
            if (_slot == null)
                continue;

            _slot.Clicked -= HandleColorSlotClicked;
            _slot.Clicked += HandleColorSlotClicked;
        }
    }

    private void BindSurfaceTabs()
    {
        // 추가:조규민 기능 설명: 바닥/벽지 탭 버튼 클릭 이벤트를 중복 없이 연결한다.
        ResolveSurfaceTabs();

        if (_floorTabButton != null)
        {
            _floorTabButton.onClick.RemoveListener(NotifyFloorTabClicked);
            _floorTabButton.onClick.AddListener(NotifyFloorTabClicked);
        }

        if (_wallpaperTabButton != null)
        {
            _wallpaperTabButton.onClick.RemoveListener(NotifyWallpaperTabClicked);
            _wallpaperTabButton.onClick.AddListener(NotifyWallpaperTabClicked);
        }
    }

    private void UnbindSurfaceTabs()
    {
        // 추가:조규민 기능 설명: 바닥/벽지 탭 버튼 클릭 이벤트를 해제한다.
        if (_floorTabButton != null)
            _floorTabButton.onClick.RemoveListener(NotifyFloorTabClicked);

        if (_wallpaperTabButton != null)
            _wallpaperTabButton.onClick.RemoveListener(NotifyWallpaperTabClicked);
    }

    private void UnbindColorSlots()
    {
        // 기능: 각 색상 슬롯의 클릭 이벤트 연결을 해제한다.
        if (_wallpaperSlots == null)
            return;

        for (int _index = 0; _index < _wallpaperSlots.Length; _index++)
        {
            HousingWallpaperSlotView _slot = _wallpaperSlots[_index];
            if (_slot == null)
                continue;

            _slot.Clicked -= HandleColorSlotClicked;
        }
    }

    private void RefreshColorSlots()
    {
        // 기능: 슬롯 표시 상태를 현재 색상 데이터와 해금 진행도 기준으로 갱신한다.
        ResolveColorSlots();

        if (_wallpaperSlots == null)
            return;

        for (int _index = 0; _index < _wallpaperSlots.Length; _index++)
        {
            HousingWallpaperSlotView _slot = _wallpaperSlots[_index];
            if (_slot == null)
                continue;

            bool _isDisplaySlot = _index < _currentSlotCount;
            _slot.gameObject.SetActive(_isDisplaySlot);

            if (!_isDisplaySlot)
                continue;

            bool _isUnlocked = IsSlotUnlocked(_index);
            bool _showUnlockCondition = _index == _focusedLockedSlotIndex;
            Color _slotColor = GetSlotColor(_index);
            _slot.SetData(_index, _slotColor, _index == _selectedIndex);
            _slot.SetUnlockState(_isUnlocked, GetUnlockConditionText(_index), _showUnlockCondition);
            ApplyPurchaseState(_slot, _index);
        }
    }

    private void HandleColorSlotClicked(int _slotIndex)
    {
        // 기능: 잠금 슬롯은 조건 표시만 열고, 해금 슬롯은 선택 이벤트를 전달한다.
        if (_slotIndex < 0 || _slotIndex >= _currentSlotCount)
            return;

        if (!TryPurchaseSlotIfNeeded(_slotIndex))
        {
            _focusedLockedSlotIndex = _slotIndex;
            RefreshColorSlots();
            return;
        }

        if (!IsSlotUnlocked(_slotIndex))
        {
            _focusedLockedSlotIndex = _slotIndex;
            RefreshColorSlots();
            return;
        }

        if (_currentColors == null)
            return;

        if (_slotIndex >= _currentColors.Length)
            return;

        _focusedLockedSlotIndex = NoFocusedLockedSlotIndex;
        _selectedIndex = _slotIndex;
        RefreshColorSlots();
        ColorSelected?.Invoke(_slotIndex, _currentColors[_slotIndex]);
    }

    private void NotifyFloorTabClicked()
    {
        // 추가:조규민 기능 설명: 왼쪽 바닥 탭 클릭을 Presenter로 전달한다.
        FloorTabClicked?.Invoke();
    }

    private void NotifyWallpaperTabClicked()
    {
        // 추가:조규민 기능 설명: 왼쪽 벽지 탭 클릭을 Presenter로 전달한다.
        WallpaperTabClicked?.Invoke();
    }

    private void HandleProgressUpdated()
    {
        // 기능: 진행도 변경 시 잠금/해금 표시를 즉시 다시 그린다.
        RefreshColorSlots();
    }

    private void HandleCurrencyChanged(int _gold, int _parchment)
    {
        // 추가: 조규민 - 재화 수량 변경 시 구매 가능 표시를 다시 계산한다.
        RefreshColorSlots();
    }

    private void HandlePurchaseStateChanged()
    {
        // 추가: 조규민 - 구매 상태 변경 시 잠금 표시를 다시 계산한다.
        RefreshColorSlots();
    }

    private int GetColorCount()
    {
        // 기능: 현재 카테고리에서 사용할 수 있는 색상 개수를 반환한다.
        if (_currentColors == null)
            return 0;

        return _currentColors.Length;
    }

    private Color GetSlotColor(int _slotIndex)
    {
        // 기능: 슬롯 색상이 부족할 때도 잠금 슬롯 표시용 기본 색상을 제공한다.
        if (_currentColors != null && _slotIndex >= 0 && _slotIndex < _currentColors.Length)
            return _currentColors[_slotIndex];

        return new Color(0.16f, 0.17f, 0.16f, 1f);
    }

    private void ApplyPurchaseState(HousingWallpaperSlotView _slot, int _slotIndex)
    {
        // 추가: 조규민 - 구매형 슬롯의 비용과 버튼 가능 상태를 슬롯 View에 반영한다.
        if (_slot == null || !IsPurchaseSlot(_slotIndex))
            return;

        bool _isPurchased = IsSlotUnlocked(_slotIndex);
        bool _canPurchase = CanPurchaseSlot(_slotIndex);
        string _priceText = GetPurchasePriceText(_slotIndex);
        _slot.SetPurchaseState(_isPurchased, _canPurchase, _priceText);
    }

    private bool TryPurchaseSlotIfNeeded(int _slotIndex)
    {
        // 추가: 조규민 - 구매형 슬롯은 재화 차감과 구매 저장이 성공해야 선택할 수 있다.
        if (!IsPurchaseSlot(_slotIndex))
            return true;

        if (_purchaseModel == null)
            return false;

        if (_purchaseModel.IsPurchased(_slotIndex))
            return true;

        return _purchaseModel.TryPurchase(_slotIndex, _currencyModel);
    }

    private bool CanPurchaseSlot(int _slotIndex)
    {
        if (_purchaseModel == null)
            return false;

        return _purchaseModel.CanPurchase(_slotIndex, _currencyModel);
    }

    private bool IsPurchaseSlot(int _slotIndex)
    {
        if (_purchaseModel == null)
            return _slotIndex >= PurchaseSlotStartIndex;

        return _purchaseModel.IsPurchaseSlot(_slotIndex);
    }

    private string GetPurchasePriceText(int _slotIndex)
    {
        if (_purchaseModel == null)
            return string.Empty;

        return _purchaseModel.GetPriceText(_slotIndex);
    }

    private bool IsSlotUnlocked(int _slotIndex)
    {
        // 기능: 1~8번 슬롯은 기본 해금, 9~12번 슬롯은 챕터 클리어 여부로 해금한다.
        if (_slotIndex < LockedSlotStartIndex)
            return true;

        if (IsPurchaseSlot(_slotIndex))
            return _purchaseModel != null && _purchaseModel.IsPurchased(_slotIndex);

        return HousingUnlockUtility.IsChapterCleared(_progressModel, GetRequiredChapter(_slotIndex));
    }

    private string GetUnlockConditionText(int _slotIndex)
    {
        // 기능: 잠긴 슬롯에 표시할 챕터 클리어 조건 문구를 만든다.
        if (IsSlotUnlocked(_slotIndex))
            return string.Empty;

        if (IsPurchaseSlot(_slotIndex))
            return GetPurchasePriceText(_slotIndex);

        int _chapter = GetRequiredChapter(_slotIndex);
        return "챕터 " + _chapter + " 클리어 시 해금";
    }

    private int GetRequiredChapter(int _slotIndex)
    {
        // 기능: 잠금 슬롯 인덱스를 필요한 챕터 번호로 변환한다.
        return UnlockConditionStartChapter + (_slotIndex - LockedSlotStartIndex);
    }

    private void SetTitle(string _title)
    {
        // 기능: 팝업 제목 TMP 텍스트를 갱신한다.
        if (_titleText != null)
            _titleText.text = _title;
    }

    private void SetColorContentActive(bool _isActive)
    {
        // 기능: 색상 슬롯 Content 표시 여부를 전환한다.
        if (_wallpaperContentRoot != null)
            _wallpaperContentRoot.SetActive(_isActive);
    }

    private void SetSurfaceTabsActive(bool _isActive)
    {
        // 추가:조규민 기능 설명: 배경 카테고리에서만 바닥/벽지 하위 탭을 표시한다.
        if (_surfaceTabRoot != null)
            _surfaceTabRoot.SetActive(_isActive);
    }

    private void SetSurfaceTabSelected(bool _isFloorSelected)
    {
        // 추가:조규민 기능 설명: 선택된 바닥/벽지 탭을 색상으로 강조한다.
        if (_floorTabBackground != null)
            _floorTabBackground.color = _isFloorSelected ? _surfaceTabSelectedColor : _surfaceTabNormalColor;

        if (_wallpaperTabBackground != null)
            _wallpaperTabBackground.color = _isFloorSelected ? _surfaceTabNormalColor : _surfaceTabSelectedColor;
    }

    private void ShowRoot()
    {
        // 기능: 팝업 루트 오브젝트를 활성화한다.
        if (_popupRoot != null)
            _popupRoot.SetActive(true);
    }

    private void ValidateReferences()
    {
        // 기능: 필수 Inspector 참조가 빠졌을 때 원인 파악용 경고를 출력한다.
        if (_popupRoot == null)
            Debug.LogWarning("[HousingCategoryPopupView] 팝업 루트가 연결되지 않았습니다.", this);

        if (_titleText == null)
            Debug.LogWarning("[HousingCategoryPopupView] 제목 텍스트가 연결되지 않았습니다.", this);

        if (_closeButton == null)
            Debug.LogWarning("[HousingCategoryPopupView] 닫기 버튼이 연결되지 않았습니다.", this);

        if (_wallpaperContentRoot == null)
            Debug.LogWarning("[HousingCategoryPopupView] 색상 슬롯 Content가 연결되지 않았습니다.", this);

        if (_wallpaperSlots == null || _wallpaperSlots.Length == 0)
            Debug.LogWarning("[HousingCategoryPopupView] 색상 슬롯이 연결되지 않았습니다.", this);

        if (_currencyModel == null)
            Debug.LogWarning("[HousingCategoryPopupView] 재화 모델이 연결되지 않아 구매형 벽지 슬롯 구매가 제한됩니다.", this);

        if (_purchaseModel == null)
            Debug.LogWarning("[HousingCategoryPopupView] 벽지 구매 모델이 연결되지 않아 런타임에 자동 생성됩니다.", this);
    }

    private static Transform FindChildByName(Transform _root, string _name)
    {
        // 추가:조규민 기능 설명: 비활성 자식까지 포함해 이름이 일치하는 UI Transform을 찾는다.
        if (_root == null)
            return null;

        if (_root.name == _name)
            return _root;

        for (int _index = 0; _index < _root.childCount; _index++)
        {
            Transform _found = FindChildByName(_root.GetChild(_index), _name);
            if (_found != null)
                return _found;
        }

        return null;
    }
}
