//담당자: 조규민

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 개별 가구의 입력을 전달하고 활성 연출 오브젝트를 표시합니다.
/// </summary>
// 가구 클릭 입력 전달과 상호작용 종류별 캐릭터 시각 오브젝트 표시
public class HousingInteractionView : MonoBehaviour, IPointerClickHandler
{
    [Header("상호작용 식별")]
    [Tooltip("다른 가구와 겹치지 않는 상호작용 식별자입니다.")]
    [SerializeField] private string _interactionId;

    [Header("상호작용 연출")]
    [SerializeField] private GameObject[] _objectsShownWhileActive;
    [SerializeField] private GameObject[] _objectsHiddenWhileActive;

    [Header("플레이어 이동")]
    [SerializeField] private bool _pausePlayerMovement;
    [Tooltip("상호작용 종료 시 플레이어를 하우징 시작 위치로 되돌립니다.")]
    [SerializeField] private bool _restorePlayerPositionOnExit = true;

    public event Action<HousingInteractionView> OnClicked;

    public string InteractionId => _interactionId;
    public bool PausePlayerMovement => _pausePlayerMovement;
    public bool RestorePlayerPositionOnExit => _restorePlayerPositionOnExit;

    private void Awake()
    {
        SetInteractionActive(false);
    }

    private void OnValidate()
    {
        if (TryGetComponent(out Graphic _graphic))
        {
            _graphic.raycastTarget = true;
        }
    }

    // 활성 입력 상태에서 가구 선택 이벤트 전달
    public void OnPointerClick(PointerEventData _eventData)
    {
        OnClicked?.Invoke(this);
    }

    // 상호작용 여부에 따른 기본 가구와 캐릭터 연출 오브젝트 전환
    public void SetInteractionActive(bool _isActive)
    {
        SetObjectsActive(_objectsShownWhileActive, _isActive);
        SetObjectsActive(_objectsHiddenWhileActive, !_isActive);
    }

    private static void SetObjectsActive(GameObject[] _targets, bool _isActive)
    {
        if (_targets == null)
        {
            return;
        }

        for (int _index = 0; _index < _targets.Length; _index++)
        {
            GameObject _target = _targets[_index];

            if (_target != null)
            {
                _target.SetActive(_isActive);
            }
        }
    }
}
