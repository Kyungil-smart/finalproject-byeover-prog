// 담당자 : 홍정옥
// 설명   : 로비 설정 버튼 → 설정 팝업 열기/닫기 연결
//          ScreenNavigator 없이도 팝업이 정상 닫히도록 OnCloseClicked를 직접 구독

using UnityEngine;
using UnityEngine.UI;

public class LobbyOptionPopupConnector : MonoBehaviour
{
    [Header("설정 버튼")]
    [SerializeField] private Button _settingButton;

    [Header("설정 팝업")]
    [SerializeField] private OptionView _optionView;

    private void Awake()
    {
        if (_settingButton != null)
            _settingButton.onClick.AddListener(OpenPopup);
        else
            Debug.LogWarning("[LobbyOptionPopupConnector] 설정 버튼이 연결되지 않았습니다.", this);

        if (_optionView != null)
            _optionView.OnCloseClicked += ClosePopup;
        else
            Debug.LogWarning("[LobbyOptionPopupConnector] OptionView가 연결되지 않았습니다.", this);
    }

    private void OnDestroy()
    {
        if (_settingButton != null)
            _settingButton.onClick.RemoveListener(OpenPopup);

        if (_optionView != null)
            _optionView.OnCloseClicked -= ClosePopup;
    }

    private void OpenPopup()
    {
        _optionView?.Show();
    }

    private void ClosePopup()
    {
        _optionView?.Hide();
    }
}
