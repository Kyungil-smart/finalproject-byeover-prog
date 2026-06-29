//담당자: 조규민

using System.Collections;
using UnityEngine;

/// <summary>
/// 로그인 계정의 클라우드 데이터를 로비 모델에 적용하고 변경 내용을 저장한다.
/// </summary>
public class LobbyCloudDataBinder : MonoBehaviour
{
    [Header("모델 참조")]
    [SerializeField] private PlayerProgressModel _progressModel;
    [SerializeField] private CurrencyModel _currencyModel;

    [Header("저장 설정")]
    [Tooltip("연속 이벤트를 한 번의 저장으로 묶기 위한 대기 시간입니다.")]
    [SerializeField] private float _saveDelaySeconds = 0.4f;

    private Coroutine _saveCoroutine;
    private bool _isBound;
    private bool _isApplyingCloudData;

    private void Start()
    {
        ResolveReferences();
        ApplyCloudData();
        BindEvents();
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    private void ResolveReferences()
    {
        if (_progressModel == null)
        {
            _progressModel = FindFirstObjectByType<PlayerProgressModel>();
        }

        if (_currencyModel == null)
        {
            _currencyModel = FindFirstObjectByType<CurrencyModel>();
        }
    }

    private void ApplyCloudData()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[LobbyCloudDataBinder] GameManager가 없어 클라우드 데이터를 적용하지 못했습니다.", this);
            return;
        }

        _isApplyingCloudData = true;
        GameManager.Instance.ApplyCloudDataToOutGameModels(_progressModel, _currencyModel);
        _isApplyingCloudData = false;
    }

    private void BindEvents()
    {
        if (_isBound)
        {
            return;
        }

        if (_progressModel != null)
        {
            _progressModel.OnCharacterLevelChanged += HandleOutGameDataChanged;
            _progressModel.OnProgressUpdated += HandleOutGameDataChanged;
        }

        if (_currencyModel != null)
        {
            _currencyModel.OnCurrencyChanged += HandleOutGameDataChanged;
        }

        _isBound = true;
    }

    private void UnbindEvents()
    {
        if (!_isBound)
        {
            return;
        }

        if (_progressModel != null)
        {
            _progressModel.OnCharacterLevelChanged -= HandleOutGameDataChanged;
            _progressModel.OnProgressUpdated -= HandleOutGameDataChanged;
        }

        if (_currencyModel != null)
        {
            _currencyModel.OnCurrencyChanged -= HandleOutGameDataChanged;
        }

        _isBound = false;
    }

    private void HandleOutGameDataChanged()
    {
        ScheduleSave();
    }

    private void HandleOutGameDataChanged(int _)
    {
        ScheduleSave();
    }

    private void HandleOutGameDataChanged(int _, int __)
    {
        ScheduleSave();
    }

    private void ScheduleSave()
    {
        if (_isApplyingCloudData || GameManager.Instance == null)
        {
            return;
        }

        if (_saveCoroutine != null)
        {
            StopCoroutine(_saveCoroutine);
        }

        _saveCoroutine = StartCoroutine(SaveAfterDelay());
    }

    private IEnumerator SaveAfterDelay()
    {
        float delaySeconds = Mathf.Max(0f, _saveDelaySeconds);
        if (delaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);
        }

        _saveCoroutine = null;
        GameManager.Instance.SaveOutGameProgress(_progressModel);
    }
}
