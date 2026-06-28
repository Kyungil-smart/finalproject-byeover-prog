//담당자: 조규민

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
    [FormerlySerializedAs("_placementView")]
    [SerializeField] private HousingFurnitureSlotView _slotView;

    [Header("데이터")]
    [SerializeField] private bool _useHousingRepo = true;
    [Tooltip("Resources 기준 하우징 아이콘 폴더입니다. 비워두면 DB의 ICO 값을 그대로 사용합니다.")]
    [SerializeField] private string _iconResourceFolder = "Icons/Housing";
    [SerializeField] private HousingPlacementItemData[] _items;

    [Header("적용 방식")]
    [Tooltip("적용 버튼이 아직 연결되지 않은 상태에서 테스트할 때만 켭니다. 켜면 슬롯 클릭 즉시 FurnitureRoot에 적용합니다.")]
    [SerializeField] private bool _applyImmediatelyOnItemClick = true;

    private HousingPlacementModel _model;
    private HousingPlacementPresenter _presenter;
    private HousingPlacementItemMapper _itemMapper;
    private List<HousingPlacementItemData> _placementItems;

    private void Awake()
    {
        _itemMapper = new HousingPlacementItemMapper(_iconResourceFolder);
        _slotView = ResolveSlotView();
        _placementItems = new List<HousingPlacementItemData>(BuildInitialItems());
        _model = new HousingPlacementModel(_placementItems);
        _presenter = new HousingPlacementPresenter(_model, _buttonView, _popupView, _slotView, _applyImmediatelyOnItemClick, HandleFurnitureApplied);
        _presenter.Initialize();
        RestoreSavedPlacements();
    }

    private void OnDestroy()
    {
        _presenter?.Release();
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
