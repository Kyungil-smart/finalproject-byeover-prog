using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 제작 / 돌파 공용 확인 타입
public enum ArtifactConfirmType
{
    Craft,        // 제작
    Breakthrough  // 돌파
}

// 작성자 : 홍정옥
// 설명   : 제작 / 돌파 공용 확인 팝업(POPUP_CraftBreakthroughConfirm).
//          Open(type, cost, onConfirm) 로 호출하면 문구를 세팅하고, 확인 클릭 시 onConfirm 을 실행한다.
//          - 제작 : "조각 N개를 사용하여 제작하시겠습니까?"
//          - 돌파 : "조각 N개를 사용하여 돌파하시겠습니까?"
//          취소/확인 모두 팝업을 닫으며, 빠른 연속 클릭으로 중복 확정되지 않도록 방어한다.
public class CraftBreakthroughConfirmPopup : MonoBehaviour
{
    [Header("POPUP_CraftBreakthroughConfirm")]
    [Tooltip("팝업 루트. 비우면 이 컴포넌트가 붙은 게임오브젝트를 사용한다.")]
    [SerializeField] private GameObject _popup;
    [SerializeField] private TMP_Text _messageText;  // Text_Message
    [SerializeField] private Button _confirmButton;  // Button_Confirm
    [SerializeField] private Button _cancelButton;   // Button_Cancel

    private Action _onConfirm;
    private bool _isProcessing;

    public bool IsOpen { get; private set; }

    private GameObject Root => _popup != null ? _popup : gameObject;

    private void Awake()
    {
        if (_confirmButton != null) _confirmButton.onClick.AddListener(HandleConfirm);
        if (_cancelButton != null) _cancelButton.onClick.AddListener(Close);
        Root.SetActive(false);
    }

    // type 에 맞는 문구를 세팅하고 팝업을 연다. cost = 소모 조각 수(제작 5 / 돌파 3).
    public void Open(ArtifactConfirmType type, int cost, Action onConfirm)
    {
        if (IsOpen) return; // 중복 오픈 방어

        _onConfirm = onConfirm;
        _isProcessing = false;
        IsOpen = true;

        if (_messageText != null)
        {
            switch (type)
            {
                case ArtifactConfirmType.Craft:
                    _messageText.text = $"조각 {cost}개를 사용하여 제작하시겠습니까?";
                    break;
                case ArtifactConfirmType.Breakthrough:
                    _messageText.text = $"조각 {cost}개를 사용하여 돌파하시겠습니까?";
                    break;
            }
        }

        if (_confirmButton != null) _confirmButton.interactable = true;
        Root.SetActive(true);
    }

    private void HandleConfirm()
    {
        if (_isProcessing) return;  // 빠른 연속 클릭으로 인한 중복 확정 방어
        _isProcessing = true;
        if (_confirmButton != null) _confirmButton.interactable = false;

        Action callback = _onConfirm;
        _onConfirm = null;

        // 콜백 실행 전에 팝업을 먼저 닫아, 콜백이 부모 팝업을 닫을 때 상태가 꼬이지 않게 한다.
        Close();
        callback?.Invoke();
    }

    public void Close()
    {
        IsOpen = false;
        _isProcessing = false;
        _onConfirm = null;
        Root.SetActive(false);
    }
}
