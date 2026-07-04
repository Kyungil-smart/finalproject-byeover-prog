// 작성자 : 홍정옥
// 인게임 다이제틱 대화 시퀀스를 구동한다.
// 라인 리스트(이름/텍스트/화자)만 받아 연출하며 데이터 소스는 알지 못한다.
// 씬의 기존 플레이어를 앵커/에이프릴 화자로 재사용하고, 래리는 월드 스프라이트로 옆에 소환한다.
// 화자 스프라이트는 원색, 비화자는 어둡게(회색) 처리한다.

using System;
using System.Collections.Generic;
using UnityEngine;

public class InGameTalkPresenter : MonoBehaviour
{
    public event Action OnFinished;

    [Header("참조")]
    [SerializeField] private InGameTalkBubble _bubble;
    [Tooltip("플레이어 옆에 소환할 래리(월드 SpriteRenderer 프리팹).")]
    [SerializeField] private SpriteRenderer _raryPrefab;

    [Header("래리 배치")]
    [Tooltip("플레이어 위치 기준 래리 오프셋(월드).")]
    [SerializeField] private Vector2 _raryOffset = new Vector2(1.2f, 0f);

    [Header("화자 하이라이트")]
    [Tooltip("비화자 어둡기(0=검정, 1=원색).")]
    [SerializeField, Range(0f, 1f)] private float _dimAmount = 0.45f;

    private Camera _camera;
    private Transform _playerAnchor;
    private SpriteRenderer _aprilRenderer;
    private SpriteRenderer _raryRenderer;
    private Color _aprilBaseColor;
    private Color _raryBaseColor;
    private bool _aprilColorCached;

    private IReadOnlyList<TalkLine> _lines;
    private int _index;
    private bool _playing;

    public bool IsPlaying => _playing;

    public void SetBubbleViewportPosition(Vector2 viewportPosition, Vector2 screenOffset)
    {
        if (_bubble != null)
            _bubble.UseViewportPosition(viewportPosition, screenOffset);
    }

    public void ClearBubblePositionOverride()
    {
        if (_bubble != null)
            _bubble.UseAnchorPosition();
    }

    // 대화 시작. 종료되면 OnFinished 발생.
    public void Play(IReadOnlyList<TalkLine> lines)
    {
        if (_playing) return;
        if (lines == null || lines.Count == 0)
        {
            OnFinished?.Invoke();
            return;
        }

        _lines = lines;
        _index = 0;
        _playing = true;

        ResolveSceneRefs();
        SpawnRary();

        if (_bubble != null)
        {
            _bubble.Bind(_playerAnchor, _camera);
            _bubble.OnAdvanceRequested -= HandleAdvance;
            _bubble.OnAdvanceRequested += HandleAdvance;
        }

        ShowCurrentLine();
    }

    private void ResolveSceneRefs()
    {
        _camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();

        var playerView = FindFirstObjectByType<PlayerView>();
        if (playerView != null)
        {
            _playerAnchor = playerView.transform;
            _aprilRenderer = FindBodyRenderer(playerView.transform);
        }
        else
        {
            var model = FindFirstObjectByType<PlayerModel>();
            if (model != null)
            {
                _playerAnchor = model.transform;
                _aprilRenderer = model.GetComponentInChildren<SpriteRenderer>();
            }
        }

        if (_aprilRenderer != null && !_aprilColorCached)
        {
            _aprilBaseColor = _aprilRenderer.color;
            _aprilColorCached = true;
        }
    }

    // PlayerView는 "Body" 자식에 SpriteRenderer를 만든다. 없으면 첫 렌더러로 폴백.
    private static SpriteRenderer FindBodyRenderer(Transform playerRoot)
    {
        Transform body = playerRoot.Find("Body");
        if (body != null)
        {
            var sr = body.GetComponent<SpriteRenderer>();
            if (sr != null) return sr;
        }
        return playerRoot.GetComponentInChildren<SpriteRenderer>();
    }

    private void SpawnRary()
    {
        if (_raryPrefab == null || _playerAnchor == null) return;

        _raryRenderer = Instantiate(_raryPrefab);
        _raryRenderer.transform.position = _playerAnchor.position + (Vector3)_raryOffset;
        _raryBaseColor = _raryRenderer.color;
    }

    private void ShowCurrentLine()
    {
        if (!_playing) return;
        if (_lines == null || _index >= _lines.Count)
        {
            Finish();
            return;
        }

        TalkLine line = _lines[_index];
        ApplyHighlight(line.Speaker);
        if (_bubble != null) _bubble.PlayLine(line.Name, line.Text);
    }

    // 타이핑 중 클릭은 버블이 CompleteText로 소화하므로 여기로 오지 않는다.
    private void HandleAdvance()
    {
        _index++;
        ShowCurrentLine();
    }

    private void ApplyHighlight(TalkSpeaker speaker)
    {
        if (_aprilRenderer != null)
            _aprilRenderer.color = Tint(_aprilBaseColor, speaker == TalkSpeaker.April);
        if (_raryRenderer != null)
            _raryRenderer.color = Tint(_raryBaseColor, speaker == TalkSpeaker.Rary);
    }

    private Color Tint(Color baseColor, bool isSpeaker)
    {
        if (isSpeaker) return baseColor;
        return new Color(baseColor.r * _dimAmount, baseColor.g * _dimAmount, baseColor.b * _dimAmount, baseColor.a);
    }

    private void Finish()
    {
        _playing = false;

        if (_bubble != null)
        {
            _bubble.OnAdvanceRequested -= HandleAdvance;
            _bubble.Hide();
        }

        RestoreHighlight();
        DestroyRary();

        OnFinished?.Invoke();
    }

    private void RestoreHighlight()
    {
        if (_aprilRenderer != null && _aprilColorCached)
            _aprilRenderer.color = _aprilBaseColor;
    }

    private void DestroyRary()
    {
        if (_raryRenderer == null) return;
        Destroy(_raryRenderer.gameObject);
        _raryRenderer = null;
    }

    private void OnDestroy()
    {
        if (_bubble != null) _bubble.OnAdvanceRequested -= HandleAdvance;
        RestoreHighlight();
        DestroyRary();
    }
}
