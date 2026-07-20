// 작성자 : 홍정옥
// 텍스트에 붙여 번역 ID로 현재 언어 텍스트를 자동 표시한다. 언어 전환 시 자동 갱신.
// 정적 라벨용(상점/강화/홈 등). 런타임 값이 들어가는 {0} 텍스트는 각 프리젠터가 Get을 직접 호출.

using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedTextBinder : MonoBehaviour
{
    [SerializeField] private int _id;
    [SerializeField] private LocalizingType _type = LocalizingType.UI;

    private TMP_Text _text;
    private bool _subscribed;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        Subscribe();
        Apply();
    }

    private void OnDisable()
    {
        if (_subscribed && LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= Apply;
        _subscribed = false;
    }

    private void Subscribe()
    {
        if (_subscribed || LocalizationManager.Instance == null) return;
        LocalizationManager.Instance.OnLanguageChanged += Apply;
        _subscribed = true;
    }

    private void Apply()
    {
        if (_text == null) _text = GetComponent<TMP_Text>();
        if (_text == null || LocalizationManager.Instance == null) return;
        _text.text = LocalizationManager.Instance.Get(_id, _type);
    }

    // 런타임에 ID를 바꿔 다시 표시할 때.
    public void SetId(int id, LocalizingType type = LocalizingType.UI)
    {
        _id = id;
        _type = type;
        Subscribe();
        Apply();
    }
}
