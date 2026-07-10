// 담당자 : 정승우
// 설명   : 조합식 View 인터페이스

// 수정자 : 김영찬 
// 수정 내용 : 조합식 등록 이벤트를 받기 위한 함수 수정

public interface ICombinationView
{
    void SetRecipe(int slotIdx, int recipeKey, int[] ingredients);
    void MarkIngredientFulfilled(int slotIdx, int ingredientIdx);
    void PlayActivationEffect(int slotIdx);
    void ClearRecipe(int slotIdx);
    void ClearRegisteredRecipe(int slotIdx);
}
