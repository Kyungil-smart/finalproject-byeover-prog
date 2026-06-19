using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 홍정옥
// 설명 : 최대 보유 초과로 자동 분해가 발생했을 때의 결과 안내 팝업.
// 등급별 자동 분해 수량 + 획득 강화석/조각 총량을 표시한다.
// 닫기 버튼을 누르면 onClosed 콜백으로 큐에 다음 팝업을 알린다.
public class AutoDecomposeResultPopup : MonoBehaviour
{
    [Header("팝업 루트(실제 팝업 오브젝트)")]
    [SerializeField] private GameObject _root;
    [SerializeField] private Button _closeButton;

    [Header("표시 텍스트 (선택 — 비운 항목은 갱신하지 않음)")]
    [Tooltip("한 줄 요약(예: '레어 2 · 에픽 1 자동 분해'). 개별 텍스트 대신 사용 가능.")]
    [SerializeField] private TMP_Text _summaryText;
    [SerializeField] private TMP_Text _rareCountText;
    [SerializeField] private TMP_Text _epicCountText;
    [SerializeField] private TMP_Text _legendaryCountText;
    [SerializeField] private TMP_Text _stoneText;
    [SerializeField] private TMP_Text _shardText;

    private Action _onClosed;
    private bool _bound;

    private void Awake() => Bind();

    private void Bind()
    {
        if (_bound) return;
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(HandleClose); // 중복 등록 방지
            _closeButton.onClick.AddListener(HandleClose);
        }
        _bound = true;
    }

    public void Open(ArtifactGachaResult result, Action onClosed)
    {
        Bind();
        _onClosed = onClosed;

        if (result != null)
        {
            if (_summaryText != null) _summaryText.text = BuildSummary(result);
            if (_rareCountText != null) _rareCountText.text = result.RareDecomposed.ToString();
            if (_epicCountText != null) _epicCountText.text = result.EpicDecomposed.ToString();
            if (_legendaryCountText != null) _legendaryCountText.text = result.LegendaryDecomposed.ToString();
            if (_stoneText != null) _stoneText.text = result.TotalStone.ToString();
            if (_shardText != null) _shardText.text = result.TotalShard.ToString();
        }

        if (_root != null) _root.SetActive(true);
    }

    private static string BuildSummary(ArtifactGachaResult r)
    {
        var parts = new System.Text.StringBuilder();
        if (r.RareDecomposed > 0) parts.Append($"레어 {r.RareDecomposed} ");
        if (r.EpicDecomposed > 0) parts.Append($"에픽 {r.EpicDecomposed} ");
        if (r.LegendaryDecomposed > 0) parts.Append($"레전더리 {r.LegendaryDecomposed} ");
        parts.Append("자동 분해");
        if (r.TotalStone > 0) parts.Append($" / 강화석 +{r.TotalStone}");
        if (r.TotalShard > 0) parts.Append($" / 조각 +{r.TotalShard}");
        return parts.ToString();
    }

    private void HandleClose()
    {
        if (_root != null) _root.SetActive(false);

        Action cb = _onClosed;
        _onClosed = null;
        cb?.Invoke();
    }
}
