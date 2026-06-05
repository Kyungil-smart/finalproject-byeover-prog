//담당자: 조규민
//설명: 하우징 가구 선택 시 플레이어 이동과 상호작용 UI 표시 흐름을 연결한다.

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하우징 임시 가구 선택 후 플레이어 이동과 상호작용 표시를 조율한다.
/// </summary>
public class HousingInteractionController : MonoBehaviour
{
    [Header("하우징 구성")]
    [SerializeField] private HousingWanderer _playerMover;
    [SerializeField] private HousingInteractionView _interactionView;
    [SerializeField] private List<HousingFurnitureView> _furnitures = new List<HousingFurnitureView>();

    private void Awake()
    {
        if (_playerMover == null)
            Debug.LogWarning("[HousingInteractionController] 플레이어 이동 컴포넌트가 연결되지 않았습니다.", this);

        if (_interactionView == null)
            Debug.LogWarning("[HousingInteractionController] 상호작용 View가 연결되지 않았습니다.", this);
    }

    private void OnEnable()
    {
        foreach (HousingFurnitureView _furniture in _furnitures)
        {
            if (_furniture == null)
                continue;

            _furniture.Clicked += OnFurnitureClicked;
        }
    }

    private void OnDisable()
    {
        foreach (HousingFurnitureView _furniture in _furnitures)
        {
            if (_furniture == null)
                continue;

            _furniture.Clicked -= OnFurnitureClicked;
        }
    }

    private void OnFurnitureClicked(HousingFurnitureView _furniture)
    {
        if (_playerMover == null || _interactionView == null || _furniture == null)
            return;

        _interactionView.Hide();
        _playerMover.MoveToInteractionTarget(
            _furniture.GetInteractionPosition(),
            () => _interactionView.Show(_furniture));
    }
}
