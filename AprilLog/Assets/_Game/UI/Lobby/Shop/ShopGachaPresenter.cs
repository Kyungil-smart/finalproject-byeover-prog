using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 홍정옥
// 설명   : 상점 페이지의 뽑기 UI 컨트롤러
//          - 1회/10회 뽑기 버튼 -> 재화 체크 -> GachaManager 실행 -> 결과창 슬롯 채우기
//          - 재화가 없으면 재화 부족 팝업 표시
//          - 결과창의 1회/10회 버튼으로 같은 박스 재추첨, 확인 버튼으로 닫기
public class ShopGachaPresenter : MonoBehaviour
{
    public enum CostCurrency { Gold, Parchment, Diamond }

    // 가챠 별 소모 재화 / 비용 / 아이콘 설정
    // 일반상자,고급상자가 서로 다른 재화를 쓰므로, 가챠 ID 별로 분리해서 지정
    [System.Serializable]
    public class GachaCostConfig
    {
        [Tooltip("이 설정을 적용할 가챠 ID (GachaBoxTable 의 Gacha_ID)")]
        public int gachaId;
        [Tooltip("이 가챠가 소모하는 재화 종류")]
        public CostCurrency currency = CostCurrency.Gold;
        [Tooltip("이 가챠의 소모 재화 아이콘 (결과창/뽑기 버튼에 표시)")]
        public Sprite costIcon;
    }

    [Header("시스템 참조")]
    [Tooltip("씬의 GachaManager")]
    [SerializeField] private GachaManager _gachaManager;
    [Tooltip("씬의 CurrencyModel 비우면 자동 탐색")]
    [SerializeField] private CurrencyModel _currencyModel;
    [Tooltip("등급별 기어 추첨용 GearMasterTable SO. 현재 추첨 데이터 소스라 필수")]
    [SerializeField] private GearMasterTable _gearTable;

    [Header("뽑기 박스 ID (GachaBoxTable 의 Gacha_ID)")]
    [SerializeField] private int _gachaId = 1;

    [Header("가챠(상자)별 소모 재화 / 아이콘")]
    [Tooltip("가챠 ID 별로 소모 재화와 아이콘을 지정\n현재 활성 가챠에 해당하는 설정을 사용하며 없으면 아래 기본 재화로 폴백한다.")]
    [SerializeField] private GachaCostConfig[] _gachaCosts;
    [Tooltip("현재 활성 가챠의 소모 재화 아이콘을 표시할 Image 들(결과창/뽑기 버튼의 재화 아이콘). 선택.")]
    [SerializeField] private Image[] _costIconTargets;

    [Header("기본 재화 (가챠별 설정에 없을 때 폴백)")]
    [Tooltip("가챠별 설정(_gachaCosts)에 해당 ID 가 없을 때 사용할 기본 재화.\n비용 금액은 데이터(PaidGachaBox / GachaBox)에서 가져온다.")]
    [SerializeField] private CostCurrency _costCurrency = CostCurrency.Gold;

    [Header("1회 결과창")]
    [SerializeField] private GameObject _singleResultPopup;
    [Tooltip("미리 배치해 둔 1회 결과 슬롯(1칸)")]
    [SerializeField] private GachaResultSlotView[] _singleSlots;
    [Tooltip("POPUP_OnceGacha 의 자동 분해 보상 표시(RewardPreviewSlot). 선택.")]
    [SerializeField] private GachaDecomposeRewardView _singleDecomposeView;

    [Tooltip("1회 결과창의 추가 뽑기(1회 더/10회 더) 버튼 묶음. 무료/튜토 뽑기에선 숨긴다.")]
    [SerializeField] private GameObject _singleRedrawGroup;
    [Tooltip("1회 결과창 확인 버튼. 추가 뽑기를 숨길 때 가운데로 옮겨 대칭을 맞춘다.")]
    [SerializeField] private RectTransform _singleConfirmRect;

    [Header("10회 결과창")]
    [SerializeField] private GameObject _tenResultPopup;
    [Tooltip("미리 배치해 둔 10회 결과 슬롯(10칸)")]
    [SerializeField] private GachaResultSlotView[] _tenSlots;
    [Tooltip("POPUP_TenGacha 의 자동 분해 보상 표시(RewardPreviewSlot 2개). 선택.")]
    [SerializeField] private GachaDecomposeRewardView _tenDecomposeView;

    [Header("재화 부족 팝업")]
    [SerializeField] private GameObject _insufficientPopup;

    [Header("튜토리얼")]
    [Tooltip("튜토리얼 뽑기에서 고정 지급할 기어 ID(수습 마법사의 인장)")]
    [SerializeField] private int _tutorialSealGearId;
    [Tooltip("튜토리얼 강화용 골드 지급량")]
    [SerializeField] private int _tutorialGrantGold = 5000;
    [Tooltip("튜토리얼 강화용 강화석 지급량")]
    [SerializeField] private int _tutorialGrantStone = 100;

    [Header("뽑기 후처리 / 결과 팝업")]
    [Tooltip("획득 반영·등급별 한도·자동 분해·누적 보상 처리를 담당. 비우면 폴백으로 단순 보유 추가만 한다.")]
    [SerializeField] private ArtifactGachaPostProcessor _postProcessor;
    [Tooltip("메인 복귀 시 누적 보상 팝업을 순차 출력하는 큐(선택).")]
    [SerializeField] private ArtifactGachaPopupQueue _popupQueue;
    [Tooltip("천장(레전더리 확정) 카운터. 레전더리 등장 시 0 리셋(기획서 6-2-1). 비우면 누적뽑기 나머지로 폴백.")]
    [SerializeField] private ArtifactPityTracker _pityTracker;
    [Tooltip("레전더리 확정(천장) 안내 표시(Confirmed Gacha Information). 선택.")]
    [SerializeField] private GachaPityInfoView _pityInfoView;
    [Tooltip("확정/누적 보상 진행도 표시(20Reward·CumulativeCompensation). 선택.")]
    [SerializeField] private GachaProgressView _progressView;

    private const int TenDrawCount = 10;

    private void Awake()
    {
        if (_gachaManager == null)
            _gachaManager = FindFirstObjectByType<GachaManager>(FindObjectsInactive.Include);
        if (_currencyModel == null)
            _currencyModel = FindFirstObjectByType<CurrencyModel>(FindObjectsInactive.Include);
    }

    private void Start()
    {
        // 시작 시 기본 활성 가챠(_gachaId)의 소모 재화 아이콘을 한 번 반영.
        RefreshCostIcon();
    }

    // ---------- 버튼 진입점 ----------

    // 메인 상점의 1회 뽑기 버튼 OnClick 에 연결
    public void OnClickSingleDraw() => TryDraw(1, _singleResultPopup, _singleSlots, _singleDecomposeView);

    // 메인 상점의 10회 뽑기 버튼 OnClick 에 연결
    public void OnClickTenDraw() => TryDraw(TenDrawCount, _tenResultPopup, _tenSlots, _tenDecomposeView);

    // 가챠 종류 전환(탭/선택 버튼 OnClick 에 가챠 ID 를 정적 인자로 연결).
    // 전환 후 천장 안내를 해당 가챠 기준으로 갱신한다.
    public void SetActiveGacha(int gachaId)
    {
        _gachaId = gachaId;
        RefreshCostIcon();   // 활성 가챠가 바뀌면 소모 재화 아이콘도 그 가챠에 맞춰 갱신
        if (_pityInfoView != null) _pityInfoView.Refresh(_gachaId);
        if (_progressView != null) _progressView.Refresh(_gachaId);
    }

    // 현재 활성 가챠(_gachaId)에 해당하는 비용 설정. 없으면 null.
    private GachaCostConfig GetCostConfig(int gachaId)
    {
        if (_gachaCosts == null) return null;
        for (int i = 0; i < _gachaCosts.Length; i++)
            if (_gachaCosts[i] != null && _gachaCosts[i].gachaId == gachaId)
                return _gachaCosts[i];
        return null;
    }

    // 현재 활성 가챠가 소모하는 재화 종류. 설정이 없으면 기본 재화로 폴백.
    private CostCurrency CurrentCurrency()
    {
        GachaCostConfig cfg = GetCostConfig(_gachaId);
        return cfg != null ? cfg.currency : _costCurrency;
    }

    // 현재 활성 가챠의 소모 재화 아이콘을 모든 아이콘 표시 대상에 반영.
    private void RefreshCostIcon()
    {
        if (_costIconTargets == null) return;

        GachaCostConfig cfg = GetCostConfig(_gachaId);
        Sprite icon = cfg != null ? cfg.costIcon : null;

        for (int i = 0; i < _costIconTargets.Length; i++)
        {
            Image img = _costIconTargets[i];
            if (img == null) continue;

            if (icon != null)
            {
                img.sprite = icon;
                img.enabled = true;
            }
            else
            {
                // 아이콘이 지정되지 않은 가챠면 숨겨서 다른 가챠 아이콘이 잘못 남지 않게 한다.
                img.enabled = false;
            }
        }
    }

    // [프리젠터 1개 공유 방식용] 버튼 OnClick 에 가챠 ID(1/2/3)를 정적 인자로 넘겨 그 가챠를 1회/10회 뽑는다.
    // (가챠별 프리젠터를 따로 두는 방식이면 이 두 메서드는 안 써도 된다.)
    public void DrawSingle(int gachaId)
    {
        SetActiveGacha(gachaId);
        OnClickSingleDraw();
    }

    public void DrawTen(int gachaId)
    {
        SetActiveGacha(gachaId);
        OnClickTenDraw();
    }

    // [광고 보상형 무료 뽑기] AdGachaController 가 호출한다.
    // 비용 차감 없음 + 천장(레전더리 확정) 제외 + 누적(마일리지) 제외.
    // 결과 표시는 기존 1회 결과창/슬롯을 그대로 재사용한다.
    // 정상적으로 1개 이상 뽑히면 true 반환(호출 측 쿨타임 시작 판단용).
    public bool FreeDrawSingle(int gachaId)
    {
        SetActiveGacha(gachaId);

        // 천장 미적용(usePity:false) — 무료뽑기는 천장 카운트에 포함하지 않는다.
        List<int> drawn = ExecuteDraw(_gachaId, 1, usePity: false);

        FillSlots(_singleSlots, drawn);

        // 누적 미집계(countMileage:false) — 무료뽑기는 마일리지 보상에 포함하지 않는다.
        ArtifactGachaResult post = ProcessDraw(_gachaId, drawn, countMileage: false);

        // 자동 분해 보상(한도 초과분)은 유료 뽑기와 동일하게 결과창에 표시한다.
        if (_singleDecomposeView != null)
            _singleDecomposeView.Show(post.TotalStone, post.TotalShard);

        ConfigureSingleResult(hideRedraw: true);   // 무료 뽑기는 추가 뽑기 숨김 + 확인 가운데

        if (_singleResultPopup != null)
            _singleResultPopup.SetActive(true);

        return drawn != null && drawn.Count > 0;
    }

    // 결과창 확인 버튼 OnClick 에 연결 (두 결과창 모두 닫음)
    public void OnClickConfirm()
    {
        if (_singleResultPopup != null) _singleResultPopup.SetActive(false);
        if (_tenResultPopup != null) _tenResultPopup.SetActive(false);

        // 결과 확인 → 메인 화면 복귀 시점 : 대기 중인 누적 보상 팝업을 순차 출력한다.
        // (뽑기 연출/결과 확인 도중에는 출력하지 않고, 여기서만 Flush. 자동 분해 보상은 결과 팝업에 이미 표시됨)
        if (_popupQueue != null) _popupQueue.Flush();
    }

    // 재화 부족 팝업 확인 버튼 OnClick 에 연결
    public void OnClickCloseInsufficient()
    {
        if (_insufficientPopup != null) _insufficientPopup.SetActive(false);
    }

    // ---------- 핵심 로직 ----------

    private void TryDraw(int count, GameObject resultPopup, GachaResultSlotView[] slots, GachaDecomposeRewardView decomposeView)
    {
        // 튜토리얼 뽑기는 비용 없이 마법사 인장 고정 지급
        if (IsTutorialGacha())
        {
            DrawTutorialFixed(resultPopup, slots, decomposeView);
            return;
        }

        int cost = GetCost(count);

        // 비용 0(무료/광고 전용 상자)은 일반 뽑기로 뽑을 수 없다. 무료 뽑기는 광고/쿨타임 흐름(FreeDrawSingle) 전용.
        // (이게 없으면 결과창 '1회 더'/상점 '1회 뽑기'로 무료 상자를 무한 뽑을 수 있다 — 이슈 #198)
        if (cost <= 0)
        {
            Debug.LogWarning($"[ShopGachaPresenter] 무료/비용 미설정 가챠(id:{_gachaId})는 일반 뽑기로 뽑을 수 없습니다. 광고/쿨타임 전용.", this);
            return;
        }

        // 재화 체크 & 차감
        if (!TrySpend(cost))
        {
            ShowInsufficient();
            return;
        }

        // 뽑기 실행 → 뽑힌 Gear_ID 목록 수신
        List<int> drawn = ExecuteDraw(_gachaId, count);

        FillSlots(slots, drawn);

        // 획득 반영(한도/자동분해/누적보상). 다중 뽑기의 각 획득 건을 순차적으로 정확히 계산.
        ArtifactGachaResult post = ProcessDraw(_gachaId, drawn, countMileage: true);

        // 자동 분해 보상 → 이 결과 팝업의 RewardPreviewSlot 에 표시(없으면 슬롯 전부 끔).
        if (decomposeView != null)
            decomposeView.Show(post.TotalStone, post.TotalShard);

        // 유료 뽑기는 추가 뽑기 버튼을 보이고 확인 버튼을 원위치로(무료 뽑기에서 바뀐 상태 복구).
        if (resultPopup == _singleResultPopup)
            ConfigureSingleResult(hideRedraw: false);

        // 누적 보상 → 결과 확인 후 메인 복귀 시점에 별도 팝업으로 출력하도록 큐에 적재.
        if (_popupQueue != null)
            _popupQueue.Enqueue(post);

        // 천장 안내 갱신(레전더리 가챠에서만 표시됨).
        if (_pityInfoView != null)
            _pityInfoView.Refresh(_gachaId);

        // 확정/누적 보상 진행도 갱신.
        if (_progressView != null)
            _progressView.Refresh(_gachaId);

        if (resultPopup != null)
            resultPopup.SetActive(true);
    }

    // 튜토리얼 가챠 단계(GachaButton 강조)인지
    private bool IsTutorialGacha()
    {
        TutorialManager tm = TutorialManager.Instance;
        if (tm == null || !tm.IsRunning) return false;
        TutorialStep step = tm.CurrentStep;
        return step != null && step.highlightTargetId == "GachaButton";
    }

    // 튜토리얼 전용 : 비용·천장·마일리지 없이 마법사 인장 1개 고정 지급
    private void DrawTutorialFixed(GameObject resultPopup, GachaResultSlotView[] slots, GachaDecomposeRewardView decomposeView)
    {
        if (_tutorialSealGearId <= 0)
        {
            Debug.LogWarning("[ShopGachaPresenter] 튜토리얼 인장 Gear_ID가 설정되지 않았습니다.", this);
            return;
        }

        var drawn = new List<int> { _tutorialSealGearId };
        FillSlots(slots, drawn);

        ArtifactGachaResult post = ProcessDraw(_gachaId, drawn, countMileage: false);
        if (decomposeView != null)
            decomposeView.Show(post.TotalStone, post.TotalShard);

        GrantTutorialUpgradeMaterials();

        ConfigureSingleResult(hideRedraw: true);   // 튜토 뽑기도 추가 뽑기 숨김 + 확인 가운데

        if (resultPopup != null)
            resultPopup.SetActive(true);
    }

    // 1회 결과창의 추가 뽑기 버튼 표시/숨김 + 확인 버튼 위치(가운데 정렬) 전환.
    private bool _confirmXCached;
    private float _confirmDefaultX;

    private void ConfigureSingleResult(bool hideRedraw)
    {
        if (_singleRedrawGroup != null)
            _singleRedrawGroup.SetActive(!hideRedraw);

        if (_singleConfirmRect != null)
        {
            if (!_confirmXCached)
            {
                _confirmDefaultX = _singleConfirmRect.anchoredPosition.x;
                _confirmXCached = true;
            }
            Vector2 p = _singleConfirmRect.anchoredPosition;
            p.x = hideRedraw ? 0f : _confirmDefaultX;
            _singleConfirmRect.anchoredPosition = p;
        }
    }

    // 튜토리얼 강화·돌파에 필요한 재료를 지급한다(강화석/골드/돌파용 중복 인장)
    private void GrantTutorialUpgradeMaterials()
    {
        ArtifactManager mgr = GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null;
        if (mgr != null)
        {
            mgr.AddStone(_tutorialGrantStone);          // 레벨업용 강화석
            mgr.AddArtifact(_tutorialSealGearId);       // 돌파용 중복 인장
        }

        if (GameManager.Instance != null && _tutorialGrantGold > 0)
            GameManager.Instance.AddCurrency(_tutorialGrantGold, 0, "튜토리얼 아티팩트 강화");
    }

    // 뽑힌 Gear_ID 목록을 유저 데이터에 반영하고 자동분해/누적보상 결과를 돌려준다.
    // countMileage=false 면 누적(마일리지) 보상 집계를 건너뛴다(광고 무료뽑기용).
    private ArtifactGachaResult ProcessDraw(int gachaId, List<int> drawn, bool countMileage)
    {
        if (_postProcessor != null)
            return _postProcessor.Process(gachaId, drawn, countMileage);

        // 폴백 : 후처리기 미연결 시 기존처럼 단순 보유 추가만 한다(자동분해/누적보상 미동작).
        ArtifactManager mgr = GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null;
        if (mgr != null && drawn != null)
            foreach (int id in drawn) mgr.AddArtifact(id);

        return new ArtifactGachaResult();
    }

    // 뽑기 실행 후 뽑힌 Gear_ID 목록을 반환한다(획득 반영은 ProcessDraw 에서 처리).
    //
    // [임시] GachaManager.ExecuteGacha 가 List<int> 를 반환하도록 바뀌면,
    // 아래 전체를 다음 한 줄로 교체
    // return _gachaManager.ExecuteGacha(gachaId, count);
    //
    private List<int> ExecuteDraw(int gachaId, int count, bool usePity = true)
    {
        var result = new List<int>();

        GachaBoxData box = DataManager.Instance != null && DataManager.Instance.GearRepo != null
            ? DataManager.Instance.GearRepo.GetGachaBox(gachaId)
            : null;
        if (box == null)
        {
            Debug.LogWarning($"[ShopGachaPresenter] GachaBox(id:{gachaId}) 데이터를 찾지 못했습니다.", this);
            return result;
        }

        // SFX 가이드 아웃게임 3: 뽑기 진행음. 10연차는 포탈 루프를 추가 재생(모든 뽑기 경로가 이 함수를 지난다).
        AudioManager.Play(SfxId.ArtifactGachaDraw);
        if (count >= 10) AudioManager.Play(SfxId.ArtifactGachaTen);

        // 천장(레전더리 확정) : 마지막 레전더리 이후 카운트가 PityCount 에 도달하면 그 개봉에서 확정.
        // 레전더리(자연/확정)가 나오면 카운트 0으로 리셋(기획서 6-2-1). 누적/마일리지 카운트와는 별개.
        // usePity=false(광고 무료뽑기)면 천장 적용 안 함 — 순수 확률 추첨만(카운트도 건드리지 않음).
        bool pityBox = usePity && box.PityType == "RandomLegendary" && box.PityCount > 0;

        // 천장 시작 카운트 : 트래커 우선, 없으면 누적뽑기 나머지로 폴백(리셋 미동작).
        int pity = 0;
        if (pityBox)
            pity = _pityTracker != null
                ? _pityTracker.GetCount(gachaId)
                : (_postProcessor != null ? _postProcessor.GetDrawTotal(gachaId) % box.PityCount : 0);

        for (int i = 0; i < count; i++)
        {
            // 이번 개봉이 천장 도달(카운트+1 == PityCount)이면 레전더리 확정, 아니면 확률 추첨.
            bool pityHit = pityBox && (pity + 1 >= box.PityCount);
            string grade = pityHit ? "Legendary" : DetermineGrade(box);

            int gearId = SelectRandomGearByGrade(grade);
            if (gearId == 0) continue;

            // 레전더리(자연/확정) → 천장 카운트 0 리셋, 그 외 → +1.
            if (pityBox)
                pity = (grade == "Legendary") ? 0 : pity + 1;

            result.Add(gearId);
            Debug.Log($"[ShopGachaPresenter] 가챠 성공! {grade} 등급 (ID: {gearId}){(pityHit ? " [천장 확정]" : "")} 획득");
        }

        // 갱신된 천장 카운트 영구 저장(트래커가 있을 때만).
        if (pityBox && _pityTracker != null)
            _pityTracker.SetCount(gachaId, pity);

        return result;
    }

    // GachaManager.DetermineGrade 와 동일 규칙(임시 복제)
    private string DetermineGrade(GachaBoxData box)
    {
        float rand = Random.value;
        if (rand < box.RareRate) return "Rare";
        if (rand < box.RareRate + box.EpicRate) return "Epic";
        return "Legendary";
    }

    // GachaManager.SelectRandomGearByGrade 와 동일 규칙(임시 복제, reflection 대신 SO 직접 참조)
    private int SelectRandomGearByGrade(string grade)
    {
        if (_gearTable == null || _gearTable.rows == null)
        {
            Debug.LogWarning("[ShopGachaPresenter] GearMasterTable SO 가 연결되지 않았습니다.", this);
            return 0;
        }

        var filtered = _gearTable.rows.Where(g => g != null && g.GearGrade == grade).ToList();
        if (filtered.Count == 0) return 0;

        return filtered[Random.Range(0, filtered.Count)].Gear_ID;
    }

    // 결과창 안의 '1회 더' 버튼 OnClick 에 연결 (같은 박스 재추첨)
    public void OnClickRedrawSingle() => TryDraw(1, _singleResultPopup, _singleSlots, _singleDecomposeView);

    // 결과창 안의 '10회 더' 버튼 OnClick 에 연결
    public void OnClickRedrawTen() => TryDraw(TenDrawCount, _tenResultPopup, _tenSlots, _tenDecomposeView);

    // 뽑기 비용은 전부 데이터에서 가져온다.
    //  1순위 : PaidGachaBox(가챠ID + 뽑기횟수) — 횟수별 비용/할인이 데이터에 정의된 경우.
    //  2순위 : GachaBox.CostAmount(1회 기준) x 횟수 — 횟수별 데이터가 없을 때의 데이터 폴백.
    //  둘 다 없으면 0(무료) 처리하고 경고.
    private int GetCost(int count)
    {
        GearRepo repo = DataManager.Instance != null ? DataManager.Instance.GearRepo : null;
        if (repo != null)
        {
            PaidGachaBoxData paid = repo.GetPaidGachaBox(_gachaId, count);
            if (paid != null && paid.CostAmount > 0)
                return paid.CostAmount;

            GachaBoxData box = repo.GetGachaBox(_gachaId);
            if (box != null && box.CostAmount > 0)
                return box.CostAmount * Mathf.Max(1, count);
        }

        Debug.LogWarning($"[ShopGachaPresenter] 가챠 비용 데이터를 찾지 못했습니다. gachaId={_gachaId}, count={count} → 무료(0) 처리", this);
        return 0;
    }

    private bool TrySpend(int cost)
    {
        if (cost <= 0) return true;// 비용 미설정 -> 무료 처리
        if (_currencyModel == null)
        {
            Debug.LogWarning("[ShopGachaPresenter] CurrencyModel 미연결 -> 재화 체크 생략(무료 진행)", this);
            return true;
        }

        // 현재 활성 가챠(_gachaId)의 재화로 결제 — 일반/고급 상자가 서로 다른 재화를 쓰도록 분리.
        switch (CurrentCurrency())
        {
            case CostCurrency.Gold:      return _currencyModel.SpendGold(cost);
            case CostCurrency.Parchment: return _currencyModel.SpendParchment(cost);
            case CostCurrency.Diamond: return _currencyModel.SpendDiamond(cost);// 추가 : 홍정옥 다이아 
            default:                     return _currencyModel.SpendGold(cost);
        }
    }

    private void ShowInsufficient()
    {
        if (_insufficientPopup != null)
            _insufficientPopup.SetActive(true);
        else
            Debug.LogWarning("[ShopGachaPresenter] 재화 부족 팝업이 연결되지 않았습니다.", this);
    }

    private void FillSlots(GachaResultSlotView[] slots, List<int> drawn)
    {
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            if (drawn != null && i < drawn.Count)
                slots[i].SetData(drawn[i]);
            else
                slots[i].Clear(); // 결과보다 슬롯이 많으면 나머지는 비움
        }
    }
}
