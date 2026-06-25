//담당자: 조규민
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// JGM 하우징 페이지 프리팹을 공용 로비 UI 프리팹에 연결합니다.
/// </summary>
public static class LobbyHousingPrefabApplier
{
    private const string _lobbyScenePath = "Assets/Scenes/_Lobby.unity";
    private const string _housingPagePrefabPath = "Assets/_Game/Prefabs/UI/Lobby/Page/Housing/Page_Housing.prefab";
    private const string _pageRootName = "Page";
    private const string _housingPageName = "Page_Housing";
    private const string _oldHousingPageName = "Page_Housing_Old";

    [MenuItem("Tools/Housing/Apply JGM Housing To _Lobby Scene")]
    public static void ApplyJgmHousingToLobbyScene()
    {
        Scene _scene = EditorSceneManager.OpenScene(_lobbyScenePath, OpenSceneMode.Single);
        GameObject _lobbyUiRoot = FindSceneObject(_scene, "--- LobbyUI ---");

        if (_lobbyUiRoot == null)
        {
            Debug.LogError("[LobbyHousingPrefabApplier] _Lobby 씬에서 --- LobbyUI --- 오브젝트를 찾을 수 없습니다.");
            return;
        }

        ApplyHousingPage(_lobbyUiRoot);
        EditorSceneManager.MarkSceneDirty(_scene);
        EditorSceneManager.SaveScene(_scene);
        Debug.Log("[LobbyHousingPrefabApplier] _Lobby 씬에 JGM 하우징 페이지를 연결했습니다.");
    }

    private static void ApplyHousingPage(GameObject _lobbyPrefabRoot)
    {
        Transform _pageRoot = FindChildRecursive(_lobbyPrefabRoot.transform, _pageRootName);

        if (_pageRoot == null)
        {
            Debug.LogError($"[LobbyHousingPrefabApplier] {_pageRootName} 오브젝트를 찾을 수 없습니다.");
            return;
        }

        BackupOldHousingPages(_pageRoot);

        GameObject _housingPagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(_housingPagePrefabPath);

        if (_housingPagePrefab == null)
        {
            Debug.LogError($"[LobbyHousingPrefabApplier] 하우징 페이지 프리팹을 찾을 수 없습니다. 경로: {_housingPagePrefabPath}");
            return;
        }

        GameObject _housingPage = PrefabUtility.InstantiatePrefab(_housingPagePrefab, _pageRoot) as GameObject;

        if (_housingPage == null)
        {
            Debug.LogError("[LobbyHousingPrefabApplier] 하우징 페이지 프리팹 인스턴스 생성에 실패했습니다.");
            return;
        }

        _housingPage.name = _housingPageName;
        _housingPage.SetActive(false);
        StretchToParent(_housingPage.GetComponent<RectTransform>());
        ConnectLobbyPageController(_lobbyPrefabRoot, _housingPage);
        EditorUtility.SetDirty(_lobbyPrefabRoot);
    }

    private static void BackupOldHousingPages(Transform _pageRoot)
    {
        int _backupIndex = 0;

        for (int _index = _pageRoot.childCount - 1; _index >= 0; _index--)
        {
            Transform _child = _pageRoot.GetChild(_index);

            if (_child.name != _housingPageName)
            {
                continue;
            }

            _backupIndex++;
            _child.name = $"{_oldHousingPageName}_{_backupIndex}";
            _child.gameObject.SetActive(false);
        }
    }

    private static void ConnectLobbyPageController(GameObject _lobbyPrefabRoot, GameObject _housingPage)
    {
        LobbyPageController _pageController = _lobbyPrefabRoot.GetComponent<LobbyPageController>();

        if (_pageController == null)
        {
            Debug.LogError("[LobbyHousingPrefabApplier] LobbyPageController를 찾을 수 없습니다.");
            return;
        }

        SerializedObject _serializedController = new SerializedObject(_pageController);
        SerializedProperty _pages = _serializedController.FindProperty("pages");

        if (_pages == null)
        {
            Debug.LogError("[LobbyHousingPrefabApplier] LobbyPageController.pages를 찾을 수 없습니다.");
            return;
        }

        for (int _index = 0; _index < _pages.arraySize; _index++)
        {
            SerializedProperty _entry = _pages.GetArrayElementAtIndex(_index);
            SerializedProperty _pageType = _entry.FindPropertyRelative("pageType");

            if (_pageType.enumValueIndex != (int)LobbyPageType.Housing)
            {
                continue;
            }

            _entry.FindPropertyRelative("pageObject").objectReferenceValue = _housingPage;
            _serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(_pageController);
            return;
        }

        Debug.LogWarning("[LobbyHousingPrefabApplier] Housing 페이지 항목을 찾지 못했습니다. Inspector에서 직접 연결이 필요합니다.");
    }

    private static void StretchToParent(RectTransform _rectTransform)
    {
        if (_rectTransform == null)
        {
            return;
        }

        _rectTransform.anchorMin = Vector2.zero;
        _rectTransform.anchorMax = Vector2.one;
        _rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _rectTransform.anchoredPosition = Vector2.zero;
        _rectTransform.sizeDelta = Vector2.zero;
        _rectTransform.localScale = Vector3.one;
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

    private static GameObject FindSceneObject(Scene _scene, string _name)
    {
        GameObject[] _roots = _scene.GetRootGameObjects();

        for (int _index = 0; _index < _roots.Length; _index++)
        {
            Transform _found = FindChildRecursive(_roots[_index].transform, _name);

            if (_found != null)
            {
                return _found.gameObject;
            }
        }

        return null;
    }
}
#endif
