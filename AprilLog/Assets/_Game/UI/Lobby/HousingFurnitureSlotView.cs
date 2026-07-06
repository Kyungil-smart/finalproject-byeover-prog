//담당자: 조규민

// 하우징 가구 이미지를 Resources 폴더가 아닌 Inspector에 연결된 Imports Sprite 참조에서 찾도록 변경
// 가구 적용 때마다 위치와 Sprite 목록을 반복 탐색하지 않도록 런타임 캐시를 추가

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하우징 가구 데이터를 방 안의 고정 슬롯 이미지에 적용합니다.
/// </summary>
// 배치 아이템 위치 키에 맞는 이미지와 Sprite 탐색 후 가구 슬롯 표시 갱신
// 위치·Sprite 키 캐시 구성으로 반복 탐색 비용 절감
public class HousingFurnitureSlotView : MonoBehaviour
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

    [Header("가구 이미지 연결")]
    [Tooltip("DB Resources 값과 연결할 Imports/OutUI/Housing 가구 Sprite 목록입니다.")]
    [SerializeField] private HousingSpriteBinding[] _furnitureSprites;

    private readonly Dictionary<string, Image> _targetImageByLocation = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Sprite> _spriteByResourceKey = new(StringComparer.Ordinal);
    private bool _isCacheBuilt;

    private void Awake()
    {
        BuildCaches();
    }

    private void OnValidate()
    {
        _isCacheBuilt = false;
    }

    // 위치 키와 Sprite 키 검증 후 대상 가구 이미지 교체
    public bool ApplyFurniture(HousingPlacementItemData _itemData)
    {
        if (_itemData == null)
        {
            return false;
        }

        Image _targetImage = ResolveTargetImage(_itemData.Location);

        if (_targetImage == null)
        {
            Debug.LogWarning($"[HousingFurnitureSlotView] 배치 위치를 찾지 못했습니다. Location: {_itemData.Location}", this);
            return false;
        }

        Sprite _sprite = LoadFurnitureSprite(_itemData.ResourceKey);

        if (_sprite == null)
        {
            _sprite = _itemData.Icon;
        }

        if (_sprite == null)
        {
            Debug.LogWarning($"[HousingFurnitureSlotView] 가구 이미지를 찾지 못했습니다. Furniture: {_itemData.FurnitureId}, Resource: {_itemData.ResourceKey}", this);
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

    // 정규화된 위치 키 기반 가구 슬롯 이미지 탐색
    private Image ResolveTargetImage(string _locationKey)
    {
        EnsureCaches();
        string _normalizedLocation = NormalizeKey(_locationKey);

        if (string.IsNullOrEmpty(_normalizedLocation))
        {
            return null;
        }

        return _targetImageByLocation.TryGetValue(_normalizedLocation, out Image _targetImage)
            ? _targetImage
            : null;
    }

    private Sprite LoadFurnitureSprite(string _resourceKey)
    {
        EnsureCaches();
        string _normalizedKey = NormalizeSpriteKey(_resourceKey);

        if (string.IsNullOrEmpty(_normalizedKey))
        {
            return null;
        }

        return _spriteByResourceKey.TryGetValue(_normalizedKey, out Sprite _sprite)
            ? _sprite
            : null;
    }

    private void EnsureCaches()
    {
        if (_isCacheBuilt)
        {
            return;
        }

        BuildCaches();
    }

    // 배치 위치 이미지와 Resources 가구 Sprite 캐시 구성
    private void BuildCaches()
    {
        _targetImageByLocation.Clear();
        _spriteByResourceKey.Clear();
        CacheBoundLocationImages();
        CacheDefaultLocationImages();
        CacheFurnitureSprites();
        _isCacheBuilt = true;
    }

    private void CacheBoundLocationImages()
    {
        if (_locationBindings == null)
        {
            return;
        }

        for (int _index = 0; _index < _locationBindings.Length; _index++)
        {
            LocationBinding _binding = _locationBindings[_index];

            if (_binding == null || _binding.TargetImage == null)
            {
                continue;
            }

            string _normalizedLocation = NormalizeKey(_binding.LocationKey);

            if (string.IsNullOrEmpty(_normalizedLocation) || _targetImageByLocation.ContainsKey(_normalizedLocation))
            {
                continue;
            }

            _targetImageByLocation.Add(_normalizedLocation, _binding.TargetImage);
        }
    }

    private void CacheDefaultLocationImages()
    {
        for (int _index = 0; _index < _defaultLocationBindings.Length; _index++)
        {
            DefaultLocationBinding _binding = _defaultLocationBindings[_index];
            string _normalizedLocation = NormalizeKey(_binding.LocationKey);

            if (string.IsNullOrEmpty(_normalizedLocation) || _targetImageByLocation.ContainsKey(_normalizedLocation))
            {
                continue;
            }

            Transform _target = FindChildRecursive(transform, _binding.ObjectName);
            Image _targetImage = _target != null ? _target.GetComponent<Image>() : null;

            if (_targetImage != null)
            {
                _targetImageByLocation.Add(_normalizedLocation, _targetImage);
            }
        }
    }

    private void CacheFurnitureSprites()
    {
        if (_furnitureSprites == null)
        {
            return;
        }

        for (int _index = 0; _index < _furnitureSprites.Length; _index++)
        {
            HousingSpriteBinding _binding = _furnitureSprites[_index];

            if (_binding == null || _binding.Sprite == null)
            {
                continue;
            }

            string _normalizedKey = NormalizeSpriteKey(_binding.SpriteKey);

            if (string.IsNullOrEmpty(_normalizedKey) || _spriteByResourceKey.ContainsKey(_normalizedKey))
            {
                continue;
            }

            _spriteByResourceKey.Add(_normalizedKey, _binding.Sprite);
        }
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

    private static string NormalizeSpriteKey(string _value)
    {
        return string.IsNullOrWhiteSpace(_value)
            ? string.Empty
            : Path.GetFileNameWithoutExtension(_value.Trim()).ToLowerInvariant();
    }
}
