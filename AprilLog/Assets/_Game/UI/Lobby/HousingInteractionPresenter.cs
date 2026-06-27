//담당자: 조규민

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가구 입력과 상호작용 상태 및 플레이어 연출을 중재합니다.
/// </summary>
public class HousingInteractionPresenter
{
    private readonly HousingInteractionModel _model;
    private readonly IReadOnlyList<HousingInteractionView> _interactionViews;
    private readonly HousingInteractionExitView _exitView;
    private readonly Action _pausePlayerMovement;
    private readonly Action<bool> _resumePlayerMovement;

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

        _isInitialized = false;
    }

    private void HandleFurnitureClicked(HousingInteractionView _view)
    {
        if (_view == null)
        {
            return;
        }

        if (HasDuplicateId(_view.InteractionId))
        {
            Debug.LogWarning($"[HousingInteractionPresenter] 중복된 상호작용 ID입니다: {_view.InteractionId}");
            return;
        }

        _model.Activate(_view.InteractionId);
    }

    private void HandleExitClicked()
    {
        _model.Clear();
    }

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
        if (string.IsNullOrWhiteSpace(_interactionId) || _interactionViews == null)
        {
            return null;
        }

        for (int _index = 0; _index < _interactionViews.Count; _index++)
        {
            HousingInteractionView _view = _interactionViews[_index];

            if (_view != null && string.Equals(_view.InteractionId, _interactionId, StringComparison.Ordinal))
            {
                return _view;
            }
        }

        return null;
    }

    private bool HasDuplicateId(string _interactionId)
    {
        if (string.IsNullOrWhiteSpace(_interactionId))
        {
            return true;
        }

        int _matchCount = 0;

        for (int _index = 0; _index < _interactionViews.Count; _index++)
        {
            HousingInteractionView _view = _interactionViews[_index];

            if (_view == null || string.Equals(_view.InteractionId, _interactionId, StringComparison.Ordinal) == false)
            {
                continue;
            }

            _matchCount++;

            if (_matchCount > 1)
            {
                return true;
            }
        }

        return false;
    }
}
