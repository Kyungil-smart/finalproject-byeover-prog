// 담당자 : 조규민
// 구현원리 : Boot 기본 화면의 하단 로딩바 진행 완료를 Bootstrap에 이벤트로 전달하고 앱 버전을 표시한다.

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 기본 화면 로딩바 완료 상태를 로그인 진입 이벤트로 변환한다.
/// </summary>
public class StartView : MonoBehaviour
{
    public event Action OnLoadingCompleted;

    [Header("로딩바")]
    [SerializeField] private Slider _loadingSlider;
    [SerializeField] private TMP_Text _loadingPercentText;
    [Tooltip("초기 화면 하단 로딩바가 100%까지 차는 시간입니다.")]
    [SerializeField] private float _loadingDuration = 2f;

    [Header("버전")]
    [Tooltip("초기 Boot 화면에 앱 버전을 표시할 TMP Text입니다.")]
    [SerializeField] private TMP_Text _appVersionText;

    private Coroutine _loadingCoroutine;

    public bool IsLoadingCompleted { get; private set; }

    // 화면이 활성화될 때 하단 로딩바 진행을 시작한다.
    private void OnEnable()
    {
        SetAppVersionText();
        StartLoading();
    }

    // 화면이 비활성화될 때 진행 중인 로딩 코루틴을 정리한다.
    private void OnDisable()
    {
        StopLoading();
    }

    // 로딩 상태를 초기화하고 Slider 참조가 있으면 0%부터 채우기 시작한다.
    private void StartLoading()
    {
        StopLoading();

        IsLoadingCompleted = false;

        if (_loadingSlider == null)
        {
            Debug.LogWarning("[StartView] 하단 로딩바 Slider 참조가 없습니다.", this);
            CompleteLoading();
            return;
        }

        SetLoadingProgress(0f);

        _loadingCoroutine = StartCoroutine(FillLoadingSlider());
    }

    // 진행 중인 로딩바 코루틴을 중단한다.
    private void StopLoading()
    {
        if (_loadingCoroutine == null) return;

        StopCoroutine(_loadingCoroutine);
        _loadingCoroutine = null;
    }

    // Inspector에서 설정한 시간 동안 Slider와 퍼센트 텍스트를 100%까지 증가시킨다.
    private IEnumerator FillLoadingSlider()
    {
        float duration = Mathf.Max(0.1f, _loadingDuration);
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
            SetLoadingProgress(progress);
            yield return null;
        }

        _loadingCoroutine = null;
        CompleteLoading();
    }

    // 로딩 완료 상태를 기록하고 Bootstrap에 완료 이벤트를 전달한다.
    private void CompleteLoading()
    {
        if (IsLoadingCompleted) return;

        StopLoading();

        SetLoadingProgress(1f);

        IsLoadingCompleted = true;
        OnLoadingCompleted?.Invoke();
    }

    // Slider 값과 중앙 퍼센트 텍스트를 같은 진행률로 갱신한다.
    private void SetLoadingProgress(float progress)
    {
        if (_loadingSlider != null)
        {
            _loadingSlider.value = Mathf.Lerp(_loadingSlider.minValue, _loadingSlider.maxValue, progress);
        }

        if (_loadingPercentText == null) return;

        int percent = Mathf.RoundToInt(progress * 100f);
        _loadingPercentText.text = percent + "%";
    }

    private void SetAppVersionText()
    {
        if (_appVersionText == null)
        {
            return;
        }

        _appVersionText.SetText("v" + Application.version);
    }
}
