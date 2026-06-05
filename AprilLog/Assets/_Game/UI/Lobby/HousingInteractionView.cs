//담당자: 조규민
//설명: 하우징 가구 상호작용 결과 문구와 캐릭터 이모티콘 표시를 담당한다.

using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하우징 상호작용 결과 UI와 임시 이모티콘을 표시한다.
/// </summary>
public class HousingInteractionView : MonoBehaviour
{
    [Header("표시 대상")]
    [SerializeField] private GameObject _interactionRoot;
    [SerializeField] private TMP_Text _interactionText;

    [Header("표시 설정")]
    [SerializeField] private float _messageSeconds = 1.4f;
    [SerializeField] private float _emoteSeconds = 0.9f;

    private Coroutine _hideCoroutine;
    private RectTransform _interactionRect;
    private Tween _interactionTween;

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

    private void OnDisable()
    {
        StopHideCoroutine();
        _interactionTween?.Kill();
    }

    public void Show(HousingFurnitureView _furniture)
    {
        if (_furniture == null)
            return;

        ShowMessage(_furniture.InteractionMessage);
    }

    public void ShowMessage(string _message)
    {
        ShowText(string.IsNullOrWhiteSpace(_message) ? "상호작용했습니다." : _message, _messageSeconds);
    }

    public void ShowEmote(string _emote)
    {
        ShowText(string.IsNullOrWhiteSpace(_emote) ? "!" : _emote, _emoteSeconds);
    }

    public void Hide()
    {
        StopHideCoroutine();

        if (_interactionRoot != null)
            _interactionRoot.SetActive(false);
    }

    private void ShowText(string _text, float _seconds)
    {
        if (_interactionRoot != null)
            _interactionRoot.SetActive(true);

        EnsureTextTarget();
        DisableRootImage();

        if (_interactionText != null)
            _interactionText.text = _text;

        PlayShowTween();
        StopHideCoroutine();
        _hideCoroutine = StartCoroutine(HideAfterSeconds(_seconds));
    }

    private IEnumerator HideAfterSeconds(float _seconds)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, _seconds));
        Hide();
    }

    private void PlayShowTween()
    {
        if (_interactionRoot == null)
            return;

        _interactionRect = _interactionRect != null
            ? _interactionRect
            : _interactionRoot.transform as RectTransform;

        if (_interactionRect == null)
            return;

        _interactionTween?.Kill();
        _interactionRect.localScale = Vector3.one * 0.85f;
        _interactionTween = _interactionRect.DOScale(Vector3.one, 0.18f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    private void StopHideCoroutine()
    {
        if (_hideCoroutine == null)
            return;

        StopCoroutine(_hideCoroutine);
        _hideCoroutine = null;
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
