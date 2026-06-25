//담당자: 조규민

using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 하우징 배치 MVP 객체를 연결합니다.
/// </summary>
public class HousingPlacementController : MonoBehaviour
{
    private const string CATEGORY_FUNCTION = "function";
    private const string CATEGORY_BACKGROUND = "background";
    private const string CATEGORY_NONE = "none";
    private const string TYPE_DECORATION = "decoration";

    [Header("View")]
    [SerializeField] private HousingPlacementButtonView _buttonView;
    [SerializeField] private HousingPlacementPopupView _popupView;

    [Header("데이터")]
    [SerializeField] private bool _useHousingRepo = true;
    [Tooltip("Resources 기준 하우징 아이콘 폴더입니다. 비워두면 DB의 ICO 값을 그대로 사용합니다.")]
    [SerializeField] private string _iconResourceFolder = "Icons/Housing";
    [SerializeField] private HousingPlacementItemData[] _items;

    private HousingPlacementModel _model;
    private HousingPlacementPresenter _presenter;

    private void Awake()
    {
        _model = new HousingPlacementModel(BuildInitialItems());
        _presenter = new HousingPlacementPresenter(_model, _buttonView, _popupView);
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

        List<HousingPlacementItemData> _repoItems = BuildItemsFromRepo(_housingRepo);

        if (_repoItems.Count <= 0)
        {
            Debug.LogWarning("[HousingPlacementController] HousingRepo에서 표시할 가구 데이터를 찾지 못해 Inspector 표시 데이터를 사용합니다.", this);
            return _items;
        }

        return _repoItems;
    }

    private List<HousingPlacementItemData> BuildItemsFromRepo(HousingRepo _housingRepo)
    {
        List<HousingPlacementItemData> _result = new();
        HashSet<string> _addedKeys = new();

        AddFurnitureList(_result, _addedKeys, _housingRepo.GetFurnitureListByType(CATEGORY_FUNCTION));
        AddFurnitureList(_result, _addedKeys, _housingRepo.GetFurnitureListByType(CATEGORY_BACKGROUND));
        AddFurnitureList(_result, _addedKeys, _housingRepo.GetFurnitureListByType(CATEGORY_NONE));

        AddFurnitureList(_result, _addedKeys, _housingRepo.GetFurnitureListByCategory(CATEGORY_FUNCTION));
        AddFurnitureList(_result, _addedKeys, _housingRepo.GetFurnitureListByCategory(CATEGORY_BACKGROUND));
        AddFurnitureList(_result, _addedKeys, _housingRepo.GetFurnitureListByCategory(TYPE_DECORATION));

        return _result;
    }

    private void AddFurnitureList(
        List<HousingPlacementItemData> _target,
        HashSet<string> _addedKeys,
        List<HousingFurnitureData> _furnitures)
    {
        if (_furnitures == null)
        {
            return;
        }

        for (int _index = 0; _index < _furnitures.Count; _index++)
        {
            HousingFurnitureData _furniture = _furnitures[_index];

            if (_furniture == null)
            {
                continue;
            }

            string _key = BuildFurnitureKey(_furniture);

            if (_addedKeys.Contains(_key))
            {
                continue;
            }

            _addedKeys.Add(_key);
            _target.Add(CreateItemData(_furniture));
        }
    }

    private HousingPlacementItemData CreateItemData(HousingFurnitureData _furniture)
    {
        string _itemId = _furniture.Furniture_ID.ToString();
        string _displayName = ResolveDisplayName(_furniture);
        HousingPlacementCategory _category = ResolvePlacementCategory(_furniture);
        Sprite _icon = LoadIcon(_furniture.ICO);
        bool _isOwned = _furniture.Price <= 0;

        return new HousingPlacementItemData(
            _furniture.Furniture_ID,
            _furniture.Name_ID,
            _itemId,
            _displayName,
            _category,
            _icon,
            _furniture.ICO,
            _isOwned,
            true,
            _furniture.Item_ID,
            _furniture.Price,
            _furniture.Location,
            _furniture.Category,
            _furniture.Type,
            _furniture.Resources);
    }

    private string ResolveDisplayName(HousingFurnitureData _furniture)
    {
        if (_furniture.Name_ID > 0)
        {
            return $"Name ID: {_furniture.Name_ID}";
        }

        string _type = string.IsNullOrWhiteSpace(_furniture.Type) ? "Furniture" : _furniture.Type;
        return $"{_type} #{_furniture.Furniture_ID}";
    }

    private HousingPlacementCategory ResolvePlacementCategory(HousingFurnitureData _furniture)
    {
        string _category = NormalizeKey(_furniture.Category);
        string _type = NormalizeKey(_furniture.Type);

        if (_category == CATEGORY_BACKGROUND)
        {
            return HousingPlacementCategory.Background;
        }

        if (_category == CATEGORY_FUNCTION)
        {
            return HousingPlacementCategory.Function;
        }

        if (_category == CATEGORY_NONE || _category == TYPE_DECORATION || _type == TYPE_DECORATION)
        {
            return HousingPlacementCategory.Decoration;
        }

        return HousingPlacementCategory.Decoration;
    }

    private Sprite LoadIcon(string _iconKey)
    {
        if (string.IsNullOrWhiteSpace(_iconKey))
        {
            return null;
        }

        string _fileNameWithoutExtension = Path.GetFileNameWithoutExtension(_iconKey);
        string _resourcePath = string.IsNullOrWhiteSpace(_iconResourceFolder)
            ? _fileNameWithoutExtension
            : $"{_iconResourceFolder.TrimEnd('/')}/{_fileNameWithoutExtension}";

        return Resources.Load<Sprite>(_resourcePath);
    }

    private static string BuildFurnitureKey(HousingFurnitureData _furniture)
    {
        return $"{_furniture.Furniture_ID}|{_furniture.Location}|{_furniture.Category}|{_furniture.Type}";
    }

    private static string NormalizeKey(string _value)
    {
        return string.IsNullOrWhiteSpace(_value) ? string.Empty : _value.Trim().ToLowerInvariant();
    }
}
