using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BlockDefenceAttack;
using System;

using Random = UnityEngine.Random;


abstract public class BossEnemy : Enemy //보스 몬스터 클래스 => 공통된 기능 및 필요한 메소드 정리 - 해당 클래스를 상속받아 각 보스 구현
{

    public enum BossClass { Rare, Epic, Legendary }; // 보스 고유 클래스
    public enum BossDifficultyLevel { Easy, Normal, Hard } // 난이도
    public enum BossState { Idle, Mission/*, Shield */} //보스 상태

    public BossClass bossType = BossClass.Legendary;
    public BossDifficultyLevel bossDiffLevel = BossDifficultyLevel.Easy; // 보스 난이도
    public BossState bossState = BossState.Idle;

    //  protected int bossMode = 0; // 강화 여부 번호 0: 기본 , 1: 강화모드

    [SerializeField]
    protected bool isPattern = false; //  패턴 진행여부

    public BossMissionManager bossMissionMgr;

    [SerializeField] protected Transform attackPos; // 보스 투사체 시작 지점

    [SerializeField]
    protected BaseBossPatternData baseBossPatternData = null;

    //보스 강화 패턴 데이터 리스트
    protected List<BaseBossPowerPatternData> baseBossPowerPatternData = new List<BaseBossPowerPatternData>();
    [SerializeField]
    protected List<BaseBossMissionPatternData> baseBossMissionPatternData;
   
    protected int curMissionPatternNum = 0; // 현재 진행한 미션 패턴 번호


    protected Coroutine co_bossPattern; // 현재 보스 패턴 코루틴

    protected Dictionary<string, GameObject> bossPoolSampleDic = new Dictionary<string, GameObject>(); // 이펙트 샘플
    protected Dictionary<string, Queue<GameObject>> bossPoolDic = new Dictionary<string, Queue<GameObject>>(); // 이펙트 오브젝트 풀

    // 추가로 사용되는 이펙트ID 리스트
    [SerializeField] protected List<int> bossEffectIDList = new List<int>();


    [Header("InGamePattern")]

    protected List<IEnumerator> normalPatternList = new List<IEnumerator>(); // 기본 패턴
    protected List<IEnumerator> powerPatternList = new List<IEnumerator>(); // 강화패턴

    [SerializeField] int normalPatternCount;  // 현재 진행한 기본 패턴 횟수

    protected int preNormalPattern = -1; //최근 진행한 기본 패턴
    protected int prePowerPattern = -1; // 최근 진행한 강화패턴

    // 기본 패턴 번호 리스트 -> 해당 번호로 무작위 패턴
   [SerializeField]  protected List<int> nPatternIndexList = new List<int>();
    // 강화 패턴 번호 리스트 -> 해당 번호로 무작위 패턴
   [SerializeField] protected List<int> pPatternIndexList = new List<int>();

    // 현재 진행중인 공격 이벤트
    protected delegate void AttackEvent();
    AttackEvent curAttackEvent;
    // AttackEvent normalAtkEvent;


    // 보스 기본 공격 패턴 세팅
    abstract protected void SetBossNormalPattern();

    // 보스 강화 공격 패턴 세팅
    abstract protected void SetBossPowerPattern();

    // 보스 광폭화
   // abstract protected void StrengthenBossPattern();

    // 보스 미션 패턴 실패
    abstract protected void FailBossMissionPattern();


    // 보스 몬스터 기본 세팅
    public override void SetEnemy(int _level, int _monIndex, GameObject monObj = null)
    {
        base.SetEnemy(_level, _monIndex, monObj);

        mon.SetMonsterStatus(monIndex, level, gradeNum);

        mon.SetBossEnemy();

        AdjustBossHp(_level); // 보스 HP보정

        // 전투 데이터 세팅
        enemyAtkHandler = transform.GetComponentInChildren<EnemyAttackHandler>();
        enemyAtkHandler.SetAttackStatus(mon, _level);
        enemyAtkHandler.SetEnemyComponet(GetComponent<Enemy>());

        // 패턴 관련 세팅
        LoadPatternData();

        ObjectPoolManager.Instance.CreatePoolQueue(bossPoolDic, bossPoolSampleDic);


        // 기타 기능 세팅
       // matchUISet = transform.GetChild(1).GetComponent<BossMatchUI>();
        buffHandler = GetComponent<BuffHandler>();
        bossMissionMgr = GetComponent<BossMissionManager>();

    }

    protected override void EndEnemyDiaAnimation()
    {
        base.EndEnemyDiaAnimation();
        EnemyManager.Instance.livingEnemyList.Remove(this);
        if (type == EnemyType.MiddleBoss)
        {
            EnemyManager.Instance.ResultMiddleBossDie();
        }

        Destroy(gameObject);
    }

    // 처음 시작 이동 -> 기본 패턴 시작
    public virtual void StartMoveBoss()
    {
        //보스 hp UI 세팅
        PuzzleUIManager.Instance.SetBossHpSlider(enemyAtkHandler.atkStatus.hp);

        //MoveBossEnemy(-12f, 2f, BossPatternCoro());

    }

    // 패턴 데이터 불러오기
    protected virtual void LoadPatternData()
    {
        baseBossPatternData = DataManager.Instance.dataTable.GetBaseBossPatternData(monIndex);


        // 강화 패턴 데이터 로드
        string[] pType = baseBossPatternData.powerPattern.Split('/');
        string[] pDiff = baseBossPatternData.pPatternDiff.Split('/'); // 난이도 조건
        string[] pID = baseBossPatternData.pPatternID.Split('/');

        int diffLevel = (int)bossDiffLevel; // 현재 보스 난이도

        for (int i = 0; i < pType.Length; i++)
        {
            if (pType[i] == "-1") continue;

            int pDiffLevel = int.Parse(pDiff[i]);


            BaseBossPowerPatternData data = new BaseBossPowerPatternData(int.Parse(pType[i]), pDiffLevel, pID[i]);

            baseBossPowerPatternData.Add(data);
        }

        // 미션 관련 패턴 불러오기
        string[] mType = baseBossPatternData.missionPattern.Split('/');

        if (mType[0] == "-1") return;

        string[] mDiff = baseBossPatternData.mDiffCondition.Split('/');
        string[] mHp = baseBossPatternData.mHpCondition.Split('/');

        //string mIdData = baseBossPatternData.mPatternID.Remove(baseBossPatternData.mPatternID.Length - 1);

        string[] mID = baseBossPatternData.mPatternID.Split('/');

        Debug.Log("미션아이디" + mID[0]);

        baseBossMissionPatternData = new List<BaseBossMissionPatternData>();

        for (int i = 0; i < mType.Length; i++)
        {
            if (int.Parse(mDiff[i]) > diffLevel) continue;

            BaseBossMissionPatternData mData = new BaseBossMissionPatternData(int.Parse(mType[i]), int.Parse(mDiff[i]), float.Parse(mHp[i]), mID[i]);
            baseBossMissionPatternData.Add(mData);
        }

        CreateBossEffectSample();
    }


    // 보스 몬스터 피격 시 미션 패턴 체크
    protected virtual void CheckMissionPattern()
    {
        // 미션 패턴이 없으면 reutrn
        if (baseBossMissionPatternData == null) return;

        if (baseBossMissionPatternData.Count == 0) return;
        // 더 진행 할 미션이 없으면 return
        if (curMissionPatternNum + 1 > baseBossMissionPatternData.Count) return;

        // 이미 미션 중이면 return
        if (bossState == BossState.Mission) return;

        if (!enemyAtkHandler.isDie)
        {
            float hpRate = (enemyAtkHandler.atkStatus.hp / mon.monStatData.hp) * 100f;

            Debug.Log("hpRate" + hpRate);

            //int level = (int)bossDiffLevel;

            if (hpRate <= baseBossMissionPatternData[curMissionPatternNum].startHp)
            {          
                // 현재 미션 타입을 넘겨줌
                StartCoroutine(SetMissionCoro(baseBossMissionPatternData[curMissionPatternNum].type));
            }

        }

    }

    // 미션 패턴 세팅
    IEnumerator SetMissionCoro(int type)
    {
        bossState = BossState.Mission;
        bossMissionMgr.enabled = true;

        // 패턴 끝날때까지 대기
        while (isPattern) yield return null;

        StopBossPattern();

        string id = baseBossMissionPatternData[curMissionPatternNum].id;


        if (type == 1)
        {
            BaseBossMatchPatternData baseData = DataManager.Instance.dataTable.GetBaseBossMatchData(id, (int)bossDiffLevel);

            BossMatchPatternData data = new BossMatchPatternData();

            data.ConvertMatchPattern(baseData);

            StartMatchMissionPattern(data);
        }

        //소환형

        if (type == 2)
        {
            BaseBossSummonPatternData baseData = DataManager.Instance.dataTable.GetBaseBossSummonData(id, (int)bossDiffLevel);
            BossSummonPatternData data = new BossSummonPatternData();

            data.ConvertSummonPattern(baseData, gradeNum);
            Debug.Log("summon");
            StartCoroutine(StartSummonMissionPattern(data));
        }

        
    }


    // 보스HP 조정
    void AdjustBossHp(int level)
    {
       // int levelValue = (level / 5)+1;

        float adjustValue = /*((int)bossDiffLevel + 1) **/ (mon.monStatData.classNum + 1) + level * 0.04f;

        Debug.Log("adjustValue" + adjustValue);
        Debug.Log(" mon.monStatData.hp" + mon.monStatData.hp);

        mon.monStatData.hp *= adjustValue;
    }

    // 보스 도망치는 연출
    public void EscapeBoss()
    {
        transform.localRotation = Quaternion.Euler(Vector3.zero);
        MoveBossEnemy(20f, 3f, CheckDieCoro(null));
    }



    // 보스 z축 움직임
    protected void MoveBossEnemy(float moveValue, float moveTime, IEnumerator coro)
    {
        mon.anim.SetBool("isWalk", true);

        transform.DOMoveZ(moveValue, moveTime).SetRelative().OnComplete(() =>
        {
            mon.anim.SetBool("isWalk", false);
            co_bossPattern = StartCoroutine(coro);
        });
    }



    // 보스에 사용할 투사체 샘플 생성
    protected virtual void CreateBossProjectileSample(ProjectilePattern bossProjectilePattern)
    {

        ProjectilePattern pattern = bossProjectilePattern;

        int effID = pattern.effectID;


        GameObject projectile = EffectManager.Instance.MakeMonsterProjectileByID(effID, mon.monStatData.attNum, transform);

        projectile.name = pattern.patternID;

        projectile.SetActive(false);

        if (bossPoolSampleDic.ContainsKey(pattern.patternID) == false)
        {
            bossPoolSampleDic[pattern.patternID] = projectile;
        }


    }

    // 보스에 사용할 투사체 샘플 생성
    protected virtual void CreateBossObstacleSample(BossObstaclePattern bossObstaclePattern)
    {
        BossObstaclePattern pattern = bossObstaclePattern;

        GameObject obstacle = EffectManager.Instance.GetEnemyObstacleByID(pattern.obstacleID, transform);
        obstacle.SetActive(false);

        obstacle.name = pattern.patternID;

        bossPoolSampleDic[pattern.patternID] = obstacle;

        if (bossPoolSampleDic.ContainsKey(pattern.patternID) == false)
        {
            bossPoolSampleDic[pattern.patternID] = obstacle;
        }

    }

    // 보스에 사용할 투사체 샘플 생성
    protected virtual void CreateBossEffectSample()
    {
        if (bossEffectIDList.Count > 0)
        {
            for (int i = 0; i < bossEffectIDList.Count; i++)
            {
                GameObject effect = EffectManager.Instance.InstantiateMonsterEffect(bossEffectIDList[i]);
                // effect.layer = 17;
                effect.SetActive(false);

                string id = bossEffectIDList[i].ToString();

                if (bossPoolSampleDic.ContainsKey(id) == false)
                { bossPoolSampleDic[id] = effect; }

            }
        }

    }

    // 보스 이펙트 반납
    public void ReturnBossEffect(string id, GameObject pooledObj)
    {
        pooledObj.SetActive(false);

        pooledObj.transform.localEulerAngles = Vector3.zero;

        ObjectPoolManager.Instance.ReturnPooledObject(bossPoolDic[id], pooledObj);
    }


    // 보스 기본 패턴 -> 기본 타입은 블록 타게팅 공격 / 지정 방향 투사체 공격 / 기본 방해물 공격 -> 동일한 패턴을 연속 X
    protected IEnumerator BossPatternCoro()
    {
        normalPatternCount = 0;
        preNormalPattern = -1;

        float intervalTime = baseBossPatternData.nAtkTimeInv;

        yield return YieldCache.WaitForSeconds(1.0f);

        while (!IsDie)
        {
            isPattern = true;
            normalPatternCount++;
            yield return StartCoroutine(StartBossNormalPattern());


            yield return YieldCache.WaitForSeconds(intervalTime);

            // 정해진 기본 패턴 횟수 진행 후 강화패턴
            if (normalPatternCount == baseBossPatternData.pAtkCntInv)
            {
                normalPatternCount = 0;

                isPattern = true;
                yield return StartCoroutine(StartBossPowerPattern());
                


                yield return YieldCache.WaitForSeconds(intervalTime);
            }

        }

    }

    // 기본패턴 등록
    protected void AddNormalPatternList(IEnumerator pattern)
    {
        normalPatternList.Add(pattern);

        nPatternIndexList.Add(normalPatternList.Count - 1);
    }

    // 강화패턴 등록
    protected void AddPowerPatternList(IEnumerator pattern)
    {
        powerPatternList.Add(pattern);

        pPatternIndexList.Add(powerPatternList.Count - 1);
    }

    // 보스 기본 패턴 시작 코루틴
    protected virtual IEnumerator StartBossNormalPattern()
    {
        SetBossNormalPattern();

        int patternIndex;

        if (nPatternIndexList.Count > 1)
        {
            if (preNormalPattern >= 0)
            {
                nPatternIndexList.Remove(preNormalPattern);
            }

            patternIndex = nPatternIndexList[Random.Range(0, nPatternIndexList.Count)];
        }
        else if(nPatternIndexList.Count ==1)
        {
            patternIndex = 0;
        }
        else
        {
            isPattern = false;
            yield break;
        }

        preNormalPattern = patternIndex;
        // Debug.Log("pre " + preNormalPattern);

        IEnumerator normalPatternCoro = normalPatternList[preNormalPattern];

       // Debug.Log("check" + normalPatternCoro);
        yield return StartCoroutine(normalPatternCoro);

        nPatternIndexList.Clear();
        normalPatternList.Clear();

        isPattern = false;
    }

    // 보스 강화 패턴 시작 코루틴
    IEnumerator StartBossPowerPattern()
    {
        SetBossPowerPattern();

        //  List<int> patternList = new List<int>(pPatternIndexList);

        int patternIndex;

        if (pPatternIndexList.Count > 1)
        {
            if (prePowerPattern >= 0)
            {
                pPatternIndexList.Remove(prePowerPattern);
            }

            patternIndex = pPatternIndexList[Random.Range(0, pPatternIndexList.Count)];
        }
        else if(pPatternIndexList.Count ==1)
        {
            patternIndex = 0;
        }
        else
        {
            isPattern = false;  
            yield break;
        }

        
        prePowerPattern = patternIndex;

        IEnumerator powerPatternCoro = powerPatternList[prePowerPattern];

        yield return StartCoroutine(powerPatternCoro);

        pPatternIndexList.Clear();
        powerPatternList.Clear();

        isPattern = false;
    }

    // 보스 논타겟 투사체 공격 패턴 - 한번에 여러개를 쏘는 패턴
    protected virtual void BossNonTargetProjectilePattern(/*int minCount, int maxCount/*, float scale*/ ProjectilePattern pattern)
    {
        int count = Random.Range(pattern.minProjectileCount, pattern.maxProjectileCount);

        for (int i = 0; i < count; i++)
        {
            NonTargetProjectileAttack(attackPos, pattern/*, scale*/);
        }

        SoundManager.Instance.PlaySfx((SoundManager.SfxType)(mon.monStatData.attNum * 2));
        //  isPattern = false;
    }


    // 보스 논타겟 투사체 공격  
    public void NonTargetProjectileAttack(Transform iPos, ProjectilePattern pattern)
    {

        MonsterProjectile enemyProjectile = GetBossProjectileObject(iPos, pattern);

        enemyProjectile.ShootMonsterProjectile(enemyAtkHandler.CalculateAttackDamage()/*, scale*/);

    }

    // 보스 타겟팅 공격 패턴
    protected void BossTargetProjectilePattern(ProjectilePattern pattern)
    {
        int count = Random.Range(pattern.minProjectileCount, pattern.maxProjectileCount);

        for (int i = 0; i < count; i++)
        {
            BossTargetingAttackTemplate(pattern);
        }
    }


    // 보스 타게팅 공격
    protected void BossTargetingAttackTemplate(ProjectilePattern pattern)
    {

        MonsterProjectile projectile = GetBossProjectileObject(mon.attackPos, pattern);

        Vector3 targetPos = projectile.FindBlockTargetPosition();


        Utils.RotateTargetPosition(projectile.transform, targetPos);

        projectile.ShootMonsterProjectile(enemyAtkHandler.CalculateAttackDamage()/*, explodeScale*/);
        SoundManager.Instance.PlaySfx((SoundManager.SfxType)(mon.monStatData.attNum * 2));
        //return projectile;

    }

    // 투사체 가져오기 -> iPos: 생성위치
    protected MonsterProjectile GetBossProjectileObject(Transform iPos, ProjectilePattern pattern)
    {
        GameObject projectile = ObjectPoolManager.Instance.GetPooledObject(bossPoolDic[pattern.patternID]);  

        MonsterProjectile enemyProjectile;

        if (projectile == null)
        {
            //ProjectilePattern pattern = bossPatternData.projectilePatternData[patternOrder];

            projectile = Instantiate(bossPoolSampleDic[pattern.patternID], transform);
            enemyProjectile = projectile.GetComponent<MonsterProjectile>();
            enemyProjectile.SetMonsterProjectile(pattern, mon.monStatData.attNum, false,ReturnBossEffect);
        }
        else
        {
            enemyProjectile = projectile.GetComponent<MonsterProjectile>();
        }
        Transform trf = projectile.transform;
        // trf.SetParent(parent);
        trf.position = iPos.position;
        trf.localEulerAngles = Vector3.zero;
        projectile.SetActive(true);



        return enemyProjectile;
    }

 
    // 부하 몬스터 소환 패턴
    protected IEnumerator UnderlingMonsterSummonPattern(BossSummonPatternData summonData)
    {
        mon.anim.SetTrigger("Summon");

        // Random.Range(bossPatternData.minSummonCount, bossPatternData.maxSummonCount + 1);

        int minCount = summonData.minCount;
        int maxCount = summonData.maxCount;

        // 총 소환 숫자
        int totalSummonCnt = minCount == maxCount ? maxCount : Random.Range(minCount, maxCount + 1);

        // 레벨 계산
        int levelValue = level - summonData.sMonLevelGap;

        // 소환 몬스터 레벨
        int summonLevel = levelValue > 0 ? levelValue : 1;

        float delayTime = summonData.summonTime;

        for (int i = 0; i < totalSummonCnt; i++)
        {
            int index = Random.Range(0, summonData.sMonID.Length);

            int id = summonData.sMonID[index];

            //z좌표 범위
            int rnd = Random.Range(2, 10);

            EnemyManager.Instance.SpawnBossSummonEnemy(id, summonLevel, rnd);

            yield return YieldCache.WaitForSeconds(delayTime);
        }

        //  isPattern = false;
    }


    //블록판 랜덤 위치 가져오기
    public List<int> MakeRandomBoardCell(int needCnt)
    {
        int totalCount = GamePlay.Instance.boardGrid.Count;

        List<int> rndList = new List<int>();
        List<int> result = new List<int>();

        for (int i = 0; i < totalCount; i++)
        {
            rndList.Add(i);
        }

        for (int j = 0; j < needCnt; j++)
        {
            int index = Random.Range(0, rndList.Count);

            result.Add(rndList[index]);

            rndList.RemoveAt(index);

        }

        return result;
    }


    // 보스 피격
    public override void OnDamaged(float _damage, int _attackerAttNum, bool isCri = false/*, bool _isCheckDie*/)
    {
        base.OnDamaged(_damage, _attackerAttNum, isCri = false/*, _isCheckDie*/);

        PuzzleUIManager.Instance.UpdateBossHpSlider(enemyAtkHandler.atkStatus.hp);

        CheckMissionPattern();
        /*if (type == EnemyType.Boss)
        {
            CheckMissionPattern();
        }*/

    }

    // 체크
    public override void CheckDie()
    {
        //CheckMissionPattern();

        base.CheckDie();
    }

    // 죽음 체크
    public override void Die()
    {
        PuzzleUIManager.Instance.bossHpSlider.gameObject.SetActive(false);
        EnemyManager.Instance.spawnBossEnemy.Remove(GetComponent<BossEnemy>());
       // if (type == EnemyType.MiddleBoss) { }
       
        if(type == EnemyType.Boss)
        {
            Debug.Log("보스 죽음");
            EnemyManager.Instance.CheckGameClear();
            PlayerManager.Instance.ReturnFriendBossStage();
        }

       

        StopBossPattern();

        base.Die();

    }

    // 보스 패턴 중지 -> EX) 죽었을 떄
    protected void StopBossPattern()
    {
        if (co_bossPattern != null)
            StopCoroutine(co_bossPattern);
    }

    // 매치 미션 패턴 시작
    public void StartMatchMissionPattern(BossMatchPatternData data)
    {
        bossMissionMgr.SetMatchUI(data);

        StartCoroutine(CheckMissionCoro());
    }

    // 소환미션 코루틴 시작
    IEnumerator StartSummonMissionPattern(BossSummonPatternData data)
    {
      StartCoroutine(bossMissionMgr.SetSummonMission(level, data));

        yield return StartCoroutine(CheckMissionCoro());
    }


    // 미션 상태 체크 코루틴 -> 제한시간 체크
    protected IEnumerator CheckMissionCoro()
    {
        PuzzleUIManager.Instance.bossHpSlider.gameObject.SetActive(false);
        PuzzleUIManager.Instance.ShowPuzzleNarPanel((int)bossMissionMgr.bossMissionType-1);

        mon.anim.SetBool("isCasting", true);

       // float time = matchUISet.timeSlider.maxValue;

        while (bossState == BossState.Mission)
        {
           
            yield return YieldCache.WaitForSeconds(0.1f);

            if (bossMissionMgr.bossMissionState == BossMissionManager.BossMissionState.Clear) break;


            // 시간 초과 실패시
            if (bossMissionMgr.bossMissionState == BossMissionManager.BossMissionState.Fail)
            {              
                FailBossMissionPattern();
                break;
            }
        }

        EndBossMission();
    }

    // 보스 미션 종료
    private void EndBossMission()
    {
        Debug.Log("미션 종료");

        bossMissionMgr.bossMissionState = BossMissionManager.BossMissionState.Ready;

        bossMissionMgr.enabled = false;
        bossState = BossState.Idle;

        mon.anim.SetBool("isCasting", false);

        curMissionPatternNum++;

        PuzzleUIManager.Instance.bossHpSlider.gameObject.SetActive(true);

        co_bossPattern = StartCoroutine(BossPatternCoro());
    }


    // 매치 미션 매치 공격
    public void AttackMatchMission(int dirType)
    {
        bossMissionMgr.matchEvent?.Invoke(dirType);

        if (bossMissionMgr.bossMissionState == BossMissionManager.BossMissionState.Clear)
        {
            mon.anim.SetTrigger("TakeDamage");
           // EndBossMission();
        }
    }

    // 무작위 가로 이동
    protected float MoveRadomHorizontalPosition(float speedValue)
    {
        int col = Random.Range(0, PuzzleManager.Instance.totalCol);
        float xValue = GetMovePos(col);

        float moveTime = GetMoveTime(xValue, speedValue);

        BossMoveHorizontal(xValue, moveTime);

        return moveTime;
    }

    // 보스 가로 움직임 연출 //-1 왼쪽 , 1 오른쪽
    protected void BossMoveHorizontal(float moveValue, float moveTime)
    {
        mon.anim.SetBool("isWalk", true);

        int value = 1;

        if (moveValue - transform.position.x < 0)
        {
            value = -1;
        }

        transform.localRotation = Quaternion.Euler(new Vector3(0, 90 * value, 0));

        transform.DOLocalMoveX(moveValue, moveTime).OnComplete(() =>
        {
            mon.anim.SetBool("isWalk", false);
            transform.localRotation = Quaternion.Euler(new Vector3(0, 180, 0));
        });
    }

    // 세로줄 x좌표 가져오기
    protected float GetMovePos(int col)
    {
        float value = -3.5f + 1f * col;

        return value;
    }
    // 움직임 시간 계산
    protected float GetMoveTime(float _moveValue, float speedValue)
    {
        float moveValue = _moveValue;

        if (moveValue < 0) moveValue *= -1;

        float moveTime = moveValue / speedValue;

        return moveTime;
    }

    protected void DangerMaker(DangerLine dangerLine, Vector3 fPos)
    {
        dangerLine.EndPosition = fPos;
        dangerLine.gameObject.SetActive(true);
    }


    // 애니메이션에 맞춰 공격 조절
    protected IEnumerator AttackDelayCoro(string animName,float delayTime, AttackEvent _attackEvent)
    {
        mon.anim.SetTrigger(animName);

        curAttackEvent = _attackEvent;

        yield return YieldCache.WaitForSeconds(delayTime);

        curAttackEvent?.Invoke();

        curAttackEvent = null;

        // isPattern = false;
    }

    // 유저 타겟 투사체공격
    protected virtual void ProjectileAttackToUser(ProjectilePattern pattern)
    {
        mon.anim.SetTrigger("Projectile");

        MonsterProjectile projectile = GetBossProjectileObject(mon.attackPos, pattern);

        projectile.damage = enemyAtkHandler.CalculateAttackDamage();

        projectile.gameObject.SetActive(true);

        projectile.StartMove();

        projectile.LookAtTarget(PlayerManager.Instance.userColTrf.position);

        projectile.transform.DOMove(PlayerManager.Instance.userColTrf.position, 2f).SetEase(Ease.OutQuart);

        SoundManager.Instance.PlaySfx((SoundManager.SfxType)(mon.monStatData.attNum * 2));
    }


}

