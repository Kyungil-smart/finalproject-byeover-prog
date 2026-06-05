// 생성자 : 김영찬
// 인첸트 교체 UI에 필요한 인첸트 비교 창을 구동하기 위한 스크립트
// ToDo : UI 참조만 걸려있음으로 스크립트는 인첸트 담당자가 작성해야됨

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
}
