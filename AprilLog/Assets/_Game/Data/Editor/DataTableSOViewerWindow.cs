#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// JSON Import로 생성된 데이터 테이블 SO를 조회하기 위한 EditorWindow.
/// 원본 데이터는 JSON이고 SO는 Import 결과물이므로 기본적으로 읽기 전용으로 표시한다.
/// </summary>
public class DataTableSOViewerWindow : EditorWindow
{
    // 추가 : 홍정옥
    // 내용 : 프로젝트 전체 SO가 아니라 JSON Import 결과물인 데이터 테이블 SO 폴더만 조회한다.
    private const string DATA_SO_ROOT = "Assets/_Project/Data/SO";

    // 추가 : 홍정옥
    // 내용 : 실수로 SO를 직접 수정하지 않도록 기본값은 읽기 전용으로 고정한다.
    private bool allowDirectEdit = false;

    // 추가 : 홍정옥
    // 내용 : 데이터 담당자가 파일을 빠르게 찾을 수 있도록 타입명/파일명 검색을 지원한다.
    private string searchKeyword = string.Empty;

    private readonly Dictionary<string, List<ScriptableObject>> soDatabase = new Dictionary<string, List<ScriptableObject>>();
    private readonly Dictionary<ScriptableObject, string> soPathLookup = new Dictionary<ScriptableObject, string>();
    private List<string> typeNames = new List<string>();

    private int selectedTypeIndex = 0;
    private ScriptableObject selectedSO;
    private Vector2 leftScrollPos;
    private Vector2 rightScrollPos;

    [MenuItem("Tools/Data/SO 데이터 뷰어")]
    public static void ShowWindow()
    {
        GetWindow<DataTableSOViewerWindow>("Data SO Viewer");
    }

    private void OnEnable()
    {
        LoadDataTableSOs();
    }

    private void LoadDataTableSOs()
    {
        soDatabase.Clear();
        soPathLookup.Clear();
        selectedSO = null;

        if (!AssetDatabase.IsValidFolder(DATA_SO_ROOT))
        {
            typeNames = new List<string>();
            return;
        }

        // 추가 : 홍정옥
        // 내용 : Assets 전체가 아닌 Assets/_Project/Data/SO 하위의 ScriptableObject만 검색한다.
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { DATA_SO_ROOT });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid).Replace("\\", "/");
            ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);

            if (so == null)
            {
                continue;
            }

            Type soType = so.GetType();
            string typeName = soType.Name;

            if (!soDatabase.TryGetValue(typeName, out List<ScriptableObject> list))
            {
                list = new List<ScriptableObject>();
                soDatabase.Add(typeName, list);
            }

            list.Add(so);
            soPathLookup[so] = assetPath;
        }

        foreach (List<ScriptableObject> list in soDatabase.Values)
        {
            list.Sort((a, b) => string.Compare(GetAssetPath(a), GetAssetPath(b), StringComparison.OrdinalIgnoreCase));
        }

        typeNames = soDatabase.Keys.OrderBy(x => x).ToList();

        if (selectedTypeIndex >= typeNames.Count)
        {
            selectedTypeIndex = 0;
        }
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (!AssetDatabase.IsValidFolder(DATA_SO_ROOT))
        {
            EditorGUILayout.HelpBox(
                $"데이터 SO 루트 폴더를 찾을 수 없습니다.\n경로: {DATA_SO_ROOT}",
                MessageType.Warning
            );
            return;
        }

        if (typeNames.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "조회 가능한 데이터 테이블 SO가 없습니다.\nTools > Data > Import All (JSON -> SO)를 먼저 실행하세요.",
                MessageType.Info
            );
            return;
        }

        GUILayout.BeginHorizontal();
        DrawLeftPanel();
        GUILayout.Space(8);
        DrawRightPanel();
        GUILayout.EndHorizontal();
    }

    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            LoadDataTableSOs();
            GUI.FocusControl(null);
        }

        GUILayout.Space(8);
        GUILayout.Label("타입", GUILayout.Width(30));

        using (new EditorGUI.DisabledScope(typeNames.Count == 0))
        {
            int previousIndex = selectedTypeIndex;
            selectedTypeIndex = EditorGUILayout.Popup(
                selectedTypeIndex,
                typeNames.Count == 0 ? new[] { "없음" } : typeNames.ToArray(),
                EditorStyles.toolbarPopup,
                GUILayout.Width(220)
            );

            if (previousIndex != selectedTypeIndex)
            {
                selectedSO = null;
                GUI.FocusControl(null);
            }
        }

        GUILayout.Space(8);
        GUILayout.Label("검색", GUILayout.Width(30));
        searchKeyword = GUILayout.TextField(searchKeyword, EditorStyles.toolbarTextField, GUILayout.Width(180));

        GUILayout.FlexibleSpace();

        // 추가 : 홍정옥
        // 내용 : JSON이 원본이므로 SO 직접 수정은 기본 비활성화하고, 필요할 때만 명시적으로 허용한다.
        bool newAllowDirectEdit = GUILayout.Toggle(
            allowDirectEdit,
            "직접 수정 허용",
            EditorStyles.toolbarButton,
            GUILayout.Width(110)
        );

        if (newAllowDirectEdit != allowDirectEdit)
        {
            allowDirectEdit = newAllowDirectEdit;
            GUI.FocusControl(null);
        }

        GUILayout.EndHorizontal();
    }

    private void DrawLeftPanel()
    {
        GUILayout.BeginVertical(GUILayout.Width(300), GUILayout.ExpandHeight(true));

        string selectedType = typeNames[selectedTypeIndex];
        List<ScriptableObject> currentList = GetFilteredList(soDatabase[selectedType]);

        EditorGUILayout.LabelField($"{selectedType} ({currentList.Count}개)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(DATA_SO_ROOT, EditorStyles.miniLabel);

        leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos, EditorStyles.helpBox, GUILayout.ExpandHeight(true));

        foreach (ScriptableObject so in currentList)
        {
            string path = GetAssetPath(so);
            string folder = GetRelativeFolder(path);
            string label = string.IsNullOrEmpty(folder) ? so.name : $"{folder}/{so.name}";

            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = selectedSO == so ? new Color(0.55f, 0.85f, 1f) : Color.white;

            if (GUILayout.Button(label, EditorStyles.miniButton))
            {
                selectedSO = so;
                GUI.FocusControl(null);
            }

            GUI.backgroundColor = previousColor;
        }

        EditorGUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawRightPanel()
    {
        GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos, EditorStyles.helpBox, GUILayout.ExpandHeight(true));

        if (selectedSO == null)
        {
            EditorGUILayout.LabelField("데이터 SO 뷰어", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("좌측 목록에서 확인할 데이터 테이블 SO를 선택하세요.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();
            return;
        }

        string assetPath = GetAssetPath(selectedSO);

        EditorGUILayout.LabelField(selectedSO.name, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("타입", selectedSO.GetType().Name);
        EditorGUILayout.LabelField("경로", assetPath);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Project에서 선택", GUILayout.Width(120)))
        {
            Selection.activeObject = selectedSO;
            EditorGUIUtility.PingObject(selectedSO);
        }

        if (GUILayout.Button("폴더 열기", GUILayout.Width(90)))
        {
            OpenContainingFolder(assetPath);
        }

        GUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        if (!allowDirectEdit)
        {
            EditorGUILayout.HelpBox(
                "읽기 전용 모드입니다. SO는 JSON Import 결과물이므로 값 수정은 JSON에서 진행하는 것을 권장합니다.",
                MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "직접 수정 모드입니다. 이후 JSON Import를 다시 실행하면 이 창에서 수정한 값은 덮어씌워질 수 있습니다.",
                MessageType.Warning
            );
        }

        DrawSelectedSerializedObject();

        EditorGUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawSelectedSerializedObject()
    {
        if (selectedSO == null)
        {
            return;
        }

        try
        {
            SerializedObject serializedSO = new SerializedObject(selectedSO);
            serializedSO.Update();

            SerializedProperty property = serializedSO.GetIterator();
            bool enterChildren = true;

            using (new EditorGUI.DisabledScope(!allowDirectEdit))
            {
                while (property.NextVisible(enterChildren))
                {
                    if (property.name == "m_Script")
                    {
                        enterChildren = false;
                        continue;
                    }

                    EditorGUILayout.PropertyField(property, true);
                    enterChildren = false;
                }
            }

            if (allowDirectEdit)
            {
                serializedSO.ApplyModifiedProperties();
            }
        }
        catch (Exception e)
        {
            // 추가 : 홍정옥
            // 내용 : 스크립트 참조가 깨진 SO나 TypeTree 캐시 오류가 있을 때 창 전체가 멈추지 않도록 방어한다.
            EditorGUILayout.HelpBox(
                "선택한 SO를 표시하는 중 오류가 발생했습니다. 스크립트 참조가 깨졌거나 Unity TypeTree 캐시가 꼬였을 수 있습니다.\n" +
                e.Message,
                MessageType.Error
            );
        }
    }

    private List<ScriptableObject> GetFilteredList(List<ScriptableObject> source)
    {
        if (string.IsNullOrWhiteSpace(searchKeyword))
        {
            return source;
        }

        string keyword = searchKeyword.Trim().ToLowerInvariant();

        return source
            .Where(so =>
            {
                string path = GetAssetPath(so).ToLowerInvariant();
                string name = so.name.ToLowerInvariant();
                string type = so.GetType().Name.ToLowerInvariant();

                return path.Contains(keyword) || name.Contains(keyword) || type.Contains(keyword);
            })
            .ToList();
    }

    private string GetAssetPath(ScriptableObject so)
    {
        if (so == null)
        {
            return string.Empty;
        }

        return soPathLookup.TryGetValue(so, out string path)
            ? path
            : AssetDatabase.GetAssetPath(so).Replace("\\", "/");
    }

    private string GetRelativeFolder(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return string.Empty;
        }

        string normalizedRoot = DATA_SO_ROOT.Replace("\\", "/").TrimEnd('/');
        string normalizedPath = assetPath.Replace("\\", "/");
        string folder = Path.GetDirectoryName(normalizedPath)?.Replace("\\", "/") ?? string.Empty;

        if (!folder.StartsWith(normalizedRoot, StringComparison.Ordinal))
        {
            return folder;
        }

        return folder.Substring(normalizedRoot.Length).TrimStart('/');
    }

    private void OpenContainingFolder(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return;
        }

        string absolutePath = Path.GetFullPath(assetPath);
        string folder = Path.GetDirectoryName(absolutePath);

        if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
        {
            EditorUtility.RevealInFinder(folder);
        }
    }
}
#endif
