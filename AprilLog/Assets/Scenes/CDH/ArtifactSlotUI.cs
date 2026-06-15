using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class ArtifactSlotUI : MonoBehaviour
{
    public Image artifactIcon;
    public TextMeshProUGUI countText;

    private ArtifactInstance _data;

    public void Setup(ArtifactInstance data)
    {
        _data = data;

        string iconPath = data.MasterData.IconSprite;
        artifactIcon.sprite = Resources.Load<Sprite>(iconPath);

        int maxLimit = GetMaxLimit(data.MasterData.GearGrade);
        countText.text = $"{data.CurrentCount}/{maxLimit}";
    }

    private int GetMaxLimit(string grade)
    {
        if (grade == "Rare") return 2;
        if (grade == "Epic") return 4;
        return 6; // Legendary
    }
}
