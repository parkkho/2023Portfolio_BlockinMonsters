using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BlockMissile : MonoBehaviour // 블록 공격 투사체
{

    public Collider col;
    public Rigidbody rigid;
    public ParticleSystem missileEff;
    public ParticleSystem explodeEff;


    public float turn;
    public float velocity;

    public float damage = 0f; // 데미지

    public bool isCritical = false;

    public Enemy target; // 타겟
    public Transform targetTrf; // 타겟 트랜스폼

    public int att; // 속성
  
    float time =  0f;

    bool isMove = false; // 움직임 여부

    private IObjectPool<BlockMissile> _ManagedPool;

 
    public void SetMissleInfo(float _damage , int _att , bool _isCri , Enemy _target)
    {

        if(_target == null)
        {

            gameObject.SetActive(false);
            DestroyMissile();

            return;
        } 

        damage = _damage;
        att = _att;

        isCritical = _isCri;

        target = _target;
        targetTrf = target.mon.transform;

        MoveMissile();
    }

    public void MoveMissile()
    {
        time = 0f;
        transform.rotation = Quaternion.Euler(-90f, 0f, 0);
        rigid.velocity = transform.forward * velocity*0.5f;
        missileEff.gameObject.SetActive(true);
        missileEff.Play();

        SoundManager.Instance.PlaySfx((SoundManager.SfxType)(att * 2));
        Debug.Log("블록 미사일 공격");
        isMove = true;
    }

    public void FixedUpdate()
    {

        if (isMove)
        {
            time += Time.deltaTime;

            if (targetTrf != null && time > 0.1f)
            {
                
                rigid.velocity = transform.forward * velocity;
               
                var targetRotaion = Quaternion.LookRotation(target.mon.damagedPos.position - transform.position);
                rigid.MoveRotation(Quaternion.RotateTowards(transform.rotation, targetRotaion, turn));
            }
           
            if(target.gameObject.activeInHierarchy == false)
            {

                isMove = false;

                DestroyMissile();
            }
        }
      
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (targetTrf != null)
            {
                if (other.gameObject == targetTrf.gameObject) 
                {

                    isMove = false;
                   // targetTrf = null;
                    rigid.velocity = Vector3.zero;
                   
                    missileEff.gameObject.SetActive(false);
                    StartCoroutine(ExplodeCoro());

                    SoundManager.Instance.PlaySfx((SoundManager.SfxType)(att * 2 + 1));

                    target.OnDamaged(damage, att , isCritical/*, true*/);
          
                }
            }
        }
    }

    // 폭발 코루틴
    IEnumerator ExplodeCoro()
    {
        col.enabled = false;
        explodeEff.gameObject.SetActive(true);
        explodeEff.Play();

        yield return new WaitForSeconds(1.0f);

        explodeEff.gameObject.SetActive(false);
        col.enabled = true;
        DestroyMissile();
    }

    public void SetManagedPool(IObjectPool<BlockMissile> pool)
    {
        _ManagedPool = pool;
    }

    public void DestroyMissile()
    {
        target = null;
        targetTrf = null;

        _ManagedPool.Release(this);
    }
}
