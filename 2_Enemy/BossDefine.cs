using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using BlockDefenceAttack;

/// <summary>
/// 보스 몬스터 관련 공통되게 필요한 클래스 및 인터페이스 
/// </summary>


// 기초 보스 패턴 데이터 -> 전체적인 내용
[Serializable]
public class BaseBossPatternData
{
    public int id;
    public float nAtkTimeInv; // 기본공격 간격 시간
    public float pAtkCntInv; // 강화공격 발동 간격 -> EX) 기본공격 N회마다

    public int firstNpattern; // 첫번째 기본공격 패턴
    public int secondNpattern; // 두번째 기본공격 패턴

    public string fPatternID; // 패턴 데이터ID
    public string sPatternID;

    public string powerPattern; // 강화 공격 타입
    public string pPatternDiff; // 강화 패턴 난이도 조건
    public string pPatternID; // 강화 패턴 데이터ID

    public string mPatternID; //미션 패턴 ID
    public string missionPattern; // 미션 타입
    public string mDiffCondition; // 미션 난이도 조건
    public string mHpCondition; // 미션 시작 체력 기준 조건

}

[Serializable] // 보스 강화패턴 베이스데이터
public class BaseBossPowerPatternData
{
    public int type;
    public int diffLevel;
    public string id;

    public BaseBossPowerPatternData(int _type, int _diffLevel, string _id)
    {
        type = _type;
        diffLevel = _diffLevel;
        id = _id;
    }

}

[Serializable] // 보스 강화패턴 베이스데이터
public class BaseBossMissionPatternData
{
    public int type;
    public int diffLevel;
    public float startHp;
    public string id;

    public BaseBossMissionPatternData(int _type, int _diffLevel, float _startHp, string _id)
    {
        type = _type;
        diffLevel = _diffLevel;
        startHp = _startHp;
        id = _id;
    }

}

// 보스 매치 패턴 기본 데이터
[Serializable]
public class BaseBossMatchPatternData
{
    public string key;

    public string patternID;
    public int diffLevel;

    public float limitTime;
    public int matchCount;
    public string matchType;

    public bool isOrder;
}

// 보스 소환 패턴 베이스 데이터
[Serializable]
public class BaseBossSummonPatternData
{
    public string key;

    public string patternID;
    public int diffLevel;

    public string sMonID;
    public int minCount;
    public int maxCount;

    public int sMonLevelGap; // 보스몬스터 기준 소환 몬스터 레벨 차이

    public float summonTime; // 소환 간격 시간

}


[Serializable]
public class BossMatchPatternData // 매치 미션에서 필요한 정보
{

    public float limitTime; // 제한시간

    public int matchCount;
    public int[] matchType; // 매치 패턴  -1: 랜덤 , 0 가로 세로 둘다 가능,  1: 가로만 2: 세로만
    public bool isOrder = false; // 순서 여부

    public BossMatchPatternData DeepCopy()
    {
        BossMatchPatternData data = new BossMatchPatternData();

        // data.startHp = startHp;
        //data.diffLevel = diffLevel;
        data.isOrder = isOrder;
        data.limitTime = limitTime;
        data.matchType = (int[])matchType.Clone();


        return data;
    }

    // 베이스 데이터를 통한 변환
    public void ConvertMatchPattern(BaseBossMatchPatternData baseData)
    {

        limitTime = baseData.limitTime;
        matchCount = baseData.matchCount;

        isOrder = baseData.isOrder;

        string[] type = baseData.matchType.Split('/');

        if (int.Parse(type[0]) == -1)
        {
            matchType = new int[] { -1 };
        }
        else
        {
            matchType = new int[type.Length];

            for (int i = 0; i < type.Length; i++)
            {
                matchType[i] = int.Parse(type[i]);
            }
        }
    }
}

// 인게임 사용 보스 소환 패턴 데이터 클래스
[Serializable]
public class BossSummonPatternData
{
    public int[] sMonID;
    public int minCount;
    public int maxCount;

    public int sMonLevelGap; // 보스몬스터 기준 소환 몬스터 레벨 차이

    public float summonTime; // 소환 간격 시간

    // 베이스 데이터를 통한 변환
    public void ConvertSummonPattern(BaseBossSummonPatternData baseData, int gradeNum)
    {
        minCount = baseData.minCount;
        maxCount = baseData.maxCount;

        sMonLevelGap = baseData.sMonLevelGap;
        summonTime = baseData.summonTime;

        string[] type = baseData.sMonID.Split('/');

        sMonID = new int[type.Length];

        for (int i = 0; i < type.Length; i++)
        {
            sMonID[i] = int.Parse(type[i]);
        }
        CaculatingByGrade(gradeNum);
    }

    // 등급에 따른 개수 추가
    public void CaculatingByGrade(int gradeNum)
    {
        int gradeValue = (gradeNum + 1) / 2;

        if (gradeValue == 0) return;


        minCount += gradeValue;
        maxCount += gradeValue;

    }

}



[Serializable] // 공통된 패턴 구현에 필요한 정보들
public class BossPatternData
{
    //public int monID;
    public string monType; // 몬스터 계열

    public float intervalTime; // 기본 패턴 간격 시간
    public int powerfulIntervalCount; // 강화 패턴 발동 간격 횟수 -> 기본 패턴 n번 진행 후 진행

    public int missionCount; // 미션 패턴 수

    [Header("ProjectilePattern")]

    public List<ProjectilePattern> projectilePatternData = new List<ProjectilePattern>();

    [Header("ObstaclePattern")]

    public List<BossObstaclePattern> obstaclePatternData = new List<BossObstaclePattern>();


    [Header("SummonPattern")]
    public int[] summonMonID; // 소환 몬스터 종류

    public int minSummonCount = 1; // 잡몹 소환 최소 수
    public int maxSummonCount = 1;// 잡몹 소환 최대 수

    [Header("MatchMissioPattern")]

    public List<BossMatchPatternData> matchMissionData = new List<BossMatchPatternData>();

    public void CaculatingByGrade(int gradeNum)
    {
        int gradeValue = (gradeNum + 1) / 2;

        if (gradeValue == 0) return;



        minSummonCount += gradeValue; // 잡몹 소환 최소 수
        maxSummonCount += gradeValue;// 잡몹 소환 최대 수
    }

    public BossPatternData DeepCopy()
    {
        BossPatternData data = new BossPatternData();

        data.monType = monType;
        data.intervalTime = intervalTime;
        data.powerfulIntervalCount = powerfulIntervalCount;
        data.missionCount = missionCount;

        for (int i = 0; i < projectilePatternData.Count; i++)
        {
            data.projectilePatternData.Add(projectilePatternData[i].DeepCopy());
        }

        for (int i = 0; i < obstaclePatternData.Count; i++)
        {
            data.obstaclePatternData.Add(obstaclePatternData[i].DeepCopy());
        }

        for (int i = 0; i < matchMissionData.Count; i++)
        {
            data.matchMissionData.Add(matchMissionData[i].DeepCopy());
        }

        data.summonMonID = (int[])summonMonID.Clone();

        data.minSummonCount = minSummonCount;
        data.maxSummonCount = maxSummonCount;

        return data;

    }


} // end class




// 방해물 패턴 데이터
[Serializable]
public class BossObstaclePattern
{

    public string patternID; // 패턴 ID
    public int obstacleID = 0; // 방해물 오브젝트ID
    public int obstacleType;

    public float obstacleTime; // 방해시간
    public int obstalceHp; // 체력

    public float obstacleVelocity = 1; // 방해물 속도
    public float obstacleScale = 1;

    public bool isCollide;

    public bool isExplode;
    public float explodeScale;

    public int minObstacleCount; // 최소 방해물 수
    public int maxObstacleCount;// 최대 방해물 수

    public float intervalTime;

    public BossObstaclePattern DeepCopy()
    {
        BossObstaclePattern data = new BossObstaclePattern();

        data.patternID = patternID;
        data.obstacleID = obstacleID;

        data.obstacleType = obstacleType;

        data.obstacleTime = obstacleTime;
        data.obstalceHp = obstalceHp;

        data.obstacleVelocity = obstacleVelocity;
        data.obstacleScale = obstacleScale;
        // data.limitTime = limitTime;

        data.isCollide = isCollide;

        data.isExplode = isExplode;
        data.explodeScale = explodeScale;

        data.minObstacleCount = minObstacleCount;
        data.maxObstacleCount = maxObstacleCount;

        data.intervalTime = intervalTime;

        return data;

    }

    // 베이스 데이터를 통한 변환
    public void ConvertObstaclePattern(BaseObstaclePatternData baseData, int gradeNum)
    {
        //ProjectilePattern data = new ProjectilePattern();

        patternID = baseData.patternID;
        obstacleID = baseData.effectID;
        obstacleType = baseData.obstacleType;

        obstacleTime = baseData.obstacleTime;
        obstalceHp = baseData.obstacleHp;

        obstacleVelocity = baseData.velocity;
        obstacleScale = baseData.scale;

        isCollide = baseData.isCollide;

        isExplode = baseData.isExplode;
        explodeScale = baseData.explodeScale;

        minObstacleCount = baseData.minCount;
        maxObstacleCount = baseData.maxCount;

        intervalTime = baseData.intervalTime;

        CaculatingByGrade(gradeNum);

        //  return data;
    }


    // 등급에 따른 갯수 추가
    public void CaculatingByGrade(int gradeNum)
    {
        int gradeValue = (gradeNum + 1) / 2;

        if (gradeValue == 0) return;

        minObstacleCount += gradeValue;
        maxObstacleCount += gradeValue;
    }
}

[Serializable]
public class MeleeAttackPatternData //근접공격 패턴 데이터 클래스
{
    public string key;

    public float detectSpeed;
    public float detectDelay;
    public float detectRange;

    public float chaseSpeed;
    public float chaseDistance;

    public string pAtkCntInv;

    public float patternTime;

    public float buffRate;
    public float buffTime;

}


public interface IBossAttack
{
    float BasicPatternAnimTime { get; set; }

}

// 타겟 원거리 기본 공격
public interface IBossTargetingAttack
{
    void BossTargetingAttack();
}

// 논타겟 원거리 기본 공격
public interface IBossNonTargetingAttack
{
    void BossNonTargetingAttack();
}

public interface IBossShieldMode
{
    Slider ShieldSlider { get; set; }

    void SetShieldMode(); // 최초 세팅

    void DamagedShield(float damage); // 실드 피해

    void DestroyShield(); //보호막 파괴 처리

    void FailShieldMode(); // 실패 처리

    IEnumerator StartShieldModeCoro();
}
