// 생성자 : 김영찬
// 인첸트 교체 UI에 필요한 인첸트 비교 창을 구동하기 위한 스크립트

// 2차 수정자 : 조규민
// 수정 내용 : 보유 인챈트 상세 정보 갱신 시 빈 데이터 방어 및 정보 초기화 기능 추가

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

    public void SetInfo(EnchantDisplayData _newData)
    {
        if (_newData == null)
        {
            ClearInfo();
            return;
        }

        if(_nameText != null)
        {
            _nameText.text = _newData.Name;
        }
        
        if(_typeText != null)
        {
            _typeText.text = _newData.TypeLabel;
        }
        
        if(_descriptionText != null)
        {
            _descriptionText.text = _newData.Description;
        }
        
        if (_skillImage != null)
        {
            // 추가: 조규민 - 교체 확인 정보 테이블에도 선택 카드와 같은 인챈트 아이콘을 표시한다.
            EnchantIconLoader.ApplyIcon(_skillImage, _newData.ImageKey);
        }
    }

    public void ClearInfo()
    {
        if (_nameText != null)
        {
            _nameText.text = string.Empty;
        }

        if (_typeText != null)
        {
            _typeText.text = string.Empty;
        }

        if (_descriptionText != null)
        {
            _descriptionText.text = string.Empty;
        }

        if (_skillImage != null)
        {
            _skillImage.sprite = null;
            _skillImage.enabled = false;
        }
    }
}
