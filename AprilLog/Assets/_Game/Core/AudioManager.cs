// 담당자 : 정승우
// 설명   : BGM/SFX 재생 관리

using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("오디오 소스")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;

    // ---------- SfxId 기반 재생 (SFX 가이드 매핑) ----------
    // 호출부는 파일명 대신 SfxId만 쓴다. 클립/중복재생/간격 정책은 Resources/SoundLibrary.asset 이 단일 소스.
    private SoundLibrary _library;
    private readonly System.Collections.Generic.Dictionary<SfxId, float> _blockedUntil = new System.Collections.Generic.Dictionary<SfxId, float>();
    private SfxId _currentBgmId;

    /// <summary>훅에서 쓰는 짧은 진입점. 씬에 AudioManager가 없어도 자동 생성되어 동작한다(미배선 사고 방지).</summary>
    public static void Play(SfxId id) => Ensure().PlaySfx(id);
    public static void Bgm(SfxId id) => Ensure().PlayBgm(id);

    public static AudioManager Ensure()
    {
        if (Instance != null) return Instance;

        var found = FindFirstObjectByType<AudioManager>();
        if (found != null) return found;

        var go = new GameObject("AudioManager");
        return go.AddComponent<AudioManager>();   // Awake가 Instance/DontDestroyOnLoad/소스 생성을 처리
    }

    private SoundLibrary Library
    {
        get
        {
            if (_library == null)
            {
                _library = Resources.Load<SoundLibrary>("SoundLibrary");
                if (_library == null)
                    Debug.LogWarning("[AudioManager] Resources/SoundLibrary.asset 을 찾지 못했습니다. SfxId 재생이 비활성화됩니다.");
            }
            return _library;
        }
    }

    public void PlayBgm(SfxId id)
    {
        var entry = Library != null ? Library.Get(id) : null;
        if (entry == null || entry.clip == null) return;
        if (_currentBgmId == id && _bgmSource != null && _bgmSource.isPlaying) return;

        _currentBgmId = id;
        PlayBGM(entry.clip);
    }

    public void PlaySfx(SfxId id)
    {
        var entry = Library != null ? Library.Get(id) : null;
        if (entry == null || entry.clip == null || _sfxSource == null) return;

        // 중복재생 금지(가이드 X)면 클립 길이만큼, minInterval이 있으면 그 간격만큼 재요청을 무시한다.
        // 정산 팝업(timeScale=0) 중에도 UI 사운드는 나가야 하므로 unscaled 시간 기준.
        float now = Time.unscaledTime;
        if (_blockedUntil.TryGetValue(id, out float until) && now < until) return;

        float block = entry.allowOverlap ? 0f : entry.clip.length;
        if (entry.minInterval > block) block = entry.minInterval;
        if (block > 0f) _blockedUntil[id] = now + block;

        _sfxSource.PlayOneShot(entry.clip, Mathf.Clamp01(entry.volume));
    }

    /// <summary>전체 볼륨 (AudioListener)</summary>
    public float MasterVolume
    {
        get => AudioListener.volume;
        set
        {
            AudioListener.volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("MasterVolume", AudioListener.volume);
        }
    }

    public float BGMVolume
    {
        get => _bgmSource != null ? _bgmSource.volume : PlayerPrefs.GetFloat("BGMVolume", 1f);
        set
        {
            if (_bgmSource != null)
                _bgmSource.volume = value;
            PlayerPrefs.SetFloat("BGMVolume", value);
        }
    }

    public float SFXVolume
    {
        get => _sfxSource != null ? _sfxSource.volume : PlayerPrefs.GetFloat("SFXVolume", 1f);
        set
        {
            if (_sfxSource != null)
                _sfxSource.volume = value;
            PlayerPrefs.SetFloat("SFXVolume", value);
        }
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);   // _Boot→_Lobby→_InGame 씬 전환에도 오디오 유지

        // 공용 버튼 클릭음(SFX 가이드 아웃게임 7): 버튼마다 일일이 달지 않도록 씬 로드 때 일괄 바인딩한다.
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoadedBindButtons;
        HandleSceneLoadedBindButtons(default, default);   // 이미 로드된 현재 씬(Boot 등)도 1회 바인딩

        _bgmSource = EnsureAudioSource(_bgmSource, nameof(_bgmSource));
        _sfxSource = EnsureAudioSource(_sfxSource, nameof(_sfxSource));

        AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);

        if (_bgmSource != null)
            _bgmSource.volume = PlayerPrefs.GetFloat("BGMVolume", 1f);

        if (_sfxSource != null)
            _sfxSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoadedBindButtons;
    }

    // 씬에 배치된 모든 버튼(비활성 팝업 포함)에 클릭음을 바인딩한다. 마커 컴포넌트로 중복 바인딩을 막는다.
    // 런타임에 Instantiate되는 동적 버튼은 여기서 못 잡으므로, 동적 UI는 생성부에서 직접 Play(SfxId.ButtonClick)를 달 것.
    private void HandleSceneLoadedBindButtons(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        foreach (var button in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button.GetComponent<ButtonClickSfxMarker>() != null) continue;
            button.gameObject.AddComponent<ButtonClickSfxMarker>();
            button.onClick.AddListener(() => Play(SfxId.ButtonClick));
        }
    }

    /// <summary>버튼 클릭음이 이미 바인딩됐음을 표시하는 마커. 로직 없음.</summary>
    public class ButtonClickSfxMarker : MonoBehaviour { }

    public void PlayBGM(AudioClip clip)
    {
        if (_bgmSource == null)
        {
            Debug.LogWarning("[AudioManager] BGM AudioSource is missing. PlayBGM skipped.");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] BGM clip is null. PlayBGM skipped.");
            return;
        }

        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;
        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmSource.Play();
    }

    public void StopBGM()
    {
        if (_bgmSource == null)
            return;

        _bgmSource.Stop();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        if (_sfxSource == null)
        {
            Debug.LogWarning("[AudioManager] SFX AudioSource is missing. PlaySFX skipped.");
            return;
        }

        _sfxSource.PlayOneShot(clip);
    }

    private AudioSource EnsureAudioSource(AudioSource source, string fieldName)
    {
        if (source != null)
            return source;

        source = GetComponent<AudioSource>();
        if (source != null)
        {
            Debug.LogWarning($"[AudioManager] {fieldName} was not assigned. Reusing AudioSource on this GameObject.");
            return source;
        }

        Debug.LogWarning($"[AudioManager] {fieldName} was not assigned. New AudioSource was added automatically.");
        return gameObject.AddComponent<AudioSource>();
    }
}
