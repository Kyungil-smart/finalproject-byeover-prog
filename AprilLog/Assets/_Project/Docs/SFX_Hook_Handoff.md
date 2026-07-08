# SFX 연결 완료 안내 (담당별 위치 공유)

사운드 가이드(이가현·이균호) 35항목의 훅이 **전부 연결됐습니다**. 각자 도메인에 추가된 위치를 공유하니 확인해 주세요. 문제 있으면 정승우에게.

## 시스템 사용법 (신규 사운드 추가 시)

```csharp
AudioManager.Play(SfxId.ButtonClick);   // 효과음
AudioManager.Bgm(SfxId.HousingBgm);     // BGM 교체 (같은 곡이면 무시)
```

- 배선/씬 배치 불필요 — AudioManager 없으면 자동 생성.
- **파일명 문자열 사용 금지.** 클립·볼륨·중복재생·재생간격은 `Resources/SoundLibrary.asset` 인스펙터에서 관리(밸런스 튜닝도 여기서, 코드 수정 없음).

## 적용 위치 (도메인별)

### 인게임 (정승우)
BGM(InGameBootstrap), 정산 클리어/오버(ShowSettlement), 자동공격/sort(CombatSystem), 유닛 선택(SortInputHandler)/드롭(SortSystem — 매칭 성공 시 드롭음 제외 규칙 반영), 조커(JokerSystem), 인챈트 선택·리롤(EnchantSequenceSelectPresenter)·교체(EnchantChangePresenter), 강공 5종(SkillSystem.FireSkill), 원소 피격 5종(MonsterAI.TakeDamage)

### 아웃게임
- 레벨업 성공: LobbyCharacterLevelUI.TryLevelUp (레벨 반영 직후)
- 뽑기/10연차: ShopGachaPresenter.ExecuteDraw (모든 뽑기 경로 공용 지점, count 10 이상이면 포탈 루프 추가)
- 돌파: ArtifactManager.AttemptAscension (성공 판정 후 1곳)
- 분해: ArtifactManager.ManualDisassemble
- 제조: ArtifactCraftService.TryCraft
- 팝업 등장: ScreenNavigator.OpenMenu (공용 단일 지점)
- 공용 버튼: **AudioManager가 씬 로드 때 모든 Button에 자동 바인딩** — 개별 버튼에 달 필요 없음. 단 런타임 Instantiate되는 동적 버튼은 못 잡으니 생성부에서 직접 Play 필요.
- 인게임 입장: TempSceneLoader.LoadScene (현재 PLAY 버튼 유일 사용처. 로비 정식 배선 시 이 파일 삭제와 함께 스타트 버튼으로 이전할 것)

### 하우징 (조규민 확인 요청)
- BGM: LobbyPageController.ShowPage — 하우징 페이지면 하우징 BGM, 나가면 로비 BGM 자동 복귀
- 대사: HousingPlayerReactionPresenter.HandleTouched
- 발소리: HousingPlayerMoveController.MovePlayer (재생 간격 0.2초로 걸음 리듬)
- 책장: HousingBookshelfReplayBinder.OpenReplayStoryPopup
- 커피머신: HousingAdRewardButtonView.OnPointerClick
- 재화 생산기: HousingIdleRewardController.HandleClaimRequested (지급 성공 시)
- 침대: HousingInteractionPresenter (bed_sleep 터치) — **훅은 있고 클립만 비어 있음. 기획이 파일 정하면 SoundLibrary.asset에 꽂기만 하면 됨**

## 기획 확인 필요 (2건)
- 침대(HousingBed) 사운드 파일 지정
- OutgameCast(Neutral_Medium_Cast_06) — 가이드 아웃게임 10.0 항목 용도가 비어 있음 (SfxId 예약됨)
