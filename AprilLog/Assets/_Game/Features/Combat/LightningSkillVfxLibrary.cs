// 설명   : 번개(전기) 속성 스킬 VFX 프리팹 모음 (lightning_Asset 패키지 연결용 ScriptableObject)
//          Resources/LightningSkillVfxLibrary.asset 으로 두고 SkillSystem이 Resources.Load로 읽는다.
//          기획 v2.02 4장(스킬 에셋 및 판정 구조) 기준. 스케일은 인스펙터에서 조정.

using UnityEngine;

[CreateAssetMenu(fileName = "LightningSkillVfxLibrary", menuName = "AprilLog/Lightning Skill VFX Library")]
public class LightningSkillVfxLibrary : ScriptableObject
{
    [Header("구형 번개 (StandardID 401) — Projectile_Lightning_Ball_Lv3 / 지속형 장판")]
    public GameObject orbPrefab;
    [Tooltip("구형 번개 VFX 스케일 보정. ⚠ 이 prefab은 내부에 100배 스케일 자식(scalingMode Hierarchy)이 있어 1이면 화면을 덮음 → 0.015 정도로 강하게 축소. 근본 해결은 prefab의 100배 자식 스케일을 정상화하는 것")]
    public float orbScale = 0.015f;

    [Header("벼락 (StandardID 404) — Lightning_Big / 단발 낙뢰 반복")]
    public GameObject thunderboltPrefab;
    [Tooltip("벼락 VFX 스케일 보정 (기획 4-4 Scale 400x300)")]
    public float thunderboltScale = 0.35f;

    [Header("뇌격 (StandardID 405) — Lazer_purple / 세로 레이저")]
    public GameObject laserPrefab;
    [Tooltip("뇌격 VFX 스케일 — Lazer_purple prefab의 m_MaxParticleSize를 0.5(정상)로 고쳐서 키워도 안 터짐. 플레이어 세로 레이저로 크게(0.5~, 더 키워도 됨). 인스펙터 튜닝")]
    public float laserScale = 0.5f;
    [Tooltip("뇌격 레이저 회전(도) — Lazer_purple prefab의 startRotation이 0이라 위아래가 뒤집혀 보임. 스펙 4-5-2 'Start Rotation 180' 기본. 방향 안 맞으면 0/90 등으로 조정")]
    public float laserRotationDeg = 180f;

    [Header("사슬 번개 (StandardID 402) — Clone(전기선 몸체) + CFXR Electrified 2(몬스터 타격 이펙트)")]
    public GameObject chainBolt;   // CFXR Electric Barrier (HDR, Purple)(Clone) = 전기선 몸체
    [Tooltip("전기선 몸체 스케일 — ⚠ CFXR Barrier는 prefab 루트가 100배 스케일이고 SpawnVfx가 곱셈(localScale*scale)이라 실제크기 = 100×이 값. 0.0075면 실제 ~0.75배. 0.04(=4배)는 짧은 체인 구간에 과대. 인스펙터 튜닝")]
    public float chainScale = 0.0075f;
    [Tooltip("전기선 회전 보정(도) — April→타겟 방향 정렬. 전기선이 비스듬하면 조정")]
    public float chainBoltRotationTrimDeg = 0f;
    [Tooltip("⚠ 에디터에서 드래그: 사슬 적 타격 이펙트 = CFXR Electrified 2 (Purple) — 몬스터 몸체에 재생")]
    public GameObject chainHitEffect;
    [Tooltip("타격 이펙트 스케일 — 작게 시작(0.1~). 인스펙터 튜닝")]
    public float chainHitScale = 0.1f;
    [Tooltip("체인 최대 타겟 수 — 전기줄이 1→2→3… 순차 점프 (기획 5타겟)")]
    public int chainMaxTargets = 5;
    [Tooltip("체인 점프 간격(초) — 작을수록 '다다닥' 빠르게 이어짐")]
    public float chainHopDelay = 0.07f;

    [Header("방전 (StandardID 403) — Clone(전기선) + Charge_Lightning(양옆 구슬) + CFXR Electric Barrier(밑 연결점)")]
    public GameObject dischargeBarrier;
    [Tooltip("방전 가운데 번개막 스케일 — ⚠ CFXR Barrier는 루트 100배라 0.04여도 실제 4배. 화면 덮음 방지로 프리팹 m_MaxParticleSize도 0.15로 캡함. 더 작게=0.02, 더 넓게=0.06. 인스펙터 튜닝")]
    public float dischargeScale = 0.04f;
    public GameObject dischargeOrb;   // Charge_Lightning (양옆 구슬)
    [Tooltip("양옆 구슬 스케일 — ⚠ Charge_Lightning은 내부 200배 자식이라 아주 작게(0.005 이하). 인스펙터에서 직접 보며 튜닝")]
    public float dischargeOrbScale = 0.005f;
    [Tooltip("양옆 구슬 좌우 거리(px) — 기획 4-3-2 ±600")]
    public float dischargeOrbOffsetPx = 600f;
    [Tooltip("⚠ 에디터에서 드래그: 방전 밑 전기선 연결점 = CFXR Electric Barrier (HDR, Purple)")]
    public GameObject dischargeConnector;
    [Tooltip("밑 연결점 스케일 — 100배 자식이라 작게(0.1~). 인스펙터 튜닝")]
    public float dischargeConnectorScale = 0.1f;
    [Tooltip("밑 연결점 세로 오프셋(px) — '밑에' 위치. 음수=아래")]
    public float dischargeConnectorYOffsetPx = -120f;
}
