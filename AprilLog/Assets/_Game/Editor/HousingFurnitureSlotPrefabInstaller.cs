//담당자: 조규민

#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Page_Housing 프리팹의 FurnitureRoot에 고정 배치 슬롯을 구성합니다.
/// </summary>
// 가구 위치별 슬롯 이미지 생성과 RectTransform·Image 설정 복사
// 고정 가구의 상호작용 컴포넌트와 책장 다시보기 입력 연결
public static class HousingFurnitureSlotPrefabInstaller
{
    private const string _prefabPath = "Assets/_Game/Prefabs/UI/Lobby/Page/Housing/Page_Housing.prefab";
    private const string _furnitureRootName = "FurnitureRoot";

    private readonly struct SlotDefinition
    {
        public readonly string LocationKey;
        public readonly string SlotName;
        public readonly string SourceName;
        public readonly Vector2 AnchoredPosition;
        public readonly Vector2 SizeDelta;

        public SlotDefinition(
            string _locationKey,
            string _slotName,
            string _sourceName,
            Vector2 _anchoredPosition,
            Vector2 _sizeDelta)
        {
            LocationKey = _locationKey;
            SlotName = _slotName;
            SourceName = _sourceName;
            AnchoredPosition = _anchoredPosition;
            SizeDelta = _sizeDelta;
        }
    }

    private readonly struct StaticFurnitureDefinition
    {
        public readonly string ObjectName;
        public readonly Vector2 AnchoredPosition;
        public readonly Vector2 SizeDelta;
        public readonly bool EnableReplayStory;

        public StaticFurnitureDefinition(
            string _objectName,
            Vector2 _anchoredPosition,
            Vector2 _sizeDelta,
            bool _enableReplayStory)
        {
            ObjectName = _objectName;
            AnchoredPosition = _anchoredPosition;
            SizeDelta = _sizeDelta;
            EnableReplayStory = _enableReplayStory;
        }
    }

    private static readonly SlotDefinition[] _slotDefinitions =
    {
        new SlotDefinition("Location7", "Location7_Wall", "Image_HousingRoomBackground", Vector2.zero, Vector2.zero),
        new SlotDefinition("Location6", "Location6_Floor", "Panel_HousingRoom", Vector2.zero, Vector2.zero),
        new SlotDefinition("Location1", "Location1_Bed", "Housing_Bed", new Vector2(-499f, -294f), new Vector2(262f, 262f)),
        new SlotDefinition("Location2", "Location2_Coffee", "Housing_Sink", new Vector2(80f, -257f), new Vector2(260f, 260f)),
        new SlotDefinition("Location3", "Location3_Reward", "Housing_coingenerator", new Vector2(-287f, -77f), new Vector2(247f, 247f)),
        new SlotDefinition("Location4", "Location4_DecorationA", "Housing_Desk", new Vector2(310f, -155f), new Vector2(251f, 251f)),
        new SlotDefinition("Location5", "Location5_DecorationB", "Housing_Sofa", new Vector2(512f, -257f), new Vector2(259f, 259f))
    };

    private static readonly StaticFurnitureDefinition[] _staticFurnitureDefinitions =
    {
        new StaticFurnitureDefinition("Static_Furniture_01", new Vector2(-360f, -215f), new Vector2(180f, 180f), true),
        new StaticFurnitureDefinition("Static_Furniture_02", new Vector2(0f, -215f), new Vector2(180f, 180f), false),
        new StaticFurnitureDefinition("Static_Furniture_03", new Vector2(360f, -215f), new Vector2(180f, 180f), false)
    };

    [MenuItem("Tools/Housing/Install FurnitureRoot Location Slots")]
    // 가구 루트 위치별 슬롯과 고정 가구 상호작용 구성 설치
    public static void InstallFurnitureRootSlots()
    {
        GameObject _prefabRoot = PrefabUtility.LoadPrefabContents(_prefabPath);

        try
        {
            Transform _furnitureRoot = FindChildRecursive(_prefabRoot.transform, _furnitureRootName);

            if (_furnitureRoot == null)
            {
                Debug.LogError($"[HousingFurnitureSlotPrefabInstaller] {_furnitureRootName}를 찾지 못했습니다.");
                return;
            }

            EnsureFurnitureRootLayout(_furnitureRoot);
            HousingFurnitureSlotView _slotView = EnsureSlotView(_furnitureRoot);
            SerializedObject _serializedView = new SerializedObject(_slotView);
            SerializedProperty _bindings = _serializedView.FindProperty("_locationBindings");
            _bindings.arraySize = _slotDefinitions.Length;

            for (int _index = 0; _index < _slotDefinitions.Length; _index++)
            {
                SlotDefinition _definition = _slotDefinitions[_index];
                Image _slotImage = EnsureSlotImage(_prefabRoot.transform, _furnitureRoot, _definition);
                ApplySlotLayout(_definition, _slotImage);
                _slotImage.transform.SetSiblingIndex(_index);

                SerializedProperty _binding = _bindings.GetArrayElementAtIndex(_index);
                _binding.FindPropertyRelative("_locationKey").stringValue = _definition.LocationKey;
                _binding.FindPropertyRelative("_targetImage").objectReferenceValue = _slotImage;
            }

            EnsureStaticFurnitureRoot(_furnitureRoot);

            _serializedView.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(_prefabRoot, _prefabPath);
            Debug.Log("[HousingFurnitureSlotPrefabInstaller] FurnitureRoot Location 슬롯 구성을 완료했습니다.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(_prefabRoot);
        }
    }

    private static HousingFurnitureSlotView EnsureSlotView(Transform _furnitureRoot)
    {
        HousingFurnitureSlotView _slotView = _furnitureRoot.GetComponent<HousingFurnitureSlotView>();

        if (_slotView != null)
        {
            return _slotView;
        }

        return _furnitureRoot.gameObject.AddComponent<HousingFurnitureSlotView>();
    }

    private static void EnsureFurnitureRootLayout(Transform _furnitureRoot)
    {
        RectTransform _rootRect = _furnitureRoot as RectTransform;

        if (_rootRect == null)
        {
            _rootRect = _furnitureRoot.gameObject.AddComponent<RectTransform>();
        }

        StretchToParent(_rootRect);
    }

    // 기존 위치 이미지 재사용 또는 신규 슬롯 Image 생성
    private static Image EnsureSlotImage(Transform _prefabRoot, Transform _furnitureRoot, SlotDefinition _definition)
    {
        Transform _slot = _furnitureRoot.Find(_definition.SlotName);

        if (_slot == null)
        {
            Transform _source = FindChildRecursive(_prefabRoot, _definition.SourceName);

            if (_source != null && _source.parent == _furnitureRoot)
            {
                _source.name = _definition.SlotName;
                _slot = _source;
            }
            else
            {
                GameObject _slotObject = new GameObject(_definition.SlotName, typeof(RectTransform), typeof(Image));
                _slotObject.transform.SetParent(_furnitureRoot, false);
                _slot = _slotObject.transform;
                CopyRectTransform(_source as RectTransform, _slot as RectTransform);
                CopyImage(_source != null ? _source.GetComponent<Image>() : null, _slot.GetComponent<Image>());
            }
        }

        RectTransform _slotRect = _slot as RectTransform;

        if (_slotRect == null)
        {
            _slotRect = _slot.gameObject.AddComponent<RectTransform>();
        }

        Image _slotImage = _slot.GetComponent<Image>();

        if (_slotImage == null)
        {
            _slotImage = _slot.gameObject.AddComponent<Image>();
        }

        _slotImage.raycastTarget = false;
        _slot.gameObject.SetActive(true);
        return _slotImage;
    }

    private static void ApplySlotLayout(SlotDefinition _definition, Image _slotImage)
    {
        if (_slotImage == null)
        {
            return;
        }

        RectTransform _slotRect = _slotImage.transform as RectTransform;

        if (_slotRect == null)
        {
            return;
        }

        if (IsRoomSurface(_definition.LocationKey))
        {
            StretchToParent(_slotRect);
            _slotImage.preserveAspect = false;
            return;
        }

        _slotRect.anchorMin = new Vector2(0.5f, 0.5f);
        _slotRect.anchorMax = new Vector2(0.5f, 0.5f);
        _slotRect.pivot = new Vector2(0.5f, 0.5f);
        _slotRect.anchoredPosition = _definition.AnchoredPosition;
        _slotRect.sizeDelta = _definition.SizeDelta;
        _slotRect.localScale = Vector3.one;
        _slotImage.preserveAspect = true;
    }

    private static void EnsureStaticFurnitureRoot(Transform _furnitureRoot)
    {
        Transform _staticRoot = _furnitureRoot.Find("StaticFurnitureRoot");

        if (_staticRoot == null)
        {
            GameObject _staticRootObject = new GameObject("StaticFurnitureRoot", typeof(RectTransform));
            _staticRootObject.transform.SetParent(_furnitureRoot, false);
            _staticRoot = _staticRootObject.transform;
        }

        RectTransform _staticRootRect = _staticRoot as RectTransform;

        if (_staticRootRect == null)
        {
            _staticRootRect = _staticRoot.gameObject.AddComponent<RectTransform>();
        }

        StretchToParent(_staticRootRect);
        _staticRoot.SetAsLastSibling();

        for (int _index = 0; _index < _staticFurnitureDefinitions.Length; _index++)
        {
            EnsureStaticFurnitureImage(_staticRoot, _staticFurnitureDefinitions[_index]);
        }
    }

    private static void EnsureStaticFurnitureImage(Transform _staticRoot, StaticFurnitureDefinition _definition)
    {
        Transform _staticFurniture = _staticRoot.Find(_definition.ObjectName);

        if (_staticFurniture == null)
        {
            GameObject _staticFurnitureObject = new GameObject(_definition.ObjectName, typeof(RectTransform), typeof(Image));
            _staticFurnitureObject.transform.SetParent(_staticRoot, false);
            _staticFurniture = _staticFurnitureObject.transform;
        }

        RectTransform _rectTransform = _staticFurniture as RectTransform;

        if (_rectTransform == null)
        {
            _rectTransform = _staticFurniture.gameObject.AddComponent<RectTransform>();
        }

        Image _image = _staticFurniture.GetComponent<Image>();

        if (_image == null)
        {
            _image = _staticFurniture.gameObject.AddComponent<Image>();
        }

        _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _rectTransform.anchoredPosition = _definition.AnchoredPosition;
        _rectTransform.sizeDelta = _definition.SizeDelta;
        _rectTransform.localRotation = Quaternion.identity;
        _rectTransform.localScale = Vector3.one;

        _image.raycastTarget = _definition.EnableReplayStory;
        _image.preserveAspect = true;
        ConfigureStaticFurnitureInteraction(_staticFurniture.gameObject, _image, _definition.EnableReplayStory);

        if (_image.sprite == null && _image.color.a <= 0f)
        {
            _image.color = new Color(1f, 1f, 1f, 0.18f);
        }

        _staticFurniture.gameObject.SetActive(true);
    }

    // 고정 가구 Raycast와 책장 다시보기 컴포넌트 설정
    private static void ConfigureStaticFurnitureInteraction(GameObject _target, Image _image, bool _enableReplayStory)
    {
        if (_target == null)
        {
            return;
        }

        Button _button = _target.GetComponent<Button>();
        HousingBookshelfReplayBinder _binder = _target.GetComponent<HousingBookshelfReplayBinder>();

        if (!_enableReplayStory)
        {
            if (_button != null)
            {
                UnityEngine.Object.DestroyImmediate(_button, true);
            }

            if (_binder != null)
            {
                UnityEngine.Object.DestroyImmediate(_binder, true);
            }

            return;
        }

        if (_button == null)
        {
            _button = _target.AddComponent<Button>();
        }

        _button.targetGraphic = _image;
        _button.transition = Selectable.Transition.ColorTint;
        _button.interactable = true;

        if (_binder == null)
        {
            _binder = _target.AddComponent<HousingBookshelfReplayBinder>();
        }
    }

    private static void StretchToParent(RectTransform _rectTransform)
    {
        _rectTransform.anchorMin = Vector2.zero;
        _rectTransform.anchorMax = Vector2.one;
        _rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;
        _rectTransform.localRotation = Quaternion.identity;
        _rectTransform.localScale = Vector3.one;
    }

    private static bool IsRoomSurface(string _locationKey)
    {
        return _locationKey == "Location6" || _locationKey == "Location7";
    }

    private static void CopyRectTransform(RectTransform _source, RectTransform _target)
    {
        if (_target == null)
        {
            return;
        }

        if (_source == null)
        {
            _target.anchorMin = new Vector2(0.5f, 0.5f);
            _target.anchorMax = new Vector2(0.5f, 0.5f);
            _target.pivot = new Vector2(0.5f, 0.5f);
            _target.anchoredPosition = Vector2.zero;
            _target.sizeDelta = new Vector2(160f, 160f);
            _target.localScale = Vector3.one;
            return;
        }

        _target.anchorMin = _source.anchorMin;
        _target.anchorMax = _source.anchorMax;
        _target.pivot = _source.pivot;
        _target.anchoredPosition = _source.anchoredPosition;
        _target.sizeDelta = _source.sizeDelta;
        _target.localRotation = _source.localRotation;
        _target.localScale = _source.localScale;
    }

    private static void CopyImage(Image _source, Image _target)
    {
        if (_target == null)
        {
            return;
        }

        if (_source == null)
        {
            _target.color = new Color(1f, 1f, 1f, 0.18f);
            _target.preserveAspect = true;
            return;
        }

        _target.sprite = _source.sprite;
        _target.color = _source.color;
        _target.type = _source.type;
        _target.preserveAspect = _source.preserveAspect;
        _target.material = _source.material;
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
#endif
