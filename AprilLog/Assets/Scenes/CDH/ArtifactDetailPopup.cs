using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.UI;

public class ArtifactDetailPopup : MonoBehaviour
{
    [SerializeField] private Button _btnAction;
  
    private ArtifactInstance _currentArtifact;

    private void Start()
    {
        if (_btnAction != null)
        {
            _btnAction.onClick.RemoveAllListeners();
            _btnAction.onClick.AddListener(OnBtnActionClicked);
            Debug.Log("버튼 리스너가 Start에서 강제로 할당되었습니다.");
        }
    }

    public void Setup(int gearId)
    {
        var artifact = GameStateManager.Instance.ArtifactManager.MyArtifacts
                   .Find(a => a != null && a.MasterId == gearId);

        _currentArtifact = artifact;

        if (_currentArtifact == null)
        {
            Debug.LogError($"[에러] 데이터를 찾을 수 없습니다! ID: {gearId}");
            return;
        }

        RefreshUI();
    }
    
    public void RefreshUI()
    {
        var latestData = GameStateManager.Instance?.ArtifactManager?.MyArtifacts
                      .Find(a => a != null && a.UniqueId == _currentArtifact?.UniqueId);

        if (latestData != null)
        {
            _currentArtifact = latestData;

            Debug.Log($"UI 갱신 완료: Lv.{_currentArtifact.CurrentLevel}");
        }
    }

    private void OnEnable()
    {
        var artifactManager = GameStateManager.Instance?.ArtifactManager;

        if (artifactManager == null)
        {
            Debug.LogError("ArtifactManager를 찾을 수 없습니다.");
            return;
        }

        artifactManager.OnInventoryUpdated += RefreshUI;

        var presenter = FindAnyObjectByType<ArtifactDetailPopupPresenter>();
        if (presenter != null && presenter.CurrentGearId != 0)
        {
            int gearId = presenter.CurrentGearId;
            _currentArtifact = artifactManager.MyArtifacts.Find(a => a != null && a.MasterId == gearId);

            RefreshUI();
        }
    }

    private void OnDisable()
    {
        if (GameStateManager.Instance?.ArtifactManager != null)
            GameStateManager.Instance.ArtifactManager.OnInventoryUpdated -= RefreshUI;
    }

    private void OnBtnActionClicked()
    {
        Debug.Log($"버튼 클릭 감지됨! 현재 아티팩트: {(_currentArtifact == null ? "NULL임!!" : _currentArtifact.MasterId)}");
        if (_currentArtifact == null) return;

        if (!_currentArtifact.CanLevelUp())
            OnClickAscend();
        else
            OnClickLevelUp();
    }

    public void OnClickLevelUp()
    {
        if (_currentArtifact == null) return;

        GameStateManager.Instance.ArtifactManager.RequestUpgrade(_currentArtifact.UniqueId);
        Debug.Log("레벨업 완료!");
    }

    public void OnClickAscend()
    {
        if (_currentArtifact == null) return;

        GameStateManager.Instance.ArtifactManager.AscendArtifact(_currentArtifact);
        Debug.Log("돌파 성공!");
    }
}
