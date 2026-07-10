// 작성자 : 김영찬
// 설명 : 조합 슬롯 UI 작동 정의

// 2차 수정자 : 조규민
// 수정 내용 : 조합 인챈트 교체로 레시피가 제거될 때 슬롯 전체를 비울 수 있는 API 추가

using UnityEngine;
using UnityEngine.UI;

public class CombinationSlotUI : MonoBehaviour
{
    [SerializeField] private Image _skillIconImage;
    [SerializeField] private Image[] _ingredientImages; // 조합에 사용되는 책 표기
    [SerializeField] private GameObject[] _borderObjects; // 충족 시 켜질 테두리 오브젝트

    public void SetupSlot(Sprite skillIcon, Sprite[] ingredientSprites)
    {
        gameObject.SetActive(true);
        if (_skillIconImage != null) _skillIconImage.sprite = skillIcon;

        for (int i = 0; i < 3; i++)
        {
            if (_ingredientImages[i] != null)
            {
                _ingredientImages[i].sprite = ingredientSprites[i];
            }
            if (_borderObjects[i] != null) _borderObjects[i].SetActive(false);
        }
    }

    public void FillIngredient(int index)
    {
        if (index >= 0 && index < _borderObjects.Length && _borderObjects[index] != null)
            _borderObjects[index].SetActive(true);
    }

    public void ClearProgress()
    {
        for (int i = 0; i < 3; i++)
        {
            if (_borderObjects[i] != null) _borderObjects[i].SetActive(false);
        }
    }

    public void ClearSlot()
    {
        if (_skillIconImage != null) _skillIconImage.sprite = null;

        for (int i = 0; i < 3; i++)
        {
            if (_ingredientImages[i] != null) _ingredientImages[i].sprite = null;
            if (_borderObjects[i] != null) _borderObjects[i].SetActive(false);
        }

        gameObject.SetActive(false);
    }
}
