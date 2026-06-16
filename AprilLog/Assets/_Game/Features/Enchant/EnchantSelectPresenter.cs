// 담당자 : 정승우
// 설명   : 인챈트 선택 Presenter

// 수정자 : 김영찬
// DataManager 최신화 중 기존 연결을 Legacy로 변경

// 수정자 : 김영찬
// ScreenNavigator의 변경에 따른 버튼 연결 최신화

// 수정자 : 김영찬
// 수정내용 : 기획서 - v1.04_인게임 성장 시스템_이균호 > 인첸트 시트 반영 (확률 직렬화 Config 연동, 스킬/스탯 통합 출력, 한도 초과 시 교체 진입점 구성)

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인챈트 선택지 생성 + 유저 선택 처리.
/// </summary>
public class EnchantSelectPresenter
{
    private readonly IEnchantSelectView _view;
    private readonly EnchantModel _model;
    private readonly SpellRepo _repo;
    private readonly ScreenNavigator _navigator;
    private readonly EnchantChangePresenter _changePresenter;
    
    // 확률 생성기
    private readonly EnchantSelector _selector; 
    
    // 현재 화면에 떠 있는 3개의 카드 정보
    private List<EnchantCandidate> _currentChoices;

    public EnchantSelectPresenter(IEnchantSelectView view, EnchantModel model, SpellRepo repo, ScreenNavigator navigator, EnchantProbabilityConfig config, EnchantChangePresenter changePresenter)
    {
        _view = view;
        _model = model;
        _repo = repo;
        _navigator = navigator;
        _changePresenter = changePresenter;
        _selector = new EnchantSelector(_repo, config);

        _view.OnChoiceSelected += HandleChoice;
        _view.OnSkipSelected += HandleSkip;
    }

    public void Dispose()
    {
        _view.OnChoiceSelected -= HandleChoice;
        _view.OnSkipSelected -= HandleSkip;
    }

    public void ShowSelection(int pickCount = 3)
    {
        _currentChoices = _selector.GenerateChoices(_model, pickCount);
        DisplayChoicesToView();
    }
    
    // ---------- UI용 포멧으로 변환 후 전달 ----------
    private void DisplayChoicesToView()
    {
        var displayData = new EnchantDisplayData[_currentChoices.Count];
        
        for (int i = 0; i < _currentChoices.Count; i++)
        {
            var candidate = _currentChoices[i];

            displayData[i] = new EnchantDisplayData
            {
                EnchantId = candidate.Specific_ID,
                Level = candidate.Level,
                // 카드 상단에 스킬/스탯 텍스트 표시
                TypeLabel = candidate.Type == EnchantType.Skill ? "스킬" : "스탯", 
                
                // 추후 로컬라이징 연동
                Name = $"NameID: {candidate.Name_ID}", 
                Description = candidate.Type == EnchantType.Skill ? 
                    $"데미지: {candidate.SkillData.Dmg}" : 
                    $"수치 증가: {candidate.StatData.Variation_2}"
            };
        }
        
        _view.SetChoices(displayData);
    }

    // ---------- 유저 클릭 처리 ----------
    private void HandleChoice(int index)
    {
        if (index < 0 || index >= _currentChoices.Count) return;
        var selected = _currentChoices[index];

        if (selected.Type == EnchantType.Skill)
        {
            if (_model.CanAcquireNewSkill(selected.Name_ID, selected.SkillData.SkillGroup_ID))
            {
                _model.AcquireSkill(selected.Name_ID, selected.SkillData.SkillGroup_ID);
                ClosePopup(); 
            }
            else
            {
                _changePresenter.OpenChangePopup(selected); 
            }
        }
        else if (selected.Type == EnchantType.Stat)
        {
            if (_model.CanAcquireNewStat(selected.Name_ID, selected.StatData.StatGroup_ID))
            {
                _model.AcquireStat(selected.Name_ID, selected.StatData.StatGroup_ID);
                ClosePopup();
            }
            else
            {
                _changePresenter.OpenChangePopup(selected); 
            }
        }
    }
    
    private void HandleSkip()
    {
        ClosePopup();
    }

    private void ClosePopup()
    {
        _navigator.OnCloseButtonClick();
    }
}
