//담당자: 조규민

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하우징 배치 MVP 객체를 연결합니다.
/// </summary>
public class HousingPlacementController : MonoBehaviour
{
    private const string _furnitureRootName = "FurnitureRoot";

    [Header("View")]
    [SerializeField] private HousingPlacementButtonView _buttonView;
    [SerializeField] private HousingPlacementPopupView _popupView;
    [SerializeField] private HousingFurniturePlacementView _placementView;

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
    private HousingPlacementItemBuilder _itemBuilder;

    private void Awake()
    {
        _itemBuilder = new HousingPlacementItemBuilder(_iconResourceFolder);
        _placementView = ResolvePlacementView();
        _model = new HousingPlacementModel(BuildInitialItems());
        _presenter = new HousingPlacementPresenter(_model, _buttonView, _popupView, _placementView, _applyImmediatelyOnItemClick);
        _presenter.Initialize();
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

        List<HousingPlacementItemData> _repoItems = _itemBuilder.Build(_housingRepo);

        if (_repoItems.Count <= 0)
        {
            Debug.LogWarning("[HousingPlacementController] HousingRepo에서 표시할 가구 데이터를 찾지 못해 Inspector 표시 데이터를 사용합니다.", this);
            return _items;
        }

        return _repoItems;
    }

    private HousingFurniturePlacementView ResolvePlacementView()
    {
        if (_placementView != null)
        {
            return _placementView;
        }

        Transform _furnitureRoot = FindChildRecursive(GetPageRoot(), _furnitureRootName);

        if (_furnitureRoot == null)
        {
            Debug.LogWarning("[HousingPlacementController] FurnitureRoot를 찾지 못했습니다.", this);
            return null;
        }

        HousingFurniturePlacementView _foundView = _furnitureRoot.GetComponent<HousingFurniturePlacementView>();

        if (_foundView != null)
        {
            return _foundView;
        }

        return _furnitureRoot.gameObject.AddComponent<HousingFurniturePlacementView>();
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
