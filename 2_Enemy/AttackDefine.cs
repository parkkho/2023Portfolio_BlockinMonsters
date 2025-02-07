using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;


// 전투 관련 공통되게 필요한 클래스 모음
namespace BlockDefenceAttack 
{

    // 투사체 패턴 관련 베이스 데이터 
    [Serializable]
    public class BaseProjectilePatternData
    {
        public string patternID;

        public int projectileType;
        public int targetType;
        public int moveType;

        public float velocity;
        public float scale;

        public float damageRate;

        public bool isMultiShot;
        public int minCount;
        public int maxCount;

        public bool isExplode;
        public float explodeScale;
        public float exDamageRate;

        public int effectID;

    }

    // 방해물 패턴 관련 베이스 데이터 
    [Serializable]
    public class BaseObstaclePatternData
    {
        public string patternID;

        public int obstacleType;
        public float obstacleTime;
        public int obstacleHp;

        public float velocity;
        public float scale;

        public int minCount;
        public int maxCount;

        public bool isCollide;

        public bool isExplode;
        public float explodeScale;

        public float intervalTime;

        public int effectID;

    }

    // 투사체 패턴 데이터 - 인게임사용
    [Serializable]
    public class ProjectilePattern
    {
        public string patternID; // 패턴 번호

        public int effectID = 0; // 이펙트 ID

        public int projectileType = 0; // 기본 or 관통
        public int targetType = 0; //0: 타겟팅 1: 논타겟팅(방향) 2: 라인 3: 후방 우선(5~8행)
        public int moveType;

        public float projectileVelocity = 1; // 공격 투사체 속도
        public float projectileScale = 1;

        public int minProjectileCount; // 투사체 최소 수
        public int maxProjectileCount; // 투사체 최대 수


        public bool isExplode; // 광역 폭발 여부
        public float explodeScale; //폭발 범위

        public float damageRate; //데미지 비율

        public float exDamageRate; // 폭발 데미지 비율

        public ProjectilePattern DeepCopy()
        {
            ProjectilePattern data = new ProjectilePattern();

            data.patternID = patternID;
            data.effectID = effectID;

            data.projectileType = projectileType;
            data.targetType = targetType;
            data.moveType = moveType;

            data.projectileVelocity = projectileVelocity;
            data.projectileScale = projectileScale;

            data.isExplode = isExplode;
            data.explodeScale = explodeScale;

            data.minProjectileCount = minProjectileCount;
            data.maxProjectileCount = maxProjectileCount;

            data.damageRate = damageRate;
            data.exDamageRate = exDamageRate;

            return data;

        }

        // 베이스 데이터를 통한 변환
        public void ConvertProjectilePattern(BaseProjectilePatternData baseData, int gradeNum)
        {
            //ProjectilePattern data = new ProjectilePattern();

            patternID = baseData.patternID;

            effectID = baseData.effectID;

            projectileType = baseData.projectileType;
            targetType = baseData.targetType;
            moveType = baseData.moveType;

            projectileVelocity = baseData.velocity;
            projectileScale = baseData.scale;

            isExplode = baseData.isExplode;
            explodeScale = baseData.explodeScale;

            damageRate = baseData.damageRate;
            exDamageRate = baseData.exDamageRate;

            minProjectileCount = baseData.minCount;
            maxProjectileCount = baseData.maxCount;

            CaculatingByGrade(gradeNum);

            //  return data;
        }

        // 등급에 따른 개수 추가
        public void CaculatingByGrade(int gradeNum)
        {
            int gradeValue = (gradeNum + 1) / 2;

            if (gradeValue == 0) return;


            minProjectileCount += gradeValue;
            maxProjectileCount += gradeValue;

        }

    }

    // 도트 데미지 데이터
    [Serializable]
    public class DamageOverTimeData
    {
        public float totalTime; // 데미지 주는 총 시간
        public float intervalTime; // 데미지 기준 간격 시간
        public float damageRate; // 데미지 비율 EX) 몬스터 데미지 * damageRate(0.0~1.0)
    }

    [Serializable]
    public class DebuffData // 디버프 관련 데이터
    {
        public int type; // 디버프 타입
        public EffectPool debuffEffect = null;

        public void EndDebuff()
        {
            if (debuffEffect != null) debuffEffect.ReturnEffect();
        }
    }

    public class ProjectileAttack : MonoBehaviour // 투사체 공격
    {
        // 일반 / 관통형

        // public Collider col; // 발사 충돌체 콜라이더
        public Rigidbody rigid;
        public ParticleSystem attackEff;
        public ParticleSystem explodeEff;

        // public GameObject effectObj;

        public GameObject projectileObj = null;
        public GameObject explodeObj = null;

        public float velocity;
        public float damage = 0f; // 데미지
        public float damageRate;
        public float explodeDamageRate;
        public int att; //속성

        public bool isMove = false; //움직임 여부 -> 멈출때와 비교해서 
        public bool isExplode = false; // 폭발 공격(광역) 여부
                                       //  protected Vector3 targetPos;

        // 이펙트 오브젝트 세팅
        public void SetEffect(GameObject _projectileObj, GameObject _explosionObj)
        {
            projectileObj = _projectileObj;

            Transform pTrf = projectileObj.transform;

            pTrf.SetParent(transform);
            pTrf.SetSiblingIndex(0);
            pTrf.localPosition = Vector3.zero;
            pTrf.localEulerAngles = Vector3.zero;

            Transform hitTrf = _explosionObj.transform;

            hitTrf.SetParent(explodeObj.transform);
            hitTrf.localPosition = Vector3.zero;
            hitTrf.localEulerAngles = Vector3.zero;

            attackEff = pTrf.GetComponent<ParticleSystem>();
            explodeEff = hitTrf.GetComponent<ParticleSystem>();

        }

        // 기본 정보 세팅
        public void SetProjectile(float _velocity, float _scale, bool _isExplode, int _att, float _damageRate, float _exDamageRate)
        {
            velocity = _velocity;
            // damage = _damage;
            att = _att;

            isExplode = _isExplode;

            damageRate = _damageRate;

            explodeDamageRate = _exDamageRate;

            if (_scale > 1f && projectileObj != null)
            {
                projectileObj.transform.localScale = Vector3.one * _scale;

            }
        }

        //타겟 바라보게
        public void LookAtTarget(Vector3 targetPos)
        {

            Vector3 targetDir = targetPos - transform.position;
            transform.rotation = Quaternion.LookRotation(targetDir);
        }

        // 직선 이동
        public void LineMove()
        {
            rigid.velocity = transform.forward * velocity;
        }


        // 타겟을 향한 직선 이동
        public void TargetStraightMove(Vector3 targetPos)
        {
            //1) 타겟 방향 바라보게
            LookAtTarget(targetPos);

            transform.DOMove(targetPos, velocity).SetSpeedBased();
        }

        public void BezierMove(Vector3 targetPos, float yValue, float time)
        {

            Vector3 firstPos = transform.position;

            Vector3 dir = targetPos - firstPos;

            dir.y = 0;

            Vector3 normalDir = dir.normalized;

            float mag = dir.magnitude * 0.25f;


            Vector3 secondPos = new Vector3((targetPos.x + firstPos.x) * 0.5f, firstPos.y + yValue, (targetPos.z + firstPos.z) * 0.5f);

            transform.DOPath(new[] { secondPos, firstPos, secondPos - normalDir * mag, targetPos, secondPos + normalDir * mag, targetPos }, time, PathType.CubicBezier);
        }

        public IEnumerator BezierMoveCoro(Vector3 targetPos, float yValue, float time)
        {
            // bool isRot = true;

            float movetime = 0f;

            BezierMove(targetPos, yValue, time);

            Vector3 prePos = transform.position;


            while (movetime < time && isMove)
            {
                yield return new WaitForEndOfFrame();

                movetime += Time.deltaTime;
  
                Vector3 curPos = transform.position;            

                Vector3 targetDir = curPos - prePos;

                if (targetDir != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(targetDir);

                prePos = curPos;
               
            }



        }

    }

    // 속성 관련 공격
    public class AttributeAttack// : MonoBehaviour
    {

        // 지속 데미지 속성 공격
        public static IEnumerator DoTDamageAttack(float totalTime, float attackTime, float damage, int att, int effectID, List<GameObject> targetList)
        {

            List<IDamage> damagedTargetList = new List<IDamage>();
            List<BuffHandler> buffHandlerList = new List<BuffHandler>();

            List<EffectPool> effectList = new List<EffectPool>();

           
            for (int i = 0; i < targetList.Count; i++)
            {
                IDamage damaged = targetList[i].GetComponent<IDamage>();
                BuffHandler buffHandler = targetList[i].GetComponent<BuffHandler>();

                damagedTargetList.Add(damaged);
                buffHandlerList.Add(buffHandler);

                // 이펙트 생성
                GameObject dotEffect = EffectManager.Instance.GetDefenceEffect(effectID);
                EffectPool effect = dotEffect.GetComponent<EffectPool>();

                dotEffect.transform.position = damagedTargetList[i].DamagedTrf.position;// + iPos;
                dotEffect.transform.SetParent(damagedTargetList[i].DamagedTrf);

                effect.PlayEffect();

                buffHandlerList[i].AddBuffEffect(effect);
                effectList.Add(effect);
            }



            float checkTime = 0;

            float checkDmgTime = 0;

            while (checkTime < totalTime)
            {
                yield return YieldCache.WaitForSeconds(0.1f);

                checkTime += 0.1f;
                checkDmgTime += 0.1f;


                for (int i = damagedTargetList.Count - 1; i >= 0; i--)
                {
                    // 공격불가 이펙트
                    if (damagedTargetList[i].CanAttackTarget() == false)
                    {
                        // effectList[i].ReturnEffect();
                        // buffHandlerList[i].RemoveBuffEffect(effectList[i]);
                        effectList.RemoveAt(i);
                        damagedTargetList.RemoveAt(i);
                        buffHandlerList.RemoveAt(i);


                    }
                }


                if (checkDmgTime >= attackTime)
                {
                    checkDmgTime -= attackTime;

                    foreach (IDamage target in damagedTargetList)
                    {
                        target.OnDamaged(damage, att/*, true*/);
                    }

                }


            }

            for (int i = effectList.Count - 1; i >= 0; i--)
            {
                effectList[i].ReturnEffect();
                buffHandlerList[i].RemoveBuffEffect(effectList[i]);

                effectList.RemoveAt(i);
                //  damagedTargeList.RemoveAt(i);
                buffHandlerList.RemoveAt(i);
            }

        }
    }

    // 여러 공격 패턴에 대한 정리
    public class AttackPattern
    {
        // 지속 데미지 속성 공격
        public static IEnumerator DoTDamageAttack(float totalTime, float attackTime, float damage, int att/*, int effectID*/, List<IDamage> targetList)
        {

            float checkTime = 0;

            float checkDmgTime = 0;

            while (checkTime < totalTime)
            {
                yield return YieldCache.WaitForSeconds(0.1f);

                checkTime += 0.1f;
                checkDmgTime += 0.1f;



                if (checkDmgTime >= attackTime)
                {

                    checkDmgTime -= attackTime;


                    for (int i = targetList.Count - 1; i >= 0; i--)
                    {
                        // 공격불가 이펙트
                        if (targetList[i].CanAttackTarget() == false)
                        {                     
                            targetList.RemoveAt(i);                          
                        }
                    }


                    foreach (IDamage target in targetList)
                    {
                        target.OnDamaged(damage, att/*, true*/);
                    }

                }


            }

        }
    }


    public interface IDamage
    {

        Transform DamagedTrf { get; /*set;*/ }

        // 데미지, 공격자 속성, 즉시 죽음 체크 여부
        void OnDamaged(float damage, int attackerAtt , bool isCri = false/* , bool isCheckDie*/);

        // 공격가능여부
        bool CanAttackTarget();
    }

    // 죽음 체크
    public interface ICheckDie
    {
        void CheckDie();
    }

    public interface IDie
    {
        void Die();
    }

    public interface ISkill
    {
        void SkillAttack();
    }

    public interface IMonsterLayer // 피아 식별 layer 변경
    {
        void SetMonsterLayer(bool isFriend);
    }
}
