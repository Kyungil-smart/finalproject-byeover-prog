using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class JokerSystem : MonoBehaviour, IPointerClickHandler
{
    public bool IsActive { get; private set; }

    [SerializeField] private SortInputHandler _inputHandler;
    [SerializeField] private JokerPatternLibrary _patternLibrary;
    [SerializeField] private GameObject _jokerClonePrefab;
    [SerializeField] private List<Transform> _tableParents;
    [SerializeField] private RectTransform _effectSprite;
    [SerializeField] private SortModel _model;
    [SerializeField] private SortSystem _sortSystem;

    private JokerPatternData _activePattern;
    private int _currentIndex = 0;

    public void OnPointerClick(PointerEventData eventData)
    {
        ActivateJoker();
    }

    public void ActivateJoker()
    {
        if (IsActive || _patternLibrary == null || _patternLibrary.patterns.Count == 0) return;

        _activePattern = _patternLibrary.patterns[Random.Range(0, _patternLibrary.patterns.Count)];
        _currentIndex = 0;

        int firstTargetTable = _activePattern.tableIndices[0];
        int baseUnitType = FindFirstValidUnitInTable(firstTargetTable);

        StartCoroutine(JokerRoutine(baseUnitType));
    }

    private int FindFirstValidUnitInTable(int tableIdx)
    {
        for (int i = 0; i < 3; i++)
        {
            int unit = _model.GetUnit(tableIdx, i);
            if (unit >= 0) return unit;
        }
        return 0;
    }

    private int GetNextJokerTarget()
    {
        int target = _activePattern.tableIndices[_currentIndex];
        _currentIndex = (_currentIndex + 1) % _activePattern.tableIndices.Count;
        return target;
    }

    private IEnumerator JokerRoutine(int baseUnitType)
    {
        JokerManager.Instance?.SetJokerActive(true);

        IsActive = true;
        if (_inputHandler != null) _inputHandler.enabled = false;
        if (_effectSprite != null) _effectSprite.gameObject.SetActive(true);

        Debug.Log("조커 시스템 작동 시작");

        if (_effectSprite != null) _effectSprite.gameObject.SetActive(true);

        for (int i = 0; i < 12; i++)
        {
            int targetTable = GetNextJokerTarget();
            int currentTableColor = FindFirstValidUnitInTable(targetTable);
            Debug.Log($"[조커 연출] {i + 1}회차: 타겟 테이블 {targetTable}");
            _model.ReplaceTableUnits(targetTable, currentTableColor);

            GameObject clone = Instantiate(_jokerClonePrefab, _effectSprite);
            clone.transform.position = _tableParents[targetTable].position;

            var img = clone.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.enabled = false;

            if (_model.IsTableMatched(targetTable))
            {
                yield return StartCoroutine(_sortSystem.ProcessMatch(targetTable));
            }

            yield return new WaitForSeconds(0.3f);

            Destroy(clone);
        }

        if (_effectSprite != null) _effectSprite.gameObject.SetActive(false);

        Debug.Log("조커 시스템 작동 종료");
        if (_inputHandler != null) _inputHandler.enabled = true;
        
        IsActive = false;
        JokerManager.Instance?.SetJokerActive(false);
    }
}
