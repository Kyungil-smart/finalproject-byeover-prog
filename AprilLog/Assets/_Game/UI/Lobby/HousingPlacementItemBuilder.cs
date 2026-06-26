//담당자: 조규민

using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// HousingRepo의 가구 데이터를 하우징 배치 UI 표시 데이터로 변환합니다.
/// </summary>
public class HousingPlacementItemBuilder
{
    private const string _categoryFunction = "function";
    private const string _categoryBackground = "background";
    private const string _categoryNone = "none";
    private const string _typeDecoration = "decoration";

    private readonly string _iconResourceFolder;

    public HousingPlacementItemBuilder(string _iconResourceFolder)
    {
        this._iconResourceFolder = NormalizeResourceFolder(_iconResourceFolder);
    }

    public List<HousingPlacementItemData> Build(HousingRepo _housingRepo)
    {
        List<HousingPlacementItemData> _items = new();

        if (_housingRepo == null)
        {
            return _items;
        }

        HashSet<string> _addedKeys = new();

        AddByCurrentRepoCategory(_items, _addedKeys, _housingRepo, _categoryFunction);
        AddByCurrentRepoCategory(_items, _addedKeys, _housingRepo, _categoryBackground);
        AddByCurrentRepoCategory(_items, _addedKeys, _housingRepo, _categoryNone);
        AddByCurrentRepoType(_items, _addedKeys, _housingRepo, _typeDecoration);

        return _items;
    }

    private void AddByCurrentRepoCategory(
        List<HousingPlacementItemData> _target,
        HashSet<string> _addedKeys,
        HousingRepo _housingRepo,
        string _category)
    {
        // 현재 HousingRepo는 GetFurnitureListByType이 Category 캐시를 바라본다.
        AddFurnitureList(_target, _addedKeys, _housingRepo.GetFurnitureListByType(_category));
    }

    private void AddByCurrentRepoType(
        List<HousingPlacementItemData> _target,
        HashSet<string> _addedKeys,
        HousingRepo _housingRepo,
        string _type)
    {
        // 현재 HousingRepo는 GetFurnitureListByCategory가 Type 캐시를 바라본다.
        AddFurnitureList(_target, _addedKeys, _housingRepo.GetFurnitureListByCategory(_type));
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
        string _iconKey = NormalizeFileKey(_furniture.ICO);
        Sprite _icon = LoadIcon(_iconKey);
        bool _isOwned = _furniture.Price <= 0;

        return new HousingPlacementItemData(
            _furniture.Furniture_ID,
            _furniture.Name_ID,
            _itemId,
            _displayName,
            _category,
            _icon,
            _iconKey,
            _isOwned,
            true,
            _furniture.Item_ID,
            Mathf.Max(0, _furniture.Price),
            NormalizeText(_furniture.Location),
            NormalizeKey(_furniture.Category),
            NormalizeKey(_furniture.Type),
            NormalizeFileKey(_furniture.Resources));
    }

    private string ResolveDisplayName(HousingFurnitureData _furniture)
    {
        if (_furniture.Name_ID > 0)
        {
            return $"Name ID: {_furniture.Name_ID}";
        }

        string _typeName = ResolveTypeName(_furniture.Type);
        return $"{_typeName} #{_furniture.Furniture_ID}";
    }

    private HousingPlacementCategory ResolvePlacementCategory(HousingFurnitureData _furniture)
    {
        string _category = NormalizeKey(_furniture.Category);
        string _type = NormalizeKey(_furniture.Type);

        if (_category == _categoryBackground)
        {
            return HousingPlacementCategory.Background;
        }

        if (_category == _categoryFunction)
        {
            return HousingPlacementCategory.Function;
        }

        if (_category == _categoryNone || _category == _typeDecoration || _type == _typeDecoration)
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

        if (string.IsNullOrWhiteSpace(_fileNameWithoutExtension))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(_iconResourceFolder))
        {
            Sprite _folderIcon = Resources.Load<Sprite>($"{_iconResourceFolder}/{_fileNameWithoutExtension}");

            if (_folderIcon != null)
            {
                return _folderIcon;
            }
        }

        return Resources.Load<Sprite>(_fileNameWithoutExtension);
    }

    private static string ResolveTypeName(string _type)
    {
        switch (NormalizeKey(_type))
        {
            case "bed":
                return "침대";
            case "coffee":
                return "커피";
            case "reward":
                return "보상";
            case "decoration":
                return "장식";
            case "floor":
                return "바닥";
            case "wall":
                return "벽";
            default:
                return "가구";
        }
    }

    private static string BuildFurnitureKey(HousingFurnitureData _furniture)
    {
        return $"{_furniture.Furniture_ID}|{NormalizeText(_furniture.Location)}|{NormalizeKey(_furniture.Category)}|{NormalizeKey(_furniture.Type)}";
    }

    private static string NormalizeResourceFolder(string _value)
    {
        if (string.IsNullOrWhiteSpace(_value))
        {
            return string.Empty;
        }

        return _value.Trim().Trim('/');
    }

    private static string NormalizeFileKey(string _value)
    {
        return NormalizeText(_value);
    }

    private static string NormalizeKey(string _value)
    {
        return string.IsNullOrWhiteSpace(_value) ? string.Empty : _value.Trim().ToLowerInvariant();
    }

    private static string NormalizeText(string _value)
    {
        return string.IsNullOrWhiteSpace(_value) ? string.Empty : _value.Trim();
    }
}
