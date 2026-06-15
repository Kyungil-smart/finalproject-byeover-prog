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
            // ToDo : 스킬 아이콘 이미지 받아오는거 확정 되면 연결
        }
    }
}
