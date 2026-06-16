using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 홍정옥
// 설명   : 리스트용 아티팩트 슬롯. 공통(아이콘/배경/등급/돌파 테두리) + 보유개수 게이지(기획서 3-4-4).
//          돌파 단계 숫자·레벨은 표시하지 않는다.
public class ArtifactListSlotView : ArtifactSlotView
{
    [Header("보유개수 게이지 (기획서 3-4-4)")]
    [Tooltip("Image Type=Filled 권장. fillAmount 로 잔여/최대 비율 표시")]
    [SerializeField] private Image _gaugeFill;
    [SerializeField] private TMP_Text _gaugeText;

    [Header("미보유 표시")]
    [Tooltip("미보유 아티팩트일 때 켜서 구분하는 딤 오버레이")]
    [SerializeField] private GameObject _dimOverlay;

    // 등급별 최대 보유 가능 개수 (gear_grade 테이블의 MaxOwned 와 동일 : 레어 2 / 에픽 4 / 레전더리 6).
    // = 본체 1개 + 최종 돌파까지 필요한 재료 수의 합.
    private static int MaxOwnedByGrade(ArtifactGrade grade) => grade switch
    {
        ArtifactGrade.Rare => 2,
        ArtifactGrade.Epic => 4,
        ArtifactGrade.Legendary => 6,
        _ => 2
    };

    // 보유 재료 게이지 산출 (기획서 3-4-4-2). 등급은 SetData 로 세팅된 _grade 를 사용한다.
    //   currentCount   : 현재 보유 수량(본체 1개 포함, 돌파에 이미 사용한 재료는 차감된 값). = ArtifactInstance.CurrentCount
    //   ascensionStage : 본체의 현재 돌파 단계(0 = 미돌파). = ArtifactInstance.AscensionCount
    //   분자(current) = 돌파용 재료로 사용 가능한 잔여 개수 = currentCount - 1(본체).
    //   분모(max)     = 최종 돌파에 필요한 총 재료 개수 = 등급 MaxOwned - 1(본체) - ascensionStage.
    // 예) 레어 소유·잔여0(cnt1,asc0) → 0/1, 에픽 1돌·잔여1(cnt2,asc1) → 1/2, 레전더리 소유·잔여3(cnt4,asc0) → 3/5.
    public void SetGaugeByOwnership(int currentCount, int ascensionStage)
    {
        int asc = Mathf.Max(0, ascensionStage);
        int spare = Mathf.Max(0, currentCount - 1);
        int needed = Mathf.Max(0, MaxOwnedByGrade(_grade) - 1 - asc);
        SetGauge(spare, needed);
    }

    // 보유개수 게이지 (저수준) : current = 잔여 재료 개수, max = 최종 돌파에 필요한 총 재료 개수. (유저 상태)
    public void SetGauge(int current, int max)
    {
        if (_gaugeFill != null)
            _gaugeFill.fillAmount = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

        if (_gaugeText != null)
            _gaugeText.text = $"{current} / {max}";
    }

    // 보유 여부 : 미보유면 딤 오버레이를 켜서 분리 표시. (유저 상태)
    // 상세 팝업은 미보유도 열 수 있고, 팝업 내부의 장착/레벨업 버튼만 비활성화한다(ArtifactDetailPopupPresenter).
    public void SetOwned(bool owned)
    {
        if (_dimOverlay != null)
            _dimOverlay.SetActive(!owned);
    }
}
