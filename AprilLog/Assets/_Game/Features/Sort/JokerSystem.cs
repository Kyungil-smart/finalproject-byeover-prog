using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class JokerSystem : MonoBehaviour, IPointerClickHandler
{
    public bool IsActive { get; private set; }

    [SerializeField] private SortInputHandler _inputHandler;
    [SerializeField] private JokerPatternLibrary _patternLibrary;
    [SerializeField] private List<Transform> _tableParents;
    [SerializeField] private RectTransform _effectSprite;
    [SerializeField] private SortModel _model;
    [SerializeField] private SortSystem _sortSystem;
    [SerializeField] private CanvasGroup[] _maskCanvasGroups;
    [SerializeField] private SortTableView _view;
    [SerializeField] private Image[] _jokerIcons;
    [SerializeField] private Image _coolTimeImage;
    [SerializeField] private TMP_Text _coolDownText;
    [SerializeField] private UnitDataManager _dataManager;

    private JokerPatternData _activePattern;
    private int _currentIndex = 0;
    private float _lastUsedTime = -60f;
    private const float _coolDown = 60f;
    private int _currentActiveIndex = 1; // 조커 몬스터 완성시 삭제 예정

    private void Start()
    {
        if (_coolTimeImage != null) _coolTimeImage.enabled = false;
        if (_coolDownText != null) _coolDownText.enabled = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ActivateJoker();
    }

    public void ActivateJoker()
    {
        if (Time.time - _lastUsedTime < _coolDown)
        {
            Debug.Log("쿨타임 중입니다!");
            return;
        }

        if (IsActive || _patternLibrary == null || _patternLibrary.patterns.Count == 0) return;

        if (_currentActiveIndex >= 0)
        {
            _jokerIcons[_currentActiveIndex].enabled = false;
            _currentActiveIndex--;
        }

        _lastUsedTime = Time.time;
        StartCoroutine(CooldownRoutine());
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
        if (_view != null) _view._isHintBlocked = true;

        IsActive = true;
        if (_inputHandler != null) _inputHandler.enabled = false;
        if (_effectSprite != null) _effectSprite.gameObject.SetActive(true);

        Debug.Log("조커 시스템 작동 시작");

        for (int i = 0; i < 12; i++)
        {
            int targetTable = GetNextJokerTarget();

            HighlightTable(targetTable);

            int currentTableColor = FindFirstValidUnitInTable(targetTable);
            Debug.Log($"[조커 연출] {i + 1}회차: 타겟 테이블 {targetTable}");
            _model.ReplaceTableUnits(targetTable, currentTableColor);

            if (_model.IsTableMatched(targetTable))
            {
                yield return StartCoroutine(_sortSystem.ProcessMatch(targetTable));
            }

            yield return new WaitForSeconds(0.3f);
        }

        if (_effectSprite != null) _effectSprite.gameObject.SetActive(false);

        Debug.Log("조커 시스템 작동 종료");
        if (_inputHandler != null) _inputHandler.enabled = true;
        if (_view != null) _view._isHintBlocked = false;

        IsActive = false;
    }

    private void HighlightTable(int targetIndex)
    {
        for (int i = 0; i < _maskCanvasGroups.Length; i++)
        {
            _maskCanvasGroups[i].alpha = (i == targetIndex) ? 0f : 0.78f;
        }
    }

    public void AcquireJokerItem()
    {
        if (_currentActiveIndex < _jokerIcons.Length - 1)
        {
            _currentActiveIndex++;
            _jokerIcons[_currentActiveIndex].enabled = true;
        }
    }

    private IEnumerator CooldownRoutine()
    {
        if (_coolTimeImage != null)
        {
            _coolTimeImage.enabled = true;
            _coolTimeImage.fillAmount = 1f;
        }

        if (_coolDownText != null) _coolDownText.enabled = true;

        float timer = _coolDown;

        while (timer > 0)
        {
            timer -= Time.deltaTime;

            if (_coolTimeImage != null) _coolTimeImage.fillAmount = timer / _coolDown;

            if (_coolDownText != null) _coolDownText.text = Mathf.CeilToInt(timer).ToString();

            yield return null;
        }

        if (_coolTimeImage != null) _coolTimeImage.enabled = false;
        if (_coolDownText != null) _coolDownText.enabled = false;
    }
}
