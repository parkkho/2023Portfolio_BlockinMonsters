using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using System;



public class BlockAttackHandler : MonoBehaviour // 블록 공격 기능
{
    [SerializeField]
    Block block;
    [SerializeField]
    BoxCollider rangeCol; // 사정거리 콜라이더

    [SerializeField]
    GameObject normalProjectile; // 기본 공격 투사체
    [SerializeField]
    GameObject skillProjectile; // 스킬 공격 투사체

    [SerializeField]
    List<NormalEnemy> atkTargetList = new List<NormalEnemy>(); // 현재 공격 타겟 리스트

    [SerializeField]
    List<NormalEnemy> targetList = new List<NormalEnemy>(); // 사거리 내 공격 가능한 적 리스트

    int att; // 속성

    int maxAtkTargetCnt; // 최대 공격 가능 한 적 수

    private IObjectPool<BlockMissile> _Pool;

    private void Awake()
    {
        _Pool = new ObjectPool<BlockMissile>(CreateMissile, OnGetMissle, OnReleaseMissile, OnDestroyMissile);
    }

    private BlockMissile CreateMissile()
    {
        BlockMissile bm = Instantiate(normalProjectile, transform).GetComponent<BlockMissile>();
        bm.SetManagedPool(_Pool);

        return bm;

    }

    private void OnGetMissle(BlockMissile bm)
    {
        bm.transform.position = new Vector3(block.transform.position.x, 2f, block.transform.position.z);
        bm.gameObject.SetActive(true);


    }

    // 풀에 돌려 줄때
    private void OnReleaseMissile(BlockMissile bm)
    {
        bm.gameObject.SetActive(false);

    }

    private void OnDestroyMissile(BlockMissile bm)
    {
        Destroy(bm.gameObject);
    }

    // 최초 공격 준비 => 사거리만큼 콜라이더 크기 조절
    public void ReadyAttack()
    {
        float colX = 0f;

        if (block.defaultStatus.detectionLevel == 4)
        {
            colX = 14f;
        }
        else
        {
            colX = (block.defaultStatus.detectionLevel - 1) * 2f;
           
        }
       
        rangeCol.size = new Vector3(0.8f + colX, 1f, block.defaultStatus.atkRange * 2.0f);
        rangeCol.enabled = true;

        att = block.defaultStatus.attNum;
        maxAtkTargetCnt = block.defaultStatus.atkTargetCnt;
    }

    // 블록 파괴 시 블록 공격 상태 초기화
    public void ResetBlockAttack()
    {
        atkTargetList.Clear();
        targetList.Clear();
        rangeCol.enabled = false;
    }

    // 사거리 적 충돌
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            NormalEnemy enemy = other.transform.GetComponent<NormalEnemy>();

            if (!enemy.enemyAtkHandler.isDie)
                targetList.Add(enemy);

            // Debug.Log("충돌");
            if (block.blkState != Block.BlockState.Attack)
            { StartAttack(); }
        }
    }

    // 공격 시작
    void StartAttack()
    {

        StartCoroutine(AttackCoro());
    }

    // 공격 멈춤
    public void StopAttack()
    {
        block.blkState = Block.BlockState.Place;

        StopCoroutine(AttackCoro());
    }

    // 공격
    public void Attack()
    {
        if (atkTargetList.Count ==0)
        {
            return;
        }

        bool isCritical = CheckCritical();

        
        float damage = block.atkStatus.atkDamage * (1 + 0.1f * PuzzleManager.Instance.MatchCount);

        if (isCritical) damage += block.atkStatus.atkDamage * CONSTANTS.criticalDamage;

        for ( int i=0; i< atkTargetList.Count; i++)
        {
            BlockMissile bm = _Pool.Get();

            bm.SetMissleInfo(damage, att, isCritical,atkTargetList[i]);
      
        }
   
    }

    //크리티컬 체크
    public bool CheckCritical()
    {
        float rnd = UnityEngine.Random.Range(0, 1000f);

        if (rnd <= block.atkStatus.critical * 10f)
        {           
            return true;
        }

        return false;
    }

    // 공격 코루틴
    IEnumerator AttackCoro()
    {

        block.blkState = Block.BlockState.Attack;

        float atkDelay = (float)Math.Truncate((BlockStatusValue.baseAtkDelay / block.defaultStatus.atkSpeed) * 10) / 10;

        yield return YieldCache.WaitForSeconds(0.2f);
    
        SearchTarget();

        while (true)
        {
           // SearchTarget();
            // 공격 대상이 없으면
            if (atkTargetList.Count==0)
            {
                StopAttack();
                yield break;
            }

            Attack();

            float time = 0f;

            while (time < atkDelay)
            {
               
                time += Time.deltaTime;

                yield return null;

            }

            CheckAttackTarget();

        }

    }

    // 공격 타겟 찾기 => 사거리 내 적 몬스터 중 우선순위
    void SearchTarget()
    {
        // targetArr.Clea 

        // 공격 가능 타겟 후보가 있을 때
        if (targetList.Count > 0)
        {
            // 공격 불가 적 (죽은 적) 제거
            targetList.RemoveAll(o => o.IsDie == true);
        }

        if (targetList.Count == 0)
        {
            return;
        }


        if (targetList.Count > 0)
        {
            int atkTargetCount = atkTargetList.Count; // 이미 공격 타겟 수

            int cnt = Math.Min(maxAtkTargetCnt - atkTargetCount, targetList.Count); // 공격 가능 최대 수와 타겟 리스트 수 중 작은 수

            // 공격 타겟 리스트에 추가 후 타겟 리스트에서는 제거
            for(int i=0; i < cnt; i++)
            {
                atkTargetList.Add(targetList[0]);
                targetList.RemoveAt(0);
            }
        }

    }

    // 공격 타겟 리스트 체크
    void CheckAttackTarget()
    {
        if (atkTargetList.Count > 0)
        {
            // 공격 불가 적 (죽은 적) 제거
            atkTargetList.RemoveAll(o => o.IsDie == true || o.gameObject.activeSelf == false);

            // 최대 공격 수보다 적다면 추가 공격 타겟 찾는다.
            if(atkTargetList.Count < maxAtkTargetCnt)
            {
                SearchTarget();
            }


        }
    }

}
