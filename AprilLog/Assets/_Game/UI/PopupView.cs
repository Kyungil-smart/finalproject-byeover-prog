// 담당자 : 홍정옥
// 설명   : 팝업 공통 처리 컴포넌트
//          - 단일 팝업 보장 : 다른 팝업이 열려 있으면 새 팝업을 열지 못하게 막는다.
//          - 여백(Dim) 클릭 닫기 : 팝업 뒤 어두운 영역을 누르면 팝업이 닫힌다.
//          팝업 루트 오브젝트에 이 컴포넌트 하나만 붙이면 동작한다.
//          Dim 이 없으면 풀스크린으로 자동 생성하고, 있으면(이름 "Dim") 찾아서 클릭 닫기만 연결한다.

using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PopupView : MonoBehaviour
{
    [Header("딤(배경 어둡게 + 클릭 차단)")]
    [Tooltip("비워두면 자식 'Dim'을 찾고, 없으면 풀스크린으로 자동 생성한다.")]
    [SerializeField] private Image _dim;

    [Tooltip("딤(여백)을 클릭하면 팝업을 닫을지 여부")]
    [SerializeField] private bool _closeOnDimClick = true;

    [Tooltip("딤을 자동 생성할 때 사용할 색 (이미 있는 딤은 색을 건드리지 않음)")]
    [SerializeField] private Color _dimColor = new Color(0f, 0f, 0f, 0.78f);

    // 현재 열려 있는 팝업 (단일 팝업 보장용)
    public static PopupView Current { get; private set; }
    public static bool IsAnyOpen => Current != null;

    private Button _dimButton;
    private bool _dimReady;

    private void Awake()
    {
        EnsureDim();
    }

    private void OnEnable()
    {
        // 이미 다른 팝업이 열려 있으면 이 팝업은 열리지 않도록 즉시 닫는다.
        if (Current != null && Current != this)
        {
            gameObject.SetActive(false);
            return;
        }

        Current = this;
    }

    private void OnDisable()
    {
        if (Current == this)
            Current = null;
    }

    /// <summary>팝업을 닫는다. (딤 클릭 / 외부 호출 공용)</summary>
    public void Close()
    {
        gameObject.SetActive(false);
    }

    // ---------- 내부 ----------

    private void EnsureDim()
    {
        if (_dimReady) return;
        _dimReady = true;

        // 1) 인스펙터 미지정이면 자식 "Dim"을 먼저 탐색
        if (_dim == null)
        {
            Transform found = transform.Find("Dim");
            if (found != null)
                _dim = found.GetComponent<Image>();
        }

        // 2) 그래도 없으면 풀스크린 딤을 새로 생성해 맨 뒤(첫 자식)로 배치
        if (_dim == null)
        {
            var go = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _dim = go.GetComponent<Image>();
            _dim.color = _dimColor;
        }

        // 뒤쪽 UI(하단바 버튼 등) 클릭을 막아 다른 팝업이 열리지 않게 한다.
        _dim.raycastTarget = true;

        if (_closeOnDimClick)
            BindDimClick();
    }

    private void BindDimClick()
    {
        _dimButton = _dim.GetComponent<Button>();
        if (_dimButton == null)
            _dimButton = _dim.gameObject.AddComponent<Button>();

        // 딤은 시각적 하이라이트가 필요 없으므로 트랜지션 제거
        _dimButton.transition = Selectable.Transition.None;

        _dimButton.onClick.RemoveListener(Close);
        _dimButton.onClick.AddListener(Close);
    }
}
