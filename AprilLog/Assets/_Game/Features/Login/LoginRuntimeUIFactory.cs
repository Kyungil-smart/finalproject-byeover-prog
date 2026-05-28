// 담당자 : 조규민
// 구현원리 : Boot 씬에 LoginView가 없을 때 UGUI와 TextMeshPro 기반의 최소 로그인 화면을 런타임에 생성한다.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 게스트 로그인 테스트가 가능한 최소 Login UI를 생성한다.
public static class LoginRuntimeUIFactory
{
    private const float REFERENCE_WIDTH = 1080f;
    private const float REFERENCE_HEIGHT = 2340f;

    // 씬에 LoginView가 없으면 모바일 세로형 기준의 런타임 UI를 만든다.
    public static LoginView EnsureExists()
    {
        var existingView = Object.FindFirstObjectByType<LoginView>(FindObjectsInactive.Include);
        if (existingView != null)
        {
            existingView.gameObject.SetActive(true);
            return existingView;
        }

        var canvasObject = new GameObject("LoginCanvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(REFERENCE_WIDTH, REFERENCE_HEIGHT);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        var safeArea = CreateRect("SafeAreaPanel", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.one);
        safeArea.gameObject.AddComponent<SafeAreaFitter>();

        var background = CreateImage("Background", safeArea, new Color(0.08f, 0.11f, 0.16f, 1f));
        Stretch(background.rectTransform);

        var title = CreateText("TitleText", safeArea, "에이프릴 로그", 72, TextAlignmentOptions.Center, new Color(0.94f, 0.91f, 0.82f, 1f));
        SetAnchoredBox(title.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(760f, 160f));

        var subtitle = CreateText("SubtitleText", safeArea, "구글 로그인 후 회원가입을 완료하세요", 34, TextAlignmentOptions.Center, new Color(0.72f, 0.77f, 0.86f, 1f));
        SetAnchoredBox(subtitle.rectTransform, new Vector2(0.5f, 0.64f), new Vector2(820f, 90f));

        var termsToggle = CreateToggle(safeArea);
        SetAnchoredBox(termsToggle.GetComponent<RectTransform>(), new Vector2(0.5f, 0.43f), new Vector2(760f, 92f));

        var termsButton = CreateButton("TermsButton", safeArea, "약관 보기", new Color(0.20f, 0.24f, 0.31f, 1f), 30);
        SetAnchoredBox(termsButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.36f), new Vector2(760f, 92f));

        var googleButton = CreateButton("GoogleLoginButton", safeArea, "구글 로그인", new Color(0.26f, 0.43f, 0.88f, 1f), 38);
        SetAnchoredBox(googleButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.29f), new Vector2(760f, 108f));

        var guestButton = CreateButton("GuestLoginButton", safeArea, "게스트로 시작", new Color(0.78f, 0.62f, 0.28f, 1f), 34);
        SetAnchoredBox(guestButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.22f), new Vector2(760f, 96f));

        var loadingText = CreateText("LoadingText", safeArea, "로그인 중...", 30, TextAlignmentOptions.Center, Color.white);
        SetAnchoredBox(loadingText.rectTransform, new Vector2(0.5f, 0.16f), new Vector2(760f, 72f));
        loadingText.gameObject.SetActive(false);

        var versionText = CreateText("VersionText", safeArea, string.Empty, 24, TextAlignmentOptions.Left, new Color(0.60f, 0.65f, 0.72f, 1f));
        SetAnchoredBox(versionText.rectTransform, new Vector2(0.08f, 0.04f), new Vector2(260f, 64f));

        var uidText = CreateText("UidText", safeArea, string.Empty, 22, TextAlignmentOptions.Right, new Color(0.60f, 0.65f, 0.72f, 1f));
        SetAnchoredBox(uidText.rectTransform, new Vector2(0.68f, 0.04f), new Vector2(600f, 64f));

        var popupPanel = CreatePopup(safeArea, out TMP_Text popupMessageText, out Button popupCloseButton);
        var registerPanel = CreateRegisterPanel(safeArea, out TMP_InputField playerIdInputField,
            out TMP_InputField passwordInputField, out TMP_Text registerMessageText, out Button registerButton);

        var view = canvasObject.AddComponent<LoginView>();
        view.Configure(guestButton, googleButton, registerButton, termsButton, popupCloseButton, termsToggle,
            loadingText.gameObject, registerPanel, popupPanel, playerIdInputField, passwordInputField,
            popupMessageText, registerMessageText, versionText, uidText);

        return view;
    }

    // RectTransform GameObject를 생성한다.
    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 size)
    {
        var rectObject = new GameObject(name);
        rectObject.transform.SetParent(parent, false);
        var rect = rectObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        return rect;
    }

    // 전체 화면에 맞는 Image를 생성한다.
    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var rect = CreateRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    // TextMeshPro 텍스트를 생성한다.
    private static TMP_Text CreateText(string name, Transform parent, string text, float fontSize,
        TextAlignmentOptions alignment, Color color)
    {
        var rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero);
        var tmpText = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = fontSize;
        tmpText.alignment = alignment;
        tmpText.color = color;
        tmpText.textWrappingMode = TextWrappingModes.Normal;
        return tmpText;
    }

    // 모바일 터치 영역을 확보한 버튼을 생성한다.
    private static Button CreateButton(string name, Transform parent, string label, Color color, float fontSize)
    {
        var image = CreateImage(name, parent, color);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        var labelText = CreateText("Label", image.transform, label, fontSize, TextAlignmentOptions.Center, Color.white);
        Stretch(labelText.rectTransform);
        return button;
    }

    private static TMP_InputField CreateInputField(string name, Transform parent, string placeholder, bool isPassword)
    {
        var background = CreateImage(name, parent, new Color(0.94f, 0.95f, 0.97f, 1f));
        var input = background.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = background;
        input.contentType = isPassword ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;

        var text = CreateText("Text", background.transform, string.Empty, 32, TextAlignmentOptions.Left, new Color(0.08f, 0.10f, 0.13f, 1f));
        text.margin = new Vector4(32f, 0f, 32f, 0f);
        Stretch(text.rectTransform);

        var placeholderText = CreateText("Placeholder", background.transform, placeholder, 30, TextAlignmentOptions.Left, new Color(0.48f, 0.52f, 0.58f, 1f));
        placeholderText.margin = new Vector4(32f, 0f, 32f, 0f);
        Stretch(placeholderText.rectTransform);

        input.textComponent = text;
        input.placeholder = placeholderText;
        return input;
    }

    // 약관 동의 토글을 생성한다.
    private static Toggle CreateToggle(Transform parent)
    {
        var root = CreateRect("TermsToggle", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero);
        var toggle = root.gameObject.AddComponent<Toggle>();

        var box = CreateImage("CheckBox", root, new Color(0.95f, 0.95f, 0.95f, 1f));
        SetAnchoredBox(box.rectTransform, new Vector2(0.06f, 0.5f), new Vector2(56f, 56f));

        var check = CreateImage("Checkmark", box.transform, new Color(0.25f, 0.63f, 0.34f, 1f));
        Stretch(check.rectTransform);
        toggle.graphic = check;
        toggle.targetGraphic = box;

        var label = CreateText("Label", root, "서비스 이용 약관에 동의합니다", 30, TextAlignmentOptions.Left, Color.white);
        SetAnchoredBox(label.rectTransform, new Vector2(0.58f, 0.5f), new Vector2(640f, 72f));
        return toggle;
    }

    private static GameObject CreateRegisterPanel(Transform parent, out TMP_InputField playerIdInputField,
        out TMP_InputField passwordInputField, out TMP_Text messageText, out Button registerButton)
    {
        var panel = CreateImage("RegisterPanel", parent, new Color(0.10f, 0.13f, 0.18f, 0.96f));
        SetAnchoredBox(panel.rectTransform, new Vector2(0.5f, 0.49f), new Vector2(840f, 620f));

        var title = CreateText("RegisterTitleText", panel.transform, "회원가입", 42, TextAlignmentOptions.Center, Color.white);
        SetAnchoredBox(title.rectTransform, new Vector2(0.5f, 0.86f), new Vector2(720f, 72f));

        playerIdInputField = CreateInputField("PlayerIdInputField", panel.transform, "아이디", false);
        SetAnchoredBox(playerIdInputField.GetComponent<RectTransform>(), new Vector2(0.5f, 0.66f), new Vector2(700f, 92f));

        passwordInputField = CreateInputField("PasswordInputField", panel.transform, "비밀번호", true);
        SetAnchoredBox(passwordInputField.GetComponent<RectTransform>(), new Vector2(0.5f, 0.49f), new Vector2(700f, 92f));

        messageText = CreateText("RegisterMessageText", panel.transform, string.Empty, 26, TextAlignmentOptions.Center, new Color(0.95f, 0.80f, 0.42f, 1f));
        SetAnchoredBox(messageText.rectTransform, new Vector2(0.5f, 0.34f), new Vector2(700f, 72f));

        registerButton = CreateButton("RegisterButton", panel.transform, "회원가입", new Color(0.25f, 0.60f, 0.34f, 1f), 34);
        SetAnchoredBox(registerButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.18f), new Vector2(700f, 92f));

        panel.gameObject.SetActive(false);
        return panel.gameObject;
    }

    // 로그인 실패와 약관 안내용 팝업을 생성한다.
    private static GameObject CreatePopup(Transform parent, out TMP_Text messageText, out Button closeButton)
    {
        var dim = CreateImage("PopupPanel", parent, new Color(0f, 0f, 0f, 0.58f));
        Stretch(dim.rectTransform);

        var box = CreateImage("PopupBox", dim.transform, new Color(0.13f, 0.15f, 0.19f, 1f));
        SetAnchoredBox(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(800f, 420f));

        messageText = CreateText("PopupMessageText", box.transform, string.Empty, 30, TextAlignmentOptions.Center, Color.white);
        SetAnchoredBox(messageText.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(680f, 180f));

        closeButton = CreateButton("PopupCloseButton", box.transform, "확인", new Color(0.78f, 0.62f, 0.28f, 1f), 32);
        SetAnchoredBox(closeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.22f), new Vector2(560f, 92f));

        dim.gameObject.SetActive(false);
        return dim.gameObject;
    }

    // RectTransform을 부모 영역 전체로 늘린다.
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // 기준 해상도 좌표가 아니라 앵커 기준 박스 크기로 UI를 배치한다.
    private static void SetAnchoredBox(RectTransform rect, Vector2 anchor, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }
}
