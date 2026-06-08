// 담당자 : 정승우
// 설명   : 인챈트 선택 Presenter

// 수정자 : 김영찬
// DataManager 최신화 중 기존 연결을 Legacy로 변경

// 수정자 : 김영찬
// ScreenNavigator의 변경에 따른 버튼 연결 최신화

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인챈트 선택지 생성 + 유저 선택 처리.
/// </summary>
public class EnchantSelectPresenter
{
    private readonly IEnchantSelectView _view;
    private readonly EnchantModel _model;
    private readonly Legacy_CharacterRepo _repo;
    private readonly ScreenNavigator _navigator;
    private EnchantSelectionLogic _selectionLogic;
    private List<Legacy_EnchantMasterData> _currentChoices;

    public EnchantSelectPresenter(IEnchantSelectView view, EnchantModel model,
        Legacy_CharacterRepo repo, ScreenNavigator navigator)
    {
        _view = view;
        _model = model;
        _repo = repo;
        _navigator = navigator;
        _selectionLogic = new EnchantSelectionLogic(repo, model, new System.Random());

        _view.OnChoiceSelected += HandleChoice;
        _view.OnSkipSelected += HandleSkip;
    }

    public void Dispose()
    {
        _view.OnChoiceSelected -= HandleChoice;
        _view.OnSkipSelected -= HandleSkip;
    }

    public void ShowSelection()
    {
        _currentChoices = _selectionLogic.GenerateChoices();
        var displayData = new Legacy_EnchantDisplayData[_currentChoices.Count];
        for (int i = 0; i < _currentChoices.Count; i++)
        {
            var master = _currentChoices[i];
            int nextLevel = _model.GetEnchantLevel(master.EnchantID) + 1;
            displayData[i] = new Legacy_EnchantDisplayData
            {
                EnchantId = master.EnchantID,
                Name = master.Name,
                Level = nextLevel,
                TypeLabel = GetTypeLabel(master.LinkedStatType),
                Description = BuildDescription(master, nextLevel),
                ImageKey = master.ImageKey
            };
        }
        _view.SetChoices(displayData);
    }

    // INTERIM: stat-type 코드 → 카드 타입 라벨. (기획 enum 확정 시 교체)
    private static string GetTypeLabel(int statType)
    {
        switch (statType)
        {
            case 1: return "공격";
            case 2: return "관통";
            case 3: return "체력";
            case 4: return "치명타 확률";
            case 5: return "치명타 피해";
            default: return "특수";
        }
    }

    // 다음 레벨에 도달했을 때의 누적 효과를 카드 설명으로 생성.
    private string BuildDescription(Legacy_EnchantMasterData master, int level)
    {
        var lv = _repo != null ? _repo.GetEnchantLevel(master.EnchantID, level) : null;
        float v = lv != null ? lv.Value : 0f;
        switch (master.LinkedStatType)
        {
            case 1: return $"공격력 +{Mathf.RoundToInt(v * 100)}%";
            case 2: return $"관통 +{Mathf.RoundToInt(v * 100)}%";
            case 3: return $"최대 체력 +{Mathf.RoundToInt(v)}";
            case 4: return $"치명타 확률 +{Mathf.RoundToInt(v * 100)}%";
            case 5: return $"치명타 피해 +{Mathf.RoundToInt(v)}%";
            default: return master.Name;
        }
    }

    private void HandleChoice(int index)
    {
        if (index < 0 || index >= _currentChoices.Count) return;
        _model.AcquireEnchant(_currentChoices[index].EnchantID);
        _navigator.OnCloseButtonClick();
    }

    private void HandleSkip()
    {
        _navigator.OnCloseButtonClick();
    }
}
