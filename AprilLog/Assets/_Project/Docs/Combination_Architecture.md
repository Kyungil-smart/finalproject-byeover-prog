# 조합식(Combination) 시스템 아키텍처

작성: 정승우 / 대상: 전투·인챈트·UI 작업자
(파일명은 CLI/git 인코딩 안전을 위해 영문. 내용은 한국어.)

유닛 3종을 정렬해 조합 스킬을 발동하는 시스템이 코드에서 어떻게 도는지 정리한다.
원본 스펙은 v.1.09 전투기획 3장 "조합식 방식".

## 1. 한 줄 요약
인챈트로 조합 스킬을 얻으면 슬롯에 등록되고, 정렬할 때마다 재료가 채워지다 3종이 모이면 발사된다. 발사 후 그 레시피는 초기화돼 재사용된다.

## 2. 전체 흐름
```
[획득] 조합 인챈트 획득
   → SkillEnchantSystem.Apply → RegisterCombinationFromTable
   → CombinationModel.RegisterRecipe(nameId, 재료[], skillId)   // 키 = nameId
   → OnRecipeRegistered → CombinationPresenter → CombinationView.SetRecipe (박스에 아이콘+재료 표시)

[발동] 유닛 정렬
   → CombatSystem.HandleSortCompleted → CombinationModel.CheckIngredient(UnitType)
   → 재료 충족 → OnIngredientFulfilled → View.MarkIngredientFulfilled (테두리 켜짐)
   → 3종 완성 → CombatSystem이 FireSkill(Combi) + ConsumeRecipe(초기화)
```

## 3. 기획 규칙 (구현이 지키는 것)
- 조합 인챈트 최대 3개 = MAX_RECIPES=3 (전투 1-6-2 / 3-1-1). 늘리지 말 것.
- 슬롯은 선택 순서대로 왼쪽 빈 칸부터 채움 (3-2-1) = RegisterRecipe가 처리.
- 재료는 순서 무관 3종 (3-1-3-1). 같은 테이블 중복재료는 1정렬당 1충족 (3-1-3-2). 다른 테이블 같은재료는 모두 충족 (3-1-3-3).
- 2개 이상 동시 완성 시 전부 사출 (HandleSortCompleted 루프).

## 4. ★ 색 매핑 함정 (중요)
- UnitType enum: Red0/Blue1/Green2/Yellow3/Purple4. 표시 스프라이트의 ground truth = UnitMasterTable(1003=노랑, 1004=초록).
- UnitDataManager가 int→UnitID를 `UnitTypeToTableId={1001,1002,1004,1003,1005}` 명시매핑으로 교정 → int2=초록, int3=노랑 표시(enum과 일치). 자세히는 메모리 [유닛 색↔UnitType 매핑] 참고.
- 레시피 재료는 ConvertRawIdToUnitType(1003→Yellow3, 1004→Green2)로 변환돼 UnitType과 일치.

## 5. 등록 키 = nameId (UI 정합)
- RegisterRecipe의 recipeKey = nameId(카드 Name). EnchantCombinationModel.FusionData도 nameId 키 → View.SetRecipe가 FusionData[nameId]로 스킬 아이콘 조회 성공.
- (과거 baseId 키였으나 FusionData(nameId)와 안 맞아 아이콘 누락 → nameId로 통일함.)

## 6. 레시피 표 (10종, 코드 UnitType 0빨/1파/2초/3노/4하)
화염작렬 {2,0,1} · 화염정령 {3,1,2} · 탄환세례 {4,2,3} · 급류 {4,3,1} · 바람칼날 {0,3,4} · 돌풍 {1,2,4} · 에너지볼 {3,2,0} · 방전 {1,0,4} · 글레이셜 {0,3,1} · 빙결지대 {2,4,0}. (테이블 SkillGroup_ID 200000000, RequiredValue_1/2/3와 일치)

## 7. UI 연동
- CombinationView(홍정옥): `_recipeSlots`(CombinationSlotUI 3개) + `_unitSprites[5]`(0빨/1파/2초록/3노랑/4하양 — 파일번호순 아님, enum순) 인스펙터 배선.
- 스킬 아이콘 경로 = `Resources/EnchantIcons/{SkillIcon_ID}` (화염작렬 100151 등).

## 8. 미완/주의
- 드래프트 캡(조합 3 / 합계 5) 강제 = EnchantSelector 미확인(가중치만, 하드캡 없음). 4개째 픽 시 RegisterRecipe가 무시.
- "중복키" 류 데이터 경고는 데이터/타 도메인.

## 부록 - 파일 위치
- 런타임 슬롯/로직: `_Game/Features/Combination/CombinationModel.cs`
- UI(MVP): CombinationView/Presenter/ICombinationView + CombinationSlotUI
- 등록: `_Game/Features/Enchant/SkillEnchantSystem.cs` (RegisterCombinationFromTable)
- 메타/도감: `_Game/Features/Enchant/EnchantCombinationModel.cs`
- 정렬 수신·발사: `_Game/Features/Combat/Combatsystem.cs` (HandleSortCompleted)
- 레시피 데이터: `_Project/Data/SO/SkillEnchantTable.asset`
