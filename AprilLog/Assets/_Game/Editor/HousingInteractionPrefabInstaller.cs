//담당자: 조규민

#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 가구 상호작용과 연출 연결을 구성
/// </summary>
public static class HousingInteractionPrefabInstaller
{
    private const string HousingPrefabPath = "Assets/_Game/Prefabs/UI/Lobby/Page/Housing/Page_Housing.prefab";
    private const string LobbyUiPrefabPath = "Assets/_Game/Prefabs/UI/Lobby/Page/--- LobbyUI ---.prefab";
    private const string SleepingSpriteSearchName = "sleeping_character";
    private const string DownCharacterName = "down_character";

    private struct DownCharacterSettings
    {
        public bool HasValue { get; }
        public Vector2 AnchorMin { get; }
        public Vector2 AnchorMax { get; }
        public Vector2 AnchoredPosition { get; }
        public Vector2 SizeDelta { get; }
        public Vector2 Pivot { get; }
        public Quaternion LocalRotation { get; }
        public Vector3 LocalScale { get; }
        public Sprite Sprite { get; }
        public bool PreserveAspect { get; }

        public DownCharacterSettings(RectTransform _rectTransform, Image _image)
        {
            HasValue = _rectTransform != null;
            AnchorMin = _rectTransform != null ? _rectTransform.anchorMin : new Vector2(0.5f, 0.5f);
            AnchorMax = _rectTransform != null ? _rectTransform.anchorMax : new Vector2(0.5f, 0.5f);
            AnchoredPosition = _rectTransform != null ? _rectTransform.anchoredPosition : Vector2.zero;
            SizeDelta = _rectTransform != null ? _rectTransform.sizeDelta : new Vector2(130f, 180f);
            Pivot = _rectTransform != null ? _rectTransform.pivot : new Vector2(0.5f, 0.5f);
            LocalRotation = _rectTransform != null ? _rectTransform.localRotation : Quaternion.identity;
            LocalScale = _rectTransform != null ? _rectTransform.localScale : Vector3.one;
            Sprite = _image != null ? _image.sprite : null;
            PreserveAspect = _image != null && _image.preserveAspect;
        }
    }

    [MenuItem("Tools/Housing/가구 상호작용 연결")]
    public static void Install()
    {
        DownCharacterSettings _downCharacterSettings = CaptureAndRemoveDownCharacterOverride();
        GameObject _prefabRoot = PrefabUtility.LoadPrefabContents(HousingPrefabPath);

        try
        {
            ConfigurePrefab(_prefabRoot, _downCharacterSettings);
            PrefabUtility.SaveAsPrefabAsset(_prefabRoot, HousingPrefabPath);
            Debug.Log("[HousingInteractionPrefabInstaller] 가구 상호작용 연결을 완료했습니다.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(_prefabRoot);
        }
    }

    private static void ConfigurePrefab(
        GameObject _prefabRoot,
        DownCharacterSettings _downCharacterSettings)
    {
        Transform _bed = FindChildRecursive(_prefabRoot.transform, "Location1_Bed");
        Transform _chair = FindChildRecursive(_prefabRoot.transform, "Location5_DecorationB");
        Transform _player = FindChildRecursive(_prefabRoot.transform, "Housing_Player");
        Transform _roomPanel = FindChildRecursive(_prefabRoot.transform, "Panel_HousingRoom");

        if (_bed == null || _chair == null || _player == null || _roomPanel == null)
        {
            throw new InvalidOperationException("하우징 침대, 의자, 플레이어 또는 방 패널을 찾지 못했습니다.");
        }

        HousingInteractionView _bedView =
            GetOrAddComponent<HousingInteractionView>(_bed.gameObject);
        RemoveReversedChairInteractionView(_chair);
        HousingInteractionView _chairView =
            GetOrCreateChairInteractionView(_chair);
        Image _bedImage = _bed.GetComponent<Image>();
        Image _chairImage = _chair.GetComponent<Image>();

        if (_bedImage != null)
        {
            _bedImage.raycastTarget = true;
        }

        if (_chairImage != null)
        {
            _chairImage.raycastTarget = false;
        }

        GameObject _sleepingCharacter = GetOrCreateSleepingCharacter(_bed);
        GameObject _downCharacter = GetOrCreateDownCharacter(_chair, _downCharacterSettings);
        HousingInteractionExitView _exitView = GetOrCreateExitView(_roomPanel);
        HousingPlayerMoveController _moveController =
            _prefabRoot.GetComponentInChildren<HousingPlayerMoveController>(true);
        HousingInteractionController _controller =
            GetOrAddComponent<HousingInteractionController>(_prefabRoot);

        ConfigureBedView(_bedView, _sleepingCharacter, _player.gameObject);
        ConfigureChairView(_chairView, _downCharacter, _player.gameObject);
        ConfigureController(_controller, _bedView, _chairView, _exitView, _moveController);
        _sleepingCharacter.SetActive(false);
        _downCharacter.SetActive(false);
        _exitView.gameObject.SetActive(false);
    }

    private static void RemoveReversedChairInteractionView(Transform _chair)
    {
        HousingInteractionView _existingView =
            _chair.GetComponent<HousingInteractionView>();

        if (_existingView != null)
        {
            UnityEngine.Object.DestroyImmediate(_existingView);
        }
    }

    private static HousingInteractionView GetOrCreateChairInteractionView(
        Transform _chair)
    {
        const string _touchAreaName = "TouchArea_Interaction";
        Transform _existing = FindDirectChild(_chair, _touchAreaName);
        GameObject _touchArea;

        if (_existing != null)
        {
            _touchArea = _existing.gameObject;
        }
        else
        {
            _touchArea = new GameObject(
                _touchAreaName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform _rectTransform = _touchArea.GetComponent<RectTransform>();
            _rectTransform.SetParent(_chair, false);
            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.one;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
            _rectTransform.localRotation = Quaternion.Inverse(_chair.localRotation);
            _rectTransform.SetAsLastSibling();
        }

        Image _image = _touchArea.GetComponent<Image>();
        _image.color = Color.clear;
        _image.raycastTarget = true;
        return GetOrAddComponent<HousingInteractionView>(_touchArea);
    }

    private static DownCharacterSettings CaptureAndRemoveDownCharacterOverride()
    {
        GameObject _lobbyUiRoot = PrefabUtility.LoadPrefabContents(LobbyUiPrefabPath);

        try
        {
            Transform _downCharacter = FindAddedOverrideRecursive(
                _lobbyUiRoot.transform,
                DownCharacterName);

            if (_downCharacter == null)
            {
                return default;
            }

            DownCharacterSettings _settings = new DownCharacterSettings(
                _downCharacter.GetComponent<RectTransform>(),
                _downCharacter.GetComponent<Image>());
            UnityEngine.Object.DestroyImmediate(_downCharacter.gameObject);
            PrefabUtility.SaveAsPrefabAsset(_lobbyUiRoot, LobbyUiPrefabPath);
            return _settings;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(_lobbyUiRoot);
        }
    }

    private static GameObject GetOrCreateSleepingCharacter(Transform _bed)
    {
        Transform _existing = FindDirectChild(_bed, SleepingSpriteSearchName);

        if (_existing != null)
        {
            return _existing.gameObject;
        }

        GameObject _sleepingCharacter = new GameObject(
            SleepingSpriteSearchName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        RectTransform _rectTransform = _sleepingCharacter.GetComponent<RectTransform>();
        _rectTransform.SetParent(_bed, false);
        _rectTransform.anchorMin = Vector2.zero;
        _rectTransform.anchorMax = Vector2.one;
        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;

        Image _image = _sleepingCharacter.GetComponent<Image>();
        _image.preserveAspect = true;
        _image.raycastTarget = false;
        _image.sprite = FindSleepingSprite();
        return _sleepingCharacter;
    }

    private static GameObject GetOrCreateDownCharacter(
        Transform _chair,
        DownCharacterSettings _settings)
    {
        Transform _existing = FindDirectChild(_chair, DownCharacterName);

        if (_existing != null)
        {
            return _existing.gameObject;
        }

        GameObject _downCharacter = new GameObject(
            DownCharacterName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        RectTransform _rectTransform = _downCharacter.GetComponent<RectTransform>();
        _rectTransform.SetParent(_chair, false);
        ApplyDownCharacterRectTransform(_rectTransform, _settings);

        Image _image = _downCharacter.GetComponent<Image>();
        _image.sprite = _settings.Sprite != null
            ? _settings.Sprite
            : FindSprite(DownCharacterName);
        _image.preserveAspect = _settings.HasValue && _settings.PreserveAspect;
        _image.raycastTarget = false;
        return _downCharacter;
    }

    private static void ApplyDownCharacterRectTransform(
        RectTransform _rectTransform,
        DownCharacterSettings _settings)
    {
        if (_settings.HasValue)
        {
            _rectTransform.anchorMin = _settings.AnchorMin;
            _rectTransform.anchorMax = _settings.AnchorMax;
            _rectTransform.anchoredPosition = _settings.AnchoredPosition;
            _rectTransform.sizeDelta = _settings.SizeDelta;
            _rectTransform.pivot = _settings.Pivot;
            _rectTransform.localRotation = _settings.LocalRotation;
            _rectTransform.localScale = _settings.LocalScale;
            return;
        }

        _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _rectTransform.anchoredPosition = Vector2.zero;
        _rectTransform.sizeDelta = new Vector2(130f, 180f);
        _rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private static HousingInteractionExitView GetOrCreateExitView(Transform _roomPanel)
    {
        const string _exitObjectName = "TouchArea_FurnitureInteractionExit";
        Transform _existing = FindDirectChild(_roomPanel, _exitObjectName);
        GameObject _exitObject;

        if (_existing != null)
        {
            _exitObject = _existing.gameObject;
        }
        else
        {
            _exitObject = new GameObject(
                _exitObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform _rectTransform = _exitObject.GetComponent<RectTransform>();
            _rectTransform.SetParent(_roomPanel, false);
            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.one;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
            _rectTransform.SetAsLastSibling();
        }

        Image _image = _exitObject.GetComponent<Image>();
        _image.color = Color.clear;
        _image.raycastTarget = true;
        return GetOrAddComponent<HousingInteractionExitView>(_exitObject);
    }

    private static void ConfigureBedView(
        HousingInteractionView _bedView,
        GameObject _sleepingCharacter,
        GameObject _player)
    {
        SerializedObject _serializedView = new SerializedObject(_bedView);
        _serializedView.FindProperty("_interactionId").stringValue = "bed_sleep";
        SetGameObjectArray(
            _serializedView.FindProperty("_objectsShownWhileActive"),
            _sleepingCharacter);
        SetGameObjectArray(
            _serializedView.FindProperty("_objectsHiddenWhileActive"),
            _player);
        _serializedView.FindProperty("_pausePlayerMovement").boolValue = true;
        _serializedView.FindProperty("_restorePlayerPositionOnExit").boolValue = true;
        _serializedView.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureChairView(
        HousingInteractionView _chairView,
        GameObject _downCharacter,
        GameObject _player)
    {
        SerializedObject _serializedView = new SerializedObject(_chairView);
        _serializedView.FindProperty("_interactionId").stringValue = "chair_sit";
        SetGameObjectArray(
            _serializedView.FindProperty("_objectsShownWhileActive"),
            _downCharacter);
        SetGameObjectArray(
            _serializedView.FindProperty("_objectsHiddenWhileActive"),
            _player);
        _serializedView.FindProperty("_pausePlayerMovement").boolValue = true;
        _serializedView.FindProperty("_restorePlayerPositionOnExit").boolValue = true;
        _serializedView.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureController(
        HousingInteractionController _controller,
        HousingInteractionView _bedView,
        HousingInteractionView _chairView,
        HousingInteractionExitView _exitView,
        HousingPlayerMoveController _moveController)
    {
        SerializedObject _serializedController = new SerializedObject(_controller);
        SerializedProperty _views = _serializedController.FindProperty("_interactionViews");
        _views.arraySize = 2;
        _views.GetArrayElementAtIndex(0).objectReferenceValue = _bedView;
        _views.GetArrayElementAtIndex(1).objectReferenceValue = _chairView;
        _serializedController.FindProperty("_exitView").objectReferenceValue = _exitView;
        _serializedController.FindProperty("_playerMoveController").objectReferenceValue = _moveController;
        _serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetGameObjectArray(SerializedProperty _property, GameObject _target)
    {
        _property.arraySize = 1;
        _property.GetArrayElementAtIndex(0).objectReferenceValue = _target;
    }

    private static Sprite FindSleepingSprite()
    {
        return FindSprite(SleepingSpriteSearchName);
    }

    private static Sprite FindSprite(string _spriteName)
    {
        string[] _assetGuids = AssetDatabase.FindAssets($"{_spriteName} t:Sprite");

        if (_assetGuids.Length == 0)
        {
            Debug.LogWarning(
                $"[HousingInteractionPrefabInstaller] {_spriteName} Sprite를 찾지 못했습니다. Inspector에서 연결해 주세요.");
            return null;
        }

        string _assetPath = AssetDatabase.GUIDToAssetPath(_assetGuids[0]);
        return AssetDatabase.LoadAssetAtPath<Sprite>(_assetPath);
    }

    private static Transform FindAddedOverrideRecursive(Transform _parent, string _name)
    {
        if (_parent.name == _name && PrefabUtility.IsAddedGameObjectOverride(_parent.gameObject))
        {
            return _parent;
        }

        for (int _index = 0; _index < _parent.childCount; _index++)
        {
            Transform _found = FindAddedOverrideRecursive(_parent.GetChild(_index), _name);

            if (_found != null)
            {
                return _found;
            }
        }

        return null;
    }

    private static T GetOrAddComponent<T>(GameObject _target) where T : Component
    {
        T _component = _target.GetComponent<T>();
        return _component != null ? _component : _target.AddComponent<T>();
    }

    private static Transform FindDirectChild(Transform _parent, string _name)
    {
        for (int _index = 0; _index < _parent.childCount; _index++)
        {
            Transform _child = _parent.GetChild(_index);

            if (_child.name == _name)
            {
                return _child;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform _parent, string _name)
    {
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
