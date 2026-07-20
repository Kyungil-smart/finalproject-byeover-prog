// 담당자 : 정승우
// 설명   : 조합식 Presenter

// 수정자 : 정승우
// 수정내용 : Model 참조가 비어 있을 때 이벤트 구독 NullReference 방지

// 수정자 : 김영찬 
// 수정 내용 : UI에서 조합식이 등록되었음을 알수 있도록 이벤트 추가

// 2차 수정자 : 조규민
// 수정 내용 : 조합 인챈트 교체 시 제거된 레시피 슬롯을 UI에서 비우도록 해제 이벤트 구독 추가

public class CombinationPresenter
{
    private readonly ICombinationView _view;
    private readonly CombinationModel _model;

    public CombinationPresenter(ICombinationView view, CombinationModel model)
    {
        _view = view;
        _model = model;

        if (_model == null) return;

        _model.OnIngredientFulfilled += HandleFulfilled;
        _model.OnRecipeCompleted += HandleCompleted;
        _model.OnRecipeConsumed += HandleConsumed;
        _model.OnRecipeUnregistered += HandleUnregistered;
        _model.OnRecipeRegistered += HandleRegistered;
    }

    public void Dispose()
    {
        if (_model == null) return;

        _model.OnIngredientFulfilled -= HandleFulfilled;
        _model.OnRecipeCompleted -= HandleCompleted;
        _model.OnRecipeConsumed -= HandleConsumed;
        _model.OnRecipeUnregistered -= HandleUnregistered;
        _model.OnRecipeRegistered -= HandleRegistered;
    }

    private void HandleFulfilled(int r, int i) => _view.MarkIngredientFulfilled(r, i);
    private void HandleCompleted(int r) => _view.PlayActivationEffect(r);
    private void HandleConsumed(int r) => _view.ClearRecipe(r);
    private void HandleUnregistered(int r) => _view.ClearRegisteredRecipe(r);
    private void HandleRegistered(int slotIdx, int recipeKey, int[] ingredients) => _view.SetRecipe(slotIdx, recipeKey, ingredients);
}
