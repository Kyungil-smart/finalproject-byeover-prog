//담당자: 조규민

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 개별 가구의 입력을 전달하고 활성 연출 오브젝트를 표시합니다.
/// </summary>
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

    public void OnPointerClick(PointerEventData _eventData)
    {
        OnClicked?.Invoke(this);
    }

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
