// 담당자 : 정승우
// 설명   : 플레이어 인게임 비주얼(플레이스홀더 사각형) + 발사 기준점 + 방벽 HP 바.
//          방벽 HP = 플레이어 HP(PlayerModel)이며, 유저에게는 방벽 체력이 깎이는 것으로 보인다.

using UnityEngine;

/// <summary>
/// 플레이어의 월드 비주얼(사각형), 발사점(FirePoint), 방벽 HP 바를 담당한다.
/// HP 바는 PlayerModel.OnHPChanged를 구독해 플레이어 체력을 그대로 표시한다.
/// </summary>
public class PlayerView : MonoBehaviour
{
    [Header("플레이스홀더 비주얼")]
    [SerializeField] private Color _color = new Color(0.3f, 0.6f, 1f, 1f);
    [SerializeField] private Vector2 _size = new Vector2(0.8f, 0.8f);
    [SerializeField] private int _sortingOrder = 30;

    [Header("발사")]
    [Tooltip("투사체 발사 기준점. 비어 있으면 사각형 상단에 자동 생성")]
    [SerializeField] private Transform _firePoint;

    [Header("방벽 HP 바")]
    [SerializeField] private bool _showHpBar = true;
    [SerializeField] private Vector2 _hpBarSize = new Vector2(2.0f, 0.2f);
    [SerializeField] private float _hpBarYOffset = 0.7f;
    [SerializeField] private Color _hpBarBgColor = new Color(0.1f, 0.1f, 0.1f, 0.85f);
    [SerializeField] private Color _hpBarFillColor = new Color(0.9f, 0.25f, 0.25f, 1f);

    public Transform FirePoint => _firePoint;

    private PlayerModel _playerModel;
    private Transform _hpFillAnchor;

    private void Awake()
    {
        BuildBody();
        EnsureFirePoint();
        if (_showHpBar) BuildHpBar();
    }

    private void Start()
    {
        // 방벽 HP = 플레이어 HP. PlayerModel을 찾아 체력 변화를 구독한다.
        _playerModel = FindFirstObjectByType<PlayerModel>();
        if (_playerModel != null)
        {
            _playerModel.OnHPChanged += HandleHp;
            HandleHp(_playerModel.CurrentHP, _playerModel.MaxHP);
        }
    }

    private void OnDestroy()
    {
        if (_playerModel != null)
            _playerModel.OnHPChanged -= HandleHp;
    }

    // ---------- 비주얼 구성 ----------
    private void BuildBody()
    {
        var body = new GameObject("Body");
        body.transform.SetParent(transform, false);
        body.transform.localScale = new Vector3(_size.x, _size.y, 1f);

        var sr = body.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Square();
        sr.color = _color;
        sr.sortingOrder = _sortingOrder;
    }

    private void EnsureFirePoint()
    {
        if (_firePoint != null) return;

        var fp = new GameObject("FirePoint").transform;
        fp.SetParent(transform, false);
        fp.localPosition = new Vector3(0f, _size.y * 0.5f, 0f);
        _firePoint = fp;
    }

    private void BuildHpBar()
    {
        // 배경
        var bg = new GameObject("HpBar_BG");
        bg.transform.SetParent(transform, false);
        bg.transform.localPosition = new Vector3(0f, _hpBarYOffset, 0f);
        bg.transform.localScale = new Vector3(_hpBarSize.x, _hpBarSize.y, 1f);
        var bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = SpriteFactory.Square();
        bgSr.color = _hpBarBgColor;
        bgSr.sortingOrder = _sortingOrder + 1;

        // 채움: 바 왼쪽 끝에 anchor를 두고, anchor.scale.x = HP비율 로 좌측 기준 줄어들게 한다.
        var anchor = new GameObject("HpBar_FillAnchor").transform;
        anchor.SetParent(transform, false);
        anchor.localPosition = new Vector3(-_hpBarSize.x * 0.5f, _hpBarYOffset, 0f);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(anchor, false);
        fill.transform.localPosition = new Vector3(_hpBarSize.x * 0.5f, 0f, 0f);
        fill.transform.localScale = new Vector3(_hpBarSize.x, _hpBarSize.y, 1f);
        var fillSr = fill.AddComponent<SpriteRenderer>();
        fillSr.sprite = SpriteFactory.Square();
        fillSr.color = _hpBarFillColor;
        fillSr.sortingOrder = _sortingOrder + 2;

        _hpFillAnchor = anchor;
    }

    // ---------- HP 갱신 ----------
    private void HandleHp(int cur, int max)
    {
        if (_hpFillAnchor == null) return;

        float ratio = max > 0 ? Mathf.Clamp01((float)cur / max) : 0f;
        var s = _hpFillAnchor.localScale;
        s.x = ratio;
        _hpFillAnchor.localScale = s;
    }
}
