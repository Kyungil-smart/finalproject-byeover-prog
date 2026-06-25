using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 작성자 : 홍정옥
// 설명   : 상점 페이지의 뽑기 UI 컨트롤러
//          - 1회/10회 뽑기 버튼 -> 재화 체크 -> GachaManager 실행 -> 결과창 슬롯 채우기
//          - 재화가 없으면 재화 부족 팝업 표시
//          - 결과창의 1회/10회 버튼으로 같은 박스 재추첨, 확인 버튼으로 닫기
public class ShopGachaPresenter : MonoBehaviour
{
    public enum CostCurrency { Gold, Parchment, Diamond }

    [Header("시스템 참조")]
    [Tooltip("씬의 GachaManager")]
    [SerializeField] private GachaManager _gachaManager;
    [Tooltip("씬의 CurrencyModel. 비우면 자동 탐색")]
    [SerializeField] private CurrencyModel _currencyModel;
    [Tooltip("등급별 기어 추첨용 GearMasterTable SO. 현재 추첨 데이터 소스라 필수(연결 유지).")]
    [SerializeField] private GearMasterTable _gearTable;

    [Header("뽑기 박스 ID (GachaBoxTable 의 Gacha_ID)")]
    [SerializeField] private int _gachaId = 1;

    [Header("임시 비용 (CostAmount 데이터가 채워지기 전까지 사용)")]
    [SerializeField] private CostCurrency _costCurrency = CostCurrency.Gold;
    [Tooltip("1회 뽑기 비용. 데이터의 CostAmount 가 0 보다 크면 그 값을 우선 사용한다.")]
    [SerializeField] private int _singleCostFallback = 500;
    [Tooltip("10회 뽑기 비용. 0 이면 1회 비용 x 10 으로 계산")]
    [SerializeField] private int _tenCostFallback = 0;

    [Header("1회 결과창")]
    [SerializeField] private GameObject _singleResultPopup;
    [Tooltip("미리 배치해 둔 1회 결과 슬롯(1칸)")]
    [SerializeField] private GachaResultSlotView[] _singleSlots;
    [Tooltip("POPUP_OnceGacha 의 자동 분해 보상 표시(RewardPreviewSlot). 선택.")]
    [SerializeField] private GachaDecomposeRewardView _singleDecomposeView;

    [Header("10회 결과창")]
    [SerializeField] private GameObject _tenResultPopup;
    [Tooltip("미리 배치해 둔 10회 결과 슬롯(10칸)")]
    [SerializeField] private GachaResultSlotView[] _tenSlots;
    [Tooltip("POPUP_TenGacha 의 자동 분해 보상 표시(RewardPreviewSlot 2개). 선택.")]
    [SerializeField] private GachaDecomposeRewardView _tenDecomposeView;

    [Header("재화 부족 팝업")]
    [SerializeField] private GameObject _insufficientPopup;

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
        if (_pityInfoView != null) _pityInfoView.Refresh(_gachaId);
        if (_progressView != null) _progressView.Refresh(_gachaId);
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
        int cost = GetCost(count);

        // 재화 체크 & 차감 (비용 0 이면 항상 통과)
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

    private int GetCost(int count)
    {
        // 데이터(CostAmount)가 채워지면 우선 사용, 아니면 임시 비용 사용
        int singleCost = _singleCostFallback;
        if (DataManager.Instance != null && DataManager.Instance.GearRepo != null)
        {
            GachaBoxData box = DataManager.Instance.GearRepo.GetGachaBox(_gachaId);
            if (box != null && box.CostAmount > 0)
                singleCost = box.CostAmount;
        }

        if (count <= 1)
            return singleCost;

        return _tenCostFallback > 0 ? _tenCostFallback : singleCost * count;
    }

    private bool TrySpend(int cost)
    {
        if (cost <= 0) return true;// 비용 미설정 -> 무료 처리
        if (_currencyModel == null)
        {
            Debug.LogWarning("[ShopGachaPresenter] CurrencyModel 미연결 -> 재화 체크 생략(무료 진행)", this);
            return true;
        }

        switch (_costCurrency)
        {
            case CostCurrency.Gold:      return _currencyModel.SpendGold(cost);
            case CostCurrency.Parchment: return _currencyModel.SpendParchment(cost);
            case CostCurrency.Diamond:   return SpendDiamond(cost);
            default:                     return _currencyModel.SpendGold(cost);
        }
    }

    // [다이아 결제] 다이아 재화는 정승우님 담당 지갑(GameManager/CurrencyModel)에 신규 추가 예정.
    // 지갑 API 가 생기면 아래 한 줄로 교체한다 :  return _currencyModel.SpendDiamond(cost);
    // 그 전까지는 결제 실패로 처리해 무료 뽑기로 새지 않게 한다(fail-closed).
    private bool SpendDiamond(int cost)
    {
        Debug.LogWarning("[ShopGachaPresenter] 다이아 재화 미구현 — CurrencyModel.SpendDiamond 추가 후 연결 필요. 결제 실패 처리.", this);
        return false;
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
