using System;
using System.Collections.Generic;
using UnityEngine;

// 작성자 : 홍정옥
// 설명   : 아티팩트 마스터 데이터를 슬롯 리스트로 생성하고 정렬을 적용한다.
//          데이터는 GearMasterTable SO(데이터 에셋)를 직접 참조해 읽는다(다른 담당자 스크립트 비수정).
//          ArtifactUIController 의 정렬 드롭다운 이벤트를 구독해 재정렬한다.
//          이름은 LocalizationManager 가 연결돼 있으면 키로 조회하고, 없으면 'Grade #ID' 로 표시한다.
public class ArtifactListBinder : MonoBehaviour
{
    // 정렬 옵션 인덱스 = Dropdown_Artifacts_Slot_Sorting 의 옵션 순서와 동일해야 한다.
    // 0 기본 / 1 등급 높은순 / 2 등급 낮은순 / 3 이름순 / 4 공격력순 / 5 HP순
    public enum SortType { Default, GradeDesc, GradeAsc, Name, Attack, Hp }

    // 슬롯 단일 클릭 → 상세 정보 팝업용 (인자 = Gear_ID). 상세 팝업 표시는 데이터/상세 프레젠터가 구독.
    public event Action<int> OnSlotClicked;

    [Header("연동")]
    [SerializeField] private ArtifactUIController _controller;  // 정렬 이벤트 구독
    [SerializeField] private Transform _content;               // 슬롯이 생성될 부모(Content)
    [SerializeField] private ArtifactListSlotView _slotPrefab; // 리스트 슬롯 카드 프리팹

    [Header("데이터 소스")]
    [Tooltip("기어 마스터 데이터 테이블 SO (Assets/_Project/Data/SO/GearMasterTable.asset)")]
    [SerializeField] private GearMasterTable _gearTable;

    [Header("이름 표시 (선택)")]
    [Tooltip("연결하면 GearName 을 현지화 키로 조회. 비우면 'Grade #ID' 로 표시한다.")]
    [SerializeField] private LocalizationManager _localization;
    [SerializeField] private string _nameKeyPrefix = "GEAR_NAME_";

    private readonly List<ArtifactListSlotView> _slots = new List<ArtifactListSlotView>();
    private List<GearMasterData> _gears = new List<GearMasterData>();
    private SortType _currentSort = SortType.Default;

    // 보유 중인 Gear_ID 집합. null 이면 보유 정보가 아직 없어 전부 보유로 간주(딤 없음).
    private HashSet<int> _ownedGearIds;

    private void OnEnable()
    {
        if (_controller != null)
            _controller.OnSortChanged += HandleSortChanged;
    }

    private void OnDisable()
    {
        if (_controller != null)
            _controller.OnSortChanged -= HandleSortChanged;
    }

    private void Start()
    {
        Build();
    }

    // 데이터 로드 -> 정렬 -> 슬롯 재생성
    public void Build()
    {
        if (_gearTable == null || _gearTable.rows == null)
        {
            Debug.LogWarning("[ArtifactListBinder] GearMasterTable SO 가 연결되지 않았습니다.", this);
            return;
        }

        _gears = new List<GearMasterData>(_gearTable.rows.Count);
        for (int i = 0; i < _gearTable.rows.Count; i++)
        {
            GearMasterData gear = _gearTable.rows[i];
            if (gear != null)
                _gears.Add(gear);
        }
        Debug.Log($"[ArtifactListBinder] 기어 {_gears.Count}개 로드");

        SortGears();
        Rebuild();
    }

    private void HandleSortChanged(int index)
    {
        _currentSort = (SortType)Mathf.Clamp(index, 0, (int)SortType.Hp);
        SortGears();
        Rebuild();
    }

    // 슬롯 클릭 → 상세 정보 팝업 요청을 외부(상세 프레젠터)로 전달.
    private void HandleSlotClicked(int gearId)
    {
        Debug.Log($"[ArtifactListBinder] 슬롯 클릭 → Gear {gearId} 상세 정보 요청");
        OnSlotClicked?.Invoke(gearId);
    }

    private void SortGears()
    {
        switch (_currentSort)
        {
            case SortType.GradeDesc:
                _gears.Sort((a, b) => GradeRank(b).CompareTo(GradeRank(a)));
                break;
            case SortType.GradeAsc:
                _gears.Sort((a, b) => GradeRank(a).CompareTo(GradeRank(b)));
                break;
            case SortType.Name:
                _gears.Sort((a, b) => string.Compare(ResolveName(a), ResolveName(b), StringComparison.Ordinal));
                break;
            case SortType.Attack:
                _gears.Sort((a, b) => b.AttackBaseAmount.CompareTo(a.AttackBaseAmount));
                break;
            case SortType.Hp:
                _gears.Sort((a, b) => b.MaxHPBaseAmount.CompareTo(a.MaxHPBaseAmount));
                break;
            default:
                _gears.Sort((a, b) => a.Gear_ID.CompareTo(b.Gear_ID));
                break;
        }
    }

    private void Rebuild()
    {
        if (_content == null || _slotPrefab == null)
        {
            Debug.LogWarning("[ArtifactListBinder] Content 또는 슬롯 프리팹이 연결되지 않았습니다.", this);
            return;
        }

        for (int i = _slots.Count - 1; i >= 0; i--)
        {
            if (_slots[i] != null)
            {
                _slots[i].Clicked -= HandleSlotClicked;
                Destroy(_slots[i].gameObject);
            }
        }
        _slots.Clear();

        for (int i = 0; i < _gears.Count; i++)
        {
            GearMasterData gear = _gears[i];
            ArtifactListSlotView slot = Instantiate(_slotPrefab, _content);
            slot.SetData(gear, ToGrade(gear.GearGrade));
            slot.SetOwned(IsOwned(gear.Gear_ID));
            slot.Clicked += HandleSlotClicked;
            // 돌파 테두리/게이지(SetAscensionBorder·SetGauge)·아이콘은 인벤토리/아이콘 소스 준비 후 채운다.
            _slots.Add(slot);
        }
    }

    // 인벤토리 데이터 연동 시 호출 : 보유 중인 Gear_ID 목록을 넘기면 미보유 슬롯에 딤을 적용한다.
    public void SetOwnedGearIds(IEnumerable<int> ownedGearIds)
    {
        _ownedGearIds = ownedGearIds != null ? new HashSet<int>(ownedGearIds) : null;

        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i] != null)
                _slots[i].SetOwned(IsOwned(_slots[i].GearId));
        }
    }

    // 보유 정보가 없으면(null) 전부 보유로 간주(딤 없음).
    private bool IsOwned(int gearId) => _ownedGearIds == null || _ownedGearIds.Contains(gearId);

    private string ResolveName(GearMasterData gear)
    {
        if (_localization != null)
            return _localization.Get(_nameKeyPrefix + gear.Gear_ID);

        // 현지화 키 규칙이 정해지기 전 임시 표시(슬롯/정렬 검증용).
        return $"{gear.GearGrade} #{gear.Gear_ID}";
    }

    private static ArtifactGrade ToGrade(string gradeName)
    {
        return Enum.TryParse(gradeName, out ArtifactGrade grade) ? grade : ArtifactGrade.Rare;
    }

    private static int GradeRank(GearMasterData gear) => (int)ToGrade(gear.GearGrade);
}
