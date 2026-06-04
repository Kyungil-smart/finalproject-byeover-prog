// 담당자 : 정승우
// 설명   : 플레이어 인게임 비주얼(플레이스홀더 사각형) + 투사체 발사 기준점 제공.
//          정식 캐릭터 아트가 나오기 전 임시 사각형으로 위치/발사점을 잡는다.

using UnityEngine;

/// <summary>
/// 플레이어의 월드 비주얼(사각형)과 발사점(FirePoint)을 담당한다.
/// SkillSystem은 이 FirePoint를 발사 기준으로 사용한다.
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

    public Transform FirePoint => _firePoint;

    private void Awake()
    {
        EnsureVisual();
        EnsureFirePoint();
    }

    private void EnsureVisual()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        if (sr.sprite == null) sr.sprite = SpriteFactory.Square();
        sr.color = _color;
        sr.sortingOrder = _sortingOrder;

        transform.localScale = new Vector3(_size.x, _size.y, 1f);
    }

    private void EnsureFirePoint()
    {
        if (_firePoint != null) return;

        var fp = new GameObject("FirePoint").transform;
        fp.SetParent(transform);
        fp.localPosition = new Vector3(0f, 0.5f, 0f); // 사각형 상단(단위 기준, 부모 스케일 반영)
        _firePoint = fp;
    }
}
