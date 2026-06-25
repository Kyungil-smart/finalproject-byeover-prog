using TMPro;
using UnityEngine;

// 작성자 : 홍정옥
// 설명   : 레전더리 확정(천장) 안내 표시 "NN회 뽑기 시도 시 레전더리 확정!"
// 천장까지 남은 횟수를 보여준다
// - 레전더리(천장) 가챠가 아니면 숨긴다
// - 남은 횟수는 ArtifactGachaPostProcessor.RemainingToPity 로 조회(단일 카운터 소스)
public class GachaPityInfoView : MonoBehaviour
{
    [Header("표시")]
    [SerializeField] private TMP_Text _text;
    [Tooltip("레전더리 가챠가 아닐 때 숨길 루트(선택). 비우면 텍스트만 비운다.")]
    [SerializeField] private GameObject _root;

    [Header("데이터")]
    [Tooltip("천장 카운터 소스(누적 카운터를 가진 후처리기)")]
    [SerializeField] private ArtifactGachaPostProcessor _postProcessor;
    [Tooltip("표시 대상 가챠 ID(레전더리 박스). 가챠 전환 시 Refresh(id)로 바뀐다.")]
    [SerializeField] private int _gachaId = 3;

    [SerializeField] private string _format = "{0}회 뽑기 시도 시 레전더리 확정!";

    public void Refresh() => Refresh(_gachaId);

    // 현재 표시 대상 가챠를 바꾸고 갱신
    public void Refresh(int gachaId)
    {
        _gachaId = gachaId;

        int remaining = _postProcessor != null ? _postProcessor.RemainingToPity(gachaId) : -1;
        bool show = remaining >= 0; // 레전더리(천장) 가챠일 때만 표시

        if (_root != null) _root.SetActive(show);

        if (_text != null)
            _text.text = show ? string.Format(_format, remaining) : string.Empty;
    }

    private void OnEnable() => Refresh();
}
