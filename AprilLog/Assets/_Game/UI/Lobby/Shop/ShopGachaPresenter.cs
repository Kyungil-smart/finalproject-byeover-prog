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

    [Header("10회 결과창")]
    [SerializeField] private GameObject _tenResultPopup;
    [Tooltip("미리 배치해 둔 10회 결과 슬롯(10칸)")]
    [SerializeField] private GachaResultSlotView[] _tenSlots;

    [Header("재화 부족 팝업")]
    [SerializeField] private GameObject _insufficientPopup;

    [Header("뽑기 후처리 / 결과 팝업")]
    [Tooltip("획득 반영·등급별 한도·자동 분해·누적 보상 처리를 담당. 비우면 폴백으로 단순 보유 추가만 한다.")]
    [SerializeField] private ArtifactGachaPostProcessor _postProcessor;
    [Tooltip("메인 복귀 시 자동분해/누적보상 팝업을 순차 출력하는 큐(선택).")]
    [SerializeField] private ArtifactGachaPopupQueue _popupQueue;

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
    public void OnClickSingleDraw() => TryDraw(1, _singleResultPopup, _singleSlots);

    // 메인 상점의 10회 뽑기 버튼 OnClick 에 연결
    public void OnClickTenDraw() => TryDraw(TenDrawCount, _tenResultPopup, _tenSlots);

    // 결과창 확인 버튼 OnClick 에 연결 (두 결과창 모두 닫음)
    public void OnClickConfirm()
    {
        if (_singleResultPopup != null) _singleResultPopup.SetActive(false);
        if (_tenResultPopup != null) _tenResultPopup.SetActive(false);

        // 결과 확인 → 메인 화면 복귀 시점 : 대기 중인 자동 분해/누적 보상 팝업을 순차 출력한다.
        // (뽑기 연출 도중에는 출력하지 않고, 여기서만 Flush)
        if (_popupQueue != null) _popupQueue.Flush();
    }

    // 재화 부족 팝업 확인 버튼 OnClick 에 연결
    public void OnClickCloseInsufficient()
    {
        if (_insufficientPopup != null) _insufficientPopup.SetActive(false);
    }

    // ---------- 핵심 로직 ----------

    private void TryDraw(int count, GameObject resultPopup, GachaResultSlotView[] slots)
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

        if (resultPopup != null)
            resultPopup.SetActive(true);
    }

    // 뽑기 실행 후 뽑힌 Gear_ID 목록을 반환한다.
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

        for (int i = 0; i < count; i++)
        {
            string grade = DetermineGrade(box);
            int gearId = SelectRandomGearByGrade(grade);
            if (gearId == 0) continue;

            result.Add(gearId);
            Debug.Log($"[ShopGachaPresenter] 가챠 성공! {grade} 등급 (ID: {gearId}) 획득");
        }

        // 획득 반영(등급별 최대 보유 한도 / 초과분 자동 분해 / 누적 보상)은 후처리기에 위임한다.
        // 다중 뽑기의 각 획득 건을 순차적으로 정확히 계산하고, 결과를 큐에 누적해 둔다.
        if (_postProcessor != null)
        {
            ArtifactGachaResult post = _postProcessor.Process(gachaId, result);
            if (_popupQueue != null) _popupQueue.Enqueue(post);
        }
        else
        {
            // 폴백 : 후처리기 미연결 시 기존처럼 단순 보유 추가만 한다(자동분해/누적보상 미동작).
            ArtifactManager mgr = GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null;
            foreach (int id in result) mgr?.AddArtifact(id);
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
    public void OnClickRedrawSingle() => TryDraw(1, _singleResultPopup, _singleSlots);

    // 결과창 안의 '10회 더' 버튼 OnClick 에 연결
    public void OnClickRedrawTen() => TryDraw(TenDrawCount, _tenResultPopup, _tenSlots);

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
