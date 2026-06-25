// 생성자 : 김영찬
// 버튼을 누르면 인첸트 교체 후보가 Before창 테이블에 표기되도록 연결하는 스크립트
// ToDo : UI 참조만 걸려있음으로 스크립트는 인첸트 담당자가 작성해야됨

using System;
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
    
    private EnchantDisplayData _enchantDisplayData;

    public event Action<int> OnEnchantSelected;

    private void OnEnable()
    {
        if(_selectButton != null)
        {
            _selectButton.onClick.AddListener(OnSelectButtonClick);
        }
    }

    private void OnDisable()
    {
        if(_selectButton != null)
        {
            _selectButton.onClick.RemoveListener(OnSelectButtonClick);
        }
    }

    public void SetInfo(EnchantDisplayData enchantDisplayData)
    {
        _enchantDisplayData = enchantDisplayData;
        if(_skillLevelText != null)
        {
            _skillLevelText.text = _enchantDisplayData.Level.ToString();
        }

        if (_skillImage != null)
        {
            // 추가: 조규민 - 보유 인챈트 선택 버튼에도 같은 ImageKey 기반 아이콘을 표시한다.
            EnchantIconLoader.ApplyIcon(_skillImage, _enchantDisplayData.ImageKey);
        }
    }

    private void OnSelectButtonClick()
    {
        if(_enchantDisplayData == null) return;
        _infoTableUI.SetInfo(_enchantDisplayData);
        OnEnchantSelected?.Invoke(_enchantDisplayData.EnchantId);
    }
}
