using UnityEngine;
using UnityEngine.UI;

public class JokerData : MonoBehaviour
{
    public int unitID = 1006;
    public UnitTableData myData;
    public Image iconImage;

    private void OnEnable()
    {
        var manager = Object.FindFirstObjectByType<UnitDataManager>();
        if (manager != null)
        {
            myData = manager.GetUnitData(5);

            if (myData != null)
            {
                if (myData.UnitSprite == null)
                    myData.UnitSprite = Resources.Load<Sprite>($"Units/{myData.Unit_Image_ID}");

                if (iconImage != null)
                {
                    iconImage.sprite = myData.UnitSprite;
                    iconImage.enabled = true;
                }
                Debug.Log($"[조커] {gameObject.name} 데이터 로드 완료: {myData.Name}");
            }
        }
    }
}
