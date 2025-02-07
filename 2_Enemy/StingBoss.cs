using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using BlockDefenceAttack;

public class StingBoss : BossEnemy, IBossAttack, IBossTargetingAttack, IBossNonTargetingAttack
{

    [SerializeField] EnemyMeleeAttack rightMeleeAttack;
    [SerializeField] EnemyMeleeAttack leftMeleeAttack;

    ProjectilePattern firstNormalPattern; // 첫번째 기본 패턴
    ProjectilePattern secondNormalPattern; // 두번째 기본 패턴

    ProjectilePattern stingPattern; // 강화패턴1 -벌침 공격
    ProjectilePattern rndStingPattern; // 강화패턴2 - 벌침 난사

    BossSummonPatternData summonPattern; // 강화패턴3 - 잡몹 소환

  
    float meleeAnimTime = 1.5f;

    float specialAnimTime = 2.0f;

    int rndAttackTimes = 2; // 랜덤 관통 투사체 공격횟수


    public float BasicPatternAnimTime { get; set; }

    /// <summary>
    /// 상속 받은 메소드
    /// </summary>
    #region

    protected override void LoadPatternData()
    {
        base.LoadPatternData();

        string fId = baseBossPatternData.fPatternID;
        string sId = baseBossPatternData.sPatternID;

        firstNormalPattern = new ProjectilePattern();
        secondNormalPattern = new ProjectilePattern();
        // stingPattern = new ProjectilePattern();

        // 데이터 불러오기
        firstNormalPattern.ConvertProjectilePattern(DataManager.Instance.dataTable.GetBaseBossProjectileData(fId), gradeNum);
        secondNormalPattern.ConvertProjectilePattern(DataManager.Instance.dataTable.GetBaseBossProjectileData(sId), gradeNum);

        CreateBossProjectileSample(firstNormalPattern);
        CreateBossProjectileSample(secondNormalPattern);

        // 강화패턴은 난이도 체크

        int diffLevel = (int)bossDiffLevel;

        if (diffLevel >= baseBossPowerPatternData[0].diffLevel)
        {
            stingPattern = new ProjectilePattern();
            stingPattern.ConvertProjectilePattern(DataManager.Instance.dataTable.GetBaseBossProjectileData(baseBossPowerPatternData[0].id), gradeNum);
            CreateBossProjectileSample(stingPattern);

        }

        if (diffLevel >= baseBossPowerPatternData[1].diffLevel)
        {
            rndStingPattern = new ProjectilePattern();
            rndStingPattern.ConvertProjectilePattern(DataManager.Instance.dataTable.GetBaseBossProjectileData(baseBossPowerPatternData[1].id), gradeNum);
            CreateBossProjectileSample(rndStingPattern);

        }

        if(diffLevel >= baseBossPowerPatternData[2].diffLevel)
        {
            summonPattern = new BossSummonPatternData();
            summonPattern.ConvertSummonPattern(DataManager.Instance.dataTable.GetBaseBossSummonData(baseBossPowerPatternData[2].id, baseBossPowerPatternData[2].diffLevel) , gradeNum);
        }


        BasicPatternAnimTime = 0.5f;

      
    }

    // 보스 움직임
    public override void StartMoveBoss()
    {
        base.StartMoveBoss();
        MoveBossEnemy(-15f, 2f, BossPatternCoro());
    }

    // 기본 패턴 메소드 등록
    protected override void SetBossNormalPattern()
    {

        AddNormalPatternList(AttackDelayCoro("Projectile",BasicPatternAnimTime, BossTargetingAttack));
        AddNormalPatternList(AttackDelayCoro("Projectile",BasicPatternAnimTime, BossNonTargetingAttack));
       
        // Sting(Epic) 보스 고유 패턴 추가 처리    
        if (mon.monStatData.index == 72)
        {
            AddNormalPatternList(MeleePatternCoro());

           // nPatternIndexList.Add(nPatternIndexList.Count);
         //   if ((int)bossDiffLevel >= 1) pPatternIndexList.Add(pPatternIndexList.Count);
        }
    }


    // 강화 패턴 메소드 등록
    protected override void SetBossPowerPattern()
    {

        AddPowerPatternList(UnderlingMonsterSummonPattern(summonPattern));
        AddPowerPatternList(PenetrationAttack());
        AddPowerPatternList(RandomPenetrationAttack());
       
        if (mon.monStatData.index == 72 && (int)bossDiffLevel >= 1)
        {          
           AddPowerPatternList(PowerMeleePatternCoro());
        }
    }

    protected override void FailBossMissionPattern()
    {
        ProjectileAttackToUser(firstNormalPattern);
    }

    #endregion  


    // 타게팅 투사체 공격
    public void BossTargetingAttack()
    { 
        BossTargetProjectilePattern(/*minCount, maxCount,*/ firstNormalPattern);
    }

    // 논타겟팅 투사체 공격
    public void BossNonTargetingAttack()
    {     
        BossNonTargetProjectilePattern(/*minCount, maxCount,*/ secondNormalPattern);
    }


    // Sting -고유 기본 공격 패턴 - 근접공격
    IEnumerator MeleePatternCoro()
    {
        //보스 움직임
        float moveTime = MoveRadomHorizontalPosition(3.5f);

        yield return YieldCache.WaitForSeconds(moveTime);

        rightMeleeAttack.SetMeleeAttack(mon.monStatData.attNum, enemyAtkHandler.CalculateAttackDamage());

        mon.anim.SetTrigger("SkillTrigger");
        mon.anim.SetInteger("SkillAttack", 1);

        yield return YieldCache.WaitForSeconds(meleeAnimTime);
        rightMeleeAttack.EndMeleeAttack();

        BossMoveHorizontal(0, moveTime);

        yield return YieldCache.WaitForSeconds(moveTime);

    }


    // 관통 공격
    IEnumerator PenetrationAttack()
    {
        float moveTime = MoveRadomHorizontalPosition(3.5f);

        yield return YieldCache.WaitForSeconds(moveTime);

        yield return AttackDelayCoro("Projectile",BasicPatternAnimTime, BeePenetrationAttack);


        yield return YieldCache.WaitForSeconds(meleeAnimTime);

        BossMoveHorizontal(0, moveTime);

        yield return YieldCache.WaitForSeconds(moveTime);

    }

    /// <summary>
    /// 무작위 방향 관통 벌침 공격
    /// </summary>
    /// <returns></returns>

    IEnumerator RandomPenetrationAttack()
    {
        for (int i = 0; i < rndAttackTimes; i++)
        {
            int rotValue = Random.Range(-30, 30);

            transform.localRotation = Quaternion.Euler(new Vector3(0, 180 + rotValue, 0));

            yield return AttackDelayCoro("Projectile",BasicPatternAnimTime, BeePenetrationAttack);

            yield return YieldCache.WaitForSeconds(1.0f);
        }

        transform.localRotation = Quaternion.Euler(new Vector3(0, 180, 0));

    }

    // 벌 보스 관통 공격
    void BeePenetrationAttack()
    {
        MonsterProjectile projectile = GetBossProjectileObject(attackPos, stingPattern);

        projectile.ShootMonsterProjectile(enemyAtkHandler.CalculateAttackDamage()/*, stingPattern.explodeScale*/);
    }


    /// <summary>
    /// 보스 고유 패턴
    /// </summary>
    /// <returns></returns>
    #region 
    // 강화 근접 공격
    IEnumerator PowerMeleePatternCoro()
    {
        //보스 움직임
        float moveTime = MoveRadomHorizontalPosition(3.5f);

        yield return new WaitForSeconds(moveTime);

        float damage = enemyAtkHandler.CalculateAttackDamage();
        int att = mon.monStatData.attNum;

        for (int i = 0; i < 2; i++)
        {
            rightMeleeAttack.SetMeleeAttack(att, damage);
            leftMeleeAttack.SetMeleeAttack(att, damage);

            mon.anim.SetTrigger("SkillTrigger");
            mon.anim.SetInteger("SkillAttack", 2);

            yield return YieldCache.WaitForSeconds(specialAnimTime);
            rightMeleeAttack.EndMeleeAttack();
            leftMeleeAttack.EndMeleeAttack();
        }

        BossMoveHorizontal(0, moveTime);

        yield return YieldCache.WaitForSeconds(moveTime);

    }

    #endregion


}
