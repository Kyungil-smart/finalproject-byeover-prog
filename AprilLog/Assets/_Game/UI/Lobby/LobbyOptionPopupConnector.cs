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

    [Header("시나리오 다시보기 버튼")]
    [SerializeField] private Button _replayStoryButton;

    [Header("시나리오 다시보기 팝업")]
    [SerializeField] private ReplayStoryPopup _replayStoryPopup;

    private void Awake()
    {
        ResolveMissingReferences();

        if (_settingButton != null)
            _settingButton.onClick.AddListener(OpenPopup);
        else
            Debug.LogWarning("[LobbyOptionPopupConnector] 설정 버튼이 연결되지 않았습니다.", this);

        if (_optionView != null)
            _optionView.OnCloseClicked += ClosePopup;
        else
            Debug.LogWarning("[LobbyOptionPopupConnector] OptionView가 연결되지 않았습니다.", this);

        if (_replayStoryButton != null)
            _replayStoryButton.onClick.AddListener(OpenReplayStoryPopup);
        else
            Debug.LogWarning("[LobbyOptionPopupConnector] 시나리오 다시보기 버튼이 연결되지 않았습니다.", this);

        if (_replayStoryPopup == null)
            Debug.LogWarning("[LobbyOptionPopupConnector] ReplayStoryPopup이 연결되지 않았습니다.", this);
    }

    private void OnDestroy()
    {
        if (_settingButton != null)
            _settingButton.onClick.RemoveListener(OpenPopup);

        if (_optionView != null)
            _optionView.OnCloseClicked -= ClosePopup;

        if (_replayStoryButton != null)
            _replayStoryButton.onClick.RemoveListener(OpenReplayStoryPopup);
    }

    private void OpenPopup()
    {
        _optionView?.Show();
    }

    private void ClosePopup()
    {
        _optionView?.Hide();
    }

    private void OpenReplayStoryPopup()
    {
        if (_replayStoryPopup == null)
        {
            Debug.LogWarning("[LobbyOptionPopupConnector] 시나리오 다시보기 팝업을 열 수 없습니다.", this);
            return;
        }

        _replayStoryPopup.Open();
    }

    private void ResolveMissingReferences()
    {
        if (_replayStoryButton == null)
        {
            _replayStoryButton = FindSceneComponentByName<Button>("Btn_RePlayStory");
            if (_replayStoryButton == null)
                _replayStoryButton = FindSceneComponentByName<Button>("Right_RePlayStory");
        }

        if (_replayStoryPopup == null)
        {
            GameObject popupObject = FindSceneObjectByName("POPUp_RePlayStory");
            if (popupObject == null)
                popupObject = FindSceneObjectByName("POPUP_RePlayStory");

            if (popupObject != null)
            {
                _replayStoryPopup = popupObject.GetComponent<ReplayStoryPopup>();
                if (_replayStoryPopup == null)
                    _replayStoryPopup = popupObject.AddComponent<ReplayStoryPopup>();
            }
        }
    }

    private static T FindSceneComponentByName<T>(string objectName) where T : Component
    {
        GameObject foundObject = FindSceneObjectByName(objectName);
        return foundObject != null ? foundObject.GetComponent<T>() : null;
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target == null || target.name != objectName)
                continue;

            GameObject gameObject = target.gameObject;
            if (gameObject.scene.IsValid())
                return gameObject;
        }

        return null;
    }
}
