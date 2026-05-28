// 담당자 : 조규민
// 구현원리 : Boot 기본 화면의 전체 터치 버튼 입력을 Bootstrap에 이벤트로 전달한다.

using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 기본 화면 터치 입력을 로그인 진입 이벤트로 변환한다.
/// </summary>
public class StartTouchView : MonoBehaviour
{
    public event Action OnStartTouched;

    [Header("버튼")]
    [SerializeField] private Button _startTouchButton;

    private bool _hasTouched;

    // View가 켜질 때 중복 터치 방지 상태를 초기화하고 버튼 이벤트를 연결한다.
    private void OnEnable()
    {
        _hasTouched = false;
        BindButton();
    }

    // View가 꺼질 때 버튼 이벤트를 해제한다.
    private void OnDisable()
    {
        UnbindButton();
    }

    // Inspector에 연결된 전체 화면 버튼 클릭을 View 이벤트로 변환할 준비를 한다.
    private void BindButton()
    {
        if (_startTouchButton == null)
        {
            Debug.LogWarning("[StartTouchView] 시작 터치 버튼 참조가 없습니다.", this);
            return;
        }

        _startTouchButton.onClick.RemoveListener(NotifyStartTouched);
        _startTouchButton.onClick.AddListener(NotifyStartTouched);
    }

    // 버튼 리스너가 남아 중복 호출되지 않도록 해제한다.
    private void UnbindButton()
    {
        if (_startTouchButton != null)
        {
            _startTouchButton.onClick.RemoveListener(NotifyStartTouched);
        }
    }

    // 첫 터치만 Bootstrap에 알리고 터치 Canvas를 비활성화한다.
    private void NotifyStartTouched()
    {
        if (_hasTouched) return;

        _hasTouched = true;
        OnStartTouched?.Invoke();
        gameObject.SetActive(false);
    }
}
