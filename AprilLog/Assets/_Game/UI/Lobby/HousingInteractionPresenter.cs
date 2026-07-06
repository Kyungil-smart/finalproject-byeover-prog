//담당자: 조규민

// 상호작용 ID를 초기화 시 검증하고 캐싱하여 클릭마다 전체 View를 탐색하지 않도록 변경

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가구 입력과 상호작용 상태 및 플레이어 연출을 중재합니다.
/// </summary>
// 가구·종료 View 입력과 Model 상태 이벤트 연결
// 상호작용 시작·종료에 따른 플레이어 이동과 캐릭터 표시 상태 조정
public class HousingInteractionPresenter
{
    private readonly HousingInteractionModel _model;
    private readonly IReadOnlyList<HousingInteractionView> _interactionViews;
    private readonly HousingInteractionExitView _exitView;
    private readonly Action _pausePlayerMovement;
    private readonly Action<bool> _resumePlayerMovement;
    private readonly Dictionary<string, HousingInteractionView> _viewByInteractionId = new(StringComparer.Ordinal);
    private readonly HashSet<string> _duplicateInteractionIds = new(StringComparer.Ordinal);

    private bool _isInitialized;

    public HousingInteractionPresenter(
        HousingInteractionModel _model,
        IReadOnlyList<HousingInteractionView> _interactionViews,
        HousingInteractionExitView _exitView,
        Action _pausePlayerMovement,
        Action<bool> _resumePlayerMovement)
    {
        this._model = _model;
        this._interactionViews = _interactionViews;
        this._exitView = _exitView;
        this._pausePlayerMovement = _pausePlayerMovement;
        this._resumePlayerMovement = _resumePlayerMovement;
    }

    // 가구·종료 View 입력과 Model 상태 변경 이벤트 등록
    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        if (_model == null || _interactionViews == null || _exitView == null)
        {
            Debug.LogWarning("[HousingInteractionPresenter] MVP 연결이 올바르지 않습니다.");
            return;
        }

        CacheInteractionViews();

        for (int _index = 0; _index < _interactionViews.Count; _index++)
        {
            HousingInteractionView _view = _interactionViews[_index];

            if (_view != null)
            {
                _view.OnClicked += HandleFurnitureClicked;
                _view.SetInteractionActive(false);
            }
        }

        _exitView.OnClicked += HandleExitClicked;
        _model.OnActiveInteractionChanged += HandleActiveInteractionChanged;
        _exitView.SetVisible(false);
        _isInitialized = true;
    }

    public void Reset()
    {
        _model?.Clear();
    }

    public void Release()
    {
        if (_interactionViews != null)
        {
            for (int _index = 0; _index < _interactionViews.Count; _index++)
            {
                HousingInteractionView _view = _interactionViews[_index];

                if (_view != null)
                {
                    _view.OnClicked -= HandleFurnitureClicked;
                }
            }
        }

        if (_exitView != null)
        {
            _exitView.OnClicked -= HandleExitClicked;
        }

        if (_model != null)
        {
            _model.OnActiveInteractionChanged -= HandleActiveInteractionChanged;
        }

        _viewByInteractionId.Clear();
        _duplicateInteractionIds.Clear();
        _isInitialized = false;
    }

    // 동일 가구 재선택 방지 후 선택 상호작용 활성화
    private void HandleFurnitureClicked(HousingInteractionView _view)
    {
        if (_view == null)
        {
            return;
        }

        string _interactionId = NormalizeInteractionId(_view.InteractionId);

        if (string.IsNullOrEmpty(_interactionId) || _duplicateInteractionIds.Contains(_interactionId))
        {
            Debug.LogWarning($"[HousingInteractionPresenter] 중복된 상호작용 ID입니다: {_view.InteractionId}");
            return;
        }

        _model.Activate(_interactionId);
    }

    private void HandleExitClicked()
    {
        _model.Clear();
    }

    // 이전·현재 상호작용 View 표시와 플레이어 이동 상태 갱신
    private void HandleActiveInteractionChanged(string _previousId, string _currentId)
    {
        HousingInteractionView _previousView = FindView(_previousId);
        HousingInteractionView _currentView = FindView(_currentId);

        _previousView?.SetInteractionActive(false);
        _currentView?.SetInteractionActive(true);
        _exitView.SetVisible(_currentView != null);
        UpdatePlayerMovement(_previousView, _currentView);
    }

    private void UpdatePlayerMovement(
        HousingInteractionView _previousView,
        HousingInteractionView _currentView)
    {
        if (_currentView != null && _currentView.PausePlayerMovement)
        {
            _pausePlayerMovement?.Invoke();
            return;
        }

        if (_previousView == null || _previousView.PausePlayerMovement == false)
        {
            return;
        }

        _resumePlayerMovement?.Invoke(_previousView.RestorePlayerPositionOnExit);
    }

    private HousingInteractionView FindView(string _interactionId)
    {
        string _normalizedId = NormalizeInteractionId(_interactionId);

        if (string.IsNullOrEmpty(_normalizedId))
        {
            return null;
        }

        return _viewByInteractionId.TryGetValue(_normalizedId, out HousingInteractionView _view)
            ? _view
            : null;
    }

    private void CacheInteractionViews()
    {
        _viewByInteractionId.Clear();
        _duplicateInteractionIds.Clear();

        for (int _index = 0; _index < _interactionViews.Count; _index++)
        {
            HousingInteractionView _view = _interactionViews[_index];
            string _interactionId = NormalizeInteractionId(_view != null ? _view.InteractionId : null);

            if (_view == null || string.IsNullOrEmpty(_interactionId))
            {
                Debug.LogWarning($"[HousingInteractionPresenter] 비어 있는 상호작용 ID가 있습니다. Index: {_index}");
                continue;
            }

            if (_viewByInteractionId.TryAdd(_interactionId, _view))
            {
                continue;
            }

            _duplicateInteractionIds.Add(_interactionId);
            Debug.LogWarning($"[HousingInteractionPresenter] 중복된 상호작용 ID입니다: {_interactionId}");
        }
    }

    private static string NormalizeInteractionId(string _interactionId)
    {
        return string.IsNullOrWhiteSpace(_interactionId)
            ? string.Empty
            : _interactionId.Trim();
    }
}
