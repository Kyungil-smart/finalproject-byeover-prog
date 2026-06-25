// 생성자 : 김영찬
// 인첸트 교체 UI에 필요한 인첸트 비교 창을 구동하기 위한 스크립트

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnchantChangeInfoTableUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image _skillImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _typeText;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    public void SetInfo(EnchantDisplayData newData)
    {
        if(_nameText != null)
        {
            _nameText.text = newData.Name;
        }
        
        if(_typeText != null)
        {
            _typeText.text = newData.TypeLabel;
        }
        
        if(_descriptionText != null)
        {
            _descriptionText.text = newData.Description;
        }
        
        if (_skillImage != null)
        {
            // 추가: 조규민 - 교체 확인 정보 테이블에도 선택 카드와 같은 인챈트 아이콘을 표시한다.
            EnchantIconLoader.ApplyIcon(_skillImage, newData.ImageKey);
        }
    }
}
