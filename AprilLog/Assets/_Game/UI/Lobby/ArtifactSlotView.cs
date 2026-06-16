using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 작성자 : 홍정옥
// 설명   : 아티팩트 슬롯 공통 베이스. (기획서 3-4)
//          공통 표시 = 아이콘 / 배경(등급 색) / 등급 / 돌파 테두리.
//          단일 클릭 시 Clicked 이벤트를 발행한다(기획서 4: 단일 선택 → 상세 정보 팝업).
//          장착용(ArtifactEquipSlotView) / 리스트용(ArtifactListSlotView) 이 상속해 각자 표시를 추가한다.
public abstract class ArtifactSlotView : MonoBehaviour, IPointerClickHandler
{
    [Header("공통 표시")]
    [SerializeField] protected Image _iconImage;        // 아이콘
    [SerializeField] protected Image _gradeBackground;  // 배경 = 등급 색 (등급은 색상으로 구분)
    [Tooltip("돌파 단계에 따라 색이 바뀌는 테두리 (기획서 3-4-2). 미돌파(0돌)는 등급 색.")]
    [SerializeField] protected Image _ascensionBorder;  // 돌파 테두리

    // 슬롯 단일 클릭 → 상세 정보 팝업용 (인자 = Gear_ID)
    public event Action<int> Clicked;

    public int GearId { get; private set; }
    protected ArtifactGrade _grade;

    // 돌파 단계별 테두리 색 (기획서 3-4-2). index 1~5 = 1~5돌, 0돌은 테두리 숨김
    private static readonly Color[] AscensionBorderColors =
    {
        Hex("#00F0FF"), // 1돌 
        Hex("#FF00FF"), // 2돌 
        Hex("#FF6B00"), // 3돌 
        Hex("#FFD600"), // 4돌 
        Hex("#FF003C"), // 5돌 
    };

    // 마스터 데이터 기준 표시(등급은 배경 색으로 구분). 테두리는 기본값(미돌파)=등급 색으로 초기화.
    public virtual void SetData(GearMasterData data, ArtifactGrade grade)
    {
        if (data == null)
            return;

        GearId = data.Gear_ID;
        _grade = grade;

        if (_gradeBackground != null) _gradeBackground.color = ArtifactGradeInfo.SlotColor(grade);
        SetAscensionBorder(0);
    }

    // 돌파 테두리 색 (유저 상태). 1~5돌은 단계 색, 미돌파(0돌)는 해당 아티팩트 등급 색.
    public void SetAscensionBorder(int ascensionStage)
    {
        if (_ascensionBorder == null)
            return;

        bool ascended = ascensionStage >= 1 && ascensionStage <= AscensionBorderColors.Length;
        _ascensionBorder.enabled = true;
        _ascensionBorder.color = ascended
            ? AscensionBorderColors[ascensionStage - 1]
            : ArtifactGradeInfo.SlotColor(_grade);
    }

    // 아이콘. 아이콘 소스 연동 시 호출.
    public void SetIcon(Sprite icon)
    {
        if (_iconImage != null && icon != null)
            _iconImage.sprite = icon;
    }
    
    public virtual void OnPointerClick(PointerEventData eventData)
    {
        Clicked?.Invoke(GearId);
    }

    protected static Color Hex(string hex)
    {
        return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.white;
    }
}
