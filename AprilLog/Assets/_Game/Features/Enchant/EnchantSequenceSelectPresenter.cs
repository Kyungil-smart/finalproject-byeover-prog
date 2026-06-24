// 작성자 : 김영찬
// 설명 : EnchantSequenceSelector를 지원하는 중재자(스킬/스텟 선택 분리)

// 수정자 : 김영찬
// 수정 내용 : 인첸트 리롤 시 기존 인첸트가 등장하지 않도록 수정

using System.Collections.Generic;
using UnityEngine;

public class EnchantSequenceSelectPresenter : IEnchantSelectPresenter
{
    private readonly IEnchantSelectView _view;
    private readonly EnchantModel _model;
    private readonly SpellRepo _repo;
    private readonly ScreenNavigator _navigator;
    private readonly EnchantChangePresenter _changePresenter;
    
    // 신규 셀렉터 및 시퀀스 설정
    private readonly EnchantSequenceSelector _sequenceSelector; 
    private readonly EnchantSequenceConfig _sequenceConfig;
    
    // 리롤 관련
    private readonly int _baseRerollCount;
    private readonly bool _unlimitedReroll;   // rerollCount < 0(테스트2 씬) → 새로고침 무한. 차감 안 하고 ∞ 표시.
    
    // 이번 턴이 스킬 차례인지 스탯 차례인지 기억 (리롤할 때 사용)
    private EnchantType _currentTurnType;
    
    // 현재 화면에 떠 있는 3개의 카드 정보
    private List<EnchantCandidate> _currentChoices;
    private int _rerollRemaining;
    private int _pickCount = 3;

    // ---------- 초기화 ----------
    public EnchantSequenceSelectPresenter(IEnchantSelectView view, EnchantModel model, SpellRepo repo, ScreenNavigator navigator, EnchantProbabilityConfig config, EnchantSequenceConfig sequenceConfig, EnchantChangePresenter changePresenter, int rerollCount)
    {
        _view = view;
        _model = model;
        _repo = repo;
        _navigator = navigator;
        _changePresenter = changePresenter;
        _sequenceConfig = sequenceConfig;
        _unlimitedReroll = rerollCount < 0;                                 // -1 = 무한(EnchantSelectView가 테스트2 씬 보고 결정)
        _baseRerollCount = _unlimitedReroll ? 0 : Mathf.Max(0, rerollCount);
        
        // 기존 EnchantSelector 대신 신규 분리형 셀렉터 할당
        _sequenceSelector = new EnchantSequenceSelector(_repo, config); 

        _view.OnChoiceSelected += HandleChoice;
        _view.OnSkipSelected += HandleSkip;
        _view.OnRerollSelected += HandleFullReroll;
        _view.OnCardRerollSelected += HandleCardReroll;
    }

    public void Dispose()
    {
        _view.OnChoiceSelected -= HandleChoice;
        _view.OnSkipSelected -= HandleSkip;
        _view.OnRerollSelected -= HandleFullReroll;
        _view.OnCardRerollSelected -= HandleCardReroll;
    }
    
    // ---------- 작동 로직 ----------
    public void ShowSelection(int pickCount = 3)
    {
        _pickCount = pickCount;
        _rerollRemaining = _baseRerollCount;
        
        // 현재 몇 번째 뽑기인지에 따라 스킬/스탯 차례 계산
        int sequenceIndex = _model.TotalDrawCount % _sequenceConfig.DrawSequence.Count;
        _currentTurnType = _sequenceConfig.DrawSequence[sequenceIndex];
        
        GenerateChoicesByTurnType(pickCount);
        DisplayChoicesToView();
    }

    // 차례에 맞는 종류만 뽑기
    private void GenerateChoicesByTurnType(int pickCount, List<int> excludedIds = null)
    {
        _currentChoices = _currentTurnType == EnchantType.Skill ? 
            _sequenceSelector.GenerateSkillChoices(_model, pickCount, excludedIds) : 
            _sequenceSelector.GenerateStatChoices(_model, pickCount, excludedIds);
    }
    
    // ---------- 유저 클릭 처리 ----------
    private void HandleChoice(int index)
    {
        if (index < 0 || index >= _currentChoices.Count) return;
        var selected = _currentChoices[index];

        switch (selected.Type)
        {
            case EnchantType.Skill:
            {
                if (_model.CanAcquireNewSkill(selected.Name_ID, selected.SkillData.SkillGroup_ID))
                {
                    _model.AcquireSkill(selected.Name_ID, selected.SkillData.SkillGroup_ID);
                    ClosePopupAndCountUp(); 
                }
                else
                {
                    _changePresenter.OpenChangePopup(selected); 
                }
                break;
            }
            case EnchantType.Stat:
            {
                if (_model.CanAcquireNewStat(selected.Name_ID, selected.StatData.StatGroup_ID))
                {
                    _model.AcquireStat(selected.Name_ID, selected.StatData.StatGroup_ID);
                    ClosePopupAndCountUp();
                }
                else
                {
                    _changePresenter.OpenChangePopup(selected); 
                }
                break;
            }
        }
    }

    private void HandleSkip()
    {
        ClosePopupAndCountUp();
    }

    private void ClosePopupAndCountUp()
    {
        _model.CountUpDrawCount();
        _navigator.OnCloseButtonClick();
    }
    
    // 리롤: 같은 레벨업에서 카드만 다시 뽑는다(선택 풀·확률은 EnchantSequenceSelector가 처리).
    // 전체 리롤 (현재 차례 타입 유지)
    private void HandleFullReroll()
    {
        Debug.Log($"[Reroll] HandleReroll 진입 remaining={(_unlimitedReroll ? "무한" : _rerollRemaining.ToString())}");
        if (!_unlimitedReroll)
        {
            if (_rerollRemaining <= 0) return;
            _rerollRemaining--;
        }
        
        List<int> excludedIds = new List<int>();
        foreach (var choice in _currentChoices)
        {
            excludedIds.Add(choice.Name_ID);
        }
        
        GenerateChoicesByTurnType(_pickCount, excludedIds);
        DisplayChoicesToView();
    }

    // 개별 카드 리롤 (현재 차례 타입 유지)
    private void HandleCardReroll(int index)
    {
        if (_currentChoices == null) return;
        if (index < 0 || index >= _currentChoices.Count) return;
        
        // 다른 카드들과 똑같은 게 나오면 안 되므로, 현재 떠 있는 3개를 전부 제외 목록에 넣음
        List<int> excludedIds = new List<int>();
        foreach (var choice in _currentChoices)
        {
            excludedIds.Add(choice.Name_ID);
        }

        var newChoice = _currentTurnType == EnchantType.Skill 
            ? _sequenceSelector.GenerateSkillChoices(_model, 1, excludedIds)[0]
            : _sequenceSelector.GenerateStatChoices(_model, 1, excludedIds)[0];
        
        if (!_unlimitedReroll)
        {
            if (_rerollRemaining <= 0) return;
            _rerollRemaining--;
        }

        // 안전장치로 인해 정상적으로 뽑혔다면 교체
        if (newChoice != null)
        {
            _currentChoices[index] = newChoice;
            DisplayChoicesToView();
        }
    }

    // ---------- 보조 함수 ----------
    // UI용 포멧으로 변환 후 전달
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
                TypeLabel = candidate.Type == EnchantType.Skill ? "스킬" : "스탯", 
                Name = $"NameID: {candidate.Name_ID}", 
                Description = candidate.Type == EnchantType.Skill ? 
                              $"데미지: {candidate.SkillData.Dmg}" : 
                              $"수치 증가: {candidate.StatData.Variation_2}"
            };
        }
        _view.SetChoices(displayData);
        // 무한(테스트2 씬)이면 항상 사용 가능 + 남은 횟수 -1(View에서 ∞ 표시).
        _view.SetRerollAvailable(_unlimitedReroll || _baseRerollCount > 0, _unlimitedReroll ? -1 : _rerollRemaining);
    }
}
