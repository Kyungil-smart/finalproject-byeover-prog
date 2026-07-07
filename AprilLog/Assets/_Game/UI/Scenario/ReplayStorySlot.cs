using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 작성자 : 홍정옥
// 기능 : 스토리 리플레이 슬롯 UI를 관리하는 클래스입니다.
// 각 슬롯은 챕터 제목, 에피소드 제목, 상태 텍스트를 표시하며, 클리어된 경우에만 보기 버튼이 활성화됩니다. 
// 잠긴 경우에는 잠김 표시가 나타납니다.

// 1차 수정자 : 조규민
// 수정 내용 : 다시보기 슬롯의 고정 문구와 데이터 표시 형식을 LocalizationManager ID 기반으로 갱신,
// LocalizationManager 초기화 전에는 숫자 ID 대신 기본 문구를 표시

public class ReplayStorySlot : MonoBehaviour
{
    private const int _viewButtonTextId = 13025;
    private const string _viewButtonFallbackKr = "다시보기";
    private const string _viewButtonFallbackEn = "Replay";

    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI textHeaderReplay;
    [SerializeField] private TextMeshProUGUI textEpisodeTitle;
    [SerializeField] private TextMeshProUGUI textState;

    [Header("버튼")]
    [SerializeField] private Button buttonView;
    [SerializeField] private TextMeshProUGUI _textView;

    [Header("배경")]
    [Tooltip("스토리별 배경/컷씬 이미지를 표시할 Image (보통 BG)")]
    [SerializeField] private Image backgroundImage;

    [Header("잠김 표시")]
    [SerializeField] private GameObject dim;
    [Tooltip("미해금 슬롯 딤 알파 (0~255)")]
    [SerializeField, Range(0, 255)] private int dimAlpha = 250;

    private UnityAction currentClickHandler;
    private ReplayStoryData _currentData;

    private void OnEnable()
    {
        SubscribeLocalization();
        UpdateLocalizedTexts();
    }

    private void OnDisable()
    {
        UnsubscribeLocalization();
    }

    public void SetData(ReplayStoryData data, Action<ReplayStoryData> onClick)
    {
        if (data == null)
        {
            Debug.LogWarning("[ReplayStorySlot] 표시할 데이터가 없습니다.", this);
            return;
        }

        _currentData = data;
        ResolveMissingReferences();
        ValidateReferences();
        EnsureDim();
        UpdateLocalizedTexts();

        bool isCleared = data.State == ReplayStoryState.Cleared;

        if (buttonView != null)
        {
            if (currentClickHandler != null)
                buttonView.onClick.RemoveListener(currentClickHandler);

            currentClickHandler = null;
            buttonView.interactable = isCleared;
            buttonView.gameObject.SetActive(isCleared); // 미해금은 다시보기 버튼을 숨긴다.

            if (isCleared && onClick != null)
            {
                currentClickHandler = () => onClick.Invoke(data);
                buttonView.onClick.AddListener(currentClickHandler);
            }
        }
        else
        {
            Debug.LogWarning("[ReplayStorySlot] Button_View 참조가 비어 있습니다.", this);
        }

        ApplyBackground(data.BackgroundResourcePath);
        ApplyDim(!isCleared);
    }

    // 데이터의 배경 경로로 Resources에서 스프라이트를 로드해 슬롯 배경에 넣는다. (없으면 기존 이미지 유지)
    private void ApplyBackground(string resourcePath)
    {
        if (backgroundImage == null)
        {
            Debug.LogWarning("[ReplayStorySlot] Background Image 참조가 없습니다. (BG 자동 탐색 실패)", this);
            return;
        }

        backgroundImage.raycastTarget = false; // 배경이 버튼 클릭을 가로채지 않게

        if (string.IsNullOrEmpty(resourcePath))
            return;

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            backgroundImage.sprite = sprite;
            backgroundImage.enabled = true;
        }
        else
        {
            Debug.LogWarning($"[ReplayStorySlot] 배경 로드 실패: Resources/{resourcePath}", this);
        }
    }

    private void ApplyDim(bool locked)
    {
        if (dim == null)
            return;

        dim.SetActive(locked);

        Image image = dim.GetComponent<Image>();
        if (image != null)
        {
            Color color = image.color;
            color.a = dimAlpha / 255f;
            image.color = color;
            image.raycastTarget = false; // 딤이 버튼 클릭을 가로채지 않게
        }
    }

    private void OnDestroy()
    {
        UnsubscribeLocalization();
        if (buttonView != null && currentClickHandler != null)
            buttonView.onClick.RemoveListener(currentClickHandler);
    }

    private void SubscribeLocalization()
    {
        if (LocalizationManager.Instance == null)
            return;

        LocalizationManager.Instance.OnLanguageChanged -= UpdateLocalizedTexts;
        LocalizationManager.Instance.OnLanguageChanged += UpdateLocalizedTexts;
    }

    private void UnsubscribeLocalization()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= UpdateLocalizedTexts;
    }

    private void UpdateLocalizedTexts()
    {
        SetText(_textView, GetLocalizedText(_viewButtonTextId, _viewButtonFallbackKr, _viewButtonFallbackEn));

        if (_currentData == null)
            return;

        // 제목/안내문구는 데이터(팝업에서 로컬라이즈)로 그대로 표시한다.
        SetText(textHeaderReplay, _currentData.ChapterTitle);
        SetText(textEpisodeTitle, _currentData.EpisodeTitle);
        SetText(textState, GetStateDisplayText(_currentData));
    }

    // 해금 상태별 안내문구. 해금 완료면 비우고, 미해금이면 해금 조건을 보여준다.
    private static string GetStateDisplayText(ReplayStoryData data)
    {
        if (data.State == ReplayStoryState.Cleared)
            return string.Empty;

        return data.UnlockConditionText ?? string.Empty;
    }

    private static string GetLocalizedText(int _id, string _fallbackKr, string _fallbackEn)
    {
        return GetLocalizedText(_id, _fallbackKr, _fallbackEn, null);
    }

    private static string GetLocalizedText(int _id, string _fallbackKr, string _fallbackEn, params object[] _args)
    {
        if (LocalizationManager.Instance == null)
            return GetSystemLanguageFallback(_fallbackKr, _fallbackEn);

        string _text = _args == null
            ? LocalizationManager.Instance.Get(_id, LocalizingType.UI)
            : LocalizationManager.Instance.Get(_id, LocalizingType.UI, _args);
        return IsMissingLocalizationText(_text, _id)
            ? GetSystemLanguageFallback(_fallbackKr, _fallbackEn)
            : _text;
    }

    private static bool IsMissingLocalizationText(string _text, int _id)
    {
        return string.IsNullOrWhiteSpace(_text) || _text == $"[{_id}]";
    }

    private static string GetSystemLanguageFallback(string _fallbackKr, string _fallbackEn)
    {
        return Application.systemLanguage == SystemLanguage.Korean ? _fallbackKr : _fallbackEn;
    }

    private void EnsureDim()
    {
        if (dim != null)
            return;

        Transform dimTransform = transform.Find("Dim");
        if (dimTransform != null)
        {
            dim = dimTransform.gameObject;
            return;
        }

        GameObject dimObject = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dimObject.transform.SetParent(transform, false);

        RectTransform rectTransform = dimObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = dimObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, dimAlpha / 255f);
        image.raycastTarget = false;

        Transform bg = transform.Find("BG");
        if (bg != null)
            dimObject.transform.SetSiblingIndex(bg.GetSiblingIndex() + 1);
        else
            dimObject.transform.SetAsFirstSibling();

        dim = dimObject;
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text == null)
            return;

        text.text = value ?? string.Empty;
    }

    private void ValidateReferences()
    {
        if (textHeaderReplay == null)
            Debug.LogWarning("[ReplayStorySlot] Text_HeaderRePlay 참조가 비어 있습니다.", this);

        if (textEpisodeTitle == null)
            Debug.LogWarning("[ReplayStorySlot] Text_EpisodeTitle 참조가 비어 있습니다.", this);

        if (textState == null)
            Debug.LogWarning("[ReplayStorySlot] Text_State 참조가 비어 있습니다.", this);
    }

    private void ResolveMissingReferences()
    {
        if (textHeaderReplay == null)
            textHeaderReplay = FindChildComponentByName<TextMeshProUGUI>("Text_HeaderRePlay");

        if (textEpisodeTitle == null)
            textEpisodeTitle = FindChildComponentByName<TextMeshProUGUI>("Text_EpisodeTitle");

        if (textState == null)
            textState = FindChildComponentByName<TextMeshProUGUI>("Text_State")
                        ?? FindChildComponentByName<TextMeshProUGUI>("Text_state");

        if (buttonView == null)
            buttonView = FindChildComponentByName<Button>("Button_View");

        if (_textView == null && buttonView != null)
            _textView = FindChildComponentByName<TextMeshProUGUI>("Text_View");

        if (backgroundImage == null)
            backgroundImage = FindChildComponentByName<Image>("BG");

        if (dim == null)
        {
            Transform dimTransform = FindChildByName(transform, "Dim");
            if (dimTransform != null)
                dim = dimTransform.gameObject;
        }
    }

    private T FindChildComponentByName<T>(string objectName) where T : Component
    {
        Transform child = FindChildByName(transform, objectName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static Transform FindChildByName(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
