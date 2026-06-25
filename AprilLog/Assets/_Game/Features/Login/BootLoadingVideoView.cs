// 담당자 : 조규민
// 구현원리 : Boot 씬에 배치된 로딩 이미지 UI와 GIF에서 추출한 Sprite 프레임을 순서대로 표시하고, 설정된 재생 시간 안에서 로비 진입 전 애니메이션을 재생한다.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 진입 전 로딩 GIF 프레임 애니메이션을 재생한다.
/// </summary>
public class BootLoadingVideoView : MonoBehaviour
{
    [Header("표시")]
    [Tooltip("로딩 애니메이션 전체 UI 루트입니다. 비워두면 현재 GameObject를 사용합니다.")]
    [SerializeField] private GameObject _root;
    [Tooltip("GIF에서 추출한 Sprite 프레임을 표시할 Image입니다.")]
    [SerializeField] private Image _animationImage;

    [Header("GIF 프레임")]
    [Tooltip("GIF를 PNG로 분해한 Sprite 프레임 목록입니다. Inspector에서 순서대로 연결합니다.")]
    [SerializeField] private Sprite[] _animationFrames;
    [Tooltip("초당 표시할 프레임 수입니다.")]
    [SerializeField] private float _framesPerSecond = 12f;
    [Tooltip("켜면 프레임 목록을 한 번만 재생하고 로비로 이동합니다. 끄면 재생 시간 동안 반복합니다.")]
    [SerializeField] private bool _playOnce = true;
    [Tooltip("반복 재생일 때 유지할 최대 재생 시간입니다.")]
    [SerializeField] private float _playDurationSeconds = 2.5f;

    [Header("스킵")]
    [Tooltip("스킵을 허용하면 버튼 입력으로 로딩 애니메이션을 종료하고 로비로 이동합니다.")]
    [SerializeField] private bool _allowSkip = true;
    [SerializeField] private Button _skipButton;

    private bool _isSkipped;
    private bool _isPlaybackRequested;

    // 추가: 씬 시작 시 로딩 애니메이션 UI가 먼저 보이지 않도록 숨긴다.
    private void Awake()
    {
        if (_isPlaybackRequested)
        {
            return;
        }

        Hide();
    }

    // 추가: 로비 진입 전 GIF 프레임 애니메이션을 재생하고 종료될 때까지 대기한다.
    public IEnumerator Play()
    {
        if (!CanPlayAnimation())
        {
            Hide();
            yield break;
        }

        _isSkipped = false;
        _isPlaybackRequested = true;
        Show();
        BindSkipButton();
        yield return StartCoroutine(PlayFrames());
        CleanupPlayback();
    }

    // 추가: Image와 프레임 목록이 연결되어 있는지 확인한다.
    private bool CanPlayAnimation()
    {
        return _animationImage != null && _animationFrames != null && _animationFrames.Length > 0;
    }

    // 추가: 프레임 목록을 설정된 FPS와 재생 시간에 맞춰 표시한다.
    private IEnumerator PlayFrames()
    {
        float frameDuration = 1f / Mathf.Max(1f, _framesPerSecond);
        float maxPlayDuration = Mathf.Max(frameDuration, _playDurationSeconds);
        float elapsedTime = 0f;
        int frameIndex = 0;

        while (!_isSkipped)
        {
            _animationImage.sprite = _animationFrames[frameIndex];
            yield return new WaitForSecondsRealtime(frameDuration);

            elapsedTime += frameDuration;
            frameIndex++;

            if (elapsedTime >= maxPlayDuration)
            {
                yield break;
            }

            if (_playOnce && frameIndex >= _animationFrames.Length)
            {
                yield break;
            }

            if (frameIndex >= _animationFrames.Length)
            {
                frameIndex = 0;
            }
        }
    }

    // 추가: 스킵 버튼 이벤트를 연결한다.
    private void BindSkipButton()
    {
        if (_skipButton == null)
        {
            return;
        }

        _skipButton.gameObject.SetActive(_allowSkip);
        _skipButton.onClick.RemoveListener(HandleSkipClicked);
        _skipButton.onClick.AddListener(HandleSkipClicked);
    }

    // 추가: 스킵 버튼 입력을 재생 종료 상태로 반영한다.
    private void HandleSkipClicked()
    {
        if (!_allowSkip)
        {
            return;
        }

        _isSkipped = true;
    }

    // 추가: 스킵 이벤트를 해제하고 UI 상태를 정리한다.
    private void CleanupPlayback()
    {
        if (_skipButton != null)
        {
            _skipButton.onClick.RemoveListener(HandleSkipClicked);
        }

        _isPlaybackRequested = false;
        Hide();
    }

    // 추가: 로딩 애니메이션 UI를 표시한다.
    private void Show()
    {
        GetRoot().transform.localScale = Vector3.one;
        GetRoot().SetActive(true);
    }

    // 추가: 로딩 애니메이션 UI를 숨긴다.
    private void Hide()
    {
        GetRoot().SetActive(false);
    }

    // 추가: 루트가 비어 있으면 현재 GameObject를 로딩 애니메이션 UI 루트로 사용한다.
    private GameObject GetRoot()
    {
        return _root != null ? _root : gameObject;
    }
}
