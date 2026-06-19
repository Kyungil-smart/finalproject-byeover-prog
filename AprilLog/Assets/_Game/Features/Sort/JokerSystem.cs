using UnityEngine;
using System.Collections;

public class JokerSystem : MonoBehaviour
{
    public bool IsActive { get; private set; }

    [SerializeField] private SortInputHandler _inputHandler;
    [SerializeField] private JokerPatternLibrary _patternLibrary;

    private JokerPatternData _activePattern;
    private int _currentIndex = 0;

    public void ActivateJoker()
    {
        if (IsActive || _patternLibrary == null || _patternLibrary.patterns.Count == 0) return;

        _activePattern = _patternLibrary.patterns[Random.Range(0, _patternLibrary.patterns.Count)];
        _currentIndex = 0;

        StartCoroutine(JokerRoutine());
    }

    private int GetNextJokerTarget()
    {
        int target = _activePattern.tableIndices[_currentIndex];
        _currentIndex = (_currentIndex + 1) % _activePattern.tableIndices.Count;
        return target;
    }

    private IEnumerator JokerRoutine()
    {
        IsActive = true;
        if (_inputHandler != null) _inputHandler.enabled = false;

        Debug.Log("조커 시스템 작동 시작");

        for (int i = 0; i < 12; i++)
        {
            int targetTable = GetNextJokerTarget();
            Debug.Log($"[조커 연출] {i + 1}회차: 타겟 테이블 {targetTable}");


            yield return new WaitForSeconds(0.3f);
        }

        Debug.Log("조커 시스템 작동 종료");
        if (_inputHandler != null) _inputHandler.enabled = true;
        IsActive = false;
    }
}
