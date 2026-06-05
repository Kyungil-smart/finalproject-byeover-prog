// 담당자 : 김영찬
// 설명   : 개별 인챈트 카드 프리팹의 비주얼 제어 및 클릭 이벤트 포워딩

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnchantCardUI : MonoBehaviour
{
    [Header("UI 컴포넌트 참조")]
    [SerializeField] private TextMeshProUGUI _typeText;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private Image _iconImage;
    
    [Header("버튼 세팅")]
    [SerializeField] private Button _selectButton;

    // 카드가 클릭되었을 때 이 카드가 몇 번째 인덱스(순서)인지 부모 뷰에 알리기 위한 이벤트
    public System.Action OnCardClicked;

    private void Awake()
    {
        if (_selectButton == null)
        {
            _selectButton = GetComponent<Button>();
        }
    }

    private void OnEnable()
    {
        if (_selectButton != null)
        {
            _selectButton.onClick.AddListener(() => OnCardClicked?.Invoke());
        }
    }

    private void OnDisable()
    {
        if (_selectButton != null)
        { 
            _selectButton.onClick.RemoveAllListeners();
        }
    }

    /// <summary>
    /// 실제 데이터 구조를 받아 UI 텍스트를 갱신
    /// </summary>
    public void Setup(Legacy_EnchantDisplayData data)
    {
        if (data == null) return;

        // 인챈트 타입 반영 (예: "스킬 인첸트", "스텟 인첸트" 등)
        if (_typeText != null) 
            _typeText.text = GetEnchantTypeText(data.EnchantId);
        
        // 인챈트 이름 반영 (예: "공격력 증가", "체인 라이트닝" 등)
        if (_nameText != null) 
            _nameText.text = data.Name;
        
        // 인챈트 상세 설명 반영
        if (_descriptionText != null)
        {
            _descriptionText.text = data.Description; 
        }

        // 인첸트 아이콘 반영
        if (_iconImage != null && data.ImageKey != null)
        {
            // ToDo : 스킬 이미지 어떻게 전달되는지에 따라 이미지 삽입 형태 변경
        }
    }

    private string GetEnchantTypeText(int enchantId)
    {
        // ToDo : 인첸트 아이디 형식에 따라 분류할 것.
        return "temp";
    }
}