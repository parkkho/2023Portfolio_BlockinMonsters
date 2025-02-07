using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BlockDefenceAttack;

public class HermitKingBoss : BossEnemy, IBossAttack, IBossTargetingAttack, IBossNonTargetingAttack /*, IBossShieldMode*/
{
    ProjectilePattern firstNormalPattern;
    ProjectilePattern secondNormalPattern;

    ProjectilePattern rotationPattern;


    // 회전공격 패턴 데이터
    float rotationTime = 3.0f;
    float rotAttackInterval = 0.5f;

    int minTornadoCount = 2;
    int maxTornadoCount = 4;

    [SerializeField] ShieldPatternConfig shieldPatternData;

    public Slider ShieldSlider { get; set; }
    public float BasicPatternAnimTime { get; set; }

    int normalPatternCnt = 0;

    protected override void LoadPatternData()
    {
        base.LoadPatternData();

        string fId = baseBossPatternData.fPatternID;
        string sId = baseBossPatternData.sPatternID;

        firstNormalPattern = new ProjectilePattern();
        secondNormalPattern = new ProjectilePattern();

        // 데이터 불러오기
        firstNormalPattern.ConvertProjectilePattern(DataManager.Instance.dataTable.GetBaseBossProjectileData(fId), gradeNum);
        secondNormalPattern.ConvertProjectilePattern(DataManager.Instance.dataTable.GetBaseBossProjectileData(sId), gradeNum);

        CreateBossProjectileSample(firstNormalPattern);
        CreateBossProjectileSample(secondNormalPattern);


        rotationPattern = new ProjectilePattern();
        rotationPattern.ConvertProjectilePattern(DataManager.Instance.dataTable.GetBaseBossProjectileData(baseBossPowerPatternData[0].id),gradeNum);

        CreateBossProjectileSample(rotationPattern);

        BasicPatternAnimTime = 0.5f;
    }

    public override void StartMoveBoss()
    {
        base.StartMoveBoss();
        MoveBossEnemy(-12f, 2f, BossPatternCoro());

    }

   

    protected override void SetBossNormalPattern()
    {
        AddNormalPatternList(AttackDelayCoro("Projectile", BasicPatternAnimTime, BossTargetingAttack));
        AddNormalPatternList(AttackDelayCoro("Projectile", BasicPatternAnimTime, BossNonTargetingAttack));
    }

    protected override void SetBossPowerPattern()
    {
        if (normalPatternCnt == 10)
        {
            normalPatternCnt = 0;

            AddPowerPatternList(StartShieldModeCoro());

            return;
        }


        AddPowerPatternList(RotationProjectileAttackCoro());

        if (mon.monStatData.index == 45 && (int)bossDiffLevel >= 1)
        {
            AddPowerPatternList(WaterTornadoAttack());
        }

    }


    protected override IEnumerator StartBossNormalPattern()
    {
        normalPatternCnt++;
        return base.StartBossNormalPattern();
    }


    protected override void FailBossMissionPattern()
    {
        ProjectileAttackToUser(firstNormalPattern);
    }

    // 보스 타게팅 공격
    public void BossTargetingAttack()
    {      
        BossTargetProjectilePattern(firstNormalPattern);
    }

    public void BossNonTargetingAttack()
    {
      
        BossNonTargetProjectilePattern(secondNormalPattern);

      
    }

    // 소라 게 회전공격 
    void RotationProjectileAttack()
    {    
        BossNonTargetProjectilePattern(rotationPattern);
    }

    IEnumerator RotationProjectileAttackCoro()
    {
        mon.anim.SetInteger("SkillAttack", 2);
        mon.anim.SetTrigger("SkillTrigger");

        float rotTime = 0f;

        while(rotTime < rotationTime)
        {
            yield return YieldCache.WaitForSeconds(rotAttackInterval);
            rotTime += 0.5f;
            RotationProjectileAttack();

            //rotTime += 0.5f;
        }

        mon.anim.SetInteger("SkillAttack", -1);

        yield return YieldCache.WaitForSeconds(1.0f);

       // isPattern = false;
    }

 

    // 방어모드 실패처리
    public void FailShieldMode()
    {
        enemyAtkHandler.MonsterHp = BuffDefine.SetHpRateBuff(enemyAtkHandler.MonsterHp, shieldPatternData.hpIncreaseRate);

        BuffDefine.SetRateBuffStatus(ref enemyAtkHandler.atkStatus.def, shieldPatternData.defIncreaseRate, true);
        enemyAtkHandler.atkStatus.GetDefAverage();
       
    }

    // 방어모드 코루틴
    public IEnumerator StartShieldModeCoro()
    {
        bossState = BossState.Mission;
        bossMissionMgr.enabled = true;

        PuzzleUIManager.Instance.bossHpSlider.gameObject.SetActive(false);
        PuzzleUIManager.Instance.ShowPuzzleNarPanel(2);

        int maxShield = (int)(mon.monStatData.hp * shieldPatternData.shieldValue);

        bossMissionMgr.SetShieldMode(maxShield, shieldPatternData.shieldTime);
        
        mon.anim.SetBool("isHide", true);
       
        while (bossState == BossState.Mission)
        {
      
            yield return YieldCache.WaitForSeconds(0.1f);

            if (bossMissionMgr.bossMissionState == BossMissionManager.BossMissionState.Clear) break;


            // 시간 초과 실패시
            if (bossMissionMgr.bossMissionState == BossMissionManager.BossMissionState.Fail)
            {
                FailShieldMode();
                break;
            }
        }

        prePowerPattern = -1;

        mon.anim.SetBool("isHide", false);

        bossMissionMgr.bossMissionState = BossMissionManager.BossMissionState.Ready;
        bossMissionMgr.enabled = false;
        bossState = BossState.Idle;

        PuzzleUIManager.Instance.bossHpSlider.gameObject.SetActive(true);

    }

    // 간헐천 패턴 => 무작위 셀 위에 이펙트 생성
    IEnumerator WaterTornadoAttack()
    {
        int insCount =  Random.Range(minTornadoCount, maxTornadoCount + 1);

        List<int> pickIndexList = new List<int>(Utils.CreateUnDuplicateRandomIndex(insCount, 64));

        mon.anim.SetTrigger("SkillTrigger");
        mon.anim.SetInteger("SkillAttack", 1);

        

        for (int i=0; i < pickIndexList.Count; i++)
        {
            StartCoroutine(OneWaterTornadoAttack(GamePlay.Instance.boardGrid[pickIndexList[i]]));
        }

        yield return YieldCache.WaitForSeconds(BasicPatternAnimTime);

        mon.anim.SetInteger("SkillAttack", 0);

    }



    IEnumerator OneWaterTornadoAttack(BoardCell boardCell)
    {     
        GameObject waterTornado = EffectManager.Instance.GetMonsterEffect(3001);

        EffectPool tornadoEff = waterTornado.GetComponent<EffectPool>();

        waterTornado.transform.position = boardCell.transform.position;
        tornadoEff.PlayEffect();

        boardCell.CanPlace = false;

        // 배치된 블록이 있을 경우 블록 아웃
        if (boardCell.isPlaced)
        {
            WaterTornadolOutSequence(boardCell.placeBlock);
            boardCell.ClearCell();
        }

        yield return YieldCache.WaitForSeconds(2.0f);

        tornadoEff.ReturnEffect();
        boardCell.CanPlace = true;


    }

    // 간헐천 공격에 따른 블록 날아가는 움직임-> 
    void WaterTornadolOutSequence(Block block)
    {
        Transform trf = block.transform;

        Sequence moveSequence = DOTween.Sequence().SetEase(Ease.OutQuart);

        moveSequence.Append(trf.DOMoveY(20f, 3f).SetRelative())
            .Join(trf.DOLocalRotate(new Vector3(-360f, 0, 0), 0.2f, RotateMode.FastBeyond360).SetLoops(10, LoopType.Incremental))                     
            .OnComplete(() => { block.ReturnBlock(); });

    }
}
