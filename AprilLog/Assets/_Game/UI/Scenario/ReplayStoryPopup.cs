using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 1차 수정자 : 조규민
// 수정 내용 : 하우징 책장 다시보기에서 ChapterTestDataSO 기준 챕터 목록을 표시하고 선택 정보를 _Story 씬으로 전달

public class ReplayStoryPopup : MonoBehaviour
{
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

    private readonly List<ReplayStorySlot> spawnedSlots = new();
    private Button boundCloseButton;
    private bool useChapterTestDataForCurrentOpen;

    private void Awake()
    {
        ResolveMissingReferences();
        BindCloseButton();
    }

    private void OnEnable()
    {
        ResolveMissingReferences();
        BindCloseButton();
        RefreshList();
    }

    private void OnDestroy()
    {
        if (boundCloseButton != null)
            boundCloseButton.onClick.RemoveListener(Close);
    }

    public void Open()
    {
        // 추가:조규민 기능 설명: 기존 로비 옵션 다시보기는 기존 임시 다시보기 목록을 유지한다.
        useChapterTestDataForCurrentOpen = false;
        OpenInternal();
    }

    public void OpenForHousingBookcase()
    {
        // 추가:조규민 기능 설명: 하우징 책장 진입 시에만 ChapterTestData.asset 기반 4개 챕터 목록을 사용한다.
        useChapterTestDataForCurrentOpen = true;
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

        if (textHeaderReplayStory != null)
            textHeaderReplayStory.text = "시나리오 다시보기";

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

        return CreateFallbackData();
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

        if (useChapterTestDataForCurrentOpen)
        {
            // 추가:조규민 기능 설명: 하우징 책장 모드에서만 선택한 챕터 정보를 _Story 씬에서 읽을 수 있도록 Context에 저장한다.
            int chapterIndex = 0;
            int.TryParse(data.StoryId, out chapterIndex);
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
