using System;
using UnityEngine;

public class JokerManager : MonoBehaviour
{
    public static JokerManager Instance { get; private set; }

    private int _activeJokerCount = 0;
    public event Action<bool> OnJokerBlockingStateChanged;

    private void Awake()
    {
        Instance = this;
    }

    public void SetJokerActive(bool isActive)
    {
        if (isActive) _activeJokerCount++;
        else _activeJokerCount = Math.Max(0, _activeJokerCount - 1);

        Debug.Log($"[매니저] 현재 연출 중인 조커: {_activeJokerCount}개");
        OnJokerBlockingStateChanged?.Invoke(_activeJokerCount > 0);
    }
}
