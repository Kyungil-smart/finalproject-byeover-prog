// 담당자 : 정승우
// 설명   : 화염 정령 (조합 스킬 '화염 정령 소환') -- 지속시간 동안 주기적으로 화염 작렬을 시전하는 소환수

using UnityEngine;

/// <summary>
/// 플레이어 양옆에 소환되어 lifetime 동안 castInterval마다
/// 연결된 스킬(화염 작렬)을 자기 위치에서 시전한다. 수명이 다하면 스스로 파괴된다.
/// 생성/설정은 SkillSystem.SummonSpirits가 담당한다.
/// </summary>
public class FireSpirit : MonoBehaviour
{
    private SkillSystem _skillSystem;
    private Legacy_SkillData _castSkill;
    private float _lifetime;
    private float _castInterval;
    private float _castTimer;

    private const float BurstShotInterval = 0.25f; // 화염 작렬 발사 간격(QA 요청 0.25초). SkillSystem.FlameBurstShotInterval과 동일
    private static readonly WaitForSeconds BurstShotWait = new WaitForSeconds(BurstShotInterval); // 발사마다 재할당 방지

    public void Init(SkillSystem skillSystem, Legacy_SkillData castSkill, float lifetime, float castInterval)
    {
        _skillSystem = skillSystem;
        _castSkill = castSkill;
        _lifetime = lifetime;
        _castInterval = castInterval;
        _castTimer = castInterval; // 소환 직후 즉시 1회 시전 → t≈0,1,2,3,4초 = 총 5회 (기획 '1초마다, 5초간')
    }

    private void Update()
    {
        _lifetime -= Time.deltaTime;
        if (_lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        _castTimer += Time.deltaTime;
        if (_castTimer >= _castInterval)
        {
            _castTimer = 0f;

            if (_skillSystem != null && _castSkill != null)
                StartCoroutine(CastBurst());
        }
    }

    // 화염 작렬은 PelletCount발 연속 발사 -- 정령 위치에서 동일하게 재현
    private System.Collections.IEnumerator CastBurst()
    {
        int shots = Mathf.Max(1, _castSkill.PelletCount);
        for (int i = 0; i < shots; i++)
        {
            _skillSystem.FireProjectileFrom(_castSkill, transform.position);
            yield return BurstShotWait;
        }
    }
}
