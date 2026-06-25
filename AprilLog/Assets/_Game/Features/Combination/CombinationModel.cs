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

    // 조합식 등록 (기획 3-2-1: 인챈트 선택 순서대로 가장 왼쪽 빈 슬롯부터 채움).
    // recipeKey = 레벨 무관 고정 식별자(인챈트 LinkedSkillID). 같은 스킬 레벨업이면 기존 슬롯 갱신(정렬 진행도 유지).
    public void RegisterRecipe(int recipeKey, int[] ingredients, int skillId)
    {
        // 이미 보유한 조합 스킬(레벨업) → 같은 슬롯의 skillId만 갱신, 충족 진행도는 그대로 둔다.
        for (int r = 0; r < MAX_RECIPES; r++)
        {
            if (_recipes[r] != null && _recipes[r].recipeKey == recipeKey)
            {
                _recipes[r].skillId = skillId;
                _recipes[r].isActive = true;
                return;
            }
        }

        // 신규 조합 스킬 → 가장 왼쪽 빈 슬롯에 배치.
        for (int r = 0; r < MAX_RECIPES; r++)
        {
            if (_recipes[r] == null || !_recipes[r].isActive)
            {
                _recipes[r] = new CombinationRecipe
                {
                    recipeKey = recipeKey,
                    ingredients = ingredients,
                    fulfilled = new bool[ingredients.Length],
                    skillId = skillId,
                    isActive = true
                };
                return;
            }
        }

        // 기획 1-6-2/3-1-1: 조합 인챈트는 최대 3개 → 드래프트에서 막혀야 하며 여기 도달하면 안 됨(방어 로그).
        Debug.LogWarning($"[CombinationModel] 조합 슬롯({MAX_RECIPES}개)이 가득 차 recipeKey {recipeKey} 등록을 건너뜁니다 (기획상 조합 인챈트 최대 3개).");
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
    public int recipeKey;   // 레벨 무관 고정 식별자(인챈트 LinkedSkillID). 레벨업 시 같은 슬롯 갱신용.
    public int[] ingredients;
    public bool[] fulfilled;
    public int skillId;
    public bool isActive;
}