// 설명   : 물(수) 속성 스킬 VFX 프리팹 모음 (Resources/WaterSkillVfxLibrary.asset 으로 두고 SkillSystem이 Resources.Load로 읽는다).
//          기획 물 속성. 물폭탄(투사체 공+착탄 폭발), 탄환세례/급류(장판), 파도소환(솟구치는 파도), 하이드로펌프(중앙 세로 빔).
//          주의: 일부 원본 프리팹 파티클이 scalingMode=Local이라 루트 스케일을 무시함 — 크기 안 먹으면 프리팹 startSize/scalingMode를 손봐야 함(얼음 전례).

using UnityEngine;

[CreateAssetMenu(fileName = "WaterSkillVfxLibrary", menuName = "AprilLog/Water Skill VFX Library")]
public class WaterSkillVfxLibrary : ScriptableObject
{
    [Header("물 폭탄 (StandardID 201) — 투사체(공) + 착탄 폭발")]
    public GameObject waterBallProjectile;
    public float waterBallScale = 1f;
    [Tooltip("진행방향 자동회전에 더하는 보정(도)")]
    public float waterBallRotationTrimDeg = 0f;
    public GameObject waterBombImpact;
    public float waterBombImpactScale = 1f;
    public float waterBombImpactRotationDeg = 0f;

    [Header("탄환 세례 (StandardID 202) — 부채꼴 스플래시 장판")]
    public GameObject bulletShowerVfx;
    public float bulletShowerScale = 1f;
    public float bulletShowerRotationDeg = 0f;
    [Tooltip("물대포 연출: VFX를 발사점에서 이만큼 위에서 뿜는다(월드 단위). 실측: 발사점=에이프릴 몸 센터(y -0.14), 비주얼 신장 1.45유닛, 정수리 +0.53 — 0.25면 얼굴/머리 높이")]
    public float bulletShowerHeadOffsetY = 0.25f;

    [Header("급류 (StandardID 203) — 폭포 띠(전체 폭)")]
    public GameObject torrentVfx;
    public float torrentScale = 1f;
    [Tooltip("폭포가 가로로 눕도록 기본 -90")]
    public float torrentRotationDeg = -90f;

    [Header("파도 소환 (StandardID 204) — Eff_Water_Rare_WaterWaveSkill_05 / 솟구치는 파도")]
    public GameObject waveVfx;
    public float waveScale = 1f;
    [Tooltip("기획 4-4: Rotation x (기본 -10)")]
    public float waveRotationXDeg = 0f;
    [Tooltip("기획 4-4: Rotation z (기본 -90)")]
    public float waveRotationDeg = 0f;

    [Header("하이드로 펌프 (StandardID 205) — 중앙 세로 빔 2초 지속")]
    public GameObject hydroBeamVfx;
    public float hydroBeamScale = 1f;
    public float hydroBeamRotationDeg = 0f;
}
