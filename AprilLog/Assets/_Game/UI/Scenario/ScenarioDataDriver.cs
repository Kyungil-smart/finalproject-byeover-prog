// 담당자 : 정승우
// 설명   : 시나리오 정식 드라이버 (데이터 기반).
//          StoryRepo의 GroupID로 대사 묶음을 가져와 ScenarioView에 한 줄씩 먹이고,
//          마지막 줄(또는 스킵)에서 OnFinished를 쏜다.
//          ScenarioDummyDriver(홍정옥, 임시 하드코딩)의 정식 교체본.
//
// 흐름:
//   Play(groupId) → StoryRepo.GetTalkGroup(groupId) → 줄 단위로 ShowLine → 끝 → OnFinished
//
// 현재 데이터 상태: 3_npc_talk의 portrait/speaker/BG/CG/BGM/SFX가 전부 0.
//   따라서 지금은 "이름 + 대사 텍스트"만 출력된다. 데이터 담당이 ID를 채우고
//   아트가 Resources에 스프라이트를 넣으면(아래 경로 규칙) 코드 수정 없이 그림이 붙는다.
//
// 스프라이트 경로 규칙(인스펙터에서 변경 가능):
//   초상화 : Resources/<_portraitPath><Story_CharacterData.Resource_ID>
//   배경   : Resources/<_backgroundPath><Story_TalkData.BG>
//   컷씬   : Resources/<_cutscenePath><Story_TalkData.CG>
//   못 찾으면 null → ScenarioView가 알아서 숨김(텍스트만).

using System;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioDataDriver : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private ScenarioView _view;

    [Header("재생 대상")]
    [Tooltip("StoryRepo의 GroupID (예: 3001 = 인트로)")]
    [SerializeField] private int _startGroupId = 3001;
    [SerializeField] private bool _playOnStart = false;

    [Header("언어")]
    [Tooltip("LocalizationManager가 없을 때 사용할 언어. 켜면 영어, 끄면 한국어")]
    [SerializeField] private bool _useEnglish = false;

    [Header("스프라이트 경로 (Resources 하위, 끝에 / 포함)")]
    [Tooltip("초상화: Resource_ID 앞에 붙는 경로")]
    [SerializeField] private string _portraitPath = "Story/Portraits/";
    [Tooltip("배경: BG ID 앞에 붙는 경로")]
    [SerializeField] private string _backgroundPath = "Story/Backgrounds/";
    [Tooltip("컷씬: CG ID 앞에 붙는 경로")]
    [SerializeField] private string _cutscenePath = "Story/Cutscenes/";

    /// <summary>마지막 대사 이후(또는 스킵) 시나리오 종료 시 호출.</summary>
    public event Action OnFinished;

    /// <summary>지금 재생 중인지.</summary>
    public bool IsPlaying => _isPlaying;

    private List<Story_TalkData> _lines;
    private int _index;
    private bool _isPlaying;
    private bool _finished;
    private bool _subscribed;
    private bool _isCloudDataLoaded;
    
    private void Awake()
    {
        if (_view == null) _view = GetComponent<ScenarioView>();
        if (_view == null) _view = FindFirstObjectByType<ScenarioView>();
        
        if (GameManager.Instance != null)
        {
            _isCloudDataLoaded = GameManager.Instance.CloudData != null;
            
            if (TutorialManager.Instance != null && !TutorialManager.Instance.IsCompleted) return;
            
            _startGroupId = GameManager.Instance.SelectedScenarioGroupId != 0 ? 
                GameManager.Instance.SelectedScenarioGroupId : _startGroupId;
        }
    }

    private void OnEnable()  => Subscribe();
    private void OnDisable() => Unsubscribe();

    private void Start()
    {
        if (_playOnStart) Play(_startGroupId);
    }

    // ---------- 외부 API ----------

    /// <summary>지정 GroupID의 시나리오를 처음부터 재생한다.</summary>
    public void Play(int groupId)
    {
        if (_view == null)
        {
            Debug.LogWarning("[ScenarioDataDriver] ScenarioView가 연결되지 않았습니다.", this);
            FinishImmediate();
            return;
        }

        StoryRepo repo = DataManager.Instance != null ? DataManager.Instance.StoryRepo : null;
        if (repo == null)
        {
            Debug.LogWarning("[ScenarioDataDriver] StoryRepo를 찾지 못했습니다. (DataManager 미초기화?)", this);
            FinishImmediate();
            return;
        }

        _lines = repo.GetTalkGroup(groupId);
        if (_lines == null || _lines.Count == 0)
        {
            Debug.LogWarning($"[ScenarioDataDriver] GroupID {groupId} 대사가 비어 있습니다.", this);
            FinishImmediate();
            return;
        }

        Subscribe();
        _index = 0;
        _finished = false;
        _isPlaying = true;
        _startGroupId = groupId;
        SaveUnlockScenario(groupId);
        Show();
    }

    // ---------- 진행 ----------

    private void Next()
    {
        if (!_isPlaying) return;

        if (_index >= _lines.Count - 1)
        {
            Finish();   // 마지막 줄에서 한 번 더 진행 → 종료
            return;
        }
        _index++;
        Show();
    }

    private void Finish()
    {
        if (_finished) return;   // 중복 방지
        _finished = true;
        _isPlaying = false;
        Debug.Log("[ScenarioDataDriver] 시나리오 끝");
        OnFinished?.Invoke();
    }

    // 재생 자체가 불가능할 때(데이터 없음 등)도 흐름이 멈추지 않게 즉시 종료를 알린다.
    private void FinishImmediate()
    {
        if (_finished) return;
        _finished = true;
        _isPlaying = false;
        OnFinished?.Invoke();
    }

    private void Show()
    {
        Story_TalkData line = _lines[_index];

        bool useEnglish = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.CurrentLanguage == "en"
            : _useEnglish;
        string speakerName = useEnglish && !string.IsNullOrWhiteSpace(line.name_EN)
            ? line.name_EN
            : line.name_KR;
        string text = useEnglish && !string.IsNullOrWhiteSpace(line.Text_EN)
            ? line.Text_EN
            : line.Text_KR;

        // 초상화 슬롯 매핑(임시 규칙): portrait1=좌 / portrait2=중 / portrait3=우.
        //   data의 direction1/2/3(방향/배치) 의미는 기획 확정 후 반영. (현재 데이터 전부 0이라 미사용)
        Sprite pLeft   = ResolvePortrait(line.portrait1);
        Sprite pCenter = ResolvePortrait(line.portrait2);
        Sprite pRight  = ResolvePortrait(line.portrait3);

        Sprite bg = ResolveScene(_backgroundPath, line.BG);
        Sprite cg = ResolveScene(_cutscenePath, line.CG);

        // speaker(int) → ScenarioSpeakerSlot. enum: None0/Left1/Center2/Right3 과 동일 체계 가정.
        ScenarioSpeakerSlot speaker = (ScenarioSpeakerSlot)Mathf.Clamp(line.speaker, 0, 3);

        bool showTextbox = line.TextBox != 0;

        _view.ShowLine(speakerName, text, showTextbox, pLeft, pCenter, pRight, speaker, bg, cg);
    }

    // ---------- 스프라이트 해석 ----------

    // 캐릭터 ID → Story_CharacterData.Resource_ID → Resources에서 스프라이트 로드.
    private Sprite ResolvePortrait(int portraitId)
    {
        if (portraitId <= 0) return null;

        // 데이터가 캐릭터 ID를 넣은 경우: 캐릭터의 Resource_ID로 매핑한다.
        StoryRepo repo = DataManager.Instance != null ? DataManager.Instance.StoryRepo : null;
        Story_CharacterData ch = repo != null ? repo.GetCharacterData(portraitId) : null;
        if (ch != null && ch.Resource_ID > 0)
            return LoadSprite(_portraitPath + ch.Resource_ID);

        // 데이터가 Resource_ID(그림 파일 번호)를 직접 넣은 경우: 값 그대로 로드한다.
        return LoadSprite(_portraitPath + portraitId);
    }

    private Sprite ResolveScene(string pathPrefix, int resourceId)
    {
        if (resourceId <= 0) return null;
        return LoadSprite(pathPrefix + resourceId);
    }

    private Sprite LoadSprite(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        Sprite sprite = Resources.Load<Sprite>(path);
        // 못 찾아도 경고만 — 데이터/아트가 아직 안 넣었을 수 있음(텍스트만 진행).
        if (sprite == null)
            Debug.Log($"[ScenarioDataDriver] 스프라이트 없음(텍스트만 진행): Resources/{path}");
        return sprite;
    }

    // ---------- 이벤트 구독 ----------

    private void Subscribe()
    {
        if (_subscribed || _view == null) return;
        _view.OnAdvanceRequested += Next;
        _view.OnSkipRequested    += Finish;   // 스킵 = 끝내고 다음으로
        if(GameManager.Instance != null && !_isCloudDataLoaded)
            GameManager.Instance.OnCloudDataReady += HandleCloudDataLoaded;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || _view == null) return;
        _view.OnAdvanceRequested -= Next;
        _view.OnSkipRequested    -= Finish;
        if(GameManager.Instance != null)
            GameManager.Instance.OnCloudDataReady -= HandleCloudDataLoaded;
        _subscribed = false;
    }

    private void SaveUnlockScenario(int groupId)
    {
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[ScenarioDataDriver] DataManager 미 감지. 시나리오 진행 저장되지 않음.");
            return;
        }

        if (!_isCloudDataLoaded)
        {
            LoadedScenarioTempContainer.UnsavedFirstReadScenarioResister(groupId);
            return;
        }
        
        GameManager.Instance.SaveFirstReadScenario(groupId);
    }

    private void HandleCloudDataLoaded()
    {
        if(_isCloudDataLoaded) return;
        LoadedScenarioTempContainer.SaveContainScenario();
        _isCloudDataLoaded = true;
    }
}
