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
        get => _bgmSource.volume;
        set { _bgmSource.volume = value; PlayerPrefs.SetFloat("BGMVolume", value); }
    }

    public float SFXVolume
    {
        get => _sfxSource.volume;
        set { _sfxSource.volume = value; PlayerPrefs.SetFloat("SFXVolume", value); }
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _bgmSource.volume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        _sfxSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    public void PlayBGM(AudioClip clip)
    {
        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;
        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmSource.Play();
    }

    public void StopBGM() => _bgmSource.Stop();

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip);
    }
}