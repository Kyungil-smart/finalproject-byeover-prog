using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 1차 수정자 : 조규민
// 수정 내용 : 하우징 책장 다시보기에서 ChapterTestDataSO 기준 챕터 목록을 표시하고 선택 정보를 _Story 씬으로 전달,
// 다시보기 제목을 언어 변경 시 LocalizationManager 기준으로 갱신,
// LocalizationManager 초기화 전에는 기본 문구를 표시하고 리스트 생성 후 ScrollRect 레이아웃을 갱신

public class ReplayStoryPopup : MonoBehaviour
{
    private const int _headerTextId = 13024;
    private const string _headerFallbackKr = "서사 보관소";
    private const string _headerFallbackEn = "Story Archive";

    [Header("상단")]
    [SerializeField] private Button buttonCloseReplayStory;
    [SerializeField] private TextMeshProUGUI textHeaderReplayStory;

    [Header("목록")]
    [SerializeField] private Transform content;
    [SerializeField] private ReplayStorySlot slotPrefab;

    [Header("시나리오 재생")]
    [SerializeField] private string storySceneName = "_Story";
    // 추가:조규민 기능 설명: 다시보기 연출 종료 후 복귀할 씬 이름을 지정한다.
    [SerializeField] private string returnSceneName = "_Lobby";

    [Header("챕터 테스트 데이터")]
    // 추가:조규민 기능 설명: 하우징 책장 다시보기 목록을 ChapterTestData.asset 기준으로 생성한다.
    [SerializeField] private ChapterTestDataSO chapterTestData;

    [Header("스토리 트리거 데이터")]
    [Tooltip("스토리 목록/해금 조건 소스. 비우면 로드된 StoryTriggerTable을 자동 탐색한다")]
    [SerializeField] private StoryTriggerTable storyTriggerTable;

    [Header("인트로(튜토리얼) 슬롯")]
    [Tooltip("항상 해금 상태로 목록 맨 앞에 넣는 인트로 스토리 그룹 ID")]
    [SerializeField] private int introStoryId = 3001;
    [SerializeField] private string introChapterTitleKr = "프롤로그";
    [SerializeField] private string introChapterTitleEn = "Prologue";
    [SerializeField] private string introEpisodeTitleKr = "마법의 책장";
    [SerializeField] private string introEpisodeTitleEn = "The Magic Bookshelf";
    [Tooltip("인트로 슬롯 배경 리소스 경로. 비우면 대사 데이터에서 자동 탐색")]
    [SerializeField] private string introBackgroundResourcePath = "Story/Cutscenes/20008";

    // 챕터 제목 Localization 규칙: 헤더 = 15000 + (챕터-1)*6, 에피소드 = 헤더 + 스테이지
    private const int _chapterTitleBaseId = 15000;
    private const int _chapterTitleStride = 6;

    private readonly List<ReplayStorySlot> spawnedSlots = new();
    private Button boundCloseButton;
    private bool useChapterTestDataForCurrentOpen;
    private bool _returnToHousingPageAfterStory;
    private ScrollRect _replayScrollRect;
    private RectTransform _contentRect;

    private void Awake()
    {
        ResolveMissingReferences();
        BindCloseButton();
    }

    private void OnEnable()
    {
        SubscribeLocalization();
        ResolveMissingReferences();
        BindCloseButton();
        UpdateLocalizedTexts();
        RefreshList();
    }

    private void OnDisable()
    {
        UnsubscribeLocalization();
    }

    private void OnDestroy()
    {
        UnsubscribeLocalization();
        if (boundCloseButton != null)
            boundCloseButton.onClick.RemoveListener(Close);
    }

    public void Open()
    {
        // 추가:조규민 기능 설명: 기존 로비 옵션 다시보기는 기존 임시 다시보기 목록을 유지한다.
        useChapterTestDataForCurrentOpen = false;
        _returnToHousingPageAfterStory = false;
        OpenInternal();
    }

    public void OpenForHousingBookcase()
    {
        // 추가:조규민 기능 설명: 하우징 책장 진입 시에만 ChapterTestData.asset 기반 4개 챕터 목록을 사용한다.
        useChapterTestDataForCurrentOpen = true;
        _returnToHousingPageAfterStory = true;
        OpenInternal();
    }

    private void OpenInternal()
    {
        if (gameObject.activeSelf)
        {
            RefreshList();
            return;
        }

        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void BindCloseButton()
    {
        if (boundCloseButton == buttonCloseReplayStory)
            return;

        if (boundCloseButton != null)
            boundCloseButton.onClick.RemoveListener(Close);

        if (buttonCloseReplayStory != null)
        {
            buttonCloseReplayStory.onClick.RemoveListener(Close);
            buttonCloseReplayStory.onClick.AddListener(Close);
        }
        else
            Debug.LogWarning("[ReplayStoryPopup] Button_CloseReplayStory 참조가 비어 있습니다.", this);

        boundCloseButton = buttonCloseReplayStory;
    }

    private void RefreshList()
    {
        ResolveMissingReferences();

        if (!ValidateReferences())
            return;

        UpdateLocalizedTexts();

        ClearSpawnedSlots();

        if (slotPrefab.gameObject.activeSelf)
            slotPrefab.gameObject.SetActive(false);

        // 추가:조규민 기능 설명: 기존 하드코딩 목록 대신 ChapterTestDataSO 우선 목록을 생성한다.
        List<ReplayStoryData> testData = CreateReplayStoryData();
        for (int i = 0; i < testData.Count; i++)
        {
            ReplayStorySlot slot = Instantiate(slotPrefab, content);
            slot.gameObject.SetActive(true);
            slot.SetData(testData[i], PlayStory);
            spawnedSlots.Add(slot);
        }

        RebuildListLayout();
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
        if (textHeaderReplayStory != null)
            textHeaderReplayStory.text = GetLocalizedText(_headerTextId, _headerFallbackKr, _headerFallbackEn);
    }

    private static string GetLocalizedText(int _id, string _fallbackKr, string _fallbackEn)
    {
        if (LocalizationManager.Instance == null)
            return GetSystemLanguageFallback(_fallbackKr, _fallbackEn);

        string _text = LocalizationManager.Instance.Get(_id, LocalizingType.UI);
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

    private void RebuildListLayout()
    {
        if (_contentRect == null && content != null)
            _contentRect = content as RectTransform;

        if (_contentRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);

        Canvas.ForceUpdateCanvases();

        if (_replayScrollRect != null)
            _replayScrollRect.verticalNormalizedPosition = 1f;
    }

    private void ClearSpawnedSlots()
    {
        for (int i = spawnedSlots.Count - 1; i >= 0; i--)
        {
            if (spawnedSlots[i] != null)
            {
                spawnedSlots[i].gameObject.SetActive(false);
                Destroy(spawnedSlots[i].gameObject);
            }
        }

        spawnedSlots.Clear();
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (content == null)
        {
            Debug.LogWarning("[ReplayStoryPopup] Content 참조가 비어 있습니다.", this);
            isValid = false;
        }

        if (slotPrefab == null)
        {
            Debug.LogWarning("[ReplayStoryPopup] Slot Prefab 참조가 비어 있습니다.", this);
            isValid = false;
        }

        if (textHeaderReplayStory == null)
            Debug.LogWarning("[ReplayStoryPopup] Text_HeaderRePlayStory 참조가 비어 있습니다.", this);

        return isValid;
    }

    private void ResolveMissingReferences()
    {
        if (buttonCloseReplayStory == null)
            buttonCloseReplayStory = FindChildComponentByName<Button>(transform, "Button_CloseReplayStory");

        if (textHeaderReplayStory == null)
            textHeaderReplayStory = FindChildComponentByName<TextMeshProUGUI>(transform, "Text_HeaderRePlayStory");

        if (content == null)
        {
            Transform contentTransform = transform.Find("RePlayList/Viewport/Content");
            if (contentTransform == null)
                contentTransform = FindChildByName(transform, "Content");

            content = contentTransform;
        }

        if (_contentRect == null && content != null)
            _contentRect = content as RectTransform;

        if (_replayScrollRect == null)
            _replayScrollRect = FindChildComponentByName<ScrollRect>(transform, "RePlayList");

        if (slotPrefab == null && content != null)
        {
            Transform slotTransform = content.Find("RePlaySlot");
            if (slotTransform == null)
                slotTransform = FindChildByName(content, "RePlaySlot");

            if (slotTransform != null)
            {
                slotPrefab = slotTransform.GetComponent<ReplayStorySlot>();
                if (slotPrefab == null)
                    slotPrefab = slotTransform.gameObject.AddComponent<ReplayStorySlot>();
            }
        }

        // 추가:조규민 기능 설명: Inspector 연결이 비어 있을 때 ChapterTestData 에셋을 자동 탐색한다.
        if (chapterTestData == null)
            chapterTestData = FindChapterTestData();

        if (storyTriggerTable == null)
            storyTriggerTable = FindStoryTriggerTable();
    }

    private static StoryTriggerTable FindStoryTriggerTable()
    {
        StoryTriggerTable[] assets = Resources.FindObjectsOfTypeAll<StoryTriggerTable>();
        return assets.Length > 0 ? assets[0] : null;
    }

    private static T FindChildComponentByName<T>(Transform root, string objectName) where T : Component
    {
        Transform child = FindChildByName(root, objectName);
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

    private List<ReplayStoryData> CreateReplayStoryData()
    {
        // 추가:조규민 기능 설명: 하우징 책장 모드에서만 ChapterTestDataSO 4개 챕터 목록을 사용한다.
        if (useChapterTestDataForCurrentOpen && chapterTestData != null && chapterTestData.ChapterCount > 0)
            return CreateChapterTestData();

        if (useChapterTestDataForCurrentOpen)
            Debug.LogWarning("[ReplayStoryPopup] ChapterTestDataSO가 연결되지 않아 기존 임시 다시보기 데이터를 사용합니다.", this);

        List<ReplayStoryData> storyData = CreateStoryTriggerData();
        return storyData.Count > 0 ? storyData : CreateFallbackData();
    }

    // 스토리 트리거 테이블 + 진행도 기반으로 전체 스토리 목록을 생성한다. (인트로 + 트리거 전체)
    private List<ReplayStoryData> CreateStoryTriggerData()
    {
        List<ReplayStoryData> list = new List<ReplayStoryData>();

        if (storyTriggerTable == null || storyTriggerTable.rows == null)
            return list;

        // 인트로: 항상 해금.
        ReplayStoryData intro = new ReplayStoryData(
            introStoryId.ToString(),
            LocalizeSystem(introChapterTitleKr, introChapterTitleEn),
            LocalizeSystem(introEpisodeTitleKr, introEpisodeTitleEn),
            ReplayStoryState.Cleared,
            string.Empty);
        intro.BackgroundResourcePath = !string.IsNullOrEmpty(introBackgroundResourcePath)
            ? introBackgroundResourcePath
            : ResolveStoryBackgroundPath(introStoryId);
        list.Add(intro);

        int currentOrder = GetCurrentStageOrder();

        for (int i = 0; i < storyTriggerTable.rows.Count; i++)
        {
            StoryTriggerData row = storyTriggerTable.rows[i];
            if (row == null)
                continue;

            int chapter = row.Target_ID / 100;
            int stage = row.Target_ID % 100;
            bool needClear = !IsStartTrigger(row.TriggerType);

            // 도달(Start): Target_ID <= 현재 진행 / 클리어(End·Theme): Target_ID < 현재 진행
            bool unlocked = needClear ? row.Target_ID < currentOrder : row.Target_ID <= currentOrder;

            ReplayStoryData data = new ReplayStoryData(
                row.Story_ID.ToString(),
                GetChapterTitle(chapter),
                GetEpisodeTitle(chapter, stage),
                unlocked ? ReplayStoryState.Cleared : ReplayStoryState.Locked,
                unlocked ? string.Empty : BuildUnlockText(chapter, stage, needClear));
            data.BackgroundResourcePath = ResolveStoryBackgroundPath(row.Story_ID);
            list.Add(data);
        }

        return list;
    }

    // 스토리 대사그룹의 대표 배경 경로를 찾는다. 배경(BG) 우선, 실제 리소스가 있는 것만 사용한다.
    private static string ResolveStoryBackgroundPath(int storyGroupId)
    {
        StoryRepo repo = DataManager.Instance != null ? DataManager.Instance.StoryRepo : null;
        if (repo == null)
        {
            Debug.LogWarning("[ReplayStoryPopup] StoryRepo가 없어 배경을 찾지 못했습니다.");
            return string.Empty;
        }

        List<Story_TalkData> lines = repo.GetTalkGroup(storyGroupId);
        if (lines == null)
            return string.Empty;

        // 대사 순서대로 BG(Backgrounds) 후보 중 실제 파일이 있는 첫 번째를 사용한다.
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i] == null || lines[i].BG <= 0) continue;
            string bgPath = "Story/Backgrounds/" + lines[i].BG;
            if (ResourceSpriteExists(bgPath))
                return bgPath;
        }

        // BG가 전부 없으면 CG(Cutscenes) 후보 시도.
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i] == null || lines[i].CG <= 0) continue;
            string cgPath = "Story/Cutscenes/" + lines[i].CG;
            if (ResourceSpriteExists(cgPath))
                return cgPath;
        }

        Debug.LogWarning($"[ReplayStoryPopup] 스토리 {storyGroupId} 배경 리소스를 찾지 못함 (해당 그룹 BG/CG 파일 없음).");
        return string.Empty;
    }

    private static bool ResourceSpriteExists(string path)
    {
        return !string.IsNullOrEmpty(path) && Resources.Load<Sprite>(path) != null;
    }

    private static bool IsStartTrigger(string triggerType)
    {
        return !string.IsNullOrEmpty(triggerType) && triggerType.Trim() == "ChapterStart";
    }

    // 현재 진행 위치를 챕터*100+스테이지 순번으로 변환 (Target_ID와 동일 체계)
    private static int GetCurrentStageOrder()
    {
        var cloud = GameManager.Instance != null ? GameManager.Instance.CloudData : null;
        int chapter = cloud != null ? Mathf.Max(1, cloud.currentChapter) : 1;
        int stage = cloud != null ? Mathf.Max(1, cloud.currentStage) : 1;
        return chapter * 100 + stage;
    }

    private static string BuildUnlockText(int chapter, int stage, bool needClear)
    {
        string stageLabel = chapter + "-" + stage;
        if (Application.systemLanguage == SystemLanguage.Korean)
            return needClear ? stageLabel + " 클리어 필요" : stageLabel + " 도달 필요";

        return needClear ? "Clear " + stageLabel + " to unlock" : "Reach " + stageLabel + " to unlock";
    }

    private static string GetChapterTitle(int chapter)
    {
        int id = _chapterTitleBaseId + (chapter - 1) * _chapterTitleStride;
        return GetChapterLocalized(id, "Chapter " + chapter);
    }

    private static string GetEpisodeTitle(int chapter, int stage)
    {
        int id = _chapterTitleBaseId + (chapter - 1) * _chapterTitleStride + stage;
        return GetChapterLocalized(id, chapter + "-" + stage);
    }

    private static string GetChapterLocalized(int id, string fallback)
    {
        if (LocalizationManager.Instance == null)
            return fallback;

        string text = LocalizationManager.Instance.Get(id, LocalizingType.Chapter);
        return IsMissingLocalizationText(text, id) ? fallback : text;
    }

    private static string LocalizeSystem(string kr, string en)
    {
        return Application.systemLanguage == SystemLanguage.Korean ? kr : en;
    }

    private List<ReplayStoryData> CreateChapterTestData()
    {
        // 추가:조규민 기능 설명: ChapterTestDataSO의 챕터 표시 데이터를 다시보기 슬롯 데이터로 변환한다.
        List<ReplayStoryData> replayData = new List<ReplayStoryData>();

        for (int i = 0; i < chapterTestData.ChapterCount; i++)
        {
            ChapterTestEntry chapter = chapterTestData.GetChapter(i);
            if (chapter == null)
                continue;

            string chapterLabel = string.IsNullOrWhiteSpace(chapter.chapterLabel)
                ? "CHAPTER." + (i + 1)
                : chapter.chapterLabel;
            string chapterName = string.IsNullOrWhiteSpace(chapter.chapterName)
                ? "Chapter " + (i + 1)
                : chapter.chapterName;

            replayData.Add(new ReplayStoryData(
                i.ToString(),
                chapterLabel,
                chapterName,
                ReplayStoryState.Cleared,
                chapter.description));
        }

        return replayData;
    }

    private List<ReplayStoryData> CreateFallbackData()
    {
        return new List<ReplayStoryData>
        {
            new("3001", "Tutorial", "마법의 책장", ReplayStoryState.Cleared, string.Empty),
            new("3002", "Chapter.1", "절벽 끝의 부름", ReplayStoryState.Cleared, string.Empty),
            new("3003", "Chapter.1", "동화 세계로의 추락", ReplayStoryState.Cleared, string.Empty),
            new("3004", "Chapter.1", "래리와의 재회", ReplayStoryState.Locked, "1-2 클리어 필요"),
            new("3005", "Chapter.1", "첫 전투 이후", ReplayStoryState.Locked, "1-3 클리어 필요"),
            new("3006", "Chapter.1", "왕을 향한 여정", ReplayStoryState.Locked, "1-4 클리어 필요"),
            new("3007", "Chapter.1", "버섯 무리의 흔적", ReplayStoryState.Locked, "1-5 클리어 필요"),
            new("3008", "Chapter.2", "낯선 숲의 아침", ReplayStoryState.Locked, "Chapter.1 클리어 필요"),
            new("3009", "Chapter.2", "사라진 길잡이", ReplayStoryState.Locked, "2-1 클리어 필요"),
            new("3010", "Chapter.2", "책장 너머의 마을", ReplayStoryState.Locked, "2-2 클리어 필요"),
            new("3011", "Chapter.2", "잠든 이야기의 문", ReplayStoryState.Locked, "2-3 클리어 필요"),
            new("3012", "Side Story", "래리의 장난", ReplayStoryState.Locked, "Chapter.2 클리어 필요"),
            new("3013", "Side Story", "무녀의 기억", ReplayStoryState.Locked, "외전 개방 필요"),
            new("3014", "Chapter.3", "오래된 왕관", ReplayStoryState.Locked, "Chapter.2 클리어 필요"),
            new("3015", "Chapter.3", "다시 쓰는 결말", ReplayStoryState.Locked, "3-1 클리어 필요"),
        };
    }

    private void PlayStory(ReplayStoryData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[ReplayStoryPopup] 재생할 시나리오 데이터가 없습니다.", this);
            return;
        }

        int chapterIndex = ResolveChapterIndex(data);
        if (_returnToHousingPageAfterStory)
        {
            ReplayStorySelectionContext.SetReplay(
                chapterIndex,
                data.ChapterTitle,
                data.EpisodeTitle,
                data.UnlockConditionText,
                returnSceneName,
                LobbyPageType.Housing);
        }
        else
        {
            ReplayStorySelectionContext.SetReplay(
                chapterIndex,
                data.ChapterTitle,
                data.EpisodeTitle,
                data.UnlockConditionText,
                returnSceneName);
        }

        Debug.Log($"[ReplayStory] 보기 클릭: {data.ChapterTitle} / {data.EpisodeTitle}");
        LoadStoryScene();
    }

    private static int ResolveChapterIndex(ReplayStoryData _data)
    {
        if (_data == null || string.IsNullOrWhiteSpace(_data.StoryId))
            return 0;

        if (int.TryParse(_data.StoryId, out int _chapterIndex))
            return _chapterIndex;

        return 0;
    }

    private void LoadStoryScene()
    {
        if (string.IsNullOrWhiteSpace(storySceneName))
        {
            Debug.LogWarning("[ReplayStoryPopup] 이동할 시나리오 씬 이름이 비어 있습니다.", this);
            return;
        }

        SceneManager.LoadScene(storySceneName);
    }

    private static ChapterTestDataSO FindChapterTestData()
    {
        // 추가:조규민 기능 설명: 씬/메모리에 로드된 ChapterTestDataSO 중 ChapterTestData 에셋을 우선 찾는다.
        ChapterTestDataSO[] assets = Resources.FindObjectsOfTypeAll<ChapterTestDataSO>();
        for (int i = 0; i < assets.Length; i++)
        {
            ChapterTestDataSO asset = assets[i];
            if (asset != null && asset.name == "ChapterTestData")
                return asset;
        }

        return assets.Length > 0 ? assets[0] : null;
    }
}
