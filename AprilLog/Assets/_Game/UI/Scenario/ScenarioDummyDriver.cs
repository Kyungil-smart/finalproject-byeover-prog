// 담당자 : 홍정옥
// 설명   : 시나리오 UI 테스트용 더미 데이터 드라이버 (임시)
//          - ScenarioView 연출 확인용. 실제 데이터 담당자 연동 시 삭제/교체.
//          - View의 OnAdvanceRequested/OnSkipRequested를 구독해 더미 대사를 순서대로 먹인다.

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

    private int _index;
    
    private void Awake()
    {
        if (_view == null)
            _view = GetComponent<ScenarioView>();
        if (_view == null)
            _view = FindFirstObjectByType<ScenarioView>();

        if (_lines == null || _lines.Count == 0)
            FillDefault();
    }

    private void OnEnable()
    {
        if (_view == null) return;
        _view.OnAdvanceRequested += Next;
        _view.OnSkipRequested    += SkipToEnd;
    }

    private void OnDisable()
    {
        if (_view == null) return;
        _view.OnAdvanceRequested -= Next;
        _view.OnSkipRequested    -= SkipToEnd;
    }

    private void Start()
    {
        if (_playOnStart)
            Begin();
    }
    
    public void Begin()
    {
        if (_view == null)
        {
            Debug.LogWarning("[ScenarioDummyDriver] ScenarioView가 연결되지 않았습니다.", this);
            return;
        }
        _index = 0;
        Show();
    }

    private void Next()
    {
        if (_index >= _lines.Count - 1)
        {
            Debug.Log("[ScenarioDummyDriver] 시나리오 끝");
            return;
        }
        _index++;
        Show();
    }

    private void SkipToEnd()
    {
        _index = _lines.Count - 1;
        Show();
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

        // --- GroupID 3001 (TextBox 0, 절벽 위) ---
        Add(false, "???",     "이 길이 맞아?",                        ScenarioSpeakerSlot.None,  _bg3001, false);
        Add(false, "???",     "그래! 여기가 맞아!",                   ScenarioSpeakerSlot.None,  _bg3001, false);
        Add(false, "???",     "하...하지만, 여긴 절벽이야. 래리!",    ScenarioSpeakerSlot.None,  _bg3001, false);
        Add(false, "래리",    "걱정마! 에이프릴.",                    ScenarioSpeakerSlot.Right, _bg3001, true);
        Add(false, "래리",    "잊었어? 여긴 동화의 세계야.",          ScenarioSpeakerSlot.Right, _bg3001, true);
        Add(false, "래리",    "동화같은 기적이 우리를 맞이할테니까!", ScenarioSpeakerSlot.Right, _bg3001, true);
        Add(false, "래리",    "가자! 야호!",                          ScenarioSpeakerSlot.Right, _bg3001, true);
        Add(false, "에이프릴", "꺄악!",                               ScenarioSpeakerSlot.Left,  _bg3001, true);

        // --- GroupID 3002 (TextBox 1, 절벽 아래) ---
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
}
