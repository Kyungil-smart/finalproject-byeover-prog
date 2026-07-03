using System;
using System.Collections.Generic;

public interface IEnchantListView
{
    void SetOwnedSkillList(List<EnchantDisplayData> _ownedSkillList);
    void SetOwnedStatList(List<EnchantDisplayData> _ownedStatList);
    void SetSelectedSkillEnchantInfo(EnchantDisplayData _selectedData);
    void SetSelectedStatEnchantInfo(EnchantDisplayData _selectedData);
    void ClearSelectedSkillEnchantInfo();
    void ClearSelectedStatEnchantInfo();
    event Action<bool> OnEnabled;
    event Action<EnchantDisplayData> OnSkillEnchantSelected;
    event Action<EnchantDisplayData> OnStatEnchantSelected;
}
