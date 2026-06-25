//담당자: 조규민

using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 인게임 인챈트 링크 버튼과 확인 팝업 UI 배치를 씬에 적용한다.
/// </summary>
public static class InGameEnchantLinkButtonSceneInstaller
{
    private const string _targetScenePathArgument = "-targetScenePath";

    [MenuItem("Tools/InGame/Apply Enchant Link Button UI Layout To Open Scene")]
    public static void ApplyToOpenSceneFromMenu()
    {
        ApplyToOpenScene();
    }

    public static void ApplyToOpenScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("[InGameEnchantLinkButtonSceneInstaller] 적용할 활성 씬을 찾지 못했습니다.");
            return;
        }

        ApplyLayout(scene);
    }

    public static void ApplyToScenePathFromCommandLine()
    {
        string scenePath = GetCommandLineValue(_targetScenePathArgument);
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            Debug.LogError("[InGameEnchantLinkButtonSceneInstaller] -targetScenePath 값이 필요합니다.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        ApplyLayout(scene);
    }

    public static void ApplyConfirmPopupOnlyToScenePathFromCommandLine()
    {
        string scenePath = GetCommandLineValue(_targetScenePathArgument);
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            Debug.LogError("[InGameEnchantLinkButtonSceneInstaller] -targetScenePath 값이 필요합니다.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject confirmPopupObject = ConfigureConfirmPopup(scene);
        if (confirmPopupObject != null)
        {
            ConfigureConfirmPopupView(confirmPopupObject);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[InGameEnchantLinkButtonSceneInstaller] {scene.path} 확인 팝업 UI 배치를 적용했습니다.");
    }

    private static void ApplyLayout(Scene scene)
    {
        GameObject linkButtonBoundary = FindSceneObject(scene, "LinkButtonBoundary");
        if (linkButtonBoundary == null)
        {
            Debug.LogError("[InGameEnchantLinkButtonSceneInstaller] LinkButtonBoundary를 찾지 못했습니다.");
            return;
        }

        RemoveRuntimeComponents(linkButtonBoundary);
        ConfigureLinkButtonBoundary(linkButtonBoundary);
        GameObject confirmPopupObject = ConfigureConfirmPopup(scene);
        ConfigureRuntimeComponents(scene, linkButtonBoundary, confirmPopupObject);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[InGameEnchantLinkButtonSceneInstaller] {scene.path} 인챈트 링크 버튼 UI 배치를 적용했습니다.");
    }

    private static void ConfigureLinkButtonBoundary(GameObject boundaryObject)
    {
        RectTransform boundaryRect = boundaryObject.GetComponent<RectTransform>();
        SetCenterRect(boundaryRect, new Vector2(0f, -575f), new Vector2(1250f, 350f));

        GameObject continueButtonSet = RenameChild(boundaryObject, "ToOptionButtonSet", "ContinueButtonSet");
        GameObject returnLobbyButtonSet = RenameChild(boundaryObject, "ToPlayButtonSet", "ReturnLobbyButtonSet");
        GameObject restartChapterButtonSet = RenameChild(boundaryObject, "ToLobbyButtonSet", "RestartChapterButtonSet");

        ConfigureLinkButtonSet(continueButtonSet, "이어하기", new Vector2(-225f, 150f));
        ConfigureLinkButtonSet(returnLobbyButtonSet, "로비로 복귀", new Vector2(0f, 150f));
        ConfigureLinkButtonSet(restartChapterButtonSet, "포기하기", new Vector2(225f, 150f));
    }

    private static void ConfigureLinkButtonSet(GameObject buttonSetObject, string label, Vector2 position)
    {
        if (buttonSetObject == null)
        {
            return;
        }

        RectTransform rectTransform = buttonSetObject.GetComponent<RectTransform>();
        SetBottomCenterRect(rectTransform, position, new Vector2(175f, 300f));

        TMP_Text text = buttonSetObject.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = label;
            text.enableAutoSizing = true;
            text.fontSizeMin = 24f;
            text.fontSizeMax = 52f;
            text.alignment = TextAlignmentOptions.Center;
        }

        ClearPersistentOnClicks(buttonSetObject);
    }

    private static GameObject ConfigureConfirmPopup(Scene scene)
    {
        GameObject popupObject = FindSceneObject(scene, "ToLobbyWarningCanvas");
        if (popupObject == null)
        {
            popupObject = FindSceneObject(scene, "InGameConfirmPopupCanvas");
        }

        if (popupObject == null)
        {
            Debug.LogWarning("[InGameEnchantLinkButtonSceneInstaller] 확인 팝업 오브젝트를 찾지 못해 팝업 배치는 건너뜁니다.");
            return null;
        }

        popupObject.name = "InGameConfirmPopupCanvas";

        RectTransform popupRect = popupObject.GetComponent<RectTransform>();
        StretchRect(popupRect);

        Canvas canvas = popupObject.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 200;
        }

        GameObject dimBackground = FindOrCreateChild(popupObject.transform, "DimBackground", true);
        StretchRect(dimBackground.GetComponent<RectTransform>());
        Image dimImage = EnsureComponent<Image>(dimBackground);
        dimImage.color = new Color(0f, 0f, 0f, 0.75f);
        dimImage.raycastTarget = true;

        GameObject viewBoundary = FindOrCreateChild(popupObject.transform, "ViewBoundary", true);
        SetCenterRect(viewBoundary.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1060f, 760f));

        Image boundaryImage = EnsureComponent<Image>(viewBoundary);
        boundaryImage.color = new Color(1f, 1f, 1f, 0f);
        boundaryImage.raycastTarget = true;

        ConfigurePopupTitle(viewBoundary.transform);
        ConfigurePopupMessage(viewBoundary.transform);
        ConfigurePopupButtons(viewBoundary.transform);

        popupObject.SetActive(false);
        return popupObject;
    }

    private static void ConfigureRuntimeComponents(Scene scene, GameObject linkButtonBoundary, GameObject confirmPopupObject)
    {
        if (confirmPopupObject == null)
        {
            Debug.LogWarning("[InGameEnchantLinkButtonSceneInstaller] 확인 팝업이 없어 기능 연결을 건너뜁니다.");
            return;
        }

        ScreenNavigator screenNavigator = UnityEngine.Object.FindFirstObjectByType<ScreenNavigator>(FindObjectsInactive.Include);
        InGameConfirmPopupView confirmPopupView = ConfigureConfirmPopupView(confirmPopupObject);
        EnchantLinkButtonBoundaryView boundaryView = ConfigureBoundaryView(linkButtonBoundary);

        SerializedObject boundarySerializedObject = new SerializedObject(boundaryView);
        SetObject(boundarySerializedObject, "_continueButtonSet", FindChildByName(linkButtonBoundary.transform, "ContinueButtonSet")?.gameObject);
        SetObject(boundarySerializedObject, "_continueButton", FindButtonInChild(linkButtonBoundary, "ContinueButtonSet"));
        SetObject(boundarySerializedObject, "_returnLobbyButtonSet", FindChildByName(linkButtonBoundary.transform, "ReturnLobbyButtonSet")?.gameObject);
        SetObject(boundarySerializedObject, "_returnLobbyButton", FindButtonInChild(linkButtonBoundary, "ReturnLobbyButtonSet"));
        SetObject(boundarySerializedObject, "_restartChapterButtonSet", FindChildByName(linkButtonBoundary.transform, "RestartChapterButtonSet")?.gameObject);
        SetObject(boundarySerializedObject, "_restartChapterButton", FindButtonInChild(linkButtonBoundary, "RestartChapterButtonSet"));
        SetObject(boundarySerializedObject, "_screenNavigator", screenNavigator);
        SetObject(boundarySerializedObject, "_confirmPopupView", confirmPopupView);
        boundarySerializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static InGameConfirmPopupView ConfigureConfirmPopupView(GameObject popupObject)
    {
        InGameConfirmPopupView popupView = popupObject.GetComponent<InGameConfirmPopupView>();
        if (popupView == null)
        {
            popupView = popupObject.AddComponent<InGameConfirmPopupView>();
        }

        SerializedObject popupSerializedObject = new SerializedObject(popupView);
        SetObject(popupSerializedObject, "_rootObject", popupObject);
        SetObject(popupSerializedObject, "_messageText", FindTextByName(popupObject.transform, "DescriptionText (TMP)"));
        SetObject(popupSerializedObject, "_yesButton", FindButtonInChild(popupObject, "YesButton"));
        SetObject(popupSerializedObject, "_noButton", FindButtonInChild(popupObject, "NoButton"));
        SetObject(popupSerializedObject, "_closeButton", FindButtonInChild(popupObject, "CloseButton"));
        popupSerializedObject.ApplyModifiedPropertiesWithoutUndo();

        return popupView;
    }

    private static EnchantLinkButtonBoundaryView ConfigureBoundaryView(GameObject boundaryObject)
    {
        EnchantLinkButtonBoundaryView boundaryView = boundaryObject.GetComponent<EnchantLinkButtonBoundaryView>();
        if (boundaryView == null)
        {
            boundaryView = boundaryObject.AddComponent<EnchantLinkButtonBoundaryView>();
        }

        return boundaryView;
    }

    private static void ConfigurePopupTitle(Transform popupRoot)
    {
        TMP_Text titleText = FindTextByName(popupRoot, "HeaderText (TMP)");
        if (titleText == null)
        {
            titleText = FindTextByValue(popupRoot, "안내");
        }

        if (titleText == null)
        {
            GameObject titleObject = CreateTextObject(popupRoot, "HeaderText (TMP)");
            titleText = titleObject.GetComponent<TMP_Text>();
        }

        titleText.text = "안내";
        titleText.fontSize = 75f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.black;
        SetTopCenterRect(titleText.rectTransform, new Vector2(0f, -95f), new Vector2(500f, 110f));

        GameObject closeButton = FindChildByAnyName(popupRoot, "CloseButton", "CancleButton");
        if (closeButton == null)
        {
            closeButton = CreateButtonObject(popupRoot, "CloseButton", "X");
        }

        closeButton.name = "CloseButton";
        SetTopRightRect(closeButton.GetComponent<RectTransform>(), new Vector2(-80f, -80f), new Vector2(96f, 96f));
        SetButtonText(closeButton, "X", 48f);
        ClearPersistentOnClick(closeButton.GetComponent<Button>());
    }

    private static void ConfigurePopupMessage(Transform popupRoot)
    {
        GameObject messageArea = FindChildByAnyName(popupRoot, "MessageBox", "WarningArea");
        if (messageArea == null)
        {
            messageArea = FindOrCreateChild(popupRoot, "MessageBox", true);
        }

        messageArea.name = "MessageBox";
        SetCenterRect(messageArea.GetComponent<RectTransform>(), new Vector2(0f, -25f), new Vector2(890f, 330f));

        Image messageImage = EnsureComponent<Image>(messageArea);
        messageImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        messageImage.raycastTarget = true;

        TMP_Text messageText = FindTextByName(messageArea.transform, "DescriptionText (TMP)");
        if (messageText == null)
        {
            messageText = messageArea.GetComponentInChildren<TMP_Text>(true);
        }

        if (messageText == null)
        {
            GameObject messageObject = CreateTextObject(messageArea.transform, "DescriptionText (TMP)");
            messageText = messageObject.GetComponent<TMP_Text>();
        }

        messageText.text = "로비로 돌아가시겠습니까?\n게임은 자동 저장됩니다.";
        messageText.fontSize = 50f;
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.color = Color.black;
        SetStretchRect(messageText.rectTransform, new Vector2(55f, 45f), new Vector2(-55f, -45f));
    }

    private static void ConfigurePopupButtons(Transform popupRoot)
    {
        GameObject buttonArea = FindChildByAnyName(popupRoot, "ButtonArea", "DicideArea", "DecideArea", "Buttons");
        if (buttonArea == null)
        {
            buttonArea = FindOrCreateChild(popupRoot, "ButtonArea", true);
        }

        buttonArea.name = "ButtonArea";
        SetBottomCenterRect(buttonArea.GetComponent<RectTransform>(), new Vector2(0f, 115f), new Vector2(820f, 170f));

        GameObject yesButton = FindChildByAnyName(buttonArea.transform, "YesButton");
        if (yesButton == null)
        {
            yesButton = CreateButtonObject(buttonArea.transform, "YesButton", "예");
        }

        GameObject noButton = FindChildByAnyName(buttonArea.transform, "NoButton");
        if (noButton == null)
        {
            noButton = CreateButtonObject(buttonArea.transform, "NoButton", "아니요");
        }

        ConfigurePopupButton(yesButton, "예", new Vector2(-215f, 0f));
        ConfigurePopupButton(noButton, "아니요", new Vector2(215f, 0f));
    }

    private static void ConfigurePopupButton(GameObject buttonObject, string label, Vector2 position)
    {
        SetCenterRect(buttonObject.GetComponent<RectTransform>(), position, new Vector2(350f, 140f));

        Image image = EnsureComponent<Image>(buttonObject);
        image.color = new Color(0.82f, 0.82f, 0.82f, 1f);
        image.raycastTarget = true;

        Button button = EnsureComponent<Button>(buttonObject);
        button.targetGraphic = image;
        ClearPersistentOnClick(button);
        SetButtonText(buttonObject, label, 48f);
    }

    private static GameObject RenameChild(GameObject parentObject, string oldName, string newName)
    {
        Transform child = FindChildByName(parentObject.transform, oldName);
        if (child == null)
        {
            child = FindChildByName(parentObject.transform, newName);
        }

        if (child == null)
        {
            Debug.LogWarning($"[InGameEnchantLinkButtonSceneInstaller] {oldName} 버튼 세트를 찾지 못했습니다.");
            return null;
        }

        child.name = newName;
        return child.gameObject;
    }

    private static void RemoveRuntimeComponents(GameObject targetObject)
    {
        MonoBehaviour[] behaviours = targetObject.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;
            if (typeName != "EnchantLinkButtonBoundaryView" && typeName != "InGameConfirmPopupView")
            {
                continue;
            }

            UnityEngine.Object.DestroyImmediate(behaviour, true);
        }
    }

    private static Button FindButtonInChild(GameObject rootObject, string objectName)
    {
        Transform child = FindChildByName(rootObject.transform, objectName);
        if (child == null)
        {
            return null;
        }

        return child.GetComponentInChildren<Button>(true);
    }

    private static void SetButtonText(GameObject buttonObject, string label, float fontSize)
    {
        TMP_Text text = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (text == null)
        {
            GameObject textObject = CreateTextObject(buttonObject.transform, "Text (TMP)");
            text = textObject.GetComponent<TMP_Text>();
            SetStretchRect(text.rectTransform, Vector2.zero, Vector2.zero);
        }

        text.text = label;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = 24f;
        text.fontSizeMax = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
    }

    private static GameObject FindOrCreateChild(Transform parent, string objectName, bool addImage)
    {
        Transform child = FindChildByName(parent, objectName);
        if (child != null)
        {
            return child.gameObject;
        }

        GameObject childObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        childObject.transform.SetParent(parent, false);
        if (addImage)
        {
            childObject.AddComponent<Image>();
        }

        return childObject;
    }

    private static GameObject CreateButtonObject(Transform parent, string objectName, string label)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        CreateTextObject(buttonObject.transform, "Text (TMP)");
        SetButtonText(buttonObject, label, 48f);
        return buttonObject;
    }

    private static GameObject CreateTextObject(Transform parent, string objectName)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        return textObject;
    }

    private static TMP_Text FindTextByName(Transform root, string objectName)
    {
        Transform child = FindChildByName(root, objectName);
        if (child == null)
        {
            return null;
        }

        return child.GetComponent<TMP_Text>();
    }

    private static TMP_Text FindTextByValue(Transform root, string value)
    {
        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text.text.Trim() == value)
            {
                return text;
            }
        }

        return null;
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            Transform result = FindChildByName(rootObject.transform, objectName);
            if (result != null)
            {
                return result.gameObject;
            }
        }

        return null;
    }

    private static GameObject FindChildByAnyName(Transform root, params string[] names)
    {
        foreach (string objectName in names)
        {
            Transform result = FindChildByName(root, objectName);
            if (result != null)
            {
                return result.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildByName(Transform root, string objectName)
    {
        if (root.name == objectName)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            Transform result = FindChildByName(child, objectName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static T EnsureComponent<T>(GameObject targetObject) where T : Component
    {
        T component = targetObject.GetComponent<T>();
        if (component != null)
        {
            return component;
        }

        return targetObject.AddComponent<T>();
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        SetStretchRect(rectTransform, Vector2.zero, Vector2.zero);
    }

    private static void SetStretchRect(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one;
    }

    private static void SetCenterRect(RectTransform rectTransform, Vector2 position, Vector2 size)
    {
        SetFixedRect(rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
    }

    private static void SetBottomCenterRect(RectTransform rectTransform, Vector2 position, Vector2 size)
    {
        SetFixedRect(rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, size);
    }

    private static void SetTopCenterRect(RectTransform rectTransform, Vector2 position, Vector2 size)
    {
        SetFixedRect(rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, size);
    }

    private static void SetTopRightRect(RectTransform rectTransform, Vector2 position, Vector2 size)
    {
        SetFixedRect(rectTransform, Vector2.one, Vector2.one, position, size);
    }

    private static void SetFixedRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one;
    }

    private static void ClearPersistentOnClick(Button button)
    {
        if (button == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(button);
        serializedObject.Update();
        SerializedProperty calls = serializedObject.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        if (calls == null)
        {
            return;
        }

        calls.ClearArray();
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(button);
    }

    private static void ClearPersistentOnClicks(GameObject rootObject)
    {
        if (rootObject == null)
        {
            return;
        }

        Button[] buttons = rootObject.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            ClearPersistentOnClick(button);
        }
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"[InGameEnchantLinkButtonSceneInstaller] {propertyName} 필드를 찾지 못했습니다.");
            return;
        }

        property.objectReferenceValue = value;
    }

    private static string GetCommandLineValue(string key)
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i < arguments.Length - 1; i++)
        {
            if (arguments[i] == key)
            {
                return arguments[i + 1];
            }
        }

        return string.Empty;
    }
}
