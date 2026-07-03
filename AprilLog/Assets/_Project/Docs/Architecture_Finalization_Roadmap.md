# AprilLog 아키텍처 안전 정리 로드맵

본 로드맵은 전수 매핑 결과를 의존 순서대로 재구성한 것이다. 원칙은 단 하나: **참조를 끊은 뒤에만 삭제한다.** 따라서 (1) 참조 0건 즉시 삭제 → (2) 신규 이식·cutover 후 레거시 삭제 → (3) 의존 그래프가 단순해진 뒤 .asmdef 모듈화 순으로 진행한다.

---

## 절대 먼저 지우면 안 되는 것 (순서 위험 경고)

아래는 "데드처럼 보이지만 실제 라이브 데이터/연출의 유일 소스"라 선이식 없이 삭제 시 즉시 회귀가 나는 것들이다.

1. **`DataManager_Legacy.cs` (Legacy_DataManager) + `Resources/Legacy_DataManager.prefab`** — 인챈트 마스터/레벨 데이터의 유일한 런타임 소스(`Legacy_CharacterRepo.GetEnchantMaster/GetEnchantLevel`). 신규 `SpellRepo`는 `_enchantMaster/_enchantLevels`를 선언만 하고 Initialize에서 채우지 않아 **항상 null**. 먼저 지우면 스탯/스킬 인챈트 전부 무동작.
2. **`CharacterRepo_Legacy.cs` (Legacy_CharacterRepo)** — 위와 동일 이유 + `Legacy_SkillDataTable` 스킬 폴백의 실 배선 소스.
3. **`Legacy_SkillDataTable.asset` 및 `Legacy_SkillData` 클래스** — `Skillsystem.cs`(~1800줄, 40여 메서드)/`FireSpirit.cs`/`Combatsystem.cs`가 전부 이 타입으로 시그니처가 잡힌 **라이브 전투 통화 타입**. 신규 `SkillTableData`는 필드 스키마가 달라 drop-in 불가. 어댑터/재작성 선행 없이는 손대지 말 것.
4. **`Legacy_EnchantMasterTable` / `Legacy_EnchantLevelTable` (.cs/.asset)** — 라이브 스탯·스킬 인챈트가 실제로 읽는 정식 소스. SpellRepo의 동명 API는 캐시 미빌드 죽은 API.
5. **`Legacy_AchievementData` / `Legacy_AchievementDataTable`** — 이름만 `Legacy_`일 뿐 **신규 `ConfigRepo.cs`가 그대로 사용**. 삭제 불가, rename만 가능.
6. **`ScenarioDummyDriver.cs` / `TempStoryToGameFlow.cs`** — 메모상 "교체본 ScenarioDataDriver"는 코드베이스에 실재하지 않음(Grep 0건). `_Story.unity`에서 **동작 중인 프로덕션 시나리오 재생기**. 인트로 대사가 코드 하드코딩이라 데이터 이관 선행 필수.
7. **`EnchantCalculator.GetStatEnhance` 버그 수정 단독 적용 금지** — 이 버그(early-return 누락 → 항상 null)를 고치는 순간 레거시+신규가 같은 이벤트를 2중 구독해 **스탯 인챈트 2중 적용**. 버그수정과 레거시 구독제거는 반드시 같은 변경에 묶어야 함.

---

## Phase 0 — 참조 0건 즉시 삭제 (Quick Win, 저위험)

선행조건 없음. cutover 불필요. 모두 소비자 0건이 전수 확인된 순수 데드 코드/필드.

### 0-1. 죽은 클라우드 필드 제거

- **목표**: 게임플레이가 읽지 않는 Firestore round-trip 필드 제거.
- **대상 파일**: `Userclouddata.cs:28-30` (hpBonus/attackBonus/shieldBonus), `Firestoreservice.cs:377-379`(write), `Firestoreservice.cs:460-467`(read).
- **작업**: write/read 6줄 + 필드 3개 삭제. 실제 아웃게임 성장은 `PlayerModel.ApplyStatBonus_OutGameBonus`(Ingamebootstrap:109-110)가 `ConfigRepo` 출력으로 적용하므로 무관.
- **선행조건**: 없음.
- **리스크**: low. 기존 Firestore 문서의 잔존 키는 무시됨(마이그레이션 불요).
- **노력**: S.
- **DoD**: 컴파일 통과 + 아웃게임 hp/attack 보너스가 인게임에 동일 적용됨 확인.

### 0-2. SpellRepo 죽은 인챈트 캐시/API 제거

- **목표**: 어디서도 빌드되지 않아 항상 null인 죽은 API 제거.
- **대상 파일**: `SpellRepo.cs:39-41`(_enchantMaster/_enchantLevels/_enchantWeights 필드), `SpellRepo.cs:211-212`(GetEnchantMaster/GetEnchantLevel).
- **작업**: 단, 이 API들은 Phase 2 cutover에서 **정식 구현으로 되살릴 대상**이다. 따라서 "지금 삭제"가 아니라 **Phase 2 시작 시 재구현으로 대체**한다. Phase 0에서는 `_enchantWeights` 필드(소비자 0건, 부활 계획 없음)만 제거.
- **선행조건**: 없음.
- **리스크**: low.
- **노력**: S.
- **DoD**: `_enchantWeights` 참조 0건 확인 후 삭제, 컴파일 통과.

### 0-3. Legacy_EnchantWeightTable 전체 제거

- **목표**: 가중치 인챈트 추첨 기능이 코드 어디에도 연결되지 않은 데드 데이터 형상 제거(EnchantSelector는 `EnchantProbabilityConfig`만 사용).
- **대상 파일**: `Legacy_EnchantWeightTable.cs` + `.asset`, `Legacy_EnchantWeightData`(DataClasses_Legacy.cs:81), `CharacterRepo_Legacy.cs:33,45,67,237-242`(_enchantWeightTable/_enchantWeights/GetEnchantWeights), `DataTableSchemaRegistry.cs:108`, `DataReferenceAutoBinder.cs:29`.
- **작업**: `GetEnchantWeights()` 호출부 0건 재확인 → 위 필드/메서드/클래스/asset/임포터 등록 일괄 삭제.
- **선행조건**: (기획 확인) 가중치 인챈트 추첨이 예정 기능이 아님을 확정. 예정이면 신규 EnchantModel 구현 후로 보류.
- **리스크**: low.
- **노력**: S.
- **DoD**: Import All 재빌드 시 임포터 에러 없음 + 인챈트 추첨 동작 무변화.

### 0-4. 참조 0건 데드 테이블/row 타입 제거 (repo plumbing만 참조)

- **목표**: 게임플레이 호출자 0건 + repo 배관만 참조하는 테이블 3종 제거.
- **대상 파일 및 작업**:
  - `Legacy_SkillMasterTable.cs`+`.asset`+`Legacy_SkillMasterData`: `GetSkillMaster` 호출자 0건. `CharacterRepo_Legacy.cs:26,40,219` + `SpellRepo.cs:36,199` 배관 제거 후 삭제. `DataTableSchemaRegistry.cs:102` 제거.
  - `Legacy_EffectDataTable.cs`+`.asset`+`Legacy_EffectData`: `GetEffect/TryGetEffect` 호출자 0건(라이브 이펙트는 신규 `EffectMasterTable/EffectTableData`). `CharacterRepo_Legacy.cs:28,42,221,255` + `SpellRepo.cs:38,210` 배관 제거 후 삭제. `DataTableSchemaRegistry.cs:104` 제거.
  - `Legacy_MapLanguageTable.cs`+`.asset`+`Legacy_MapLanguageData`: 읽는 코드 0건(임포터 등록 + 주석 FK만). `DataTableSchemaRegistry.cs:110` 제거, `DBDataClasses.cs:77-78`의 stale FK 주석 정리.
- **선행조건**: 각 테이블의 `.asset`이 임포터 파이프라인에서 누락 시 에러를 던지지 않는지 확인.
- **리스크**: low.
- **노력**: S (3건 합산 M).
- **DoD**: 3종 삭제 후 Import All 재빌드 에러 없음 + 스킬/이펙트/맵명 표시 동작 무변화.

### 0-5. 참조 0건 데드 타입(스텁) 제거

- **목표**: 정의·빈 스텁만 있고 실사용 0건인 타입 제거.
- **대상 파일**: `Legacy_MonsterPoolMasterData`(참조 0), `Legacy_StageDisplayData`(`LobbyView.cs:64` 빈 스텁 SetStageButtons + `ILobbyView.cs:9` 시그니처만) — 스텁 메서드·인터페이스 시그니처째 삭제.
- **선행조건**: 없음.
- **리스크**: low.
- **노력**: S.
- **DoD**: 컴파일 통과 + 로비 스테이지 버튼 동작 무변화.

### 0-6. ConfigRepo_Legacy 데드 중복 제거

- **목표**: 신규 `ConfigRepo`의 죽은 복제본 제거.
- **대상 파일**: `ConfigRepo_Legacy.cs` 전체. 단 `DataManager_Legacy.cs:50,54`의 `_configRepo` 필드/프로퍼티가 참조 → Phase 1에서 Legacy_DataManager prefab 정리와 함께 제거하거나, 먼저 필드/프로퍼티를 떼고 클래스 삭제.
- **작업**: `GetOutGameGrowthBonus`의 stun/slow 보너스 기획 유지 여부 확인(현재 호출부 0). 폐기면 그대로 삭제.
- **선행조건**: (기획 확인) stun/slow 아웃성장 보너스 폐기 확정.
- **리스크**: low.
- **노력**: S.
- **DoD**: 신규 `ConfigRepo` 라이브 경로(Ingamebootstrap:108, CharacterLevelData:64) 무변화.

---

## Phase 1 — Legacy_DataManager에 매달린 데드 Repo 분리·삭제 (저위험)

`Legacy_StageRepo`/`Legacy_ConfigRepo`는 런타임 호출 0건이며 유일 참조가 `Legacy_DataManager`의 필드/프로퍼티뿐이다. 단, `Legacy_DataManager` 자체는 아직 살아있으므로(`CharacterRepo` 때문에) **필드만 떼어내 데드 Repo를 분리 삭제**한다.

### 1-1. Legacy_StageRepo 분리 삭제

- **목표**: 신규 `StageRepo`로 100% 전환된 레거시 스테이지 레포 제거.
- **대상 파일**: `StageRepo_Legacy.cs`, `Legacy_StageSpawnRuleTable.cs`+`.asset`, `Legacy_StageSpawnRuleData`(DataClasses_Legacy.cs:109), `DataManager_Legacy.cs:49,53`(_stageRepo 필드/프로퍼티).
- **작업**: (a) 모든 `.unity`/`.prefab`(특히 `Legacy_DataManager.prefab`)에서 `Legacy_StageRepo` 컴포넌트 배선 확인·해제 → (b) `DataManager_Legacy.cs` 필드 49/53 제거 → (c) 파일/asset/row 타입 삭제 → (d) `DataTableSchemaRegistry` 해당 등록 제거.
- **선행조건**:
  - **(검증 필수)** 신규 `StageWaveRuleData`/`SpecialWaveRuleData`가 레거시 `GrowthType/GrowthValue/SpawnPositionType(RandomAll/SP_1..SP_7)` 의미를 모두 커버하는지 필드 파리티 확인(이번 패스에서 미확인).
  - **(검증 필수)** 어떤 라이브 씬/prefab도 `Legacy_StageRepo`를 컴포넌트로 붙이고 있지 않은지 grep(.unity/.prefab).
- **리스크**: low~medium(파리티 미확인분).
- **노력**: S.
- **DoD**: 스폰 규칙·성장·스폰 위치가 인게임에서 기존과 동일 + 컴파일/임포트 통과.

### 1-2. Legacy_ConfigRepo 분리 삭제

- **목표**: 신규 `ConfigRepo`가 라이브 100% 점유한 레거시 설정 레포 제거(0-6과 묶어 처리 가능).
- **대상 파일**: `ConfigRepo_Legacy.cs`, `DataManager_Legacy.cs:50,54`(_configRepo 필드/프로퍼티).
- **작업**: `Legacy_DataManager.prefab`에서 컴포넌트 제거 → 필드/프로퍼티 제거 → 클래스 삭제.
- **선행조건**: 0-6의 stun/slow 보너스 폐기 확정.
- **리스크**: low.
- **노력**: S.
- **DoD**: `GetOutGrowthBonusUntilLevel`/`GetAchievement` 등 신규 ConfigRepo API 동작 무변화.

> 이 시점에서 `Legacy_DataManager`에는 `CharacterRepo` 한 개만 남는다. 이것이 Phase 2의 단일 타깃이다.

---

## Phase 2 — 인챈트 데이터 소스 단일화 (핵심 cutover, 고위험)

**이번 정리의 진짜 blocker.** 인챈트 마스터/레벨 데이터의 유일 실소스가 `Legacy_CharacterRepo`이므로, **신규 SpellRepo로 데이터를 이식한 뒤에만** 호출부 전환·레거시 삭제가 가능하다. legacyOnlyCapabilities가 있으므로 **이식 먼저 → 삭제** 순서를 엄수한다.

### 2-1. SpellRepo에 Enchant/Skill 데이터 빌드 이식 (선이식)

- **목표**: 신규 `SpellRepo`가 EnchantMaster/EnchantLevel/Skill 데이터를 실제로 캐싱·제공하도록 함.
- **대상 파일**: `SpellRepo.cs`(Initialize에 `_enchantMaster/_enchantLevels` Build 추가, `_skillDataTable` 정식 배선), `DataManager.prefab`(Resources)의 SpellRepo SO 배선.
- **작업**: `Legacy_EnchantMasterTable`/`Legacy_EnchantLevelTable` SO를 SpellRepo에 배선 + `GetEnchantMaster/GetEnchantLevel`가 실데이터 반환하도록 Build. `_skillDataTable` 정상 로드로 GetSkill 폴백 불필요화.
- **선행조건**: Phase 0/1 완료(데드 캐시 정리). ID 체계 이중성(SkillEnchantTable Name_ID 50/54 ↔ Legacy EnchantID 101/301/401) 매핑 방침 결정 — `SkillEnchantSystem.ResolveLegacyEnchantId` 산술을 정식화할지 확정(김영찬 인챈트 도메인과 겹침).
- **리스크**: high.
- **노력**: L.
- **DoD**: `DataManager.Instance.SpellRepo.GetEnchantMaster/GetEnchantLevel`가 Legacy_CharacterRepo와 **동일 값** 반환(수치 일치 검증).

### 2-2. EnchantCalculator.GetStatEnhance 버그 수정 + 레거시 구독 제거 (단일 변경으로 묶음)

- **목표**: 스탯 인챈트 적용을 신규 EnchantCalculator 단일 경로로 일원화(2중 적용 차단).
- **대상 파일**: `EnchantCalculator.cs:500-527`(GetStatEnhance), `Ingamebootstrap.cs:40,121,122,133`, `_InGame` 씬 SerializeField, `EnchantApplicationSystem.cs`.
- **작업**:
  1. `GetStatEnhance` if 블록마다 early-return 추가 + 끝줄 무조건 null 대입 제거. HP/Attack/AttackSpeed 외 **Pierce/CritRate/CritDmg ValueType 매핑 추가**.
  2. `StatEnchantTable.asset`이 모든 스탯 인챈트 행을 보유하고 레거시 `Legacy_EnchantLevelTable` 값과 **수치 일치**하는지 검증.
  3. 신규 이어하기 재적용 진입점 신설: `EnchantModel.RestoreFromSave` 직후 OwnedStats 순회 → EnchantCalculator로 StatusEnhance 재적용(레거시 ReapplyFromSave의 레벨별 델타 의미를 StatusEnhance Rate 누적과 동치로 설계). `Ingamebootstrap.cs:133` 대체.
  4. `Ingamebootstrap.cs:120-122`의 `Legacy_EnchantApplicationSystem` 생성/Initialize 제거.
- **★순서 위험**: 1번(버그수정)만 단독 적용하면 즉시 스탯 2중 적용. 1~4를 **반드시 한 커밋/한 PR로 묶을 것.**
- **선행조건**: 2-1 완료(신규 데이터 소스 유효).
- **리스크**: high.
- **노력**: L.
- **DoD**: 라이브·이어하기 양 경로에서 스탯 인챈트가 **단일 적용**되고, 세이브→로드 왕복 후 누적 스탯이 동일(Rate 곱셈 보존).

### 2-3. SkillEnchantSystem 레거시 결합 제거

- **목표**: 스킬 인챈트 등록을 신규 `SkillEnchantTable`(Name→Skill 체인) 기반으로 정식화.
- **대상 파일**: `SkillEnchantSystem.cs:36-45`(NameIdToLegacyEnchantId 폴백표), `:96-112`(ResolveLegacyEnchantId), `:120-127`(Legacy EnchantMaster 역조회).
- **작업**: `OwnedSkills.Data.Skill_ID` 산술변환(이미 1순위 동작, LinkedSkillID와 산술 동치)을 정식 경로로 단순화 → Legacy EnchantMaster 역조회/폴백표 제거. 스킬/스탯 구분을 `master.LinkedSkillID<=0` 대신 `AcquiredSkillData.GroupID`(GROUP_*_SKILL vs GROUP_*_STAT)로 대체.
- **선행조건**: 2-1, 2-2 완료. `SkillEnchantSystem.ReapplyFromSave`(Ingamebootstrap:548-549)는 교체식이라 델타 불필요 — 라이브/이어하기 동시 전환.
- **리스크**: medium~high.
- **노력**: M.
- **DoD**: 모든 원소(화/수/빙/풍/뇌) 스킬 인챈트가 레거시 EnchantMaster 없이 등록·동작. `GetEnchantMaster/GetEnchantLevel` 호출부 0건.

### 2-4. 레거시 인챈트 데이터/Repo/PlayerModel 잔재 삭제

- **목표**: 호출부 0건 확인 후 레거시 인챈트 코어 일괄 제거.
- **대상 파일**:
  - `EnchantSelectView.cs:417-418`(Legacy_DataManager null 가드 2줄) 정리.
  - `PlayerModel.cs:235-279`(Legacy_ApplyAttackBonus_Rate/_HpBonus_Add/_PierceBonus_Add/_CriRateBonus_AddF/_CriDmgBonus_AddF) — EnchantApplicationSystem이 유일 호출부였으므로 동반 삭제.
  - `EnchantApplicationSystem.cs` 클래스 파일 삭제.
  - `CharacterRepo_Legacy.cs:31-32,222-223`(GetEnchantMaster/GetEnchantLevel + 캐시/SerializeField) 삭제.
  - `Legacy_EnchantMasterTable.cs`/`.asset`, `Legacy_EnchantLevelTable.cs`/`.asset`, `Legacy_EnchantMasterData`/`Legacy_EnchantLevelData`(DataClasses_Legacy.cs), `DataTableSchemaRegistry.cs:106-107` 제거.
- **선행조건**: 2-1~2-3 전부 완료 + `GetEnchantMaster/GetEnchantLevel` 전 호출부 0건 grep 확인.
- **리스크**: high.
- **노력**: M.
- **DoD**: 인챈트(스탯+스킬) 라이브/이어하기 전 경로 정상 + 컴파일/Import All 통과.

---

## Phase 3 — Legacy_SkillData → SkillTableData 전투 cutover (최고난도, 고위험)

전투 런타임의 사실상 통화 타입을 교체하는 단계. legacyOnlyCapabilities(필드 단위 런타임 계약)가 가장 광범위하므로 **어댑터 선작성 → 검증 → 삭제**를 엄수한다.

### 3-1. SkillTableData→Legacy_SkillData 어댑터 작성 (선이식)

- **목표**: 전투 시스템 재작성 없이 신규 테이블에서 전투 데이터를 공급.
- **대상 파일**: 신규 어댑터(또는 `Legacy_SkillData`→`SkillRuntimeData` rename + 매핑), `SpellRepo.GetSkill`.
- **작업**: **검증된 필드 매핑** 작성 — `Dmg(int)→DmgRate(float)`, `HitSize_X/Y→HitSize`, `Count(float)→PelletCount(int)/Interval`, `Hit_Scope(string)→AttackType 라우팅`(현 GetDamageGroupType/Hit_Scope 분기). 매핑표를 코드 반영 전 문서로 확정.
- **선행조건**: Phase 2 완료(인챈트 정리로 SpellRepo 단순화). **(검증 필수)** Legacy_SkillData↔SkillTableData 정확한 필드 의미 대조표(미확정).
- **리스크**: high.
- **노력**: L.
- **DoD**: `SpellRepo.GetSkill`이 신규 `SkillEnchantTable/SkillTableData`에서 어댑터 경유로 전투용 데이터를 반환.

### 3-2. 전투 검증 및 레거시 스킬 소스/폴백 제거

- **목표**: 전투 parity 확인 후 레거시 스킬 데이터 경로 제거.
- **대상 파일**: `SpellRepo.cs:200-209`(Legacy_CharacterRepo 폴백), `CharacterRepo_Legacy.cs:27,41,220`(_skillDataTable/GetSkill), `Skillsystem.cs`/`FireSpirit.cs`/`Combatsystem.cs`(타입을 신규/어댑터 타입으로 점진 전환), `DummyCombatTester.cs:123,134`.
- **작업**: `DummyCombatTester` + 각 원소 스킬 인게임 실행으로 parity 확인 → GetSkill 레거시 폴백 제거 → 전투 코드 타입 전환 → `Legacy_SkillDataTable.cs`/`.asset`/`Legacy_SkillData` 클래스 삭제.
- **선행조건**: 3-1 완료 + 전투 parity 통과.
- **리스크**: high.
- **노력**: L.
- **DoD**: 모든 원소 스킬 데미지/범위/투사체/주기가 기존과 동일 + 컴파일 통과. (메모: `Legacy_SkillDataTable` 누락 시 스킬 전무동작 회귀 주의 — Import All 재빌드 검증 필수.)

---

## Phase 4 — Legacy_DataManager 완전 제거 + Achievement rename + 잔여 정리 (중위험)

### 4-1. Legacy_DataManager + prefab 최종 삭제

- **목표**: Phase 2/3로 `CharacterRepo`·`SkillData` 참조가 끊긴 뒤 마지막 남은 레거시 매니저 제거.
- **대상 파일**: `DataManager_Legacy.cs`, `Resources/Legacy_DataManager.prefab`, `CharacterRepo_Legacy.cs`(잔여), `SpellRepo.cs:205`(Legacy 폴백 잔재).
- **선행조건**: `Legacy_DataManager.Instance` 전 참조(SpellRepo:205, EnchantApplicationSystem:149, SkillEnchantSystem:120, EnchantSelectView:417-418) 0건 grep 확인.
- **리스크**: high → (선행 충족 시) medium.
- **노력**: M.
- **DoD**: 부팅 시 BeforeSceneLoad 레거시 부트스트랩 미발생 + 인챈트/스킬 전부 정상.

### 4-2. Legacy_AchievementData rename (삭제 아님)

- **목표**: 신규 ConfigRepo가 쓰는 정식 타입에서 `Legacy_` 접두 제거.
- **대상 파일**: `Legacy_AchievementData`→`AchievementData`, `Legacy_AchievementDataTable`→`AchievementTable`(DBDataClasses/DataTables 정식 명명으로 이동), `ConfigRepo.cs:28,43,61,73,78,83` 리포인트, `DataTableSchemaRegistry.cs:112` 동기 수정, `.asset` 재생성/재배선.
- **작업**: (기획 확인) 업적이 라이브 기능인지 결정 — 유지면 rename, 폐기면 ConfigRepo에서 `_achievementTable/_achievements/GetAchievement(s)` 제거 후 삭제(현재 게임플레이 호출자 0건).
- **선행조건**: 업적 기능 존속 여부 기획 확정.
- **리스크**: medium(광역 rename + 임포터 동기).
- **노력**: M.
- **DoD**: rename 후 Import All 재빌드 에러 없음 + ConfigRepo 정상.

### 4-3. UI 임시 3종 cutover

- **목표**: "임시/삭제예정" UI 흐름을 정식 라우팅·데이터 재생기로 교체.
- **대상 파일 및 순서**:
  1. `TempSceneLoader.cs` — `_Lobby.unity`/`_Story.unity`/`Page_MainLobby.prefab`에서 onClick 걸린 버튼 식별 → 정식 라우팅(`GameManager.LoadLobby/LoadInGame` 또는 `ScreenNavigator`)으로 재배선 → 삭제. (low/S)
  2. `ScenarioDummyDriver.cs` + `TempStoryToGameFlow.cs` — **1:1 강결합이라 동시 처리.** 인트로(3001/3002)·다시보기 대사를 ScriptableObject/StoryRepo로 추출 → ScenarioView 인터페이스(OnAdvance/OnSkip/ShowLine/OnFinished) 유지한 신규 `ScenarioDataDriver` 작성 → 종료후 라우팅(다시보기 복귀 vs 인게임 진입) 이식 → `_Story.unity` 컴포넌트 교체 → 두 파일 삭제. (medium/M)
- **선행조건**: 시나리오 대사 데이터 이관 완료(portrait/BG 0 → 텍스트만 상태 인지). 버튼 onClick 배선은 씬/prefab 인스펙터 확인 필요(코드 grep만으로 미확정).
- **리스크**: low(SceneLoader)~medium(시나리오).
- **노력**: S + M.
- **DoD**: 로비/스토리 내비게이션 및 인트로·다시보기 재생이 기존과 동일 동작 + 임시 3종 파일 삭제.

> 이 시점에서 `DataClasses_Legacy.cs`에 남는 것은 (a) Achievement(rename 처리됨), (b) `Legacy_LanguageEntry`(LocalizationManager 실사용 — 별도 폐기 검토), (c) StageSpawnRule(Phase 1에서 제거됨) 정도. 전투/인챈트 레거시 row 타입은 Phase 2/3에서 소거됨. 잔여 정리 후 `DataClasses_Legacy.cs` 삭제 가능.

---

## Phase 5 — .asmdef 모듈화 (의존 그래프 단순화 후)

레거시 제거로 Features↔Data 강결합이 풀린 뒤에만 안전. **레거시 잔존 상태에서 asmdef를 먼저 그으면 `SpellRepo→Legacy_DataManager→Legacy_CharacterRepo` 등 경계 위반으로 빌드가 깨진다.**

### 5-1. (선행 가능) Editor 어셈블리 분리

- **목표**: 런타임 어셈블리의 UnityEditor 누수 제거(빌드 안정화). **레거시와 무관하므로 Phase 0과 병행 즉시 가능.**
- **대상**: `AprilLog.Editor.asmdef`(`_Game/Editor` + `_Project/Data/.../Editor`, includePlatforms=[Editor]) — DataTableSchemaRegistry/씬 인스톨러/프리팹 어플라이어.
- **선행조건**: 없음.
- **리스크**: low~medium.
- **노력**: M.
- **DoD**: 런타임 빌드에서 UnityEditor 의존 0 + 에디터 툴 정상.

### 5-2. 런타임 어셈블리 도입 (말단→루트)

- **목표**: 레이어별 단방향 경계 강제.
- **대상/순서**: `AprilLog.Shared`(_Game/Shared, 유틸) → `AprilLog.Data`(Repositories/DataTables/row, Shared만 참조) → `AprilLog.Features`(Combat/Enchant/HUD/Lobby) → `AprilLog.UI`(시나리오 포함) → 마지막 `AprilLog.Core`(GameManager/DataManager/Bootstrap/Network). (선택) `AprilLog.DevTest`는 define 가드/Editor 한정.
- **작업 — 최대 난관**: **Core↔Features 순환 역전.** `Ingamebootstrap`이 Features(EnchantModel/PlayerModel/SkillEnchantSystem)를 직접 참조 → Core가 Features에 의존. 부트스트랩을 Features 어셈블리로 내리거나, Core는 인터페이스(예: `IEnchantSystem`)만 알고 구현을 Features에 두는 식으로 역전. asmdef 도입 **전에** 이 순환을 해소.
- **선행조건**: Phase 2~4 완료(레거시 제거로 그래프 단순화). namespace 부재로 경계가 컴파일러 강제 안 됨 — asmdef 도입이 곧 강제 수단.
- **리스크**: medium.
- **노력**: L.
- **DoD**: 5개 런타임 어셈블리 단방향 의존 성립(역참조 0) + 전체 빌드/플레이 정상 + 컴파일 시간 측정.

---

## 1주차에 할 안전한 첫 3가지

모두 **선행조건 없음 / 저위험 / 즉시 회귀 없음**이며, 이후 고위험 cutover의 의존 그래프를 미리 가볍게 만든다.

1. **`AprilLog.Editor.asmdef` 분리 (Phase 5-1)** — 레거시와 완전 무관하고, 런타임 빌드에서 UnityEditor 누수를 제거해 빌드 안정화 효과가 즉시 크다. 가장 안전한 leaf 작업.
2. **죽은 클라우드 필드 + 데드 인챈트 가중치 제거 (Phase 0-1 + 0-3)** — `UserCloudData.hpBonus/attackBonus/shieldBonus`(소비자 0)와 `Legacy_EnchantWeightTable` 전체(EnchantSelector는 `EnchantProbabilityConfig`만 사용). 호출부 0건이 전수 확인된 순수 데드.
3. **참조 0건 데드 테이블 3종 제거 (Phase 0-4)** — `Legacy_SkillMasterTable`/`Legacy_EffectDataTable`/`Legacy_MapLanguageTable`. `GetSkillMaster/GetEffect/TryGetEffect`/맵명 읽기 모두 호출자 0건. 단, 삭제 직후 **Import All 재빌드로 임포터 에러 없음**만 확인하면 끝.

> 1주차 공통 마무리 체크: 각 작업 후 (a) 컴파일 통과, (b) Import All 재빌드 에러 0, (c) 인게임 1판 스모크(스킬 발동·인챈트 적용·스테이지 진행) 무회귀. Phase 1의 `Legacy_StageRepo` 삭제는 `StageWaveRuleData` 필드 파리티 검증이 끝나기 전까지 1주차에 넣지 말 것.

---

# 적대적 타당성 검증 (Critique)

Two more confirmations:
- `DataReferenceAutoBinder.cs` is under an `/Editor/` folder — already editor-only by Unity's folder convention. The roadmap's Phase 5-1 (split editor asmdef) is reasonable, but note `Ingamebootstrap.cs:366` has a `#if UNITY_EDITOR` block — meaning a runtime file references UnityEditor under guard. That's a real (already-handled) editor leak, and a candidate for the Phase 5-1 cleanup the roadmap mentions.

I now have enough verified evidence. Final verdict.

---

# 적대적 검증 결과: AprilLog 아키텍처 정리 로드맵

검증을 위해 실제 읽은 핵심 파일: `SpellRepo.cs`, `EnchantCalculator.cs`, `EnchantApplicationSystem.cs`, `SkillEnchantSystem.cs`, `Ingamebootstrap.cs`, `DataManager_Legacy.cs`, `Legacy_DataManager.prefab`, 그리고 전수 Grep(인챈트/스킬/스테이지/asmdef/auto-load).

## 검증으로 확인된 사실 (로드맵의 핵심 주장은 대체로 정확)

- **[확인] SpellRepo 인챈트/스킬마스터 API는 죽은 API.** `SpellRepo.Initialize()`는 `_enchantMaster`/`_enchantLevels`/`_skillMaster`/`_effects`를 빌드하지 않음. `GetEnchantMaster/GetEnchantLevel/GetSkillMaster/GetEffect`는 항상 null. → Phase 2 "선이식 먼저" 순서 정당.
- **[확인] 스탯 인챈트 2중 적용 위험은 실재.** `EnchantCalculator.cs:65-66`와 `EnchantApplicationSystem.cs:49-50`가 **동일 이벤트**(`EnchantModel.OnStatAcquired/OnStatLevelUp`)를 둘 다 구독하고 둘 다 `PlayerModel`을 변경. 현재 Calculator 경로는 `GetStatEnhance`(L500-527, early-return 없이 끝줄 `status=null`)가 항상 null이라 무력 → 레거시만 적용 중. **버그 수정 단독 = 즉시 2중 적용.** Phase 2-2의 "한 커밋 강제" 경고는 load-bearing.
- **[확인] 인챈트 데이터 유일 실소스 = Legacy_DataManager.CharacterRepo.** 스탯(`EnchantApplicationSystem.cs:120,124,136`)·스킬(`SkillEnchantSystem.cs:120-121`) 모두 `Legacy_DataManager.Instance.CharacterRepo`에서 읽음. 선이식 없는 삭제 = 인챈트 전무동작.
- **[확인] Legacy_SkillData 전투 임베드.** `Skillsystem.cs` 41건, `FireSpirit.cs` 2건. `GetSkill` 라이브 소비자 3곳(Combatsystem:173, Ingamebootstrap:537, SkillEnchantSystem:158) + SpellRepo→레거시 폴백(L206)이 현재 유일 동작 경로. Phase 3 최고난도 평가 정당.
- **[확인] asmdef 0개**(1st-party). Phase 5 "한꺼번에 의존 해소 강제" 정당.
- **[확인] Legacy_StageRepo는 어떤 .unity/.prefab 씬에도 컴포넌트로 안 붙음** — 단 `Legacy_DataManager.prefab` 내부 자식으로는 존재(SO 풀배선). 로드맵 Phase 1의 "검증 필수" 항목 중 씬 배선 건은 **클린으로 확인됨**(내가 대신 수행).

## 교정 권고 (심각도순)

**[HIGH] H1 — Phase 1: `DataManager_Legacy.InitRepo()` 호출부 누락.** 로드맵은 `_stageRepo`/`_configRepo` 제거 대상으로 `DataManager_Legacy.cs:49,53(필드/프로퍼티)`만 적었으나, `InitRepo()`의 **L87 `_stageRepo.Initialize()` / L88 `_configRepo.Initialize()`** 도 같이 지워야 컴파일된다. Phase 1-1/1-2 대상 파일 목록에 `DataManager_Legacy.cs:87,88` 추가.

**[HIGH] H2 — Phase 1 "런타임 호출 0건"은 API 호출 기준이지, 실행 기준이 아님.** `Legacy_DataManager`는 `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`(L69)로 매 부팅 prefab을 Instantiate하고 자식 3 Repo를 전부 Initialize. 즉 `Legacy_StageRepo`/`Legacy_ConfigRepo`는 "호출되진 않지만 매 실행 살아서 SO를 로드 중". 단순 삭제는 안전하나, **로드맵이 "데드라 무위험"으로 표현한 것은 과소평가** — prefab 자식 노드/InitRepo 라인까지 동시 수정해야 BeforeSceneLoad NRE가 안 난다. (H1과 묶어 처리)

**[HIGH] H3 — Phase 1-1 StageRepo 필드 파리티 검증을 1주차에서 빼라는 지침은 유지하되, prefab 배선 확인은 끝났음을 반영.** `Legacy_StageRepo`는 `_spawnRuleTable/_scalingTable/_poolMasterTable` 등 6개 SO를 prefab에 물고 있음(L101-106). 신규 `StageWaveRuleData/SpecialWaveRuleData`가 `GrowthType/GrowthValue/SpawnPositionType`·scaling·poolMaster를 모두 커버하는지 **필드 대조가 진짜 blocker**. 로드맵이 이미 "검증 필수"로 둔 건 옳음 — 미해결 상태 유지.

**[MEDIUM] M1 — Phase 0-1 "소비자 0건" 표현 부정확(결론은 동일).** `Userclouddata.hpBonus/attackBonus`는 Firestore read/write 외 게임플레이 소비자 0건 맞음(부팅은 ConfigRepo의 별도 로컬 out-var 사용). 단 `shieldBonus`는 **hp/attack과 달리 신규 대응 경로 자체가 없음**(쉴드 성장 미구현) — "동일 적용 확인" DoD가 shieldBonus엔 적용 불가. 삭제는 안전하나 DoD 문구를 hp/attack 한정으로 좁혀라.

**[MEDIUM] M2 — Phase 2-3 `ResolveLegacyEnchantId` 산술의 물폭탄 함정이 폴백표에 의존.** `SkillEnchantSystem.cs:96-112` 1순위 산술은 물폭탄 폭발행(idx 11)에서 `idx%=10`으로 보정하지만, 주석(L43-44)이 명시하듯 **폴백표 `{60,201}`가 누락 보정의 안전망**. 로드맵 2-3은 "폴백표 제거"를 목표로 하는데, 폴백표 제거 시 물폭탄 자동공격 등록 누락 회귀 가능. 2-3 DoD에 "물폭탄(Name 60) 자동공격 등록"을 명시 검증 항목으로 추가.

**[MEDIUM] M3 — Phase 5-2 Core↔Features 순환은 실재(이미 로드맵이 지목).** `Ingamebootstrap`(Core)이 `EnchantModel/PlayerModel/SkillEnchantSystem/Legacy_EnchantApplicationSystem`(Features)을 직접 생성·주입. 추가로 `Ingamebootstrap.cs:366`에 `#if UNITY_EDITOR` 런타임 에디터 누수 존재 → Phase 5-1(에디터 어셈블리 분리) 시 이 가드 블록도 정리 대상. 로드맵에 누락된 구체 지점이니 추가.

**[LOW] L1 — Phase 0-4/0-5 "0건"은 SpellRepo/CharacterRepo_Legacy 자기참조를 제외한 수치.** `GetSkillMaster/GetEffect`는 repo 내부 정의만 잡힘(외부 호출 0) — 맞음. 단 Grep DoD에 "repo-internal 정의 제외" 조건을 명시하지 않으면 검증자가 false-positive로 막힐 수 있음. 각 0-x DoD에 "정의/배관 제외" 단서 추가 권장.

**[LOW] L2 — 순서 위험은 전반적으로 잘 통제됨.** 로드맵 전체에서 "신규 이식 전 레거시 삭제" 단계는 **발견되지 않음**. Phase 0(0-2 포함)이 "지금 삭제 아님, Phase 2에서 재구현"으로 명시 보류한 것, Phase 2-2의 단일커밋 강제, Phase 3 어댑터-선작성은 모두 안전 순서. (a) 항목 = 통과.

## 한 줄 총평
순서 안전성(레거시 선삭제 함정)은 통과 — 핵심 위험(SpellRepo 죽은 API, 스탯 2중 적용, 인챈트 단일 실소스)은 코드로 모두 실증되어 로드맵 골격은 신뢰 가능하나, **Phase 1의 누락 참조(`DataManager_Legacy.InitRepo` L87-88 + prefab 자식 노드)와 "데드=무위험" 과소평가(H1·H2)만 보강하면 실행 가능한 계획이다.**