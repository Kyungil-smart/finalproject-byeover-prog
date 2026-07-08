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
    private readonly LocalizationManager _localizationManager;
    
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
    private bool _isTutorialFixedChoices;
    private bool[] _cardRerollUsed;
    
    // 이번 팝업에서 유저가 한 번이라도 본(등장한) 모든 인챈트 ID 누적용
    private HashSet<int> _seenEnchantIds = new HashSet<int>();

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
        _localizationManager = LocalizationManager.Instance;

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
        if (!HasValidSequenceConfiguration())
        {
            FailSelection("인첸트 등장 순서 설정이 비어 있어 선택 화면을 생성할 수 없습니다.");
            return;
        }

        if (pickCount <= 0)
        {
            FailSelection($"선택 카드 수가 올바르지 않습니다. pickCount={pickCount}");
            return;
        }

        _pickCount = pickCount;
        _rerollRemaining = _baseRerollCount;
        _isTutorialFixedChoices = false;
        
        _seenEnchantIds.Clear();

        if (TutorialFirstEnchantSelectionOverride.TryConsumeFixedChoices(_model, _repo, out _currentChoices))
        {
            _currentTurnType = EnchantType.Skill;
            _isTutorialFixedChoices = true;

            if (!ValidateChoices(_currentChoices, _currentTurnType, "튜토리얼 고정 선택"))
            {
                FailSelection("튜토리얼 고정 선택에 스킬이 아닌 카드가 포함되어 있습니다.");
                return;
            }

            _pickCount = _currentChoices.Count;
            ResetCardRerollState(_currentChoices.Count);
            DisplayChoicesToView();
            return;
        }

        // 현재 몇 번째 뽑기인지에 따라 스킬/스탯 차례 계산
        int sequenceIndex = _model.TotalDrawCount % _sequenceConfig.DrawSequence.Count;
        _currentTurnType = _sequenceConfig.DrawSequence[sequenceIndex];

        // 리세마라 방지: 이 뽑기 순번의 스냅샷이 있으면(강제종료 후 재진입) 그때 본 카드를 그대로 복원한다.
        if (TryRestoreFromSnapshot(_model.TotalDrawCount))
        {
            DisplayChoicesToView();
            return;
        }

        GenerateChoicesByTurnType(pickCount);

        if (!ValidateChoices(_currentChoices, _currentTurnType, "일반 선택"))
        {
            FailSelection("현재 차례와 다른 타입의 카드가 생성되었습니다.");
            return;
        }

        ResetCardRerollState(_currentChoices != null ? _currentChoices.Count : pickCount);
        SaveSnapshot(_model.TotalDrawCount);   // 카드가 화면에 보이기 전에 저장해야 표시-저장 틈의 강제종료 리롤이 안 통한다
        DisplayChoicesToView();
    }

    // 차례에 맞는 종류만 뽑기
    private void GenerateChoicesByTurnType(int pickCount)
    {
        _currentChoices = _currentTurnType == EnchantType.Skill ? 
            _sequenceSelector.GenerateSkillChoices(_model, pickCount, _seenEnchantIds) : 
            _sequenceSelector.GenerateStatChoices(_model, pickCount, _seenEnchantIds);
        
        // 새로 뽑힌 인첸트들을 관측 목록에 누적 추가
        if (_currentChoices == null) return;
        foreach (var choice in _currentChoices)
        {
            _seenEnchantIds.Add(choice.Name_ID);
        }
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
                    AudioManager.Play(SfxId.EnchantSelect);
                    ClearTutorialFixedChoiceState();
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
                    AudioManager.Play(SfxId.EnchantSelect);
                    ClearTutorialFixedChoiceState();
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
        ClearTutorialFixedChoiceState();
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
        if (_isTutorialFixedChoices) return;

        Debug.Log($"[Reroll] HandleReroll 진입 remaining={(_unlimitedReroll ? "무한" : _rerollRemaining.ToString())}");
        if (!_unlimitedReroll)
        {
            if (_rerollRemaining <= 0) return;
            _rerollRemaining--;
        }
        
        AudioManager.Play(SfxId.EnchantReroll);
        GenerateChoicesByTurnType(_pickCount);
        ResetCardRerollState(_currentChoices != null ? _currentChoices.Count : _pickCount);
        SaveSnapshot(_model.TotalDrawCount);   // 리롤 결과도 스냅샷 갱신(남은 리롤 수 포함)
        DisplayChoicesToView();
    }

    // 개별 카드 리롤 (현재 차례 타입 유지)
    private void HandleCardReroll(int index)
    {
        if (_isTutorialFixedChoices) return;
        if (_currentChoices == null) return;
        if (index < 0 || index >= _currentChoices.Count) return;
        if (IsCardRerollUsed(index)) return;

        if (!_unlimitedReroll)
        {
            if (_rerollRemaining <= 0) return;
        }

        List<EnchantCandidate> newChoices = _currentTurnType == EnchantType.Skill 
            ? _sequenceSelector.GenerateSkillChoices(_model, 1, _seenEnchantIds)
            : _sequenceSelector.GenerateStatChoices(_model, 1, _seenEnchantIds);
        if (newChoices == null || newChoices.Count == 0) return;

        var newChoice = newChoices[0];
        if (newChoice == null) return;
        if (newChoice.Type != _currentTurnType)
        {
            FailSelection($"개별 리롤에서 잘못된 타입이 생성되었습니다. expected={_currentTurnType}, actual={newChoice.Type}");
            return;
        }
        
        if (!_unlimitedReroll)
        {
            _rerollRemaining--;
        }
        
        _currentChoices[index] = newChoice;
        MarkCardRerollUsed(index);
        _seenEnchantIds.Add(newChoice.Name_ID);

        AudioManager.Play(SfxId.EnchantReroll);
        SaveSnapshot(_model.TotalDrawCount);   // 개별 리롤 결과도 스냅샷 갱신
        DisplayChoicesToView();
    }

    // ---------- 리세마라 방지 스냅샷 ----------
    // 팝업이 뜬 순간/리롤한 순간의 카드 구성을 계정(CloudData)에 저장해, 강제종료 후 재진입해도
    // 같은 뽑기 순번(TotalDrawCount)에서 같은 카드가 복원되게 한다. 소거는 판 정산에서만(GameManager).
    // 테스트 씬(무한 리롤)과 GameManager 없는 단독 씬에서는 끈다.
    private bool SnapshotEnabled => !_unlimitedReroll && GameManager.Instance != null;

    private bool TryRestoreFromSnapshot(int drawIndex)
    {
        if (!SnapshotEnabled) return false;

        EnchantDrawSnapshot snap = GameManager.Instance.GetEnchantDrawSnapshot(drawIndex);
        if (snap == null || snap.cardNameIds == null || snap.cardNameIds.Count == 0) return false;
        if (snap.cardTypes == null || snap.cardTypes.Count != snap.cardNameIds.Count) return false;
        if (snap.cardGroupIds == null || snap.cardGroupIds.Count != snap.cardNameIds.Count) return false;

        var restored = new List<EnchantCandidate>(snap.cardNameIds.Count);
        for (int i = 0; i < snap.cardNameIds.Count; i++)
        {
            EnchantCandidate candidate = RebuildCandidate((EnchantType)snap.cardTypes[i], snap.cardGroupIds[i], snap.cardNameIds[i]);
            // 하나라도 재조립 불가(만렙/보유상태 변화로 무효)거나 현재 차례 타입과 다르면 스냅샷을 버리고 새로 뽑는다.
            if (candidate == null || candidate.Type != _currentTurnType) return false;
            restored.Add(candidate);
        }

        _currentChoices = restored;
        _pickCount = restored.Count;
        _rerollRemaining = _unlimitedReroll ? 0 : Mathf.Clamp(snap.rerollRemaining, 0, _baseRerollCount);

        ResetCardRerollState(restored.Count);
        if (snap.cardRerollUsed != null)
            for (int i = 0; i < restored.Count && i < snap.cardRerollUsed.Count; i++)
                if (snap.cardRerollUsed[i] != 0) MarkCardRerollUsed(i);

        foreach (var choice in restored) _seenEnchantIds.Add(choice.Name_ID);
        return true;
    }

    // 저장된 (타입, 그룹, 이름)으로 셀렉터와 같은 규칙의 후보를 다시 조립한다.
    // 현재 보유 상태 기준 다음 레벨 데이터가 없으면(만렙/체인 소실) null.
    private EnchantCandidate RebuildCandidate(EnchantType type, int groupId, int nameId)
    {
        if (type == EnchantType.Skill)
        {
            var chain = _repo.GetSkillChainByName(groupId, nameId);
            var nextData = chain?.GetNextLevelData(_model.GetSkillLevel(nameId));
            if (nextData == null) return null;

            return new EnchantCandidate
            {
                Type = EnchantType.Skill, Name_ID = nameId, Specific_ID = nextData.Skill_ID,
                Level = nextData.Level, SkillData = nextData
            };
        }

        var statChain = _repo.GetStatChainByName(groupId, nameId);
        var nextStat = statChain?.GetNextLevelData(_model.GetStatLevel(nameId));
        if (nextStat == null) return null;

        return new EnchantCandidate
        {
            Type = EnchantType.Stat, Name_ID = nameId, Specific_ID = nextStat.StatEnchant_ID,
            Level = nextStat.StatLevel, StatData = nextStat
        };
    }

    private void SaveSnapshot(int drawIndex)
    {
        if (!SnapshotEnabled || _isTutorialFixedChoices || _currentChoices == null) return;

        var snap = new EnchantDrawSnapshot { drawIndex = drawIndex, rerollRemaining = _rerollRemaining };
        for (int i = 0; i < _currentChoices.Count; i++)
        {
            var candidate = _currentChoices[i];
            snap.cardTypes.Add((int)candidate.Type);
            snap.cardGroupIds.Add(candidate.Type == EnchantType.Skill ? candidate.SkillData.SkillGroup_ID : candidate.StatData.StatGroup_ID);
            snap.cardNameIds.Add(candidate.Name_ID);
            snap.cardRerollUsed.Add(IsCardRerollUsed(i) ? 1 : 0);
        }

        GameManager.Instance.SaveEnchantDrawSnapshot(snap);
    }

    // ---------- 보조 함수 ----------
    // UI용 포멧으로 변환 후 전달
    private void DisplayChoicesToView()
    {
        if (!ValidateChoices(_currentChoices, _currentTurnType, "화면 표시"))
        {
            FailSelection("화면 표시 직전에 선택 카드 타입 검증에 실패했습니다.");
            return;
        }

        // 추가: 조규민 - 혼합 선택을 허용하지 않고 검증된 현재 차례 타입으로 제목을 표시한다.
        _view.SetSelectionType(_currentTurnType);

        var displayData = new EnchantDisplayData[_currentChoices.Count];
        for (int i = 0; i < _currentChoices.Count; i++)
        {
            var candidate = _currentChoices[i];
            
            if (_localizationManager == null)
            {
                Debug.LogWarning("No localization manager found. No Localization.");
                displayData[i] = new EnchantDisplayData
                {
                    EnchantId = candidate.Specific_ID,
                    Level = candidate.Level,
                    // 카드 상단에 스킬/스탯 텍스트 표시
                    TypeLabel = candidate.Type == EnchantType.Skill ? "스킬" : "스탯", 
                
                    // 번역 데이터가 없음으로 ID를 출력함
                    Name = $"NameID: {candidate.Name_ID}", 
                    Description = candidate.Type == EnchantType.Skill ? 
                        $"데미지: {candidate.SkillData.Dmg}" : 
                        $"수치 증가: {candidate.StatData.Variation_2}",
                    // 추가: 조규민 - 분리형 선택 흐름에서도 인챈트 아이콘 키를 카드 UI로 전달한다.
                    ImageKey = candidate.Type == EnchantType.Skill ? 
                        $"{candidate.SkillData.SkillIcon_ID}" : 
                        $"{candidate.StatData.Image_ID}"
                };
            }
            else
            {
                displayData[i] = new EnchantDisplayData
                {
                    EnchantId = candidate.Specific_ID,
                    Level = candidate.Level,
                    // 카드 상단에 스킬/스탯 텍스트 표시
                    TypeLabel = candidate.Type == EnchantType.Skill ? "스킬" : "스탯", 
                    Name = _localizationManager.Get(candidate.Name_ID, LocalizingType.Enchant), 
                    Description = candidate.Type == EnchantType.Skill ? 
                        _localizationManager.Get(candidate.SkillData.Skill_Descrip, LocalizingType.Enchant, candidate.SkillData.RequiredValue_1) : 
                        _localizationManager.Get(candidate.StatData.StatDescrip, LocalizingType.Enchant),
                    // 추가: 조규민 - 분리형 선택 흐름에서도 인챈트 아이콘 키를 카드 UI로 전달한다.
                    ImageKey = candidate.Type == EnchantType.Skill ? 
                        $"{candidate.SkillData.SkillIcon_ID}" : 
                        $"{candidate.StatData.Image_ID}"
                };
            }
        }
        _view.SetChoices(displayData);
        // 무한(테스트2 씬)이면 항상 사용 가능 + 남은 횟수 -1(View에서 ∞ 표시).
        bool rerollAvailable = !_isTutorialFixedChoices && (_unlimitedReroll || _baseRerollCount > 0);
        int rerollRemaining = _isTutorialFixedChoices ? 0 : (_unlimitedReroll ? -1 : _rerollRemaining);
        _view.SetRerollAvailable(rerollAvailable, rerollRemaining);
        _view.SetCardRerollAvailable(BuildCardRerollAvailability());
    }

    private bool HasValidSequenceConfiguration()
    {
        return _sequenceConfig != null
            && _sequenceConfig.DrawSequence != null
            && _sequenceConfig.DrawSequence.Count > 0;
    }

    private bool ValidateChoices(List<EnchantCandidate> choices, EnchantType expectedType, string context)
    {
        if (choices == null || choices.Count == 0)
        {
            Debug.LogError($"[EnchantSequenceSelectPresenter] {context} 선택지가 비어 있습니다.");
            return false;
        }

        for (int i = 0; i < choices.Count; i++)
        {
            EnchantCandidate choice = choices[i];
            if (choice == null)
            {
                Debug.LogError($"[EnchantSequenceSelectPresenter] {context} 선택지에 null 카드가 있습니다. index={i}");
                return false;
            }

            if (choice.Type != expectedType)
            {
                Debug.LogError(
                    $"[EnchantSequenceSelectPresenter] {context} 선택지 타입이 일치하지 않습니다. " +
                    $"index={i}, expected={expectedType}, actual={choice.Type}, nameId={choice.Name_ID}");
                return false;
            }
        }

        return true;
    }

    private void FailSelection(string reason)
    {
        Debug.LogError($"[EnchantSequenceSelectPresenter] 인첸트 선택 화면을 안전하게 종료합니다. reason={reason}");
        ClearTutorialFixedChoiceState();
        _currentChoices = null;
        _view.SetRerollAvailable(false, 0);
        _view.SetCardRerollAvailable(System.Array.Empty<bool>());
        _navigator.OnCloseButtonClick();
    }

    private void ClearTutorialFixedChoiceState()
    {
        if (!_isTutorialFixedChoices) return;

        _isTutorialFixedChoices = false;
        TutorialFirstEnchantSelectionOverride.ClearFixedChoiceState();
    }

    private void ResetCardRerollState(int count)
    {
        _cardRerollUsed = new bool[Mathf.Max(0, count)];
    }

    private bool IsCardRerollUsed(int index)
    {
        return _cardRerollUsed != null
            && index >= 0
            && index < _cardRerollUsed.Length
            && _cardRerollUsed[index];
    }

    private void MarkCardRerollUsed(int index)
    {
        if (_cardRerollUsed == null || index < 0 || index >= _cardRerollUsed.Length)
        {
            return;
        }

        _cardRerollUsed[index] = true;
    }

    private bool[] BuildCardRerollAvailability()
    {
        int count = _currentChoices != null ? _currentChoices.Count : 0;
        bool[] availableByIndex = new bool[count];
        bool hasGlobalReroll = !_isTutorialFixedChoices && (_unlimitedReroll || _rerollRemaining > 0);

        for (int i = 0; i < count; i++)
        {
            availableByIndex[i] = hasGlobalReroll && !IsCardRerollUsed(i);
        }

        return availableByIndex;
    }
}
