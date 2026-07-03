// 수정자 : 조규민
// 수정 내용 :
// 저장된 인챈트 복원 후 교체 창 목록이 비어 보이지 않도록 UI 표시 목록 갱신 경로 추가
// 보유 인챈트 목록의 스킬·스탯 레벨을 테이블 행이 아닌 실제 획득 상태 기준으로 표시

using System;
using System.Collections.Generic;
using UnityEngine;

public class EnchantUIModel : MonoBehaviour
{
    // ---------- 직렬화 ----------
    [SerializeField] private EnchantModel _model;
    
    // ---------- UI용으로 가공된 정보 ----------
    public List<EnchantDisplayData> OwnedSkillList { get ; private set; }
    public List<EnchantDisplayData> OwnedStatList { get ; private set; }
    
    // ---------- private ----------
    private LocalizationManager _localizationManager;
    
    // ---------- 이벤트 ----------
    public event Action OnSkillListChanged;
    public event Action OnStatListChanged;
    
    // ---------- 초기화 ----------
    public void InitUIModel()
    {
        OwnedSkillList = new ();
        OwnedStatList = new ();

        // 머지 후 씬 미배선 방어: 역참조(EnchantModel)가 안 꽂혀 있으면 같은 오브젝트에서 찾는다.
        if (_model == null) _model = GetComponent<EnchantModel>();
        if (_model == null)
        {
            Debug.LogError("[EnchantUIModel] Enchant Model Not Found. Init Failed.");
            return; // 그래도 없으면 이벤트 구독 스킵 (크래시 방지)
        }
        
        _localizationManager = LocalizationManager.Instance;

        UnbindModelEvents();
        _model.OnSkillAcquired += HandleSkillRefresh;
        _model.OnSkillLevelUp += HandleSkillRefresh;
        _model.OnSkillRemoved += HandleSkillRefresh;
        _model.OnStatAcquired += HandleStatRefresh;
        _model.OnStatLevelUp += HandleStatRefresh;
        _model.OnStatRemoved += HandleStatRefresh;
    }

    public void RefreshAll()
    {
        EnsureLists();

        if (_model == null)
        {
            _model = GetComponent<EnchantModel>();
        }

        if (_model == null)
        {
            Debug.LogError("[EnchantUIModel] Enchant Model Not Found. Refresh Failed.");
            return;
        }

        _localizationManager = LocalizationManager.Instance;
        RefreshSkillView();
        RefreshStatView();
    }
    
    // ---------- 이벤트 핸들러 ----------
    private void HandleSkillRefresh(int nameId, int level)
    {
        RefreshSkillView();
    }

    private void HandleSkillRefresh(int nameId)
    {
        RefreshSkillView();
    }
    
    private void HandleStatRefresh(int nameId, int level)
    {
        RefreshStatView();
    }

    private void HandleStatRefresh(int nameId)
    {
        RefreshStatView();
    }
    
    private void RefreshSkillView()
    {
        EnsureLists();
        OwnedSkillList.Clear();

        if (_model.OwnedSkills.Count > 0)
        {
            foreach (var pair in _model.OwnedSkills)
            {
                var data = pair.Value;
                if (_localizationManager == null)
                {
                    OwnedSkillList.Add(new EnchantDisplayData
                    {
                        EnchantId = pair.Key,
                        Level = data.Level,
                        TypeLabel = "스킬",
                        Name = $"Skill_ID: {data.Data.Name}",
                        Description = $"Description_ID: {data.Data.Skill_Descrip}",
                        ImageKey = $"{data.Data.SkillIcon_ID}"
                    });
                }
                else
                {
                    OwnedSkillList.Add(new EnchantDisplayData
                    {
                        EnchantId = pair.Key,
                        Level = data.Level,
                        TypeLabel = "스킬",
                        Name = _localizationManager.Get(data.Data.Name, LocalizingType.Enchant),
                        Description = _localizationManager.Get(data.Data.Skill_Descrip, LocalizingType.Enchant),
                        ImageKey = $"{data.Data.SkillIcon_ID}"
                    });
                }
            }
        }
        
        OnSkillListChanged?.Invoke();
    }

    private void RefreshStatView()
    {
        EnsureLists();
        OwnedStatList.Clear();
        
        if (_model.OwnedStats.Count > 0)
        {
            foreach (var pair in _model.OwnedStats)
            {
                var data = pair.Value;
                if (_localizationManager == null)
                {
                    Debug.LogWarning("No localization manager found. No Localization.");
                    OwnedStatList.Add(new EnchantDisplayData
                    {
                        EnchantId = pair.Key,
                        Level = data.Level,
                        TypeLabel = "스텟",
                        Name = $"Skill_ID: {data.Data.StatName}",
                        Description = $"Description_ID: {data.Data.StatDescrip}",
                        ImageKey = $"{data.Data.Image_ID}"
                    });
                }
                else
                {
                    OwnedStatList.Add(new EnchantDisplayData
                    {
                        EnchantId = pair.Key,
                        Level = data.Level,
                        TypeLabel = "스텟",
                        Name = _localizationManager.Get(data.Data.StatName, LocalizingType.Enchant),
                        Description = _localizationManager.Get(data.Data.StatDescrip, LocalizingType.Enchant),
                        ImageKey = $"{data.Data.Image_ID}"
                    });
                }
            }
        }
        
        OnStatListChanged?.Invoke();
    }

    public void Discard()
    {
        if (_model == null)
        {
            return;
        }

        UnbindModelEvents();
    }

    private void EnsureLists()
    {
        if (OwnedSkillList == null)
        {
            OwnedSkillList = new List<EnchantDisplayData>();
        }

        if (OwnedStatList == null)
        {
            OwnedStatList = new List<EnchantDisplayData>();
        }
    }

    private void UnbindModelEvents()
    {
        _model.OnSkillAcquired -= HandleSkillRefresh;
        _model.OnSkillLevelUp -= HandleSkillRefresh;
        _model.OnSkillRemoved -= HandleSkillRefresh;
        _model.OnStatAcquired -= HandleStatRefresh;
        _model.OnStatLevelUp -= HandleStatRefresh;
        _model.OnStatRemoved -= HandleStatRefresh;
    }
}
