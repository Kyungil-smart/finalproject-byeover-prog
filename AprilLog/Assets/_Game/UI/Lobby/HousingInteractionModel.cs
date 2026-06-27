//담당자: 조규민

using System;

/// <summary>
/// 현재 활성화된 하우징 가구 상호작용 상태를 보관합니다.
/// </summary>
public class HousingInteractionModel
{
    private string _activeInteractionId;

    public event Action<string, string> OnActiveInteractionChanged;

    public string ActiveInteractionId => _activeInteractionId;
    public bool HasActiveInteraction => string.IsNullOrWhiteSpace(_activeInteractionId) == false;

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
