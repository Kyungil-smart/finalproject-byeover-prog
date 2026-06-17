// 설명   : 바람(풍) 속성 스킬 VFX 프리팹 모음 (Imports/WindSkill 패키지 연결용 ScriptableObject)
//          Resources/WindSkillVfxLibrary.asset 으로 두고 SkillSystem이 Resources.Load로 읽는다.
//          기획 v2.02 바람 속성. 투사체(헤이스트/바람칼날/템페스트)는 투사체 스킨, 하자드(돌풍/허리케인/부메랑)는 장판 VFX.
//          ⚠ 원본 프리팹 m_MaxParticleSize가 8이라 화면을 덮어서, 와이어링 전에 0.5로 정상화했음(번개 Lazer 전례).

using UnityEngine;

[CreateAssetMenu(fileName = "WindSkillVfxLibrary", menuName = "AprilLog/Wind Skill VFX Library")]
public class WindSkillVfxLibrary : ScriptableObject
{
    [Header("헤이스트 (StandardID 301) — Haste / 보조 투사체 스킨")]
    public GameObject hasteProjectile;
    public float hasteProjectileScale = 1f;
    [Tooltip("진행방향 자동회전에 더하는 보정(도). 뒤집히면 180")]
    public float hasteRotationTrimDeg = 0f;

    [Header("바람 칼날 (StandardID 302) — WindBlade / 관통 투사체 스킨")]
    public GameObject windBladeProjectile;
    public float windBladeProjectileScale = 1f;
    [Tooltip("Stretch 빌보드라 진행축 안 맞으면 ±90/180 보정")]
    public float windBladeRotationTrimDeg = 0f;

    [Header("템페스트 (StandardID 305) — Tempest / 8히트 관통 투사체 스킨")]
    public GameObject tempestProjectile;
    public float tempestProjectileScale = 1f;
    public float tempestRotationTrimDeg = 0f;

    [Header("돌풍 (StandardID 303) — a gust of wind / 전방 장판 단발 VFX")]
    public GameObject gustHazard;
    public float gustHazardScale = 1f;
    [Tooltip("빌보드 파티클 회전 필요 시(도). 기본 0")]
    public float gustRotationDeg = 0f;

    [Header("허리케인 (StandardID 304) — hurricane / 지속 소용돌이 (WindHeldRoutine)")]
    public GameObject hurricaneHazard;
    public float hurricaneHazardScale = 1f;
    public float hurricaneRotationDeg = 0f;

    [Header("부메랑 (백업, StandardID 306) — Boomerang / 허리케인형. ⚠ skill_data에 306 행 없어 미발동, VFX만 준비")]
    public GameObject boomerangHazard;
    public float boomerangHazardScale = 1f;
    public float boomerangRotationDeg = 0f;
}
