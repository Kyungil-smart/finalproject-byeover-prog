using UnityEngine;

public class ArtifactDetailPopup : MonoBehaviour
{
   private ArtifactInstance _data;
    
    public void Setup(ArtifactInstance data)
    {
        _data = data;

        bool isMaxLevel = _data.CurrentLevel >= _data.GetMaxLevelLimit();
        bool canAscend = _data.AscensionCount < _data.GetMaxAscensionLimit();
    }

    public void OnClickLevelUp()
    {
        if (_data == null) return;

        if (_data.CanLevelUp())
        {
            GameStateManager.Instance.ArtifactManager.LevelUpArtifact(_data);

            Setup(_data);
            Debug.Log("레벨업 완료!");
        }
    }

    public void OnClickAscend()
    {
        if (_data == null) return;

        if (_data.CanAscend())
        {
            GameStateManager.Instance.ArtifactManager.AscendArtifact(_data);

            Setup(_data);
            Debug.Log("돌파 성공!");
        }
    }
}
