//담당자: 조규민

using System;

/// <summary>
/// 현재 활성화된 하우징 가구 상호작용 상태를 보관합니다.
/// </summary>
// 현재 가구 상호작용 종류와 진행 여부 저장 및 상태 변경 알림
public class HousingInteractionModel
{
    private string _activeInteractionId;

    public event Action<string, string> OnActiveInteractionChanged;

    public string ActiveInteractionId => _activeInteractionId;
    public bool HasActiveInteraction => string.IsNullOrWhiteSpace(_activeInteractionId) == false;

    // 선택한 가구 상호작용 식별자 검증과 활성 상태 변경 알림
    public bool Activate(string _interactionId)
    {
        if (string.IsNullOrWhiteSpace(_interactionId))
        {
            return false;
        }

        string _normalizedId = _interactionId.Trim();

        if (_activeInteractionId == _normalizedId)
        {
            return false;
        }

        string _previousInteractionId = _activeInteractionId;
        _activeInteractionId = _normalizedId;
        OnActiveInteractionChanged?.Invoke(_previousInteractionId, _activeInteractionId);
        return true;
    }

    // 기존 상호작용 식별자 해제와 상태 변경 알림
    public bool Clear()
    {
        if (HasActiveInteraction == false)
        {
            return false;
        }

        string _previousInteractionId = _activeInteractionId;
        _activeInteractionId = null;
        OnActiveInteractionChanged?.Invoke(_previousInteractionId, _activeInteractionId);
        return true;
    }
}
