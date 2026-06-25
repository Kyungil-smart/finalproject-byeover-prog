using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 홍정옥
// 설명   : 아티팩트 화면의 탭 전환 / 분해 모드 / 분해 팝업의 UI 동작을 담당하는 컨트롤러
//          - TopBar_inPlayerLevel : 캐릭터 레벨(PlayerLevel) / 아티팩트(Artifact) 탭 전환
//          - Artifact 탭          : Collection + Artifacts_Slot(기본) <-> DecompositionArtifacts_Slot(분해 모드)
//          - 분해 팝업            : POPUP_Decomposition(단일) / POPUP_BATCH_DECOMPOSITION(일괄)
//          선택 대상·개수·실제 분해 처리는 데이터 연동 담당 영역이며, 여기서는 event/public 메서드로 연결한다.
public class ArtifactUIController : MonoBehaviour
{
    // ------------------------------------------------------------------
    // 데이터 연동용 이벤트 훅
    // ------------------------------------------------------------------
    public event Action<bool> OnDecompositionModeChanged;     // true=분해 모드 진입, false=종료
    public event Action<ArtifactGrade> OnDecomposeGradeSelected; // 일괄 등급 버튼 선택
    public event Action OnDecomposeRequested;                 // 분해 완료(DCM) 클릭 → 데이터가 단일/일괄 팝업 결정
    public event Action<int> OnDecomposeCountChanged;         // 단일 팝업 슬라이더 개수 변경
    public event Action OnSingleDecomposeConfirmed;           // 단일 팝업 '분해' 클릭
    public event Action OnBatchDecomposeConfirmed;            // 일괄 팝업 '분해' 클릭
    public event Action OnDecomposeCancelled;                 // 팝업 '취소' 클릭
    public event Action<int> OnSortChanged;                  // 정렬 드롭다운 선택(옵션 인덱스)
    public event Action<int> OnFilterChanged;                // 필터 드롭다운 선택(옵션 인덱스)

    // ------------------------------------------------------------------
    // 참조 (Inspector에서 직접 연결)
    // ------------------------------------------------------------------
    [Header("탭 전환 (TopBar_inPlayerLevel)")]
    [SerializeField] private Button _btnCharLv;             // Button_CharLv
    [SerializeField] private Button _btnArtifact;           // Button_Artifact
    [SerializeField] private GameObject _playerLevelTab;    // PlayerLevel
    [SerializeField] private GameObject _artifactTab;       // Artifact

    [Header("Artifact 탭 구성")]
    [SerializeField] private GameObject _collection;        // Collection (Artifact 탭에서 항상 ON)
    [SerializeField] private GameObject _artifactsSlot;     // Artifacts_Slot (기본 ON, 분해 진입 시 OFF)
    [SerializeField] private GameObject _decompositionSlot; // DecompositionArtifacts_Slot (기본 OFF, 분해 진입 시 ON)

    [Header("분해 진입 / 완료 / 취소 버튼")]
    [SerializeField] private Button _btnEnterDecompose;     // Artifacts_Slot / Button_Decomposition
    [SerializeField] private Button _btnDecomposeComplete;  // Button_DismantlingComplete (DCM)
    [SerializeField] private Button _btnExitDecompose;      // TopBar_Decomposition / Button_Cancel (분해 모드 취소)

    [Header("일괄 등급 선택 버튼 (DecompositionArtifacts_Slot)")]
    [SerializeField] private Button _btnRare;               // Button_Rare
    [SerializeField] private Button _btnEpic;               // Button_Epic
    [SerializeField] private Button _btnLegendary;          // Button_Legendary

    [Header("단일 분해 팝업 (POPUP_Decomposition)")]
    [SerializeField] private GameObject _popupDecomposition;
    [SerializeField] private Slider _sliderDecompositionCount;  // Slider_DecompositionCount
    [SerializeField] private TMP_Text _textDecompositionCount;  // Text_DecompositionCount
    [SerializeField] private Button _btnSingleDecompose;        // 단일 팝업 '분해'
    [SerializeField] private Button _btnSingleCancel;           // 단일 팝업 '취소'

    [Header("일괄 분해 팝업 (POPUP_BATCH_DECOMPOSITION)")]
    [SerializeField] private GameObject _popupBatchDecomposition;
    [SerializeField] private Button _btnBatchDecompose;         // 일괄 팝업 '분해'
    [SerializeField] private Button _btnBatchCancel;            // 일괄 팝업 '취소'

    [Header("정렬 / 필터 드롭다운 (Artifacts_Slot)")]
    [SerializeField] private TMP_Dropdown _sortingDropdown;     // Dropdown_Artifacts_Slot_Sorting (정렬)
    [SerializeField] private TMP_Dropdown _filterDropdown;      // Dropdown_Artifacts_Slot_Filter (필터)
    [Tooltip("탭 진입 시 사용자가 따로 바꾸지 않았다면 적용할 기본 정렬 옵션 인덱스")]
    [SerializeField] private int _defaultSortingIndex = 0;
    [Tooltip("탭 진입 시 사용자가 따로 바꾸지 않았다면 적용할 기본 필터 옵션 인덱스")]
    [SerializeField] private int _defaultFilterIndex = 0;

    private bool _isDecompositionMode;
    private bool _sortTouched;     // 사용자가 정렬을 직접 변경한 적이 있는가
    private bool _filterTouched;   // 사용자가 필터를 직접 변경한 적이 있는가

    private void Awake()
    {
        BindButtons();
        BindSlider();
        SetupDropdowns();
    }

    private void Start()
    {
        // Artifact 탭은 기본 상태(Artifacts_Slot 노출, 팝업 닫힘)로 정리하고,
        // 페이지 기본 탭은 PlayerLevel 로 시작한다.
        ResetArtifactTabToDefault(notify: false);
        SetActive(_playerLevelTab, true);
        SetActive(_artifactTab, false);
    }

    // ==================================================================
    // 버튼 / 슬라이더 바인딩
    // ==================================================================
    private void BindButtons()
    {
        // 탭 전환
        Bind(_btnCharLv, ShowPlayerLevelTab, "Button_CharLv");
        Bind(_btnArtifact, ShowArtifactTab, "Button_Artifact");

        // 분해 진입 / 완료 / 취소(모드 종료)
        Bind(_btnEnterDecompose, EnterDecompositionMode, "Button_Decomposition");
        Bind(_btnDecomposeComplete, RequestDecompose, "Button_DismantlingComplete");
        Bind(_btnExitDecompose, ExitDecompositionMode, "분해 모드 취소");

        // 일괄 등급 선택
        Bind(_btnRare, () => SelectGrade(ArtifactGrade.Rare), "Button_Rare");
        Bind(_btnEpic, () => SelectGrade(ArtifactGrade.Epic), "Button_Epic");
        Bind(_btnLegendary, () => SelectGrade(ArtifactGrade.Legendary), "Button_Legendary");

        // 단일 분해 팝업
        Bind(_btnSingleDecompose, ConfirmSingleDecompose, "단일 분해");
        Bind(_btnSingleCancel, CancelDecompose, "단일 취소");

        // 일괄 분해 팝업
        Bind(_btnBatchDecompose, ConfirmBatchDecompose, "일괄 분해");
        Bind(_btnBatchCancel, CancelDecompose, "일괄 취소");
    }

    private void BindSlider()
    {
        if (_sliderDecompositionCount == null)
            return;

        _sliderDecompositionCount.wholeNumbers = true;
        _sliderDecompositionCount.onValueChanged.RemoveListener(HandleSliderChanged);
        _sliderDecompositionCount.onValueChanged.AddListener(HandleSliderChanged);
    }

    private void Bind(Button button, UnityEngine.Events.UnityAction action, string label)
    {
        if (button == null)
        {
            Debug.LogWarning($"[ArtifactUIController] '{label}' 버튼이 연결되지 않았습니다.", this);
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    // ==================================================================
    // 탭 전환 (TopBar_inPlayerLevel)
    // ==================================================================
    public void ShowPlayerLevelTab()
    {
        if (_isDecompositionMode)
            ApplyDecompositionMode(false, notify: true);

        SetActive(_playerLevelTab, true);
        SetActive(_artifactTab, false);
    }

    public void ShowArtifactTab()
    {
        SetActive(_playerLevelTab, false);
        SetActive(_artifactTab, true);

        // Artifact 탭은 항상 기본 상태(Collection + Artifacts_Slot)로 진입한다.
        ResetArtifactTabToDefault(notify: _isDecompositionMode);

        // 사용자가 정렬/필터를 따로 바꾸지 않았다면 기본값으로 표시한다.
        ApplySortFilterDefaultsIfUntouched();
    }

    // ==================================================================
    // 정렬 / 필터 드롭다운 (Artifacts_Slot)
    // ==================================================================
    private void SetupDropdowns()
    {
        if (_sortingDropdown != null)
        {
            _sortingDropdown.SetValueWithoutNotify(ClampOption(_sortingDropdown, _defaultSortingIndex));
            _sortingDropdown.onValueChanged.RemoveListener(HandleSortChanged);
            _sortingDropdown.onValueChanged.AddListener(HandleSortChanged);
        }
        else
        {
            Debug.LogWarning("[ArtifactUIController] 정렬 드롭다운이 연결되지 않았습니다.", this);
        }

        if (_filterDropdown != null)
        {
            _filterDropdown.SetValueWithoutNotify(ClampOption(_filterDropdown, _defaultFilterIndex));
            _filterDropdown.onValueChanged.RemoveListener(HandleFilterChanged);
            _filterDropdown.onValueChanged.AddListener(HandleFilterChanged);
        }
        else
        {
            Debug.LogWarning("[ArtifactUIController] 필터 드롭다운이 연결되지 않았습니다.", this);
        }
    }

    // 탭 진입 시 : 사용자가 직접 바꾼 적이 없으면 기본 정렬/필터를 표시하고 데이터 쪽에도 적용시킨다.
    private void ApplySortFilterDefaultsIfUntouched()
    {
        if (!_sortTouched && _sortingDropdown != null)
        {
            int index = ClampOption(_sortingDropdown, _defaultSortingIndex);
            _sortingDropdown.SetValueWithoutNotify(index);
            OnSortChanged?.Invoke(index);
        }

        if (!_filterTouched && _filterDropdown != null)
        {
            int index = ClampOption(_filterDropdown, _defaultFilterIndex);
            _filterDropdown.SetValueWithoutNotify(index);
            OnFilterChanged?.Invoke(index);
        }
    }

    private void HandleSortChanged(int index)
    {
        _sortTouched = true;
        Debug.Log($"[ArtifactUIController] 정렬 변경 → {index}");
        OnSortChanged?.Invoke(index);
    }

    private void HandleFilterChanged(int index)
    {
        _filterTouched = true;
        Debug.Log($"[ArtifactUIController] 필터 변경 → {index}");
        OnFilterChanged?.Invoke(index);
    }

    private static int ClampOption(TMP_Dropdown dropdown, int index)
    {
        int count = dropdown.options.Count;
        return count > 0 ? Mathf.Clamp(index, 0, count - 1) : 0;
    }

    // ==================================================================
    // 분해 모드 (Artifacts_Slot <-> DecompositionArtifacts_Slot)
    // ==================================================================
    public void EnterDecompositionMode() => ApplyDecompositionMode(true, notify: true);

    public void ExitDecompositionMode() => ApplyDecompositionMode(false, notify: true);

    private void ResetArtifactTabToDefault(bool notify)
    {
        SetActive(_collection, true);
        ApplyDecompositionMode(false, notify);
    }

    private void ApplyDecompositionMode(bool enter, bool notify)
    {
        _isDecompositionMode = enter;

        CloseDecomposePopups();

        if (notify)
            OnDecompositionModeChanged?.Invoke(enter);
    }

    // 일괄 등급 선택 (DecompositionArtifacts_Slot 의 등급 버튼)
    private void SelectGrade(ArtifactGrade grade)
    {
        Debug.Log($"[ArtifactUIController] 일괄 분해 등급 선택 → {ArtifactGradeInfo.DisplayName(grade)}");
        OnDecomposeGradeSelected?.Invoke(grade);
    }

    // 분해 완료(DCM) : 단일/일괄 판정은 데이터가 선택 상태를 보고 결정 → 해당 팝업 Open 메서드 호출
    private void RequestDecompose()
    {
        Debug.Log("[ArtifactUIController] 분해 완료(DCM) 클릭 → 분해 팝업 요청");
        OnDecomposeRequested?.Invoke();
    }

    // ==================================================================
    // 단일 분해 팝업 (POPUP_Decomposition)
    // ==================================================================
    // 데이터에서 호출. spareCount = 선택한 아티팩트의 분해 가능 잔여 수량
    //  - 1개 이하  : 슬라이더 비활성(개수 고정 1)
    //  - 2개 이상  : 슬라이더로 개수 조절 가능
    public void OpenSingleDecomposePopup(int spareCount)
    {
        CloseDecomposePopups();

        int max = Mathf.Max(1, spareCount);
        bool adjustable = max >= 2;

        if (_sliderDecompositionCount != null)
        {
            _sliderDecompositionCount.minValue = 1;
            _sliderDecompositionCount.maxValue = max;
            _sliderDecompositionCount.SetValueWithoutNotify(max);
            _sliderDecompositionCount.interactable = adjustable;
            SetActive(_sliderDecompositionCount.gameObject, adjustable);
        }

        UpdateCountText(max);
        SetActive(_popupDecomposition, true);
    }

    private void HandleSliderChanged(float value)
    {
        int count = Mathf.RoundToInt(value);
        UpdateCountText(count);
        OnDecomposeCountChanged?.Invoke(count);
    }

    private void UpdateCountText(int count)
    {
        if (_textDecompositionCount != null)
            _textDecompositionCount.text = count.ToString();
    }

    private void ConfirmSingleDecompose()
    {
        Debug.Log("[ArtifactUIController] 단일 분해 확정");
        OnSingleDecomposeConfirmed?.Invoke();
        CloseDecomposePopups();
    }

    // ==================================================================
    // 일괄 분해 팝업 (POPUP_BATCH_DECOMPOSITION)
    // ==================================================================
    public void OpenBatchDecomposePopup()
    {
        CloseDecomposePopups();
        SetActive(_popupBatchDecomposition, true);
    }

    private void ConfirmBatchDecompose()
    {
        Debug.Log("[ArtifactUIController] 일괄 분해 확정");
        OnBatchDecomposeConfirmed?.Invoke();
        CloseDecomposePopups();
    }

    // ==================================================================
    // 공통 : 취소 / 팝업 닫기
    // ==================================================================
    private void CancelDecompose()
    {
        OnDecomposeCancelled?.Invoke();
        CloseDecomposePopups();
    }

    public void CloseDecomposePopups()
    {
        SetActive(_popupDecomposition, false);
        SetActive(_popupBatchDecomposition, false);
    }

    // ==================================================================
    // 헬퍼
    // ==================================================================
    private static void SetActive(GameObject go, bool value)
    {
        if (go != null && go.activeSelf != value)
            go.SetActive(value);
    }
}
