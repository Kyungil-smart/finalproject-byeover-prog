// 담당자 : 정승우
// 설명   : 불 속성 스킬 VFX 프리팹 모음 (Fire_Asset 패키지 연결용 ScriptableObject)
//          Resources/FireSkillVfxLibrary.asset 으로 두고 SkillSystem이 Resources.Load로 읽는다.
//          → 씬마다 손으로 연결할 필요 없음. 스케일/타이밍은 인스펙터에서 조정.

using UnityEngine;

[CreateAssetMenu(fileName = "FireSkillVfxLibrary", menuName = "AprilLog/Fire Skill VFX Library")]
public class FireSkillVfxLibrary : ScriptableObject
{
    [Header("메테오 (3단: 생성 구역 → 몸체 낙하 → 착탄 폭발)")]
    [Tooltip("생성 구역 마커 (Flame_ellipse)")]
    public GameObject meteorMarker;
    [Tooltip("낙하하는 몸체 (Fireball_loop_2)")]
    public GameObject meteorBall;
    [Tooltip("착탄 폭발 (explosion_5)")]
    public GameObject meteorExplosion;

    public float meteorMarkerScale = 1f;
    public float meteorBallScale = 1f;
    public float meteorExplosionScale = 1f;

    [Tooltip("생성 구역(ellipse) 표시 후 낙하 시작까지 (예고 시간)")]
    public float meteorTelegraph = 0.5f;
    [Tooltip("몸체 낙하 시간")]
    public float meteorFallTime = 0.35f;
    [Tooltip("생성 구역이 떠 있는 높이 (착탄 지점 기준 위쪽, 월드 단위)")]
    public float meteorFallHeight = 3.5f;

    [Tooltip("생성 구역의 가로 치우침(월드 단위). 양수 = 착탄 지점보다 오른쪽 위에 생성되어 '왼쪽 사선'으로 낙하")]
    public float meteorFallOffsetX = 1.2f;

    [Tooltip("몸체 회전 미세조정(도). 기본 회전은 낙하 방향에서 자동 계산되고, 이 값은 거기에 더해진다")]
    public float meteorBallRotationTrimDeg = 0f;

    [Tooltip("몸체 소멸 후 폭발까지 한 박자(초). 폭발이 몸체를 가리는 걸 방지")]
    public float meteorExplosionDelay = 0.05f;

    [Header("추후 연결 — 화염 작렬 / 파이어브레스 / 대지 균열 / 정령")]
    public GameObject fireballProjectile;   // Fireball_2_normal
    public GameObject fireBreathCrystal;    // Magic_Sphere_normal
    public GameObject fireBreathFlame;      // Flame_normal
    public GameObject earthCrackExplosion;  // Ground_explosion_normal
    public GameObject earthCrackEmber;      // fire_big
    public GameObject spiritAttack;         // Uzi_red (용도 확인 중)
    public GameObject[] hitEffects;         // expl_01_01~05
}
