using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BlockDefenceAttack;

public class Enemy : MonoBehaviour, IDamage, IDie, ICheckDie // Enemy 최상위 클래스 적 관련 기능 정리
{
    
    public enum EnemyType { Normal, Summon, Msummon ,MiddleBoss , Boss } // 일반 몬스터 / 소환 몬스터(보스소환) /미션 소환  / 중간 보스/보스 몬스터

    public EnemyType type;

    public EnemyAttackHandler enemyAtkHandler; // 공격 관리자
    public BuffHandler buffHandler;  //버프 및 디버프 관리자


    public Rigidbody rigid;
    public Monster mon;

    public bool IsDie { get { return enemyAtkHandler.isDie; } }


    public bool isCheckDie = false; // 죽음 체크 여부

    protected int level;
    protected int monIndex; // 몬스터 도감 번호
    protected int gradeNum;

    public Transform DamagedTrf { get { return mon.damagedPos; } }

    bool isCritical;

    // 적 몬스터 세팅
    virtual public void SetEnemy(int _level ,int _index , GameObject monObj = null)
    {
        if (level == _level) return;

        level = _level;
        monIndex = _index;
        gradeNum = EnemyManager.Instance.GetEnemyGrade(level);

    }

  

    // 데미지를 받는다.
    virtual public void OnDamaged(float _damage , int _attackerAtt, bool isCri = false)
    {
        if (enemyAtkHandler.isDie) return;


        if (enemyAtkHandler.CheckAvoidance() && !PuzzleManager.Instance.isTuto && type == EnemyType.Normal)
        {
            return;
        }

        enemyAtkHandler.OnMonsterDamaged(_damage, _attackerAtt , isCri);

        CheckDie();
       
    }

    // 몬스터 죽음 체크 -> hp UI 갱신
    virtual public void CheckDie()
    {
        enemyAtkHandler.UpdateHpUI();
       //  mon.hpSlider.value = atkStatus.hp;

        if (enemyAtkHandler.isDie && isCheckDie == false)
        {
            isCheckDie = true;
            // mon.hpSlider.value = 0f;
            Die();
        }
    }

  
     // 죽음 처리
    virtual public void Die()
    {
        //isDie = true;
        buffHandler.ResetBuffEffect();
        rigid.velocity = Vector3.zero;


        if(type != EnemyType.Summon && type != EnemyType.Msummon)
        {
            // 이미 클리어한 스테이지면 경험치 반감
            int stgValue = PuzzleManager.Instance.isClearStage ? 5 : 10;

            int exp = level * stgValue * (mon.monStatData.classNum + 1);

            PuzzleManager.Instance.earnExp += exp;
            EarnEnemyGold();
        }

        mon.anim.SetTrigger("Die");
        StartCoroutine(CheckDieCoro(CheckDieAnimation()));
        //  Destroy(gameObject);
    }

   
   public IEnumerator CheckDieCoro(IEnumerator coro = null)
    {
        if (coro != null) { yield return StartCoroutine(coro); }

        EndEnemyDiaAnimation();

        yield return null;
    }

    // 죽음 애니메이션 체크
    IEnumerator CheckDieAnimation()
    {
        while (!mon.anim.GetCurrentAnimatorStateInfo(0).IsName("Die") || mon.anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

    }

    // 몬스터 죽었을 때 죽음 애니메이션 종료 후 체크
    protected virtual void EndEnemyDiaAnimation()
    {
        Debug.Log("게임체크");

        gameObject.SetActive(false);
       
      
    }

    // 움직임 시작 직선으로 내려가는 움직임
    public void LineMoveEnemy()
    {
      //  Debug.Log("움직임");
        mon.anim.SetBool("isWalk", true);

        Vector3 dir = Vector3.back;

#if TEST_MODE

        if(TestModeManager.Instance.curViewType ==2)
        dir = Vector3.right;
#endif

        rigid.velocity = dir * enemyAtkHandler.atkStatus.moveSpeed * 1.5f;


        // StartCoroutine(MoveEnemyCoro());
    }

    // 몬스터 움직임 멈춤
    public void StopEnemy()
    {
        rigid.velocity = Vector3.zero;
        mon.anim.SetBool("isWalk", false);
    }

    // 적 죽음 골드 획득
    void EarnEnemyGold()
    {
        int goldValue = Random.Range(level, level * 3 + 1);

        if(GameManager.Instance !=null)
        GameManager.Instance.UserGold += goldValue;

        EffectManager.Instance.MoveGoldEffect(transform.position);

    }

    // 공격 가능 여부
    public bool CanAttackTarget()
    {
        return !enemyAtkHandler.isDie;
    }


    /// <summary>
    /// 블록 공격 타겟 찾는 메소드 모음
    /// </summary>

    #region
    // 타겟 블록 찾기
    Vector3 blockPosValue = new Vector3(0, 0.5f, 0); //블록 중앙 위치

    public Vector3 FindBlockTargetPosition()
    {
        // target = null;

        List<BoardCell> targetList = GamePlay.Instance.boardGrid.FindAll(o => o.isPlaced == true);

        int cnt = targetList.Count;

        if (cnt == 0)
        {
            return FindRandomDirection();
        }
        else
        {
            int rnd = Random.Range(0, cnt);

            return targetList[rnd].placeBlock.transform.position + blockPosValue;

        }
    }

    // 랜덤 방향 찾기
    public Vector3 FindRandomDirection()
    {
        int totalCount = GamePlay.Instance.boardGrid.Count;

        int rnd = Random.Range(0, totalCount);

        return GamePlay.Instance.boardGrid[rnd].transform.position;
    }



    #endregion


}
