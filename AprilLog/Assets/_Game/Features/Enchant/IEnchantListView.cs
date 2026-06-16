using System.Collections.Generic;

public interface IEnchantListView
{
    void SetOwnedSkillList(List<EnchantDisplayData> ownedSkillList);
    void SetOwnedStatList(List<EnchantDisplayData> ownedStatList);
}
