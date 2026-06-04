// 담당자 : 홍정옥
// 설명   : 시나리오 로그(백로그) UI
//          - 로그 버튼을 누르면 지금까지 진행된 대사 기록을 패널로 표시
//          - ScenarioView.History 를 읽어 항목을 생성한다 (열 때마다 갱신)

using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScenarioLogView : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private ScenarioView _view;

    [Header("패널")]
    [Tooltip("로그 패널 전체 (기본 비활성)")]
    [SerializeField] private GameObject _logPanel;
    [SerializeField] private Button _openButton;    // 로그 버튼
    [SerializeField] private Button _closeButton;

    [Header("목록")]
    [Tooltip("스크롤뷰 (선택 — 있으면 열 때 맨 아래로 스크롤)")]
    [SerializeField] private ScrollRect _scroll;
    [Tooltip("항목들이 들어갈 부모 (Vertical Layout Group 권장)")]
    [SerializeField] private RectTransform _content;
    [Tooltip("로그 한 줄 프리팹 (TMP_Text)")]
    [SerializeField] private TMP_Text _entryPrefab;

    private void Awake()
    {
        if (_view == null)
            _view = FindFirstObjectByType<ScenarioView>();

        if (_openButton != null)  _openButton.onClick.AddListener(Open);
        if (_closeButton != null) _closeButton.onClick.AddListener(Close);

        if (_logPanel != null)
            _logPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_openButton != null)  _openButton.onClick.RemoveListener(Open);
        if (_closeButton != null) _closeButton.onClick.RemoveListener(Close);
    }
    
    public void Open()
    {
        if (_logPanel != null)
            _logPanel.SetActive(true);

        // 로그가 떠 있는 동안 자동진행 멈춤
        if (_view != null)
            _view.PauseAuto();

        Rebuild();
        ScrollToBottom();
    }

    public void Close()
    {
        if (_logPanel != null)
            _logPanel.SetActive(false);

        // 자동진행 재개 (켜져 있었다면)
        if (_view != null)
            _view.ResumeAuto();
    }

    public void Toggle()
    {
        if (_logPanel != null && _logPanel.activeSelf) Close();
        else Open();
    }
    
    private void Rebuild()
    {
        if (_content == null || _entryPrefab == null || _view == null)
        {
            Debug.LogWarning("[ScenarioLogView] content/entryPrefab/view 연결을 확인하세요.", this);
            return;
        }

        // 기존 항목 정리
        for (int i = _content.childCount - 1; i >= 0; i--)
            Destroy(_content.GetChild(i).gameObject);

        // 기록을 순서대로 생성
        var history = _view.History;
        for (int i = 0; i < history.Count; i++)
        {
            TMP_Text entry = Instantiate(_entryPrefab, _content);
            entry.gameObject.SetActive(true);
            entry.text = Format(history[i]);
        }
    }

    private static string Format(ScenarioLogEntry e)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(e.Name))
            sb.Append("<b>").Append(e.Name).Append("</b>\n");
        sb.Append(e.Text);
        return sb.ToString();
    }

    private void ScrollToBottom()
    {
        if (_scroll == null) return;
        // 레이아웃 갱신 후 맨 아래로
        Canvas.ForceUpdateCanvases();
        _scroll.verticalNormalizedPosition = 0f;
    }
}
