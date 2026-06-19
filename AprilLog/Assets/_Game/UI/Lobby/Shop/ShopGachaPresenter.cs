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
    public enum CostCurrency { Gold, Parchment }

    [Header("시스템 참조")]
    [Tooltip("씬의 GachaManager. 비우면 자동 탐색 (CDH 반환 추가 후 사용)")]
    [SerializeField] private GachaManager _gachaManager;
    [Tooltip("씬의 CurrencyModel. 비우면 자동 탐색")]
    [SerializeField] private CurrencyModel _currencyModel;
    [Tooltip("[임시] 등급별 기어 추첨용 GearMasterTable SO. CDH가 ExecuteGacha 반환을 추가하면 더 이상 필요 없음")]
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
    [Tooltip("레전더리 확정(천장) 안내 표시(Confirmed Gacha Information). 선택.")]
    [SerializeField] private GachaPityInfoView _pityInfoView;

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
        ArtifactGachaResult post = ProcessDraw(_gachaId, drawn);

        // 자동 분해 보상 → 이 결과 팝업의 RewardPreviewSlot 에 표시(없으면 슬롯 전부 끔).
        if (decomposeView != null)
            decomposeView.Show(post.TotalStone, post.TotalShard);

        // 누적 보상 → 결과 확인 후 메인 복귀 시점에 별도 팝업으로 출력하도록 큐에 적재.
        if (_popupQueue != null)
            _popupQueue.Enqueue(post);

        // 천장 안내 갱신(레전더리 가챠에서만 표시됨).
        if (_pityInfoView != null)
            _pityInfoView.Refresh(_gachaId);

        if (resultPopup != null)
            resultPopup.SetActive(true);
    }

    // 뽑힌 Gear_ID 목록을 유저 데이터에 반영하고 자동분해/누적보상 결과를 돌려준다.
    private ArtifactGachaResult ProcessDraw(int gachaId, List<int> drawn)
    {
        if (_postProcessor != null)
            return _postProcessor.Process(gachaId, drawn);

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
    private List<int> ExecuteDraw(int gachaId, int count)
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

        // 천장(레전더리 확정) : 레전더리 박스에서 누적 N번째 뽑기마다 레전더리 확정.
        bool pityBox = box.PityType == "RandomLegendary" && box.PityCount > 0;
        int preTotal = (pityBox && _postProcessor != null) ? _postProcessor.GetDrawTotal(gachaId) : 0;

        for (int i = 0; i < count; i++)
        {
            // 이번 뽑기의 누적 절대 횟수가 천장 배수면 레전더리 확정, 아니면 확률 추첨.
            bool pityHit = pityBox && ((preTotal + i + 1) % box.PityCount == 0);
            string grade = pityHit ? "Legendary" : DetermineGrade(box);

            int gearId = SelectRandomGearByGrade(grade);
            if (gearId == 0) continue;

            result.Add(gearId);
            Debug.Log($"[ShopGachaPresenter] 가챠 성공! {grade} 등급 (ID: {gearId}){(pityHit ? " [천장 확정]" : "")} 획득");
        }

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

        return _costCurrency == CostCurrency.Gold
            ? _currencyModel.SpendGold(cost)
            : _currencyModel.SpendParchment(cost);
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
