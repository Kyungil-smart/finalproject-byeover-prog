// 담당자 : 정승우
// 설명   : 화면(팝업) 전환 전담 - SRP: 이것만 Show/Hide 한다

using UnityEngine;

/// <summary>
/// 인게임 팝업의 Show/Hide를 관리한다.
/// 어떤 Presenter도 다른 View를 직접 조작하지 않고 여기한테 요청한다.
/// </summary>
public class ScreenNavigator : MonoBehaviour
{
    // ---------- SerializeField ----------
    [Header("팝업 참조")]
    [Tooltip("인챈트 선택 팝업")]
    [SerializeField] private GameObject _enchantPopup;

    [Tooltip("정산 화면")]
    [SerializeField] private GameObject _settlementPopup;

    [Tooltip("옵션 팝업")]
    [SerializeField] private GameObject _optionPopup;

    // ---------- 인챈트 선택 ----------
    public void ShowEnchantSelection()
    {
        Time.timeScale = 0f;    // 전투 일시정지 (기획서)
        if (_enchantPopup != null)
            _enchantPopup.SetActive(true);
    }

    public void HideEnchantSelection()
    {
        if (_enchantPopup != null)
            _enchantPopup.SetActive(false);
        Time.timeScale = 1f;
    }

    // ---------- 정산 ----------
    public void ShowSettlement()
    {
        if (_settlementPopup != null)
            _settlementPopup.SetActive(true);
    }

    public void HideSettlement()
    {
        if (_settlementPopup != null)
            _settlementPopup.SetActive(false);
    }

    // ---------- 옵션 ----------
    public void ShowOption()
    {
        Time.timeScale = 0f;
        if (_optionPopup != null)
            _optionPopup.SetActive(true);
    }

    public void HideOption()
    {
        if (_optionPopup != null)
            _optionPopup.SetActive(false);
        Time.timeScale = 1f;
    }
}
