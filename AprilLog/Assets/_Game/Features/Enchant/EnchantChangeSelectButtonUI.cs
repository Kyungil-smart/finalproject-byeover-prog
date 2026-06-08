// 생성자 : 김영찬
// 버튼을 누르면 인첸트 교체 후보가 Before창 테이블에 표기되도록 연결하는 스크립트
// ToDo : UI 참조만 걸려있음으로 스크립트는 인첸트 담당자가 작성해야됨

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnchantChangeSelectButtonUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image _skillImage;
    [SerializeField] private TextMeshProUGUI _skillLevelText;
    [SerializeField] private Button _selectButton;
    [Tooltip("정보를 넘겨줄 정보테이블 UI")]
    [SerializeField] private EnchantChangeInfoTableUI _infoTableUI;
}
