using System;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 홍정옥
// 설명   : 슬롯(리스트/장착) 단일 클릭을 받아 상세 정보 팝업(POPUP_ArtifactInfo)을 연다. (기획서 4)
//          팝업 표시/닫기까지가 UI 영역이며, 내부 데이터 채우기는 OnPopupOpened(Gear_ID)를 구독해 처리한다.
public class ArtifactDetailPopupPresenter : MonoBehaviour
{
    [Header("슬롯 클릭 소스")]
    [SerializeField] private ArtifactListBinder _listBinder;   // 리스트 슬롯 클릭
    [SerializeField] private ArtifactEquipBinder _equipBinder; // 장착 슬롯 클릭(선택)

    [Header("팝업")]
    [SerializeField] private GameObject _popup;     // POPUP_ArtifactInfo
    [SerializeField] private Button _closeButton;   // 닫기 버튼(선택)

    [Header("보유 시에만 활성화되는 버튼 (미보유면 비활성)")]
    [Tooltip("장착 버튼. 미보유 아티팩트면 interactable=false")]
    [SerializeField] private Button _equipButton;
    [Tooltip("레벨업 버튼. 미보유 아티팩트면 interactable=false")]
    [SerializeField] private Button _levelUpButton;

    [Header("보유 판정 소스")]
    [Tooltip("비우면 GameStateManager.Instance 의 ArtifactManager 를 사용한다.")]
    [SerializeField] private ArtifactManager _artifactManager;

    // 팝업이 열릴 때 발행 (인자 = Gear_ID). 데이터 담당이 구독해 팝업 내용을 채운다.
    public event Action<int> OnPopupOpened;

    public int CurrentGearId { get; private set; }

    private void OnEnable()
    {
        if (_listBinder != null) _listBinder.OnSlotClicked += HandleSlotClicked;
        if (_equipBinder != null) _equipBinder.OnSlotClicked += HandleSlotClicked;
        if (_closeButton != null) _closeButton.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        if (_listBinder != null) _listBinder.OnSlotClicked -= HandleSlotClicked;
        if (_equipBinder != null) _equipBinder.OnSlotClicked -= HandleSlotClicked;
        if (_closeButton != null) _closeButton.onClick.RemoveListener(Close);
    }

    private void Start()
    {
        if (_popup != null)
            _popup.SetActive(false);
    }

    private void HandleSlotClicked(int gearId)
    {
        CurrentGearId = gearId;

        if (_popup != null)
            _popup.SetActive(true);

        // 미보유(비활성) 아티팩트는 상세 정보는 보되 장착/레벨업은 할 수 없다.
        bool owned = IsOwned(gearId);
        if (_equipButton != null) _equipButton.interactable = owned;
        if (_levelUpButton != null) _levelUpButton.interactable = owned;

        OnPopupOpened?.Invoke(gearId);
    }

    // 해당 Gear_ID 를 보유 중인지 판정. 인벤토리 매니저가 없으면 미보유로 간주(버튼 비활성).
    private bool IsOwned(int gearId)
    {
        ArtifactManager mgr = _artifactManager != null
            ? _artifactManager
            : (GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null);

        return mgr != null && mgr.MyArtifacts.Exists(a => a != null && a.MasterId == gearId);
    }

    public void Close()
    {
        if (_popup != null)
            _popup.SetActive(false);
    }
}
