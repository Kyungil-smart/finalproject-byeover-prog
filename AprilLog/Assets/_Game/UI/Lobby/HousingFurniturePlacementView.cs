//담당자: 조규민

using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하우징 가구 데이터를 FurnitureRoot의 고정 위치 이미지에 적용합니다.
/// </summary>
public class HousingFurniturePlacementView : MonoBehaviour
{
    [Serializable]
    public class LocationBinding
    {
        [Header("배치 위치")]
        [SerializeField] private string _locationKey;
        [SerializeField] private Image _targetImage;

        public string LocationKey => _locationKey;
        public Image TargetImage => _targetImage;
    }

    private struct DefaultLocationBinding
    {
        public string LocationKey { get; }
        public string ObjectName { get; }

        public DefaultLocationBinding(string _locationKey, string _objectName)
        {
            LocationKey = _locationKey;
            ObjectName = _objectName;
        }
    }

    private static readonly DefaultLocationBinding[] _defaultLocationBindings =
    {
        new DefaultLocationBinding("Location1", "Location1_Bed"),
        new DefaultLocationBinding("Location2", "Location2_Coffee"),
        new DefaultLocationBinding("Location3", "Location3_Reward"),
        new DefaultLocationBinding("Location4", "Location4_DecorationA"),
        new DefaultLocationBinding("Location5", "Location5_DecorationB"),
        new DefaultLocationBinding("Location6", "Location6_Floor"),
        new DefaultLocationBinding("Location7", "Location7_Wall")
    };

    [Header("가구 위치 연결")]
    [SerializeField] private LocationBinding[] _locationBindings;

    [Header("리소스 경로")]
    [Tooltip("Resources 기준 가구 배치 이미지 폴더입니다. 비워두면 DB의 Resources 파일명만 사용합니다.")]
    [SerializeField] private string _resourceFolder = "Icons/Housing";

    public bool ApplyFurniture(HousingPlacementItemData _itemData)
    {
        if (_itemData == null)
        {
            return false;
        }

        Image _targetImage = ResolveTargetImage(_itemData.Location);

        if (_targetImage == null)
        {
            Debug.LogWarning($"[HousingFurniturePlacementView] 배치 위치를 찾지 못했습니다. Location: {_itemData.Location}", this);
            return false;
        }

        Sprite _sprite = LoadFurnitureSprite(_itemData.ResourceKey);

        if (_sprite == null)
        {
            _sprite = _itemData.Icon;
        }

        if (_sprite == null)
        {
            Debug.LogWarning($"[HousingFurniturePlacementView] 가구 이미지를 찾지 못했습니다. Furniture: {_itemData.FurnitureId}, Resource: {_itemData.ResourceKey}", this);
            return false;
        }

        _targetImage.sprite = _sprite;
        _targetImage.color = EnsureVisibleColor(_targetImage.color);
        _targetImage.enabled = true;
        return true;
    }

    private static Color EnsureVisibleColor(Color _color)
    {
        if (_color.a > 0f)
        {
            return _color;
        }

        _color.a = 1f;
        return _color;
    }

    private Image ResolveTargetImage(string _locationKey)
    {
        Image _boundImage = FindBoundImage(_locationKey);

        if (_boundImage != null)
        {
            return _boundImage;
        }

        string _objectName = FindDefaultObjectName(_locationKey);

        if (string.IsNullOrWhiteSpace(_objectName))
        {
            return null;
        }

        Transform _target = FindChildRecursive(transform, _objectName);
        return _target != null ? _target.GetComponent<Image>() : null;
    }

    private Image FindBoundImage(string _locationKey)
    {
        if (_locationBindings == null)
        {
            return null;
        }

        string _normalizedLocation = NormalizeKey(_locationKey);

        for (int _index = 0; _index < _locationBindings.Length; _index++)
        {
            LocationBinding _binding = _locationBindings[_index];

            if (_binding == null || _binding.TargetImage == null)
            {
                continue;
            }

            if (NormalizeKey(_binding.LocationKey) != _normalizedLocation)
            {
                continue;
            }

            return _binding.TargetImage;
        }

        return null;
    }

    private Sprite LoadFurnitureSprite(string _resourceKey)
    {
        if (string.IsNullOrWhiteSpace(_resourceKey))
        {
            return null;
        }

        string _fileName = Path.GetFileNameWithoutExtension(_resourceKey);

        if (string.IsNullOrWhiteSpace(_fileName))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(_resourceFolder))
        {
            Sprite _folderSprite = Resources.Load<Sprite>($"{_resourceFolder.Trim().TrimEnd('/')}/{_fileName}");

            if (_folderSprite != null)
            {
                return _folderSprite;
            }
        }

        return Resources.Load<Sprite>(_fileName);
    }

    private static string FindDefaultObjectName(string _locationKey)
    {
        string _normalizedLocation = NormalizeKey(_locationKey);

        for (int _index = 0; _index < _defaultLocationBindings.Length; _index++)
        {
            DefaultLocationBinding _binding = _defaultLocationBindings[_index];

            if (NormalizeKey(_binding.LocationKey) == _normalizedLocation)
            {
                return _binding.ObjectName;
            }
        }

        return null;
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

    private static string NormalizeKey(string _value)
    {
        return string.IsNullOrWhiteSpace(_value) ? string.Empty : _value.Trim().ToLowerInvariant();
    }
}
