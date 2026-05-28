// 담당자 : 정승우
// 설명   : 조합식 View -- 재료 충족 상태 표시

// 수정자 : 정승우
// 수정내용 : CombinationModel 참조가 비어 있을 때 Presenter 생성을 건너뛰어 테스트 씬 NullReference 방지

using UnityEngine;

/// <summary>
/// 조합식 테이블 UI 표시. 로직 없음.
/// </summary>
public class CombinationView : MonoBehaviour, ICombinationView
{
    [Header("참조")]
    [SerializeField] private CombinationModel _model;

    [Header("UI")]
    [SerializeField] private Transform[] _recipeSlots;

    private CombinationPresenter _presenter;

    private void Awake()
    {
        if (_model == null)
        {
            Debug.LogWarning("[CombinationView] CombinationModel 참조가 비어 있어 초기화를 건너뜁니다.", this);
            return;
        }

        _presenter = new CombinationPresenter(this, _model);
    }

    private void OnDestroy() => _presenter?.Dispose();

    public void SetRecipe(int slotIdx, int[] ingredients) { /* 조합식 UI 세팅 */ }
    public void MarkIngredientFulfilled(int slotIdx, int ingredientIdx) { /* 충족 표시 */ }
    public void PlayActivationEffect(int slotIdx) { /* 발동 연출 */ }
    public void ClearRecipe(int slotIdx) { /* 초기화 */ }
}
