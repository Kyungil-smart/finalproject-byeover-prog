using TMPro;
using UnityEngine;

// 작성자 : 홍정옥
// 설명   : 장착용 아티팩트 슬롯. 공통(아이콘/배경/등급/돌파 테두리) + 레벨 + 돌파 수(기획서 3-4-3).
//          보유개수 게이지는 표시하지 않는다.
public class ArtifactEquipSlotView : ArtifactSlotView
{
    [Header("장착 슬롯 추가 표시")]
    [SerializeField] private TMP_Text _levelText;     // 레벨 (만렙 = Max)
    [SerializeField] private TMP_Text _ascensionText; // 돌파 수

    // 유저 보유 상태 : 레벨 / 돌파 수 + 돌파 테두리색. (유저 상태)
    public void SetOwnedState(int level, int ascensionStage, bool isMaxLevel)
    {
        if (_levelText != null)
            _levelText.text = isMaxLevel ? "Max" : $"Lv.{level}";

        if (_ascensionText != null)
            _ascensionText.text = ascensionStage > 0 ? $"{ascensionStage}돌" : string.Empty;

        SetAscensionBorder(ascensionStage);
    }
}
