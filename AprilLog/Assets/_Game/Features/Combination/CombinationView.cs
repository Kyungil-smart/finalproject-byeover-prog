// 담당자 : 정승우
// 설명   : 조합식 View -- 재료 충족 상태 표시

// 수정자 : 정승우
// 수정내용 : CombinationModel 참조가 비어 있을 때 Presenter 생성을 건너뛰어 테스트 씬 NullReference 방지

// 수정자 : 김영찬 
// 수정 내용 : 조합식 등록 이벤트를 받기 위한 함수 수정

using UnityEngine;

/// <summary>
/// 조합식 테이블 UI 표시. 로직 없음.
/// </summary>
public class CombinationView : MonoBehaviour, ICombinationView
{
    [Header("참조")]
    [SerializeField] private CombinationModel _model;
    [SerializeField] private EnchantCombinationModel _enchantCombinationModel;

    [Header("UI")]
    [SerializeField] private CombinationSlotUI[] _recipeSlots;
    
    [Header("재료 유닛 이미지 (UnitType 순서 0~4)")]
    [Tooltip("0:Red, 1:Blue, 2:Green, 3:Yellow, 4:Purple")]
    [SerializeField] private Sprite[] _unitSprites = new Sprite[5];

    private CombinationPresenter _presenter;
    
    // ToDo : 차후에 조합 스킬 아이콘 위치 정의되면 경로 수정 할 것.
    private const string PATH = "Icons/";

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

    public void SetRecipe(int slotIdx, int recipeKey, int[] ingredients)
    {
        if (_recipeSlots == null || slotIdx >= _recipeSlots.Length) return;

        if (_enchantCombinationModel != null && _enchantCombinationModel.FusionData.TryGetValue(recipeKey, out var fusionData))
        {
            // 조합 스킬 아이콘
            Sprite icon = Resources.Load<Sprite>(PATH + $"{fusionData.IconImageKey}");
            
            // 재료(UnitType) 번호를 바탕으로 실제 이미지를 찾아 배열로 묶기
            Sprite[] sprites = new Sprite[3];
            for(int i = 0; i < 3; i++) 
            {
                int unitTypeIndex = ingredients[i];
                if (unitTypeIndex >= 0 && unitTypeIndex < _unitSprites.Length)
                {
                    sprites[i] = _unitSprites[unitTypeIndex];
                }
            }

            // 슬롯에 이미지 전달
            _recipeSlots[slotIdx].SetupSlot(icon, sprites);
        }
    }

    public void MarkIngredientFulfilled(int slotIdx, int ingredientIdx)
    {
        if (_recipeSlots == null || slotIdx >= _recipeSlots.Length) return;
        _recipeSlots[slotIdx].FillIngredient(ingredientIdx);
    }

    public void PlayActivationEffect(int slotIdx)
    {
        Debug.Log($"[{slotIdx}번 슬롯] 스킬 발동 연출!!");
    }

    public void ClearRecipe(int slotIdx)
    {
        if (_recipeSlots == null || slotIdx >= _recipeSlots.Length) return;
        _recipeSlots[slotIdx].ClearProgress();
    }
}
