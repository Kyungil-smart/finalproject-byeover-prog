// 담당자 : 정승우
// 설명   : BGM/SFX 재생 관리

using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("오디오 소스")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;

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

        _bgmSource = EnsureAudioSource(_bgmSource, nameof(_bgmSource));
        _sfxSource = EnsureAudioSource(_sfxSource, nameof(_sfxSource));

        if (_bgmSource != null)
            _bgmSource.volume = PlayerPrefs.GetFloat("BGMVolume", 1f);

        if (_sfxSource != null)
            _sfxSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

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
