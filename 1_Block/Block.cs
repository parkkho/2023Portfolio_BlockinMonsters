using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BlockDefenceAttack;

public class Block : MonoBehaviour , IDamage , ICheckDie
{

    public enum BlockState { Idle, Place, Attack, Match} // 기본 상태 / 블록판 배치 상태/ 공격 하는 중 // 매치된 상태
    
    public BlockStatusData defaultStatus; // 기본 능력치

    public DefenceAttackStatus atkStatus; // 전투 능력치


    public BlockState blkState = BlockState.Idle;// = BlockState.Idle;

    public int rowID; // 행 번호 (Y축)
    public int colID; // 열 번호 (X축)

    public bool isDie = false;
    public bool isCheckDie = false;

    public Rigidbody rigid; // 리지드바디
    public BoxCollider blockCol; // 콜라이더
    public BlockAttackHandler atkHandler; // 공격 기능
    public BuffHandler buffHandler; // 버프 관리
  


    [SerializeField]
    ParticleSystem damagedEffect;

    [SerializeField]
    ParticleSystem destroyEffect;

    public GameObject blockObj;
    public Transform damagedPos; // 피격 위치

    public GameObject hpCanvas;
    public Slider hpSlider;

    bool isAttacked = false; // 공격받고있는 여부

    float checkTime = 0f;
    // BoxCollider col;

    [SerializeField] List<DebuffData> blockDebuffList = new List<DebuffData>();

    //피격 위치
    public Transform DamagedTrf { get { return damagedPos; }  }

    public void SetBlockData(int level , BlockStatusData _defalutData)
    {
        defaultStatus = _defalutData.DeepCopy();

        atkStatus = new DefenceAttackStatus(level , defaultStatus.atk, defaultStatus.def,defaultStatus.hp, defaultStatus.mp, defaultStatus.critical, defaultStatus.avoidance , defaultStatus.atkSpeed , 0f);

        hpSlider.maxValue = defaultStatus.hp;
        hpSlider.value = defaultStatus.hp;

        
    }

    // 블록 풀 반환
    public void ReturnBlock()
    {
        int att = defaultStatus.attNum;

        ResetBlock();

        BlockSpawnManager.Instance.ReturnBlock(att, this);
    }


    // 블록 파괴 시 초기화
    private void ResetBlock()
    {
        atkHandler.ResetBlockAttack();
        buffHandler.ResetBuffEffect();
        hpCanvas.SetActive(false);
        blockCol.enabled = false;
        defaultStatus = null;
        atkStatus = null;
        isDie = false;
        isCheckDie = false;
        isAttacked = false;
        checkTime = 0f;
        blkState = BlockState.Idle;
    }

    // 블록을 두면 콜라이더 활성
    public void PlacedBlock()
    {
        blkState = BlockState.Place;

        blockCol.enabled = true;

        if (atkHandler != null)
        atkHandler.ReadyAttack();
    }

    //블록 매치 상태로 변경
    public void MatchBlock()
    {
        blkState = BlockState.Match;
        blockCol.enabled = false;
    }

    // 적 블록이 되었을 때
    public void SetEnemyBlock()
    {
        if (blkState == BlockState.Attack) atkHandler.StopAttack();

        blkState = BlockState.Idle;
        blockCol.enabled = false;

        atkHandler.ResetBlockAttack();
       
    }
  
    // 회피 체크
    bool CheckAvoidance()
    {
        float rnd = Random.Range(0, 1000);

        if(rnd <= atkStatus.avoidance * 10f)
        {
            Debug.Log("블록 회피 성공");
            return true;
        }

        return false;
    }

    // 블록  HpUI 활성 여부 확인
    void CheckActiveBlockHpUI()
    {
        // 공격 받고있는 상태가 되면 hp바 ui 활성
        if (!isAttacked)
        {
            isAttacked = true;
            hpCanvas.SetActive(true);
            StartCoroutine(CheckDamaged());
        }
        else
        {
            checkTime = 0f;
        }


    }

    // 공격가능 여부
    public bool CanAttackTarget()
    {
        if (blkState == BlockState.Idle || isDie == true) return false;

        return true;
    }


    // 블록 데미지 받을 때 -> 즉시 죽음 체크여부
    public void OnDamaged(float _damage , int _attackerAtt, bool isCri = false/* ,bool _isCheckDie*/)
    {
        if (blkState == BlockState.Match) return;

        // 회피 성공 시 리턴
        if (CheckAvoidance()) return;    
      
        CheckActiveBlockHpUI();

        float damage = _damage * atkStatus.defAverage * PuzzleManager.Instance.CheckSynastry(_attackerAtt, defaultStatus.attNum);

        if(damage <= 0)
        {
            damage = 1.0f;
        }

        if (atkStatus.hp > 0f)
        {
            atkStatus.hp -= damage;

            hpSlider.value = atkStatus.hp;

            damagedEffect.Play();

            if(atkStatus.hp <= 0f && isDie == false)
            {
                atkStatus.hp = 0;
                hpSlider.value = 0f;
                isDie = true;
                //if(_isCheckDie)
               // { 
                    CheckDie();
               // }

            }
        }
    }

    //회피 불가능한 공격
    public void OnTrueDamaged(float _damage, int _attackerAtt)
    {
        if (blkState == BlockState.Match) return;

      
        CheckActiveBlockHpUI();

        float damage = _damage * atkStatus.defAverage * PuzzleManager.Instance.CheckSynastry(_attackerAtt, defaultStatus.attNum);

        if (damage <= 0)
        {
            damage = 1.0f;
        }

        if (atkStatus.hp > 0f)
        {
            atkStatus.hp -= damage;

            hpSlider.value = atkStatus.hp;

            damagedEffect.Play();

            if (atkStatus.hp <= 0f && isDie == false)
            {
                atkStatus.hp = 0;
                isDie = true;

               CheckDie();

            }
        }

    }



    // 죽음 체크
    public void CheckDie()
    {
        if (isDie && isCheckDie == false)
        {
            isCheckDie = true;
            // mon.hpSlider.value = 0f;
            DestroyBlock();
        }
    }

    // 블록 초기 상태로 변환 -> 전장 이탈할때
    public void BreakBlock()
    {
        atkStatus.hp = 0f;
        hpSlider.value = 0f;
        blockCol.enabled = false;

        PuzzleManager.Instance.boardCells[rowID, colID].ClearCell();

        if (blkState == BlockState.Attack)
        {
            atkHandler.StopAttack();
        }
    }

    // 블록 파괴
    public void DestroyBlock()
    {
        // isDie = true;

        BreakBlock();

        blockObj.SetActive(false);
        // gameObject.SetActive(false);

        StartCoroutine(DestroyCoro());
    }


    IEnumerator DestroyCoro()
    {

        destroyEffect.gameObject.SetActive(true);
        destroyEffect.Play();

        yield return new WaitForSeconds(0.5f);

        destroyEffect.gameObject.SetActive(false);
        SoundManager.Instance.PlaySfx(SoundManager.SfxType.BlockDestroy);
        //Destroy(gameObject);
        ReturnBlock();
    }

    

    // 블록이 피해를 계속 입고있는 상태인지 체크 => 2초이상 피해를 받지않을 경우 hp바 비활성
    IEnumerator CheckDamaged()
    {
        // float time = 0f;

        while (checkTime <= 2.0f)
        {
            checkTime += Time.deltaTime;

            yield return null;
        }

        isAttacked = false;
        hpCanvas.SetActive(false);
        checkTime = 0f;
    }

    // 디버프 추가
    public void AddDebuff(DebuffData data)
    {
        blockDebuffList.Add(data);
    }
    // 디버프 제거
    public void RemoveDebuff()
    {
        
    }

   
}
