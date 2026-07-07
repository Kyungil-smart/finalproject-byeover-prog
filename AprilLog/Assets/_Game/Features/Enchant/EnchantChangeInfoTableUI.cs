// 생성자 : 김영찬
// 인첸트 교체 UI에 필요한 인첸트 비교 창을 구동하기 위한 스크립트

// 2차 수정자 : 조규민
// 수정 내용 : 보유 인챈트 상세 정보 갱신 시 빈 데이터 방어 및 정보 초기화 기능 추가

using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 3차 수정자 : 조규민
// 수정 내용 : 교체 정보 테이블의 SkillImage/Image 구조 자동 참조 보강
public class EnchantChangeInfoTableUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image _skillImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _typeText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    public void SetInfo(EnchantDisplayData _newData)
    {
        ResolveMissingReferences();

        if (_newData == null)
        {
            ClearInfo();
            return;
        }

        if(_nameText != null)
        {
            SetTextVisible(_nameText, true);
            _nameText.text = _newData.Name;
        }
        
        if(_typeText != null)
        {
            SetTextVisible(_typeText, true);
            _typeText.text = _newData.TypeLabel;
        }

        if (_levelText != null)
        {
            SetTextVisible(_levelText, true);
            _levelText.SetText("Lv.{0}", Mathf.Max(1, _newData.Level));
        }
        
        if(_descriptionText != null)
        {
            SetTextVisible(_descriptionText, true);
            _descriptionText.text = _newData.Description;
        }
        
        if (_skillImage != null)
        {
            if (!_skillImage.gameObject.activeSelf)
            {
                _skillImage.gameObject.SetActive(true);
            }

            _skillImage.enabled = true;
            // 추가: 조규민 - 교체 확인 정보 테이블에도 선택 카드와 같은 인챈트 아이콘을 표시한다.
            EnchantIconLoader.ApplyIcon(_skillImage, _newData.ImageKey);
        }
    }

    public void ClearInfo()
    {
        ResolveMissingReferences();

        if (_nameText != null)
        {
            _nameText.text = string.Empty;
            SetTextVisible(_nameText, false);
        }

        if (_typeText != null)
        {
            _typeText.text = string.Empty;
            SetTextVisible(_typeText, false);
        }

        if (_levelText != null)
        {
            _levelText.text = string.Empty;
            SetTextVisible(_levelText, false);
        }

        if (_descriptionText != null)
        {
            _descriptionText.text = string.Empty;
            SetTextVisible(_descriptionText, false);
        }

        if (_skillImage != null)
        {
            _skillImage.sprite = null;
            _skillImage.enabled = false;
            _skillImage.gameObject.SetActive(false);
        }
    }

    private void SetTextVisible(TextMeshProUGUI _text, bool _isVisible)
    {
        if (_text == null)
        {
            return;
        }

        _text.enabled = _isVisible;
        _text.gameObject.SetActive(_isVisible);
    }

    private void ResolveMissingReferences()
    {
        Image _namedInfoImage = FindImageByName("SkillEnchantInfoImage") ?? FindImageByName("StatEnchantInfoImage");
        if (_namedInfoImage != null)
        {
            _skillImage = _namedInfoImage;
        }

        if (_skillImage == null)
        {
            _skillImage = FindImageByParentName("SkillImage") ?? FindImageByName("Image");
        }

        if (_nameText == null)
        {
            _nameText = FindTextByParentName("NameArea") ?? FindFirstTextByObjectName("NameText (TMP)") ?? FindFirstTextByObjectName("Text (TMP)");
        }

        if (_typeText == null)
        {
            _typeText = FindTextByParentName("TypeArea") ?? FindFirstTextByObjectName("TypeText (TMP)");
        }

        if (_levelText == null)
        {
            _levelText = FindLevelText();
        }

        if (_descriptionText == null)
        {
            _descriptionText = FindTextByParentName("DescriptionArea") ?? FindFirstTextByObjectName("EnchantChangeGuideText (TMP)");
        }
    }

    private Image FindImageByName(string _objectName)
    {
        Image[] _images = GetComponentsInChildren<Image>(true);
        for (int _index = 0; _index < _images.Length; _index++)
        {
            Image _image = _images[_index];
            if (_image != null && _image.gameObject.name == _objectName)
            {
                return _image;
            }
        }

        return null;
    }

    private Image FindImageByParentName(string _parentName)
    {
        Image[] _images = GetComponentsInChildren<Image>(true);
        for (int _index = 0; _index < _images.Length; _index++)
        {
            Image _image = _images[_index];
            if (_image == null || _image.transform.parent == null)
            {
                continue;
            }

            if (_image.transform.parent.name == _parentName)
            {
                return _image;
            }
        }

        return null;
    }

    private TextMeshProUGUI FindLevelText()
    {
        if (_skillImage != null)
        {
            TextMeshProUGUI _imageLevelText = _skillImage.GetComponentInChildren<TextMeshProUGUI>(true);
            if (_imageLevelText != null)
            {
                return _imageLevelText;
            }
        }

        TextMeshProUGUI _namedLevelText = FindFirstTextByObjectName("LevelText (TMP)");
        if (_namedLevelText != null)
        {
            return _namedLevelText;
        }

        TextMeshProUGUI[] _texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int _index = 0; _index < _texts.Length; _index++)
        {
            TextMeshProUGUI _text = _texts[_index];
            if (_text != null && _text.text.StartsWith("Lv."))
            {
                return _text;
            }
        }

        return null;
    }

    private TextMeshProUGUI FindTextByParentName(string _parentName)
    {
        TextMeshProUGUI[] _texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int _index = 0; _index < _texts.Length; _index++)
        {
            TextMeshProUGUI _text = _texts[_index];
            if (_text == null || _text.transform.parent == null)
            {
                continue;
            }

            if (_text.transform.parent.name == _parentName)
            {
                return _text;
            }
        }

        return null;
    }

    private TextMeshProUGUI FindFirstTextByObjectName(string _objectName)
    {
        TextMeshProUGUI[] _texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int _index = 0; _index < _texts.Length; _index++)
        {
            TextMeshProUGUI _text = _texts[_index];
            if (_text != null && _text.gameObject.name == _objectName)
            {
                return _text;
            }
        }

        return null;
    }
}
