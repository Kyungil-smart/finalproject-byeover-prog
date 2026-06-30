// 생성자 : 김영찬
// 버튼을 누르면 인첸트 교체 후보가 Before창 테이블에 표기되도록 연결하는 스크립트
// ToDo : UI 참조만 걸려있음으로 스크립트는 인첸트 담당자가 작성해야됨

// 2차 수정자 : 조규민
// 수정 내용 : 보유 인챈트 목록에서 선택된 인챈트 데이터를 Presenter로 전달하고 정보 테이블 미연결 시 NullReference 방지
//           선택 버튼 참조가 비어 있으면 같은 오브젝트의 Button을 사용하도록 방어
//           스킬·스탯 보유 인챈트의 레벨 TMP를 자동 복구하고 Lv.n 형식으로 표시

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnchantChangeSelectButtonUI : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private Image _skillImage;
    [Tooltip("보유 인챈트의 현재 레벨을 표시할 텍스트")]
    [SerializeField] private TextMeshProUGUI _skillLevelText;
    [SerializeField] private Button _selectButton;
    [Tooltip("정보를 넘겨줄 정보테이블 UI")]
    [SerializeField] private EnchantChangeInfoTableUI _infoTableUI;
    
    private EnchantDisplayData _enchantDisplayData;

    public event Action<int> OnEnchantSelected;
    public event Action<EnchantDisplayData> OnEnchantDisplaySelected;

    public EnchantChangeInfoTableUI InfoTableUI => _infoTableUI;

    private void OnEnable()
    {
        Button _button = GetSelectButton();
        if(_button != null)
        {
            _button.onClick.AddListener(OnSelectButtonClick);
        }
    }

    private void OnDisable()
    {
        Button _button = GetSelectButton();
        if(_button != null)
        {
            _button.onClick.RemoveListener(OnSelectButtonClick);
        }
    }

    public void SetInfo(EnchantDisplayData _enchantDisplayData)
    {
        this._enchantDisplayData = _enchantDisplayData;
        if (this._enchantDisplayData == null)
        {
            ClearInfo();
            return;
        }

        SetLevelText(_enchantDisplayData.Level);

        if (_skillImage != null)
        {
            // 추가: 조규민 - 보유 인챈트 선택 버튼에도 같은 ImageKey 기반 아이콘을 표시한다.
            EnchantIconLoader.ApplyIcon(_skillImage, _enchantDisplayData.ImageKey);
        }
    }

    public void ClearInfo()
    {
        _enchantDisplayData = null;

        TextMeshProUGUI _levelText = GetLevelText();
        if (_levelText != null)
        {
            _levelText.text = string.Empty;
        }

        if (_skillImage != null)
        {
            _skillImage.sprite = null;
            _skillImage.enabled = false;
        }
    }

    private void OnSelectButtonClick()
    {
        if(_enchantDisplayData == null) return;
        if (_infoTableUI != null)
        {
            _infoTableUI.SetInfo(_enchantDisplayData);
        }

        OnEnchantSelected?.Invoke(_enchantDisplayData.EnchantId);
        OnEnchantDisplaySelected?.Invoke(_enchantDisplayData);
    }

    private Button GetSelectButton()
    {
        if (_selectButton != null)
        {
            return _selectButton;
        }

        _selectButton = GetComponent<Button>();
        return _selectButton;
    }

    private void SetLevelText(int _level)
    {
        TextMeshProUGUI _levelText = GetLevelText();
        if (_levelText == null)
        {
            Debug.LogWarning("[EnchantChangeSelectButtonUI] 레벨 Text (TMP) 참조를 찾을 수 없습니다.", this);
            return;
        }

        if (!_levelText.gameObject.activeSelf)
        {
            _levelText.gameObject.SetActive(true);
        }

        _levelText.enabled = true;
        _levelText.canvasRenderer.SetAlpha(1f);
        _levelText.SetText("Lv.{0}", Mathf.Max(1, _level));
    }

    private TextMeshProUGUI GetLevelText()
    {
        if (_skillLevelText != null)
        {
            return _skillLevelText;
        }

        _skillLevelText = GetComponentInChildren<TextMeshProUGUI>(true);
        return _skillLevelText;
    }
}
