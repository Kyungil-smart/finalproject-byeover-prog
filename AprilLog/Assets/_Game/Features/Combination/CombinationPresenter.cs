// 담당자 : 정승우
// 설명   : 조합식 Presenter

public class CombinationPresenter
{
    private readonly ICombinationView _view;
    private readonly CombinationModel _model;

    public CombinationPresenter(ICombinationView view, CombinationModel model)
    {
        _view = view;
        _model = model;

        _model.OnIngredientFulfilled += HandleFulfilled;
        _model.OnRecipeCompleted += HandleCompleted;
        _model.OnRecipeConsumed += HandleConsumed;
    }

    public void Dispose()
    {
        _model.OnIngredientFulfilled -= HandleFulfilled;
        _model.OnRecipeCompleted -= HandleCompleted;
        _model.OnRecipeConsumed -= HandleConsumed;
    }

    private void HandleFulfilled(int r, int i) => _view.MarkIngredientFulfilled(r, i);
    private void HandleCompleted(int r) => _view.PlayActivationEffect(r);
    private void HandleConsumed(int r) => _view.ClearRecipe(r);
}
