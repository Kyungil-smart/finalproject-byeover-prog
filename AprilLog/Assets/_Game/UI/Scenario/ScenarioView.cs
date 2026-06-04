// 담당자 : 홍정옥
// 설명   : 시나리오 UI View (연출 전용)
//          - 데이터는 외부(데이터 담당) 컨트롤러가 값으로 주입한다. (SO/시트/언어 처리 X)
//          - UI 책임: 타이핑/텍스트 페이드 / 자동진행 / 스킵 / 터치 진행
//                     초상화 라이팅(회색 실루엣) / 초상화 슬라이드 인 / 텍스트박스 / BG·CG 페이드
//          - 신 Input System 환경이라 터치는 EventSystem(IPointerClickHandler)으로 처리

using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>라이팅 대상 초상화 위치 (UI 표현용)</summary>
public enum ScenarioSpeakerSlot
{
    None   = 0,
    Left   = 1,   // 왼쪽 화자
    Center = 2,   // 중앙 화자
    Right  = 3,   // 오른쪽 화자
}

public class ScenarioView : MonoBehaviour, IPointerClickHandler
{
    // 이벤트
    public event Action OnAdvanceRequested;   // 텍스트 출력 완료 후 터치 -> 다음 줄
    public event Action OnSkipRequested;      // 스킵 버튼

    // ---------- UI 참조 ----------
    [Header("텍스트 박스")]
    [SerializeField] private GameObject _textboxRoot;   // Textbox 0/1 토글
    [SerializeField] private TMP_Text  _nameText;
    [SerializeField] private TMP_Text  _dialogueText;

    [Header("초상화 (좌/중/우)")]
    [SerializeField] private Image _portraitLeft;    // 왼쪽 화자
    [SerializeField] private Image _portraitCenter;  // 중앙 화자
    [SerializeField] private Image _portraitRight;   // 오른쪽 화자

    [Header("배경 / 컷씬")]
    [SerializeField] private Image _bgImage;   // BG
    [SerializeField] private Image _cgImage;   // CG (전체 화면)

    [Header("라이팅 (회색 실루엣)")]
    [Tooltip("UI/Grayscale 셰이더 — 말 안 하는 초상화를 흑백 처리")]
    [SerializeField] private Shader _grayscaleShader;
    [SerializeField] private Color _litColor = Color.white;
    [Tooltip("회색 초상화 명도 (어둡게)")]
    [SerializeField] private Color _dimColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("텍스트 연출")]
    [SerializeField] private bool  _useTypewriter = true;
    [Tooltip("초당 출력 글자 수")]
    [SerializeField] private float _charsPerSecond = 30f;
    [Tooltip("타자기 대신 텍스트 박스를 페이드로 표시")]
    [SerializeField] private bool  _useTextFade = false;
    [SerializeField] private float _textFadeDuration = 0.2f;

    [Header("자동 진행")]
    [SerializeField] private bool  _autoPlay = false;
    [Tooltip("텍스트 출력 완료 후 다음으로 넘어가기까지 대기 시간(초)")]
    [SerializeField] private float _autoDelay = 1.5f;

    [Header("초상화 슬라이드 인")]
    [SerializeField] private bool  _usePortraitSlide = true;
    [Tooltip("등장 시 이동 거리(px)")]
    [SerializeField] private float _slideOffset = 60f;
    [SerializeField] private float _slideDuration = 0.25f;

    [Header("배경/컷씬 페이드")]
    [SerializeField] private bool  _useSceneFade = true;
    [SerializeField] private float _sceneFadeDuration = 0.3f;

    [Header("스킵 버튼 (선택)")]
    [SerializeField] private Button _skipButton;

    // ---------- 상태 ----------
    private Coroutine _typingRoutine;
    private Coroutine _autoRoutine;
    private bool _isTyping;

    private Material _grayMaterial;
    private CanvasGroup _textboxGroup;

    private readonly Vector2[] _portraitBasePos = new Vector2[3];
    private Sprite _lastBg;
    private Sprite _lastCg;

    public bool IsTyping => _isTyping;
    public bool AutoPlay => _autoPlay;

    // ==================================================================
    private void Awake()
    {
        if (_grayscaleShader != null)
            _grayMaterial = new Material(_grayscaleShader);

        // 초상화 원위치 캐싱 (슬라이드 복귀 지점)
        if (_portraitLeft   != null) _portraitBasePos[0] = _portraitLeft.rectTransform.anchoredPosition;
        if (_portraitCenter != null) _portraitBasePos[1] = _portraitCenter.rectTransform.anchoredPosition;
        if (_portraitRight  != null) _portraitBasePos[2] = _portraitRight.rectTransform.anchoredPosition;

        // 텍스트 페이드용 CanvasGroup 확보
        if (_textboxRoot != null)
        {
            _textboxGroup = _textboxRoot.GetComponent<CanvasGroup>();
            if (_textboxGroup == null)
                _textboxGroup = _textboxRoot.AddComponent<CanvasGroup>();
        }

        if (_skipButton != null)
            _skipButton.onClick.AddListener(() => OnSkipRequested?.Invoke());
    }

    private void OnDestroy()
    {
        StopAllScenarioTweens();
        if (_grayMaterial != null) Destroy(_grayMaterial);
        if (_skipButton != null)   _skipButton.onClick.RemoveAllListeners();
    }

    
    // 외부에서 호출하는 표시 API
    public void ShowLine(
        string name, string text, bool showTextbox,
        Sprite portraitLeft, Sprite portraitCenter, Sprite portraitRight,
        ScenarioSpeakerSlot speaker,
        Sprite background, Sprite cutscene)
    {
        StopAuto();

        SetTextbox(showTextbox);
        SetName(name);
        SetBackground(background);
        SetCutscene(cutscene);

        ApplyPortrait(_portraitLeft,   0, portraitLeft,   speaker == ScenarioSpeakerSlot.Left);
        ApplyPortrait(_portraitCenter, 1, portraitCenter, speaker == ScenarioSpeakerSlot.Center);
        ApplyPortrait(_portraitRight,  2, portraitRight,  speaker == ScenarioSpeakerSlot.Right);
        BringSpeakerToFront(speaker);

        PlayText(text);
    }

    public void SetTextbox(bool visible)
    {
        if (_textboxRoot != null)
            _textboxRoot.SetActive(visible);
    }

    public void SetName(string name)
    {
        if (_nameText != null)
            _nameText.text = name ?? string.Empty;
    }

    public void SetBackground(Sprite sprite)
    {
        bool changed = sprite != _lastBg;
        _lastBg = sprite;
        ApplySceneSprite(_bgImage, sprite, changed);
    }

    public void SetCutscene(Sprite sprite)
    {
        bool changed = sprite != _lastCg;
        _lastCg = sprite;
        ApplySceneSprite(_cgImage, sprite, changed);
    }

    
    // 텍스트 연출 (타자기 / 페이드)

    public void PlayText(string text)
    {
        if (_dialogueText == null) return;

        StopTyping();
        _dialogueText.text = text ?? string.Empty;

        // 텍스트 박스 페이드
        if (_useTextFade && _textboxGroup != null)
        {
            _textboxGroup.DOKill();
            _textboxGroup.alpha = 0f;
            _textboxGroup.DOFade(1f, _textFadeDuration).SetUpdate(true);
        }

        if (!_useTypewriter || string.IsNullOrEmpty(text))
        {
            _dialogueText.maxVisibleCharacters = int.MaxValue;
            _isTyping = false;
            OnTextFullyShown();
            return;
        }

        _typingRoutine = StartCoroutine(TypeRoutine());
    }

    private IEnumerator TypeRoutine()
    {
        _isTyping = true;
        _dialogueText.maxVisibleCharacters = 0;
        _dialogueText.ForceMeshUpdate();

        int total = _dialogueText.textInfo.characterCount;
        float interval = _charsPerSecond > 0f ? 1f / _charsPerSecond : 0f;

        int visible = 0;
        while (visible < total)
        {
            visible++;
            _dialogueText.maxVisibleCharacters = visible;
            if (interval > 0f) yield return new WaitForSecondsRealtime(interval);
            else               yield return null;
        }

        _dialogueText.maxVisibleCharacters = int.MaxValue;
        _isTyping = false;
        _typingRoutine = null;
        OnTextFullyShown();
    }

    /// <summary>타이핑 중이면 즉시 전체 표시</summary>
    public void CompleteText()
    {
        bool wasTyping = _isTyping;
        StopTyping();
        if (_dialogueText != null)
            _dialogueText.maxVisibleCharacters = int.MaxValue;
        if (wasTyping)
            OnTextFullyShown();
    }

    private void StopTyping()
    {
        if (_typingRoutine != null) StopCoroutine(_typingRoutine);
        _typingRoutine = null;
        _isTyping = false;
    }

   
    // 자동 진행
    
    private void OnTextFullyShown()
    {
        if (_autoPlay)
        {
            StopAuto();
            _autoRoutine = StartCoroutine(AutoAdvanceRoutine());
        }
    }

    private IEnumerator AutoAdvanceRoutine()
    {
        yield return new WaitForSecondsRealtime(_autoDelay);
        _autoRoutine = null;
        OnAdvanceRequested?.Invoke();
    }

    private void StopAuto()
    {
        if (_autoRoutine != null) StopCoroutine(_autoRoutine);
        _autoRoutine = null;
    }

    /// <summary>자동 진행 on/off (버튼에 연결 가능)</summary>
    public void SetAutoPlay(bool on)
    {
        _autoPlay = on;
        if (!on) StopAuto();
        else if (!_isTyping) OnTextFullyShown();   // 이미 텍스트 다 나왔으면 바로 타이머 시작
    }

    public void ToggleAutoPlay() => SetAutoPlay(!_autoPlay);

   
    // 터치 진행
  
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isTyping)
        {
            CompleteText();                 // 타이핑 중 → 즉시 완성
        }
        else
        {
            StopAuto();
            OnAdvanceRequested?.Invoke();   // 다음 줄
        }
    }

    
    // 초상화 / 라이팅 / 슬라이드
    
    private void ApplyPortrait(Image image, int slotIndex, Sprite sprite, bool isSpeaker)
    {
        if (image == null) return;

        bool wasActive = image.gameObject.activeSelf;

        if (sprite == null)   // 데이터 없으면 표시 X
        {
            image.gameObject.SetActive(false);
            return;
        }

        image.sprite = sprite;
        image.gameObject.SetActive(true);

        // 라이팅: 화자=원본+litColor / 비화자=그레이스케일+dimColor
        if (isSpeaker)
        {
            image.material = null;
            image.color = _litColor;
        }
        else
        {
            if (_grayMaterial != null) image.material = _grayMaterial;
            image.color = _dimColor;
        }

        // 슬라이드 인: 새로 등장할 때만
        if (_usePortraitSlide && !wasActive)
            PlaySlideIn(image, slotIndex);
    }

    private void PlaySlideIn(Image image, int slotIndex)
    {
        RectTransform rt = image.rectTransform;
        Vector2 basePos = _portraitBasePos[slotIndex];

        // 좌측은 왼쪽에서, 우측은 오른쪽에서, 중앙은 아래에서
        Vector2 from = slotIndex switch
        {
            0 => basePos + new Vector2(-_slideOffset, 0f),
            2 => basePos + new Vector2( _slideOffset, 0f),
            _ => basePos + new Vector2(0f, -_slideOffset),
        };

        rt.DOKill();
        rt.anchoredPosition = from;
        rt.DOAnchorPos(basePos, _slideDuration).SetEase(Ease.OutCubic).SetUpdate(true);
    }

    private void BringSpeakerToFront(ScenarioSpeakerSlot speaker)
    {
        Image target = speaker switch
        {
            ScenarioSpeakerSlot.Left   => _portraitLeft,
            ScenarioSpeakerSlot.Center => _portraitCenter,
            ScenarioSpeakerSlot.Right  => _portraitRight,
            _ => null,
        };
        if (target != null)
            target.transform.SetAsLastSibling();
    }

    // 배경 / 컷씬 (페이드)
    
    private void ApplySceneSprite(Image image, Sprite sprite, bool changed)
    {
        if (image == null) return;

        if (sprite == null)
        {
            image.gameObject.SetActive(false);
            return;
        }

        image.sprite = sprite;
        image.gameObject.SetActive(true);

        if (_useSceneFade && changed)
        {
            image.DOKill();
            Color c = image.color; c.a = 0f; image.color = c;
            image.DOFade(1f, _sceneFadeDuration).SetUpdate(true);
        }
        else
        {
            Color c = image.color; c.a = 1f; image.color = c;
        }
    }

    private void StopAllScenarioTweens()
    {
        StopTyping();
        StopAuto();
        if (_textboxGroup != null) _textboxGroup.DOKill();
        if (_bgImage != null) _bgImage.DOKill();
        if (_cgImage != null) _cgImage.DOKill();
        if (_portraitLeft   != null) _portraitLeft.rectTransform.DOKill();
        if (_portraitCenter != null) _portraitCenter.rectTransform.DOKill();
        if (_portraitRight  != null) _portraitRight.rectTransform.DOKill();
    }
}
