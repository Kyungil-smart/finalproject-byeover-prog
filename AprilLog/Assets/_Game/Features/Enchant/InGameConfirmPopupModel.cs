//담당자: 조규민
// 인게임 확인 팝업의 표시 여부와 안내 문구 상태 변경 알림

using System;

/// <summary>
/// 인게임 확인 팝업의 표시 상태를 보관한다.
/// </summary>
public class InGameConfirmPopupModel
{
    public event Action<string> OnMessageChanged;
    public event Action<bool> OnVisibleChanged;

    public string Message { get; private set; }
    public bool IsVisible { get; private set; }

    // 확인 문구 저장과 팝업 열림 상태 알림
    public void Open(string message)
    {
        Message = message;
        IsVisible = true;
        OnMessageChanged?.Invoke(Message);
        OnVisibleChanged?.Invoke(IsVisible);
    }

    // 팝업 닫힘 상태 알림
    public void Close()
    {
        IsVisible = false;
        OnVisibleChanged?.Invoke(IsVisible);
    }
}
