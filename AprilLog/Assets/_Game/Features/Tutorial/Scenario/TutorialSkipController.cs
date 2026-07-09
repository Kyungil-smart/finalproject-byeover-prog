// 담당자 : 홍정옥
// 설명   : 시나리오 화면의 튜토리얼 스킵 버튼(Button_SkipTuto) 처리.
//          - 튜토리얼이 진행 중일 때만 버튼을 노출한다.
//          - 버튼을 누르면 확인 팝업을 띄우고, 확인 시 튜토리얼을 완료 처리한 뒤 곧바로 로비로 이동한다.
//          대사만 넘기는 기존 스킵(ScenarioView.OnSkipRequested)과 달리 튜토리얼 전체를 끝낸다.
//          완료 상태로 로비에 들어가면 TutorialView가 로비 버튼을 전부 해금한다.

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialSkipController : MonoBehaviour
{
    [Header("스킵 버튼")]
    [SerializeField] private Button _skipTutoButton;   // Button_SkipTuto

    [Header("확인 팝업")]
    [Tooltip("확인 팝업 루트. 시작 시 꺼둔다.")]
    [SerializeField] private GameObject _confirmPopup;
    [SerializeField] private Button _confirmButton;    // 예
    [SerializeField] private Button _cancelButton;     // 아니오

    [Header("튜토리얼 중 숨길 상단 버튼")]
    [Tooltip("튜토리얼 진행 중에는 스킵/자동/로그 버튼을 숨겨 대사를 건너뛰지 못하게 한다.")]
    [SerializeField] private GameObject[] _hideDuringTutorial;   // Button_Skip / Button_AutoPlay / Button_Log

    private bool _isSkipping;

    private void Awake()
    {
        if (_skipTutoButton != null) _skipTutoButton.onClick.AddListener(OpenConfirm);
        if (_confirmButton != null)  _confirmButton.onClick.AddListener(Skip);
        if (_cancelButton != null)   _cancelButton.onClick.AddListener(CloseConfirm);

        if (_confirmPopup != null) _confirmPopup.SetActive(false);
    }

    private void OnEnable()
    {
        // 튜토리얼 진행 중일 때만 스킵 버튼 노출.
        bool running = TutorialManager.Instance != null && TutorialManager.Instance.IsRunning;
        if (_skipTutoButton != null) _skipTutoButton.gameObject.SetActive(running);

        // 튜토리얼 중에는 일반 상단 버튼(스킵/자동/로그)을 숨긴다.
        if (_hideDuringTutorial != null)
        {
            foreach (GameObject go in _hideDuringTutorial)
                if (go != null) go.SetActive(!running);
        }
    }

    private void OnDestroy()
    {
        if (_skipTutoButton != null) _skipTutoButton.onClick.RemoveListener(OpenConfirm);
        if (_confirmButton != null)  _confirmButton.onClick.RemoveListener(Skip);
        if (_cancelButton != null)   _cancelButton.onClick.RemoveListener(CloseConfirm);
    }

    private void OpenConfirm()
    {
        if (_confirmPopup != null) _confirmPopup.SetActive(true);
    }

    private void CloseConfirm()
    {
        if (_confirmPopup != null) _confirmPopup.SetActive(false);
    }

    private void Skip()
    {
        if (_isSkipping) return;   // 연속 클릭 방어
        _isSkipping = true;

        CloseConfirm();

        // 튜토리얼 완료 처리(다시 안 뜨게 저장) 후 로비로 이동.
        if (TutorialManager.Instance != null) TutorialManager.Instance.Complete();

        if (GameManager.Instance != null) GameManager.Instance.LoadLobby();
        else                              SceneManager.LoadScene("_Lobby");
    }
}
