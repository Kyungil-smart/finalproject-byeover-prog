// 담당자 : 정승우
// 설명   : BGM/SFX 재생 관리

using UnityEngine;

/// <summary>
/// BGM과 SFX 재생, 볼륨 조절을 담당한다. 싱글톤.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ---------- SerializeField ----------
    [Header("오디오 소스")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;

    [Header("설정")]
    [Tooltip("동시 재생 가능한 SFX 최대 수")]
    [SerializeField] private int _maxConcurrentSFX = 8;

    // ---------- 볼륨 ----------
    public float BGMVolume
    {
        get => _bgmSource.volume;
        set
        {
            _bgmSource.volume = value;
            PlayerPrefs.SetFloat("BGMVolume", value);
        }
    }

    public float SFXVolume
    {
        get => _sfxSource.volume;
        set
        {
            _sfxSource.volume = value;
            PlayerPrefs.SetFloat("SFXVolume", value);
        }
    }

    // ---------- 생명주기 ----------
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // 저장된 볼륨 복원
        _bgmSource.volume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        _sfxSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    // ---------- 재생 ----------
    public void PlayBGM(AudioClip clip)
    {
        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;
        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmSource.Play();
    }

    public void StopBGM()
    {
        _bgmSource.Stop();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip);
    }
}