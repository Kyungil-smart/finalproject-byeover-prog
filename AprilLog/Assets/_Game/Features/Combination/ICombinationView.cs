// 담당자 : 정승우
// 설명   : 조합식 View 인터페이스

public interface ICombinationView
{
    void SetRecipe(int slotIdx, int[] ingredients);
    void MarkIngredientFulfilled(int slotIdx, int ingredientIdx);
    void PlayActivationEffect(int slotIdx);
    void ClearRecipe(int slotIdx);
}
