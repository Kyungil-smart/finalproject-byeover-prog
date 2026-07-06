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
// 수정 내용 : 다시보기 슬롯의 고정 문구와 데이터 표시 형식을 LocalizationManager ID 기반으로 갱신

public class ReplayStorySlot : MonoBehaviour
{
    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI textHeaderReplay;
    [SerializeField] private TextMeshProUGUI textEpisodeTitle;
    [SerializeField] private TextMeshProUGUI textState;

    [Header("버튼")]
    [SerializeField] private Button buttonView;
    [SerializeField] private TextMeshProUGUI _textView;

    [Header("잠김 표시")]
    [SerializeField] private GameObject dim;

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

            buttonView.interactable = isCleared;
            currentClickHandler = null;

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

        if (dim != null)
            dim.SetActive(!isCleared);
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
        if (LocalizationManager.Instance == null)
            return;

        SetText(_textView, LocalizationManager.Instance.Get(13025, LocalizingType.UI));

        if (_currentData == null)
            return;

        string _header = LocalizationManager.Instance.Get(
            11015,
            LocalizingType.UI,
            ResolveChapterFormatValue(_currentData.ChapterTitle));
        string _episode = LocalizationManager.Instance.Get(11014, LocalizingType.UI)
            .Replace("{chaptername}", _currentData.EpisodeTitle ?? string.Empty);
        SetText(textHeaderReplay, _header);
        SetText(textEpisodeTitle, _episode);
        SetText(textState, LocalizationManager.Instance.Get(13026, LocalizingType.UI));
    }

    private static string ResolveChapterFormatValue(string _chapterTitle)
    {
        if (string.IsNullOrWhiteSpace(_chapterTitle))
            return string.Empty;

        int _separatorIndex = _chapterTitle.LastIndexOf('.');
        return _separatorIndex >= 0 && _separatorIndex < _chapterTitle.Length - 1
            ? _chapterTitle.Substring(_separatorIndex + 1).Trim()
            : _chapterTitle.Trim();
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
        image.color = new Color(0f, 0f, 0f, 100f / 255f);
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

    private static string GetStateText(ReplayStoryData data)
    {
        if (data.State == ReplayStoryState.Cleared)
            return "클리어 완료";

        if (string.IsNullOrWhiteSpace(data.UnlockConditionText))
            return "잠김 : 해금 조건 필요";

        return $"잠김 : {data.UnlockConditionText}";
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
            textState = FindChildComponentByName<TextMeshProUGUI>("Text_State");

        if (buttonView == null)
            buttonView = FindChildComponentByName<Button>("Button_View");

        if (_textView == null && buttonView != null)
            _textView = FindChildComponentByName<TextMeshProUGUI>("Text_View");

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
