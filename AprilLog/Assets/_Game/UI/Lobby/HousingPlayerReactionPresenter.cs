//담당자: 조규민

/// <summary>
/// 하우징 플레이어 터치 반응 상태를 관리합니다.
/// </summary>
// 플레이어 연속 터치 횟수와 초기화 시간을 관리하고 단계별 반응 문구 선택
public class HousingPlayerReactionPresenter
{
    private const float DisplaySeconds = 2f;
    private const float TouchCountResetSeconds = 3f;

    private int _touchCount;
    private float _hideTimer;
    private float _resetTimer;
    private bool _isShowing;

    // 연속 터치 횟수 증가와 단계별 반응 문구 반환
    public string HandleTouched()
    {
        _touchCount++;
        _hideTimer = DisplaySeconds;
        _resetTimer = TouchCountResetSeconds;
        _isShowing = true;

        return GetReactionMessage(_touchCount);
    }

    // 무입력 경과 시간 누적과 반응 초기화 시점 확인
    public bool Update(float _deltaTime)
    {
        UpdateResetTimer(_deltaTime);

        if (_isShowing == false)
        {
            return false;
        }

        _hideTimer -= _deltaTime;

        if (_hideTimer > 0f)
        {
            return false;
        }

        _isShowing = false;
        return true;
    }

    // 터치 횟수와 반응 유지 시간 초기화
    public void Reset()
    {
        _touchCount = 0;
        _hideTimer = 0f;
        _resetTimer = 0f;
        _isShowing = false;
    }

    private void UpdateResetTimer(float _deltaTime)
    {
        if (_touchCount <= 0)
        {
            return;
        }

        _resetTimer -= _deltaTime;

        if (_resetTimer > 0f)
        {
            return;
        }

        _touchCount = 0;
    }

    private string GetReactionMessage(int _count)
    {
        if (_count <= 1)
        {
            return "어... 갑자기 왜 그래?";
        }

        if (_count == 2)
        {
            return "아, 또 와줬구나!";
        }

        if (_count == 3)
        {
            return "잠깐만... 너무 자주 누르는 거 아니야?";
        }

        return "으으... 너무 정신없어...";
    }
}
