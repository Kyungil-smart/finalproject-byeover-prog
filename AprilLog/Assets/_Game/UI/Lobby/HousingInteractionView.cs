//담당자: 조규민
//설명: 하우징 가구 상호작용 결과 문구 표시를 담당한다.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하우징 가구 상호작용 결과 UI를 표시한다.
/// </summary>
public class HousingInteractionView : MonoBehaviour
{
    [Header("표시 대상")]
    [SerializeField] private GameObject _interactionRoot;
    [SerializeField] private TMP_Text _interactionText;

    private void Awake()
    {
        if (_interactionRoot == null)
            Debug.LogWarning("[HousingInteractionView] 상호작용 표시 루트가 연결되지 않았습니다.", this);

        EnsureTextTarget();
        DisableRootImage();

        if (_interactionText == null)
            Debug.LogWarning("[HousingInteractionView] 상호작용 문구 텍스트가 연결되지 않았습니다.", this);
    }

    private void OnEnable()
    {
        Hide();
    }

    public void Show(HousingFurnitureView _furniture)
    {
        if (_furniture == null)
            return;

        if (_interactionRoot != null)
            _interactionRoot.SetActive(true);

        EnsureTextTarget();
        DisableRootImage();

        if (_interactionText != null)
            _interactionText.text = _furniture.InteractionMessage;
    }

    public void Hide()
    {
        if (_interactionRoot != null)
            _interactionRoot.SetActive(false);
    }

    private void EnsureTextTarget()
    {
        if (_interactionText != null)
            return;

        if (_interactionRoot == null)
            return;

        _interactionText = _interactionRoot.GetComponent<TMP_Text>();

        if (_interactionText == null)
            _interactionText = _interactionRoot.AddComponent<TextMeshProUGUI>();

        _interactionText.raycastTarget = false;
        _interactionText.alignment = TextAlignmentOptions.Center;
        _interactionText.enableAutoSizing = true;
        _interactionText.fontSizeMin = 24f;
        _interactionText.fontSizeMax = 48f;
        _interactionText.color = Color.white;
    }

    private void DisableRootImage()
    {
        if (_interactionRoot == null)
            return;

        Image _rootImage = _interactionRoot.GetComponent<Image>();
        if (_rootImage != null)
            _rootImage.enabled = false;
    }
}
