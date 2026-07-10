// 작성자 : 김영찬
// 내용 : 인게임 씬에서 다른 씬으로 전환을 제어

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameNextSceneLoader : MonoBehaviour
{
    // ---------- SerializeField ----------
    [SerializeField] StageLoopManager _stageLoopManager;
    [SerializeField] ScreenNavigator _navigator;
    [SerializeField] ResultPopup _resultPopup;
    
    // ---------- private ----------
    private int _nextGroupId;
    
    // ---------- const ----------
    private const string LOBBY_SCENE_NAME = "_Lobby";
    private const string SCENARIO_TRIGGER_CHAPTER_END = "ChapterEnd";
    
    // ---------- Life Cycle ----------
    private void Awake()
    {
        Init();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    // ---------- 초기화 ----------
    private void Init()
    {
        _navigator = FindAnyObjectByType<ScreenNavigator>();
        _resultPopup ??= FindFirstObjectByType<ResultPopup>(FindObjectsInactive.Include);
        _nextGroupId = -1;
    }
    
    public void SetLoopManager(StageLoopManager stageLoopManager)
    {
        _stageLoopManager = stageLoopManager;
    }
    
    // ---------- 이벤트 구독/해제 ----------
    private void Subscribe()
    {
        if(_navigator != null) _navigator.OnLobbyClicked += HandleGoToLobby;
        if(_resultPopup != null)
        {
            _resultPopup.OnRetryClicked += HandleGoToRetry;
            _resultPopup.OnNextChapterClicked += HandleGoToNextChapter;
        }
    }

    private void Unsubscribe()
    {
        if(_navigator != null) _navigator.OnLobbyClicked -= HandleGoToLobby;
        if(_resultPopup != null)
        {
            _resultPopup.OnRetryClicked -= HandleGoToRetry;
            _resultPopup.OnNextChapterClicked -= HandleGoToNextChapter;
        }
    }
    
    // ---------- 이벤트 핸들러 ----------
    public void HandleChapterCleared(bool isCleared)
{
    if (!isCleared || GameManager.Instance == null)
        return;

    if (TutorialManager.Instance != null && !TutorialManager.Instance.IsCompleted)
        return;
    
    if (_resultPopup == null)
    {
        Debug.LogError("[InGameNextSceneLoader] ResultPopup이 연결되지 않았습니다.", this);
        return;
    }

    int currentChapterId = _stageLoopManager != null ? _stageLoopManager.CurrentChapterId : GameManager.Instance.SelectedChapterId;

    _resultPopup.DisableButtonForScenarioPlay(IsRemainFirstReadScenario(currentChapterId, out _nextGroupId));
}

    private void HandleGoToLobby()
    {
        // 조건이 충족되면 로비로 이동 전에 해당 스토리 감상으로 이동
        if (_nextGroupId != -1 && GameManager.Instance != null)
        {
            GameManager.Instance.LoadScenarioByGroupId(_nextGroupId);
            return;
        }
        
        if (_nextGroupId == -1 && GameManager.Instance != null)
        {
            GameManager.Instance.LoadLobby();
            return;
        }

        Debug.LogWarning("[InGameNextSceneLoader] GameManager.Instance가 없어 _Lobby 씬을 직접 로드합니다.", this);
        StartCoroutine(LoadSceneCoroutine(LOBBY_SCENE_NAME));
    }
    
    private void HandleGoToRetry()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LoadInGame();   // 현재 스테이지 다시 시작
    }

    private void HandleGoToNextChapter()
    {
        if (GameManager.Instance == null) return;

        // 옛 SelectedChapterId += 1 산술은 값이 비어(0) 있으면 존재하지 않는 챕터 1로 들어가 빈 인게임에 갇히고,
        // 풀 Stage_ID가 들어 있어도 "다음 스테이지"가 될 뿐 다음 챕터가 아니다. 방금 끝난 챕터 기준으로 데이터 역조회한다.
        _stageLoopManager ??= FindFirstObjectByType<StageLoopManager>();
        int currentChapterId = _stageLoopManager != null ? _stageLoopManager.CurrentChapterId : 0;
        int nextStageId = ResolveNextChapterFirstStageId(currentChapterId);
        if (nextStageId <= 0)
        {
            // 다음 챕터가 없으면(마지막 챕터, 튜토리얼/0챕터) 로비로.
            HandleGoToLobby();
            return;
        }

        GameManager.Instance.SelectedChapterId = nextStageId;   // 계약: SelectedChapterId에는 항상 풀 Stage_ID를 넣는다
        GameManager.Instance.LoadInGame();
    }
    
    // ---------- 보조 함수 ----------
    // 다음 챕터의 1스테이지 Stage_ID. 챕터+1 → 없으면 다음 테마 1챕터(105 다음은 201). 튜토/0챕터(98xx/99xx)는 본편 진행이 아니라 -1.
    private static int ResolveNextChapterFirstStageId(int chapterId)
    {
        if (chapterId <= 0 || chapterId >= 9000) return -1;
        var repo = DataManager.Instance != null ? DataManager.Instance.StageRepo : null;
        if (repo == null) return -1;

        int next = repo.GetStageId(chapterId + 1, 1);
        if (next > 0) return next;

        return repo.GetStageId((chapterId / 100 + 1) * 100 + 1, 1);
    }
    
    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;
    }

    private bool IsRemainFirstReadScenario(int chapterId, out int groupId)
    {
        groupId = -1;
        if (GameManager.Instance == null || GameManager.Instance.CloudData == null) return false;

        var data = DataManager.Instance.StoryRepo.GetTriggerDataByChapterID(chapterId, SCENARIO_TRIGGER_CHAPTER_END);
        if(data == null) return false;
        
        bool alreadyRead = GameManager.Instance.IsFirstReadScenario(data.Story_ID);

        if (alreadyRead) return false;
        
        groupId = data.Story_ID; 
        return true;

    }
}
