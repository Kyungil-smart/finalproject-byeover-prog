using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 홍정옥
// 설명   : 뽑기 결과창의 슬롯 1칸. 뽑힌 Gear_ID 하나를 받아 아이콘/등급 테두리를 표시
//          아이콘 소스가 아직 없으면 등급 색 테두리만 보여주고, 있으면 Resources 에서 IconSprite 이름으로 로드
public class GachaResultSlotView : MonoBehaviour
{
    [Header("표시 대상")]
    [Tooltip("기어 아이콘 Image (프리팹의 Icon 오브젝트)")]
    [SerializeField] private Image _iconImage;
    [Tooltip("등급 색을 입힐 테두리/배경 Image (없으면 비워둬도 됨)")]
    [SerializeField] private Image _frameImage;
    [Tooltip("등급/이름 텍스트 (선택)")]
    [SerializeField] private TMP_Text _label;

    [Header("등급 색")]
    [SerializeField] private Color _rareColor      = new Color(0.30f, 0.55f, 1.00f);
    [SerializeField] private Color _epicColor      = new Color(0.65f, 0.35f, 0.95f);
    [SerializeField] private Color _legendaryColor = new Color(1.00f, 0.78f, 0.25f);

    [Tooltip("아이콘 Sprite 를 Resources 에서 찾을 때의 폴더 경로 (예: Resources/Icons/Gear -> \"Icons/Gear\")")]
    [SerializeField] private string _iconResourceFolder = "Icons/Gear";

    // 외부(Presenter)에서 호출. 뽑힌 Gear_ID 하나를 슬롯에 반영한다.
    public void SetData(int gearId)
    {
        gameObject.SetActive(true);

        GearMasterData gear = DataManager.Instance != null && DataManager.Instance.GearRepo != null
            ? DataManager.Instance.GearRepo.GetGearData(gearId)
            : null;

        if (gear == null)
        {
            Debug.LogWarning($"[GachaResultSlotView] Gear_ID {gearId} 데이터를 찾지 못했습니다.", this);
            Clear();
            return;
        }

        // 등급 테두리 색
        if (_frameImage != null)
            _frameImage.color = GradeToColor(gear.GearGrade);

        // 아이콘 (소스가 준비된 경우에만)
        if (_iconImage != null)
        {
            Sprite icon = LoadIcon(gear.IconSprite);
            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
        }

        // 임시 라벨 (현지화 연결 전까지 'Grade #ID')
        if (_label != null)
            _label.text = $"{gear.GearGrade} #{gear.Gear_ID}";
    }

    // 슬롯 비우기(미사용 슬롯 숨김용)
    public void Clear()
    {
        if (_iconImage != null) _iconImage.enabled = false;
        if (_label != null) _label.text = string.Empty;
        gameObject.SetActive(false);
    }

    private Sprite LoadIcon(string iconName)
    {
        if (string.IsNullOrEmpty(iconName))
            return null;

        string path = string.IsNullOrEmpty(_iconResourceFolder)
            ? iconName
            : $"{_iconResourceFolder}/{iconName}";

        return Resources.Load<Sprite>(path);
    }

    private Color GradeToColor(string grade)
    {
        switch (grade)
        {
            case "Legendary": return _legendaryColor;
            case "Epic":      return _epicColor;
            default:          return _rareColor; // Rare 및 알 수 없는 등급
        }
    }
}
