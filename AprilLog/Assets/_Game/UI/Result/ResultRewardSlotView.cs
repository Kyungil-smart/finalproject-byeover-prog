//담당자: 조규민

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 정산 팝업의 재화 보상 슬롯 하나를 표시합니다.
/// </summary>
public class ResultRewardSlotView : MonoBehaviour
{
    [Header("보상 슬롯")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _labelText;
    [SerializeField] private TMP_Text _amountText;

    public Sprite CurrentIcon => _iconImage != null ? _iconImage.sprite : null;

    public void InitializeIfNeeded()
    {
        if (_iconImage == null)
        {
            Image[] _images = GetComponentsInChildren<Image>(true);
            for (int _index = 0; _index < _images.Length; _index++)
            {
                if (_images[_index].name.Contains("Icon"))
                {
                    _iconImage = _images[_index];
                    break;
                }
            }

            if (_iconImage == null && _images.Length > 0)
            {
                _iconImage = _images[0];
            }
        }

        TMP_Text[] _texts = GetComponentsInChildren<TMP_Text>(true);
        if (_labelText == null && _texts.Length > 0)
        {
            _labelText = _texts[0];
        }

        if (_amountText == null)
        {
            _amountText = _texts.Length > 1 ? _texts[1] : _labelText;
        }
    }

    public void SetReward(ResultRewardEntry _entry, Sprite _iconSprite, string _amountTextValue)
    {
        InitializeIfNeeded();
        gameObject.SetActive(true);

        if (_iconImage != null)
        {
            _iconImage.sprite = _iconSprite;
            _iconImage.enabled = _iconSprite != null;
        }

        if (_labelText != null)
        {
            _labelText.text = _entry._label;
        }

        if (_amountText != null)
        {
            _amountText.text = _amountTextValue;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
