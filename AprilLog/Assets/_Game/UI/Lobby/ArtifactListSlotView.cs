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

    // 보유개수 게이지 : current = 잔여 재료 개수, max = 등급 최대보유 기반 분모. (유저 상태)
    public void SetGauge(int current, int max)
    {
        if (_gaugeFill != null)
            _gaugeFill.fillAmount = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

        if (_gaugeText != null)
            _gaugeText.text = $"{current} / {max}";
    }

    // 보유 여부 : 미보유면 딤 오버레이를 켜서 분리 표시. (유저 상태)
    public void SetOwned(bool owned)
    {
        if (_dimOverlay != null)
            _dimOverlay.SetActive(!owned);
    }
}
