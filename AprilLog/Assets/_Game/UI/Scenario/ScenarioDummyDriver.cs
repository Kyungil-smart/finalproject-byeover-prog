// 담당자 : 홍정옥
// 설명   : 시나리오 UI 테스트용 더미 데이터 드라이버 (임시)
//          - ScenarioView 연출 확인용. 실제 데이터 담당자 연동 시 삭제/교체.
//          - View의 OnAdvanceRequested/OnSkipRequested를 구독해 더미 대사를 순서대로 먹인다.

// 1차 수정자 : 조규민
// 수정 내용 : 하우징 책장 다시보기로 진입한 경우 선택 챕터 기반 임시 연출 대사를 재생하도록 분기

using System;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioDummyDriver : MonoBehaviour
{
    [Serializable]
    public class DummyLine
    {
        public string name;
        [TextArea(2, 4)] public string text;
        public bool showTextbox = true;
        public ScenarioSpeakerSlot speaker = ScenarioSpeakerSlot.Left;

        [Header("이미지 (선택 — 비우면 미표시)")]
        public Sprite portraitLeft;
        public Sprite portraitCenter;
        public Sprite portraitRight;
        public Sprite background;
        public Sprite cutscene;
    }

    [Header("연결")]
    [SerializeField] private ScenarioView _view;

    [Header("더미 대사 (비우면 기본 샘플 사용)")]
    [SerializeField] private List<DummyLine> _lines = new();

    [Header("샘플 이미지 (드래그하면 기본 샘플에 자동 적용)")]
    [Tooltip("에이프릴 초상화 (좌측)")]
    [SerializeField] private Sprite _spriteApril;
    [Tooltip("래리 초상화 (우측)")]
    [SerializeField] private Sprite _spriteLarry;
    [Tooltip("3001 배경 (절벽 위)")]
    [SerializeField] private Sprite _bg3001;
    [Tooltip("3002 배경 (절벽 아래)")]
    [SerializeField] private Sprite _bg3002;

    [SerializeField] private bool _playOnStart = true;

    /// <summary>마지막 대사 이후(또는 스킵) 시나리오 종료 시 호출</summary>
    public event System.Action OnFinished;

    private int _index;
    private bool _finished;

    private void Awake()
    {
        if (_view == null)
            _view = GetComponent<ScenarioView>();
        if (_view == null)
            _view = FindFirstObjectByType<ScenarioView>();

        // 추가:조규민 기능 설명: 하우징 책장 다시보기로 진입한 경우 기본 더미 대사 대신 선택 챕터 대사를 구성한다.
        if (ReplayStorySelectionContext.IsReplayMode)
            FillReplayStory();
        else if (_lines == null || _lines.Count == 0)
            FillDefault();
    }

    private void OnEnable()
    {
        if (_view == null) return;
        _view.OnAdvanceRequested += Next;
        _view.OnSkipRequested    += Finish;   // 스킵 = 스토리 끝내고 다음으로
    }

    private void OnDisable()
    {
        if (_view == null) return;
        _view.OnAdvanceRequested -= Next;
        _view.OnSkipRequested    -= Finish;
    }

    private void Start()
    {
        if (_playOnStart)
            Begin();
    }
    
    public void Begin()
    {
        // 추가:조규민 기능 설명: Begin 시점에도 다시보기 선택 정보가 최신 상태로 반영되도록 대사를 다시 구성한다.
        if (ReplayStorySelectionContext.IsReplayMode)
            FillReplayStory();

        if (_view == null)
        {
            Debug.LogWarning("[ScenarioDummyDriver] ScenarioView가 연결되지 않았습니다.", this);
            return;
        }
        _index = 0;
        _finished = false;
        Show();
    }

    private void Next()
    {
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
        Debug.Log("[ScenarioDummyDriver] 시나리오 끝");
        OnFinished?.Invoke();
    }

    private void Show()
    {
        DummyLine l = _lines[_index];
        _view.ShowLine(
            l.name, l.text, l.showTextbox,
            l.portraitLeft, l.portraitCenter, l.portraitRight,
            l.speaker,
            l.background, l.cutscene);
    }
    
    private void FillDefault()
    {
        // 실제 데이터(시나리오 시트 3001/3002) 발췌 — UI 테스트용
        // box: TextBox 컬럼 (0=프레임 숨김, 1=표시). 화자: 래리=우, 에이프릴=좌, ???/공백=없음
        _lines = new List<DummyLine>();

        // box=TextBox, sp=화자, bg=배경, showBoth=두 캐릭터 동시 등장 여부
        void Add(bool box, string name, string kr, ScenarioSpeakerSlot sp, Sprite bg, bool showBoth)
        {
            var line = new DummyLine
            {
                showTextbox = box, name = name, text = kr, speaker = sp, background = bg,
            };

            if (showBoth)
            {
                // 둘 다 화면에 — 화자만 컬러, 나머지는 회색
                line.portraitLeft  = _spriteApril;
                line.portraitRight = _spriteLarry;
            }
            else
            {
                // 단독 등장 — 화자 위치에만 배치
                if (sp == ScenarioSpeakerSlot.Left)  line.portraitLeft  = _spriteApril;
                if (sp == ScenarioSpeakerSlot.Right) line.portraitRight = _spriteLarry;
            }

            _lines.Add(line);
        }

        
        Add(false, "???",     "이 길이 맞아?",                        ScenarioSpeakerSlot.None,  _bg3001, false);
        Add(false, "???",     "그래! 여기가 맞아!",                   ScenarioSpeakerSlot.None,  _bg3001, false);
        Add(false, "???",     "하...하지만, 여긴 절벽이야. 래리!",    ScenarioSpeakerSlot.None,  _bg3001, false);
        Add(false, "래리",    "걱정마! 에이프릴.",                    ScenarioSpeakerSlot.Right, _bg3001, true);
        Add(false, "래리",    "잊었어? 여긴 동화의 세계야.",          ScenarioSpeakerSlot.Right, _bg3001, true);
        Add(false, "래리",    "동화같은 기적이 우리를 맞이할테니까!", ScenarioSpeakerSlot.Right, _bg3001, true);
        Add(false, "래리",    "가자! 야호!",                          ScenarioSpeakerSlot.Right, _bg3001, true);
        Add(false, "에이프릴", "꺄악!",                               ScenarioSpeakerSlot.Left,  _bg3001, true);

       
        Add(true,  "",        "...",                                  ScenarioSpeakerSlot.None,  _bg3002, false);
        Add(true,  "???",     "...릴! ...프릴!",                      ScenarioSpeakerSlot.None,  _bg3002, false);
        Add(true,  "래리",    "에이프릴!",                            ScenarioSpeakerSlot.Right, _bg3002, true);
        Add(true,  "에이프릴", "후아!",                               ScenarioSpeakerSlot.Left,  _bg3002, true);
        Add(true,  "래리",    "어때? 동화처럼 살았지?",               ScenarioSpeakerSlot.Right, _bg3002, true);
        Add(true,  "에이프릴", "그건 내가 떨어질 때 마법을 걸었기 때문이잖아, 래리...", ScenarioSpeakerSlot.Left, _bg3002, true);
        Add(true,  "래리",    "헤헤.",                                ScenarioSpeakerSlot.Right, _bg3002, true);
        Add(true,  "래리",    "네가 안 걸었다면 내가 걸었을 테니까, 똑같은거지.", ScenarioSpeakerSlot.Right, _bg3002, true);
        Add(true,  "래리",    "그나저나 문제가 있어.",                ScenarioSpeakerSlot.Right, _bg3002, true);
        Add(true,  "에이프릴", "뭔데?",                               ScenarioSpeakerSlot.Left,  _bg3002, true);
        Add(true,  "래리",    "절벽 아래에도 버섯무리가 있었어.",     ScenarioSpeakerSlot.Right, _bg3002, true);
        Add(true,  "에이프릴", "래리...",                             ScenarioSpeakerSlot.Left,  _bg3002, true);
        Add(true,  "래리",    "괜찮아! 이번엔 우리가 해결할 수 있을 거야.", ScenarioSpeakerSlot.Right, _bg3002, true);
        Add(true,  "래리",    "주 무대에서 벗어났으니까.",            ScenarioSpeakerSlot.Right, _bg3002, true);
        Add(true,  "에이프릴", "후, 그럼 힘을 써도 된다는 거지?",     ScenarioSpeakerSlot.Left,  _bg3002, true);
        Add(true,  "에이프릴", "편하게 써. 마법쓰는 법은 잊지 않았지?", ScenarioSpeakerSlot.Left,  _bg3002, true);
        Add(true,  "에이프릴", "당연하지.",                           ScenarioSpeakerSlot.Left,  _bg3002, true);
        Add(true,  "에이프릴", "마법의 책장 칸에서 같은 종류의 책 3권을 모으면 그 책의 마법이 발동하지.", ScenarioSpeakerSlot.Left, _bg3002, true);
        Add(true,  "래리",    "음, 그렇지!",                          ScenarioSpeakerSlot.Right, _bg3002, true);
        Add(true,  "에이프릴", "3번 이상으로 완수할때마다 마법이 조금씩 강력해지는 콤보 상태가 되고.", ScenarioSpeakerSlot.Left, _bg3002, true);
        Add(true,  "래리",    "몬스터를 처치할 때마다 경험치를 얻을 수도 있지!", ScenarioSpeakerSlot.Right, _bg3002, true);
        Add(true,  "래리",    "경험치를 채웠으니 성장을 할 때네!",    ScenarioSpeakerSlot.Right, _bg3002, true);
        Add(true,  "래리",    "레벨업을 하면 인챈트를 선택할 수 있다고 말해줬었지?", ScenarioSpeakerSlot.Right, _bg3002, true);
    }

    private void FillReplayStory()
    {
        // 추가:조규민 기능 설명: 다시보기 팝업에서 선택한 ChapterTestData 정보를 스토리 씬 연출용 임시 대사로 변환한다.
        _lines = new List<DummyLine>();

        string chapterLabel = string.IsNullOrWhiteSpace(ReplayStorySelectionContext.ChapterLabel)
            ? "CHAPTER." + (ReplayStorySelectionContext.ChapterIndex + 1)
            : ReplayStorySelectionContext.ChapterLabel;
        string chapterName = string.IsNullOrWhiteSpace(ReplayStorySelectionContext.ChapterName)
            ? "Chapter " + (ReplayStorySelectionContext.ChapterIndex + 1)
            : ReplayStorySelectionContext.ChapterName;
        string chapterDescription = string.IsNullOrWhiteSpace(ReplayStorySelectionContext.ChapterDescription)
            ? "기록된 이야기를 다시 펼칩니다."
            : ReplayStorySelectionContext.ChapterDescription;

        void AddReplay(bool box, string name, string text, ScenarioSpeakerSlot speaker, Sprite background, bool showBoth)
        {
            // 추가:조규민 기능 설명: 기존 더미 연출과 같은 초상화/배경 규칙으로 다시보기 대사 한 줄을 추가한다.
            DummyLine line = new DummyLine
            {
                showTextbox = box,
                name = name,
                text = text,
                speaker = speaker,
                background = background
            };

            if (showBoth)
            {
                line.portraitLeft = _spriteApril;
                line.portraitRight = _spriteLarry;
            }
            else if (speaker == ScenarioSpeakerSlot.Left)
            {
                line.portraitLeft = _spriteApril;
            }
            else if (speaker == ScenarioSpeakerSlot.Right)
            {
                line.portraitRight = _spriteLarry;
            }

            _lines.Add(line);
        }

        AddReplay(false, string.Empty, chapterLabel, ScenarioSpeakerSlot.None, _bg3001, false);
        AddReplay(true, "에이프릴", "책장에 기록된 이야기를 다시 펼쳐볼게.", ScenarioSpeakerSlot.Left, _bg3001, true);
        AddReplay(true, "래리", chapterName + " 이야기구나!", ScenarioSpeakerSlot.Right, _bg3001, true);
        AddReplay(true, "에이프릴", chapterDescription, ScenarioSpeakerSlot.Left, _bg3002, true);
        AddReplay(true, "래리", "이 장면은 추후 실제 시나리오 데이터와 연결되면 같은 연출로 재생될 거야.", ScenarioSpeakerSlot.Right, _bg3002, true);
        AddReplay(true, "에이프릴", "지금은 하우징 책장 다시보기 프로토타입으로 확인 중이야.", ScenarioSpeakerSlot.Left, _bg3002, true);
    }
}
