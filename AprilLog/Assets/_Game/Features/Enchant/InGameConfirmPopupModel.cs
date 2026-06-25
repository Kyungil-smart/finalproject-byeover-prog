//담당자: 조규민

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

    public void Open(string message)
    {
        Message = message;
        IsVisible = true;
        OnMessageChanged?.Invoke(Message);
        OnVisibleChanged?.Invoke(IsVisible);
    }

    public void Close()
    {
        IsVisible = false;
        OnVisibleChanged?.Invoke(IsVisible);
    }
}
