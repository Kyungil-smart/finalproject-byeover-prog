// 생성자 : 김영찬
// 인첸트 교체 UI를 구동하기 위한 스크립트 -- View
// ToDo : UI 참조만 걸려있음으로 스크립트는 인첸트 담당자가 작성해야됨

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnchantChangeView : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button[] _changeSkillSelectButtons;
    [SerializeField] private EnchantChangeInfoTableUI _afterEnchantChangeInfoTable;

    [Tooltip("교체를 실행하는 버튼")] 
    [SerializeField] private Button _changeExecuteButton;
    [SerializeField] private Button _cancelButton;
    
    [Header("번역 대비")]
    [SerializeField] private TextMeshProUGUI _headerText;
    [SerializeField] private TextMeshProUGUI _changeExecuteButtonText;
    [SerializeField] private TextMeshProUGUI _cancelButtonText;
}
