//담당자: 조규민

// 수정 내용 : 저장된 구매 보유 가구 목록을 배치 UI 상태에 반영하고 구매 요청을 GameManager로 위임
// 수정 내용 : 하우징 아이콘을 Resources 폴더가 아닌 Inspector에 연결된 Imports Sprite 참조에서 찾도록 변경
// 수정 내용 : GameManager 없는 단독 하우징 테스트에서 CurrencyModel 재화 차감으로 구매 흐름을 확인할 수 있도록 보조
// 수정 내용 : 저장된 배치가 없는 기본 가구도 배치 팝업에서 장착됨으로 표시되도록 초기 장착 상태 보정
// 수정 내용 : 가구 구매 확인 팝업과 구매 직전 재화 보유량 검사를 하우징 MVP에 연결
// 수정 내용 : 언어 변경 시 Name_ID 기준 가구 이름을 다시 매핑해 배치 목록에 반영

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 하우징 배치 UI, 표시 데이터, 저장된 가구 배치를 연결합니다.
/// </summary>
public class HousingPlacementController : MonoBehaviour
{
    private const string _furnitureRootName = "FurnitureRoot";

    [Header("View")]
    [SerializeField] private HousingPlacementButtonView _buttonView;
    [SerializeField] private HousingPlacementPopupView _popupView;
    [SerializeField] private HousingPurchaseConfirmView _purchaseConfirmView;
    [FormerlySerializedAs("_placementView")]
    [SerializeField] private HousingFurnitureSlotView _slotView;

    [Header("데이터")]
    [SerializeField] private bool _useHousingRepo = true;
    [Tooltip("DB ICO 값과 연결할 Imports/OutUI/Housing 아이콘 Sprite 목록입니다.")]
    [SerializeField] private HousingSpriteBinding[] _iconSprites;
    [SerializeField] private HousingPlacementItemData[] _items;

    [Header("적용 방식")]
    [Tooltip("적용 버튼이 아직 연결되지 않은 상태에서 테스트할 때만 켭니다. 켜면 슬롯 클릭 즉시 FurnitureRoot에 적용합니다.")]
    [SerializeField] private bool _applyImmediatelyOnItemClick = true;

    [Header("단독 테스트")]
    [Tooltip("GameManager가 없는 테스트 씬에서만 CurrencyModel 재화로 구매 성공 처리를 허용합니다.")]
    [SerializeField] private bool _allowLocalPurchaseWithoutGameManager = true;
    [SerializeField] private CurrencyModel _currencyModel;

    private HousingPlacementModel _model;
    private HousingPlacementPresenter _presenter;
    private HousingPlacementItemMapper _itemMapper;
    private List<HousingPlacementItemData> _placementItems;

    private void Awake()
    {
        _itemMapper = new HousingPlacementItemMapper(_iconSprites);
        _slotView = ResolveSlotView();
        _placementItems = new List<HousingPlacementItemData>(BuildInitialItems());
        _model = new HousingPlacementModel(_placementItems);
        _model.SetEquippedFurnitureIds(BuildInitialEquippedFurnitureIds());
        _model.SetOwnedFurnitureIds(GetSavedOwnedFurnitureIds());
        _presenter = new HousingPlacementPresenter(
            _model,
            _buttonView,
            _popupView,
            _purchaseConfirmView,
            _slotView,
            _applyImmediatelyOnItemClick,
            HandleFurnitureApplied,
            HandleFurniturePurchaseRequested,
            CanAffordFurniturePurchase);
        _presenter.Initialize();
        BindLocalization();
        RestoreSavedPlacements();
    }

    private void OnDestroy()
    {
        UnbindLocalization();
        _presenter?.Release();
    }

    private void BindLocalization()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
        }
    }

    private void UnbindLocalization()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }
    }

    private void HandleLanguageChanged()
    {
        if (_model == null)
        {
            return;
        }

        _model.CancelPurchaseConfirmation();
        _model.SelectItem(null);
        _placementItems = new List<HousingPlacementItemData>(BuildInitialItems());
        _model.SetItems(_placementItems);
    }

    private IEnumerable<HousingPlacementItemData> BuildInitialItems()
    {
        if (!_useHousingRepo)
        {
            return _items;
        }

        HousingRepo _housingRepo = DataManager.Instance != null ? DataManager.Instance.HousingRepo : null;

        if (_housingRepo == null)
        {
            Debug.LogWarning("[HousingPlacementController] HousingRepo가 연결되지 않아 Inspector 표시 데이터를 사용합니다.", this);
            return _items;
        }

        List<HousingPlacementItemData> _repoItems = _itemMapper.Map(_housingRepo);

        if (_repoItems.Count <= 0)
        {
            Debug.LogWarning("[HousingPlacementController] HousingRepo에서 표시할 가구 데이터를 찾지 못해 Inspector 표시 데이터를 사용합니다.", this);
            return _items;
        }

        return _repoItems;
    }

    private void RestoreSavedPlacements()
    {
        if (_slotView == null || GameManager.Instance == null || GameManager.Instance.CloudData == null)
        {
            return;
        }

        List<int> _placedFurnitureIds = GameManager.Instance.CloudData.housingPlacedFurnitureIds;

        if (_placedFurnitureIds == null)
        {
            return;
        }

        for (int _index = 0; _index < _placedFurnitureIds.Count; _index++)
        {
            HousingPlacementItemData _itemData = FindItemByFurnitureId(_placedFurnitureIds[_index]);

            if (_itemData == null)
            {
                continue;
            }

            _slotView.ApplyFurniture(_itemData);
        }
    }

    private IEnumerable<int> GetSavedFurnitureIds()
    {
        if (GameManager.Instance == null || GameManager.Instance.CloudData == null)
        {
            return null;
        }

        return GameManager.Instance.CloudData.housingPlacedFurnitureIds;
    }

    private IEnumerable<int> GetSavedOwnedFurnitureIds()
    {
        if (GameManager.Instance == null || GameManager.Instance.CloudData == null)
        {
            return null;
        }

        return GameManager.Instance.CloudData.housingOwnedFurnitureIds;
    }

    private IEnumerable<int> BuildInitialEquippedFurnitureIds()
    {
        List<int> _equippedFurnitureIds = new();
        HashSet<string> _occupiedLocations = new();

        AddSavedEquippedFurnitureIds(_equippedFurnitureIds, _occupiedLocations);
        AddDefaultEquippedFurnitureIds(_equippedFurnitureIds, _occupiedLocations);

        return _equippedFurnitureIds;
    }

    private void AddSavedEquippedFurnitureIds(List<int> _equippedFurnitureIds, HashSet<string> _occupiedLocations)
    {
        IEnumerable<int> _savedFurnitureIds = GetSavedFurnitureIds();

        if (_savedFurnitureIds == null)
        {
            return;
        }

        foreach (int _furnitureId in _savedFurnitureIds)
        {
            HousingPlacementItemData _itemData = FindItemByFurnitureId(_furnitureId);

            if (_itemData == null || _itemData.FurnitureId <= 0)
            {
                continue;
            }

            _equippedFurnitureIds.Add(_itemData.FurnitureId);
            string _locationKey = NormalizeKey(_itemData.Location);

            if (!string.IsNullOrWhiteSpace(_locationKey))
            {
                _occupiedLocations.Add(_locationKey);
            }
        }
    }

    private void AddDefaultEquippedFurnitureIds(List<int> _equippedFurnitureIds, HashSet<string> _occupiedLocations)
    {
        if (_placementItems == null)
        {
            return;
        }

        for (int _index = 0; _index < _placementItems.Count; _index++)
        {
            HousingPlacementItemData _itemData = _placementItems[_index];

            if (!IsDefaultEquippedItem(_itemData))
            {
                continue;
            }

            string _locationKey = NormalizeKey(_itemData.Location);

            if (string.IsNullOrWhiteSpace(_locationKey) || _occupiedLocations.Contains(_locationKey))
            {
                continue;
            }

            _equippedFurnitureIds.Add(_itemData.FurnitureId);
            _occupiedLocations.Add(_locationKey);
        }
    }

    private static bool IsDefaultEquippedItem(HousingPlacementItemData _itemData)
    {
        if (_itemData == null || _itemData.FurnitureId <= 0)
        {
            return false;
        }

        return _itemData.IsOwned && _itemData.Price <= 0;
    }

    private bool HandleFurniturePurchaseRequested(HousingPlacementItemData _itemData)
    {
        if (_itemData == null || _itemData.FurnitureId <= 0)
        {
            return false;
        }

        if (GameManager.Instance == null)
        {
            return TryPurchaseFurnitureForLocalTest(_itemData);
        }

        return GameManager.Instance.TryPurchaseHousingFurniture(
            _itemData.FurnitureId,
            _itemData.Price,
            _itemData.PriceCurrency);
    }

    private bool CanAffordFurniturePurchase(HousingPlacementItemData _itemData)
    {
        if (_itemData == null)
        {
            return false;
        }

        int _safePrice = Mathf.Max(0, _itemData.Price);

        if (GameManager.Instance != null)
        {
            return _itemData.PriceCurrency == HousingPlacementPriceCurrency.Diamond
                ? GameManager.Instance.CanAffordDiamond(_safePrice)
                : GameManager.Instance.CanAffordCurrency(_safePrice, 0);
        }

        CurrencyModel _resolvedCurrencyModel = ResolveCurrencyModel();

        if (_resolvedCurrencyModel == null)
        {
            return false;
        }

        return _itemData.PriceCurrency == HousingPlacementPriceCurrency.Diamond
            ? _resolvedCurrencyModel.CanAffordDiamond(_safePrice)
            : _resolvedCurrencyModel.CanAfford(_safePrice, 0);
    }

    private bool TryPurchaseFurnitureForLocalTest(HousingPlacementItemData _itemData)
    {
        if (!_allowLocalPurchaseWithoutGameManager)
        {
            Debug.LogWarning("[HousingPlacementController] GameManager가 없어 하우징 구매를 저장할 수 없습니다.", this);
            return false;
        }

        CurrencyModel _resolvedCurrencyModel = ResolveCurrencyModel();

        if (_resolvedCurrencyModel == null)
        {
            Debug.LogWarning("[HousingPlacementController] GameManager와 CurrencyModel이 없어 단독 테스트 구매를 처리할 수 없습니다.", this);
            return false;
        }

        int _safePrice = Mathf.Max(0, _itemData.Price);
        bool _isPurchased = _itemData.PriceCurrency == HousingPlacementPriceCurrency.Diamond
            ? _resolvedCurrencyModel.SpendDiamond(_safePrice)
            : _resolvedCurrencyModel.SpendGold(_safePrice);

        if (!_isPurchased)
        {
            Debug.LogWarning($"[HousingPlacementController] 단독 테스트 재화가 부족합니다. Furniture: {_itemData.FurnitureId}, Price: {_safePrice}, Currency: {_itemData.PriceCurrency}", this);
            return false;
        }

        Debug.Log($"[HousingPlacementController] 단독 테스트 가구 구매 완료. Furniture: {_itemData.FurnitureId}, Price: {_safePrice}, Currency: {_itemData.PriceCurrency}", this);
        return true;
    }

    private CurrencyModel ResolveCurrencyModel()
    {
        if (_currencyModel != null)
        {
            return _currencyModel;
        }

        _currencyModel = FindFirstObjectByType<CurrencyModel>(FindObjectsInactive.Include);
        return _currencyModel;
    }

    private void HandleFurnitureApplied(HousingPlacementItemData _itemData)
    {
        if (_itemData == null || _itemData.FurnitureId <= 0 || GameManager.Instance == null)
        {
            return;
        }

        UserCloudData _cloudData = GameManager.Instance.CloudData ?? UserCloudData.CreateDefault();

        if (_cloudData.housingPlacedFurnitureIds == null)
        {
            _cloudData.housingPlacedFurnitureIds = new List<int>();
        }

        RemovePlacedFurnitureInSameLocation(_cloudData.housingPlacedFurnitureIds, _itemData);

        if (!_cloudData.housingPlacedFurnitureIds.Contains(_itemData.FurnitureId))
        {
            _cloudData.housingPlacedFurnitureIds.Add(_itemData.FurnitureId);
        }

        GameManager.Instance.SyncToCloud(_cloudData);
    }

    private void RemovePlacedFurnitureInSameLocation(List<int> _placedFurnitureIds, HousingPlacementItemData _newItemData)
    {
        string _newLocation = NormalizeKey(_newItemData.Location);

        for (int _index = _placedFurnitureIds.Count - 1; _index >= 0; _index--)
        {
            HousingPlacementItemData _savedItemData = FindItemByFurnitureId(_placedFurnitureIds[_index]);

            if (_savedItemData == null)
            {
                continue;
            }

            if (_savedItemData.FurnitureId == _newItemData.FurnitureId)
            {
                _placedFurnitureIds.RemoveAt(_index);
                continue;
            }

            if (NormalizeKey(_savedItemData.Location) != _newLocation)
            {
                continue;
            }

            _placedFurnitureIds.RemoveAt(_index);
        }
    }

    private HousingPlacementItemData FindItemByFurnitureId(int _furnitureId)
    {
        if (_placementItems == null)
        {
            return null;
        }

        for (int _index = 0; _index < _placementItems.Count; _index++)
        {
            HousingPlacementItemData _itemData = _placementItems[_index];

            if (_itemData != null && _itemData.FurnitureId == _furnitureId)
            {
                return _itemData;
            }
        }

        return null;
    }

    private static string NormalizeKey(string _value)
    {
        return string.IsNullOrWhiteSpace(_value) ? string.Empty : _value.Trim().ToLowerInvariant();
    }

    private HousingFurnitureSlotView ResolveSlotView()
    {
        if (_slotView != null)
        {
            return _slotView;
        }

        Transform _furnitureRoot = FindChildRecursive(GetPageRoot(), _furnitureRootName);

        if (_furnitureRoot == null)
        {
            Debug.LogWarning("[HousingPlacementController] FurnitureRoot를 찾지 못했습니다.", this);
            return null;
        }

        HousingFurnitureSlotView _foundView = _furnitureRoot.GetComponent<HousingFurnitureSlotView>();

        if (_foundView != null)
        {
            return _foundView;
        }

        return _furnitureRoot.gameObject.AddComponent<HousingFurnitureSlotView>();
    }

    private Transform GetPageRoot()
    {
        Transform _current = transform;

        while (_current.parent != null)
        {
            if (_current.name == "Page_Housing")
            {
                return _current;
            }

            _current = _current.parent;
        }

        return _current;
    }

    private static Transform FindChildRecursive(Transform _parent, string _name)
    {
        if (_parent == null)
        {
            return null;
        }

        if (_parent.name == _name)
        {
            return _parent;
        }

        for (int _index = 0; _index < _parent.childCount; _index++)
        {
            Transform _found = FindChildRecursive(_parent.GetChild(_index), _name);

            if (_found != null)
            {
                return _found;
            }
        }

        return null;
    }
}
