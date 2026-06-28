using UnityEngine;

public class DimMaskTest : MonoBehaviour
{
    [SerializeField] private TutorialDimMask _dim;
    [SerializeField] private RectTransform _target;
    [SerializeField] private TutorialFingerGuide _finger;
    [SerializeField] private TutorialDragArrow _arrow;
    [SerializeField] private RectTransform _tileFrom;
    [SerializeField] private RectTransform _tileTo;
    
    [ContextMenu("구멍 보기")]
    private void ShowHide() => _dim.ShowWithHole(_target);

    [ContextMenu("전체 딤")]
    private void ShowFull() => _dim.ShowFull();
    
    [ContextMenu("끄기")]
    private void HideDim() => _dim.Hide();
    
    private void Start()
    {
        _dim.ShowWithHole(_target);
        _finger.PointAt(_target);
        _arrow.ShowDrag(_tileFrom, _tileTo);
    }
    
    
}
