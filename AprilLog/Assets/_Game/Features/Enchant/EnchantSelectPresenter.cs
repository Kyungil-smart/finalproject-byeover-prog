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
                TypeLabel = master.LinkedSkillID > 0 ? "스킬" : GetTypeLabel(master.LinkedStatType),
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
        // 스킬 인챈트: 발동 조건 + 레벨 효과 요약 (인챈트 테이블 v1.03)
        if (master.LinkedSkillID > 0)
            return BuildSkillDescription(master.LinkedSkillID, level);

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

    // 스킬 인챈트 카드 설명 (베이스 SkillID 기준. 인챈트 테이블 v1.03 확정값)
    private static string BuildSkillDescription(int baseSkillId, int level)
    {
        switch (baseSkillId)
        {
            case 1011:
                int cadence = level >= 3 ? 10 : (level == 2 ? 13 : 15);
                return $"자동공격 {cadence}회마다 타겟 위치에 화염 장판 3연타";
            case 1021:
                int shots = 2 + level; // 3/4/5발
                return $"연두+빨강+파랑 정렬 조합 시 화염탄 {shots}발 발사";
            case 1031:
                return $"노랑+파랑+연두 정렬 조합 시 화염 정령 2마리 소환 (Lv{level} 화염 작렬 시전)";
            case 1041:
                int cracks = level >= 3 ? 4 : 3;
                return $"콤보 7의 배수마다 전방 전체에 대지 균열 {cracks}연타";
            case 1051:
                int meteors = 2 + level; // 3/4/5회
                return $"콤보 9의 배수마다 무작위 적에게 메테오 {meteors}회 낙하";
            default:
                return $"스킬 발동 (Lv{level})";
        }
    }

    private void HandleChoice(int index)
    {
        if (index < 0 || index >= _currentChoices.Count) return;
        Debug.Log($"[인챈트선택] index={index} id={_currentChoices[index].EnchantID} → AcquireEnchant 호출");
        _model.AcquireEnchant(_currentChoices[index].EnchantID);
        _navigator.OnCloseButtonClick();
    }

    private void HandleSkip()
    {
        _navigator.OnCloseButtonClick();
    }
}
