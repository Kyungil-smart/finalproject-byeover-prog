// 담당자 : 정승우
// 설명   : 조합식 Model -- 재료 충족 상태

using System;
using UnityEngine;

/// <summary>
/// 조합 스킬의 재료 충족 상태를 관리한다. 최대 3개 조합식 슬롯.
/// </summary>
public class CombinationModel : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<int, int> OnIngredientFulfilled;    // recipeIdx, ingredientIdx
    public event Action<int> OnRecipeCompleted;             // recipeIdx
    public event Action<int> OnRecipeConsumed;              // recipeIdx

    // ---------- 상수 ----------
    public const int MAX_RECIPES = 3;

    // ---------- 데이터 ----------
    private CombinationRecipe[] _recipes;

    // ---------- 초기화 ----------
    public void Initialize()
    {
        _recipes = new CombinationRecipe[MAX_RECIPES];
    }

    public void SetRecipe(int index, int[] ingredients, int skillId)
    {
        _recipes[index] = new CombinationRecipe
        {
            ingredients = ingredients,
            fulfilled = new bool[ingredients.Length],
            skillId = skillId,
            isActive = true
        };
    }

    // 정렬 성공할 때마다 호출. 같은 테이블 같은 재료는 1개만 충족 (기획서 3-1-3-2)
    public void CheckIngredient(UnitType type)
    {
        int typeInt = (int)type;

        for (int r = 0; r < MAX_RECIPES; r++)
        {
            if (_recipes[r] == null || !_recipes[r].isActive) continue;

            bool filled = false;
            for (int i = 0; i < _recipes[r].ingredients.Length; i++)
            {
                if (_recipes[r].ingredients[i] == typeInt
                    && !_recipes[r].fulfilled[i]
                    && !filled)
                {
                    _recipes[r].fulfilled[i] = true;
                    filled = true;
                    OnIngredientFulfilled?.Invoke(r, i);
                }
            }

            if (filled && IsAllFulfilled(r))
                OnRecipeCompleted?.Invoke(r);
        }
    }

    public bool HasCompletedRecipe()
    {
        for (int r = 0; r < MAX_RECIPES; r++)
            if (_recipes[r] != null && _recipes[r].isActive && IsAllFulfilled(r))
                return true;
        return false;
    }

    public int GetCompletedRecipeIndex()
    {
        for (int r = 0; r < MAX_RECIPES; r++)
            if (_recipes[r] != null && _recipes[r].isActive && IsAllFulfilled(r))
                return r;
        return -1;
    }

    public int GetRecipeSkillId(int index) => _recipes[index]?.skillId ?? 0;

    public void ConsumeRecipe(int index)
    {
        for (int i = 0; i < _recipes[index].fulfilled.Length; i++)
            _recipes[index].fulfilled[i] = false;
        OnRecipeConsumed?.Invoke(index);
    }

    private bool IsAllFulfilled(int index)
    {
        if (_recipes[index] == null) return false;
        for (int i = 0; i < _recipes[index].fulfilled.Length; i++)
            if (!_recipes[index].fulfilled[i]) return false;
        return true;
    }
}

[System.Serializable]
public class CombinationRecipe
{
    public int[] ingredients;
    public bool[] fulfilled;
    public int skillId;
    public bool isActive;
}