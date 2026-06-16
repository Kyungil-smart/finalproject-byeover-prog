using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// 작성자 : 홍정옥
// 설명   : 분해 팝업에서 쓰는 아티팩트 슬롯. (Decomposition_SlotList / Decomposition_SelectSlotList 공용)
//          공통(아이콘/배경/등급/돌파 테두리)은 베이스(ArtifactSlotView)를 그대로 쓰고,
//          분해 대상 한 개체(UniqueId)와 분해 가능 수량(spare)을 함께 들고 클릭 시 선택 토글을 발행한다.
//          베이스의 Clicked(상세 팝업용)는 쓰지 않고, 클릭 = 선택 토글로 동작한다.
public class ArtifactDecomposeSlotView : ArtifactSlotView
{
    [Header("분해 슬롯 추가 표시")]
    [Tooltip("선택됨 표시(테두리/체크 등). 선택 시 ON")]
    [SerializeField] private GameObject _selectedHighlight;
    [Tooltip("분해 가능 수량(보유 - 본체 1개) 표시. 없으면 비워둬도 됨")]
    [SerializeField] private TMP_Text _spareText;

    // 클릭 시 선택 토글 요청 (인자 = 이 슬롯의 UniqueId)
    public event Action<int> SelectionToggled;

    public int UniqueId { get; private set; }
    public int Spare { get; private set; }
    public ArtifactGrade Grade => _grade;

    // 보유 개체 하나를 슬롯에 채운다. spare = 분해 가능 수량(보유 수 - 본체 1개).
    public void Bind(ArtifactInstance instance, ArtifactGrade grade, int spare)
    {
        if (instance == null)
            return;

        UniqueId = instance.UniqueId;
        Spare = Mathf.Max(0, spare);

        SetData(instance.MasterData, grade);          // 아이콘 제외 공통 표시(배경/등급/테두리)
        SetAscensionBorder(instance.AscensionCount);

        if (_spareText != null)
            _spareText.text = $"x{Spare}";

        SetSelected(false);
    }

    // 선택 상태 표시만 갱신(데이터 토글은 프레젠터가 관리).
    public void SetSelected(bool selected)
    {
        if (_selectedHighlight != null)
            _selectedHighlight.SetActive(selected);
    }

    // 클릭 = 선택 토글. 베이스의 상세 팝업용 Clicked 는 발행하지 않는다.
    public override void OnPointerClick(PointerEventData eventData)
    {
        SelectionToggled?.Invoke(UniqueId);
    }
}
