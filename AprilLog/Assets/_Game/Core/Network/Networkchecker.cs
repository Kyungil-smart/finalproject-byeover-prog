// 담당자 : 정승우
// 설명   : 네트워크 연결 상태 감지 -- 온라인/오프라인 전환 이벤트

using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 주기적으로 네트워크 상태를 확인해서 온/오프라인 전환을 알린다.
/// 오프라인에서 온라인으로 복구되면 FirestoreService가 밀린 데이터를 동기화함.
/// </summary>
public class NetworkChecker : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action OnOnline;       // 오프라인 -> 온라인
    public event Action OnOffline;      // 온라인 -> 오프라인

    // ---------- SerializeField ----------
    [Header("설정")]
    [Tooltip("네트워크 상태 체크 간격(초)")]
    [SerializeField] private float _checkInterval = 5f;

    // ---------- 상태 ----------
    public bool IsOnline { get; private set; }

    private bool _previousState;

    // ---------- 생명주기 ----------
    private void Start()
    {
        IsOnline = CheckConnection();
        _previousState = IsOnline;

        StartCoroutine(CheckLoop());
    }

    private IEnumerator CheckLoop()
    {
        var wait = new WaitForSecondsRealtime(_checkInterval);

        while (true)
        {
            yield return wait;

            IsOnline = CheckConnection();

            // 상태가 바뀌었을 때만 이벤트
            if (IsOnline && !_previousState)
            {
                Debug.Log("[Network] 온라인 복구됨");
                OnOnline?.Invoke();
            }
            else if (!IsOnline && _previousState)
            {
                Debug.Log("[Network] 오프라인 전환됨");
                OnOffline?.Invoke();
            }

            _previousState = IsOnline;
        }
    }

    private bool CheckConnection()
    {
        // NotReachable면 확실히 오프라인
        // 나머지는 일단 온라인으로 취급
        return Application.internetReachability != NetworkReachability.NotReachable;
    }
}