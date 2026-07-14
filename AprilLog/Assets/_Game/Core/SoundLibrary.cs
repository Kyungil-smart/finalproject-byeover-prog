// 담당자 : 정승우
// 설명   : SFX 가이드(이가현·이균호)의 사운드 ID와 클립/재생 정책 매핑.
//          파일명이 공백·괄호를 포함해 문자열 키는 오타 지뢰라, 코드는 SfxId만 쓰고
//          실제 클립 연결은 Resources/SoundLibrary.asset 에서 관리한다(단일 소스).

using UnityEngine;

/// <summary>사운드 식별자. 값 구간: 1~ BGM / 10~ 아웃게임 / 30~ 인게임 / 60~ 하우징.
/// 피격(Hit*)·강공(Strong*)은 원소 번호 순서(1불 2물 3바람 4번개 5얼음)로 배치해 산술 변환이 가능하다.</summary>
public enum SfxId
{
    None = 0,

    // ---------- BGM ----------
    LobbyBgm = 1,            // 로그인·로딩·로비
    Chapter1Bgm = 2,         // 1챕터 진입
    HousingBgm = 3,          // 하우징 화면

    // ---------- 아웃게임 ----------
    CharacterLevelUp = 10,   // 캐릭터 레벨 업 성공
    ArtifactGachaDraw = 11,  // 뽑기 진행
    ArtifactGachaTen = 12,   // 10회 뽑기 시 추가 재생
    ArtifactAscension = 13,  // 아티팩트 돌파
    ArtifactDisassemble = 14,// 아티팩트 분해
    ArtifactCraft = 15,      // 아티팩트 제조
    ButtonClick = 16,        // 공용 버튼 클릭
    PopupOpen = 17,          // 모든 팝업 등장
    EnterInGame = 18,        // 스타트 버튼(인게임 입장)
    OutgameCast = 19,        // 용도 미정(가이드 아웃게임 10.0 Neutral_Medium_Cast_06)

    // ---------- 인게임: 피격(원소 순서 고정) ----------
    HitFire = 30,
    HitWater = 31,
    HitWind = 32,
    HitLightning = 33,
    HitIce = 34,

    AutoAttackShot = 35,     // 자동 공격 사출
    EnchantSelect = 36,      // 인챈트 선택
    EnchantReroll = 37,      // 인챈트 리롤
    EnchantChange = 38,      // 보유 인챈트 교체
    GameClear = 39,          // 클리어 정산 노출
    GameOver = 40,           // 게임오버 정산 노출
    SortSuccess = 41,        // sort 성공
    JokerClick = 42,         // 조커 클릭
    UnitSelect = 43,         // 유닛 선택
    UnitDrop = 44,           // 유닛 드롭(sort 성공 시엔 출력 안 함)

    // ---------- 인게임: 강공(원소 순서 고정) ----------
    StrongFire = 45,         // 메테오 발생
    StrongWater = 46,        // 파도 발생
    StrongWind = 47,         // 허리케인 발생
    StrongLightning = 48,    // 뇌격 발생
    StrongIce = 49,          // 얼음 결정 발생

    // ---------- 하우징 ----------
    HousingText = 60,        // 에이프릴 대사 출력
    HousingFootstep = 61,    // 에이프릴 이동
    HousingBookshelf = 62,   // 책장 터치
    HousingBed = 63,         // 침대 터치(가이드에 파일 미지정 - 기획 확정 대기)
    HousingCoffee = 64,      // 커피머신 터치
    HousingCurrencyMaker = 65, // 재화 자동 생산기 터치
    
    // ---------- 시나리오 사운드 ----------
    
}

/// <summary>SfxId → 클립/재생 정책. Resources/SoundLibrary.asset 으로 두고 AudioManager가 Resources.Load로 읽는다.</summary>
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "AprilLog/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public SfxId id;
        public AudioClip clip;
        [Tooltip("사운드별 개별 볼륨 배율")]
        public float volume = 1f;
        [Tooltip("가이드 '중복재생' X = 꺼짐. 꺼져 있으면 이 사운드가 끝나기 전 재요청을 무시한다")]
        public bool allowOverlap = true;
        [Tooltip("재생 최소 간격(초). 피격음처럼 다발 호출되는 사운드의 스팸 방지. 0 = 제한 없음")]
        public float minInterval;
    }

    [SerializeField] private Entry[] _entries;

    private System.Collections.Generic.Dictionary<SfxId, Entry> _byId;

    public Entry Get(SfxId id)
    {
        if (_byId == null)
        {
            _byId = new System.Collections.Generic.Dictionary<SfxId, Entry>();
            if (_entries != null)
            {
                foreach (var e in _entries)
                    if (e != null && e.id != SfxId.None && !_byId.ContainsKey(e.id)) _byId.Add(e.id, e);
            }
        }

        return _byId.TryGetValue(id, out var entry) ? entry : null;
    }
}
