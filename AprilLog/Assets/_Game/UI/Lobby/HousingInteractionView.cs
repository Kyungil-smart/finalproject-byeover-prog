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
        // 기능: 상호작용 문구 루트와 TMP 표시 대상을 준비한다.
        if (_interactionRoot == null)
            Debug.LogWarning("[HousingInteractionView] 상호작용 표시 루트가 연결되지 않았습니다.", this);

        EnsureTextTarget();
        DisableRootImage();

        if (_interactionText == null)
            Debug.LogWarning("[HousingInteractionView] 상호작용 문구 텍스트가 연결되지 않았습니다.", this);
    }

    private void OnEnable()
    {
        // 기능: 활성화 시 이전 상호작용 문구가 남지 않도록 숨김 상태로 시작한다.
        Hide();
    }

    private void OnDisable()
    {
        // 기능: 비활성화 시 예약된 숨김 코루틴과 표시 Tween을 정리한다.
        StopHideCoroutine();
        _interactionTween?.Kill();
    }

    public void Show(HousingFurnitureView _furniture)
    {
        // 기능: 가구 View에 설정된 상호작용 문구를 표시한다.
        if (_furniture == null)
            return;

        ShowMessage(_furniture.InteractionMessage);
    }

    public void ShowMessage(string _message)
    {
        // 기능: 일반 상호작용 메시지를 지정 시간 동안 표시한다.
        ShowText(string.IsNullOrWhiteSpace(_message) ? "상호작용했습니다." : _message, _messageSeconds);
    }

    public void ShowEmote(string _emote)
    {
        // 기능: 캐릭터 터치 반응용 짧은 이모티콘 문구를 표시한다.
        ShowText(string.IsNullOrWhiteSpace(_emote) ? "!" : _emote, _emoteSeconds);
    }

    public void Hide()
    {
        // 기능: 상호작용 문구 UI를 숨기고 예약 숨김을 취소한다.
        StopHideCoroutine();

        if (_interactionRoot != null)
            _interactionRoot.SetActive(false);
    }

    private void ShowText(string _text, float _seconds)
    {
        // 기능: 문구 내용을 갱신하고 표시 연출과 자동 숨김을 시작한다.
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
        // 기능: 지정 시간이 지난 뒤 상호작용 문구를 숨긴다.
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, _seconds));
        Hide();
    }

    private void PlayShowTween()
    {
        // 기능: 상호작용 문구가 나타날 때 간단한 확대 Tween 연출을 재생한다.
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
        // 기능: 진행 중인 자동 숨김 코루틴을 중지한다.
        if (_hideCoroutine == null)
            return;

        StopCoroutine(_hideCoroutine);
        _hideCoroutine = null;
    }

    private void EnsureTextTarget()
    {
        // 기능: TMP 텍스트가 없으면 루트 오브젝트에 추가해 표시 대상을 보장한다.
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
        // 기능: 문구 루트의 배경 Image를 꺼서 텍스트만 보이도록 한다.
        if (_interactionRoot == null)
            return;

        Image _rootImage = _interactionRoot.GetComponent<Image>();
        if (_rootImage != null)
            _rootImage.enabled = false;
    }
}
