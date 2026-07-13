// 담당자 : 최동훈
// 조커 시스템

// 수정자 : 김영찬
// 수정 내용 : 세이브/로드 구현

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
    [SerializeField] private Image _jokerIcon;
    [SerializeField] private Image _coolTimeImage;
    [SerializeField] private TMP_Text _coolDownText;
    [SerializeField] private TMP_Text _jokerCountText;
    [SerializeField] private UnitDataManager _dataManager;

    private JokerPatternData _activePattern;

    private int _currentIndex = 0;
    private int _currentCount = 0;
    private float _lastUsedTime = -60f;
    private const float _coolDown = 60f;
    private float _totalInGameTime = 0f;
    private bool _isLobby = false;
    private bool _isRestoredFromSave = false;
    private Coroutine _activeJokerRoutine;

    private void Update()
    {
        if (!_isLobby)
        {
            _totalInGameTime += Time.deltaTime;
        }
    }

    private void Start()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.ArtifactManager != null)
        {
            if (_isRestoredFromSave) return;

            var am = GameStateManager.Instance.ArtifactManager;
            am.OnInventoryUpdated += RefreshJokerCount;
            am.OnEquipmentChanged += RefreshJokerCount;

            RefreshJokerCount();
        }

        if (_coolTimeImage != null) _coolTimeImage.enabled = false;
        if (_coolDownText != null) _coolDownText.enabled = false;

        SyncCooldownUI();
        UpdateJokerUI();
    }

    private void OnDestroy()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.ArtifactManager != null)
        {
            var am = GameStateManager.Instance.ArtifactManager;
            am.OnInventoryUpdated -= RefreshJokerCount;
            am.OnEquipmentChanged -= RefreshJokerCount;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ActivateJoker();
    }

    public void SetLobbyMode(bool isLobby) => _isLobby = isLobby;

    public void ActivateJoker()
    {
        if (_totalInGameTime - _lastUsedTime < _coolDown)
        {
            Debug.Log("쿨타임 중입니다!");
            return;
        }

        if (_currentCount <= 0)
        {
            Debug.Log("사용할 수 있는 조커가 없습니다!");
            return;
        }

        if (IsActive || _patternLibrary == null || _patternLibrary.patterns.Count == 0) return;

        AudioManager.Play(SfxId.JokerClick);   // SFX 가이드 14: 조커 사용(쿨타임/미보유 거부는 무음)
        _currentCount--;
        UpdateJokerUI();

        _lastUsedTime = _totalInGameTime;

        StartCoroutine(CooldownRoutine());
        _activePattern = _patternLibrary.patterns[Random.Range(0, _patternLibrary.patterns.Count)];
        _currentIndex = 0;

        int firstTargetTable = _activePattern.tableIndices[0];
        int baseUnitType = FindFirstValidUnitInTable(firstTargetTable);

        _activeJokerRoutine = StartCoroutine(JokerRoutine(baseUnitType));
    }

    private void UpdateJokerUI()
    {
        if (_jokerCountText != null) _jokerCountText.text = $"X {_currentCount}";
        if (_jokerIcon != null) _jokerIcon.enabled = (_currentCount > 0);
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

    public void ForceStopJokerEffect()
    {
        if (_activeJokerRoutine != null)
        {
            StopCoroutine(_activeJokerRoutine);
            _activeJokerRoutine = null;
        }

        if (_effectSprite != null)
        {
            _effectSprite.gameObject.SetActive(false);
        }

        IsActive = false;
        if (_inputHandler != null) _inputHandler.enabled = true;
        if (_view != null) _view._isHintBlocked = false;

        foreach (var group in _maskCanvasGroups)
        {
            if (group != null) group.alpha = 0.78f;
        }
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
        if (_currentCount < 2)
        {
            _currentCount++;
            UpdateJokerUI();
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

        float timer = GetRemainingCooldown();

        while (timer > 0)
        {
            if (!_isLobby)
            {
                timer -= Time.deltaTime;

                if (_coolTimeImage != null) _coolTimeImage.fillAmount = timer / _coolDown;

                if (_coolDownText != null) _coolDownText.text = Mathf.CeilToInt(timer).ToString();

                yield return null;
            }
        }

        if (_coolTimeImage != null) _coolTimeImage.enabled = false;
        if (_coolDownText != null) _coolDownText.enabled = false;
    }

    public void RestoreJokerImages()
    {
        if (_jokerIcon != null)
        {
            _jokerIcon.enabled = true;
        }

        _currentCount = 2;
        UpdateJokerUI();
    }

    public void RefreshJokerCount()
    {
        if (TutorialManager.Instance != null && !TutorialManager.Instance.IsCompleted)
        {
            return;
        }

        var am = GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null;

        if (am == null)
        {
            return;
        }

        int baseCount = 0;
        int bonus = 0;

        var equippedArtifacts = am.MyArtifacts.FindAll(a => a.IsEquipped);

        foreach (var artifact in equippedArtifacts)
        {
            if (artifact.MasterId == 56002) 
            {
                bonus += 1;

                if (artifact.CurrentLevel >= artifact.GetMaxLevelLimit())
                {
                    bonus += 1;
                }
            }
        }

        _currentCount = baseCount + bonus;
        UpdateJokerUI();
    }

    private void SyncCooldownUI()
    {
        float remaining = GetRemainingCooldown();
        if (remaining > 0)
        {
            if (_coolTimeImage != null) _coolTimeImage.enabled = true;
            if (_coolDownText != null) _coolDownText.enabled = true;

            StartCoroutine(CooldownRoutine());
        }
        else
        {
            if (_coolTimeImage != null) _coolTimeImage.enabled = false;
            if (_coolDownText != null) _coolDownText.enabled = false;
        }
    }

    // ---------- 세이브 / 로드 (추가) ----------

        // 현재 조커 보유량 반환 (Index + 1)
    public int GetJokerCount() => _currentCount;
    
    public float GetRemainingCooldown()
    {
        float elapsed = _totalInGameTime - _lastUsedTime;
        return elapsed >= _coolDown ? 0f : _coolDown - elapsed;
    }

    // 세이브된 보유량을 바탕으로 시스템 복구
    public void RestoreFromSave(int savedJokerCount, float savedRemainingCooldown)
    {
        _isRestoredFromSave = true;
        _currentCount = savedJokerCount;
        UpdateJokerUI();

        if (savedRemainingCooldown > 0)
        {
            _lastUsedTime = _totalInGameTime - (_coolDown - savedRemainingCooldown);
            StartCoroutine(CooldownRoutine());
        }
        else
        {
            _lastUsedTime = -_coolDown;
        }
    }
}
