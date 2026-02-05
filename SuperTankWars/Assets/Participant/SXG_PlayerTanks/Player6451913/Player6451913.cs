using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SXG2025;

namespace Player6451913
{
	public class Player6451913 : ComPlayerBase
    {
        enum Prog
        {
            None,
            RandomMove,     // ランダム移動 
            Finish,
        }
        private Prog m_prog = Prog.None;

        private int m_shooterNo = 0;

        [SerializeField] private float m_patrolDistance = 20.0f;    // 巡回の距離(半径)

        [SerializeField] private float m_shootableTiltAngleOfBody = 20.0f;  // 射撃可能な本体の姿勢角度 

        [SerializeField] private float safeDistance = 10.0f;        //敵との距離を保持

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            SetProg(Prog.RandomMove);
        }

        private void Update()
        {
            switch (m_prog)
            {
                case Prog.RandomMove:
                    StartCoroutine(CoMoveToRandomPosition());
                    break;
                case Prog.Finish:
                    StopAllCoroutines();
                    break;
            }
            m_prog = Prog.None;
        }


        private void SetProg(Prog newProg)
        {
            m_prog = newProg;
        }

        /// ランダムな位置へ移動 
        private IEnumerator CoMoveToRandomPosition()
        {
            float COS10 = Mathf.Cos(10.0f * Mathf.PI / 180.0f);
            float COS45 = Mathf.Cos(45.0f * Mathf.PI / 180.0f);
            float time = 0f;

            Vector3 tankPosition;
            Quaternion tankRotation;
            float shootableAngle = Mathf.Cos(m_shootableTiltAngleOfBody * Mathf.PI / 180.0f);

            // 現在座標と角度を取得  
            SXG_GetPositionAndRotation(out tankPosition, out tankRotation);

            // 45度の座標を決定
            Vector3 tankPoint = tankPosition;
            tankPoint.y = 0;
            Vector3 tankDir = tankPoint.normalized;
            Vector3 positionToMove = Quaternion.AngleAxis(45.0f, Vector3.up) * (tankDir * m_patrolDistance);

            // １回の行動のランダムなタイムリミット 
            float timeLimit = Random.Range(3.0f, 10.0f);

            // 発砲タイマー 
            float shootTimer = Random.Range(0.2f, 0.5f);

            // 移動できているかチェック 
            Vector3 lastPosition = tankPosition;

            // 攻撃対象をランダム選択 
            TankInfo[] allTanksInfo = SXG_GetAllTanksInfo();
            List<int> attackTargetCandidates = new List<int>();
            for (int i = 1; i < GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE; ++i)
            {
                if (!allTanksInfo[i].IsDefeated)
                {
                    attackTargetCandidates.Add(i);  // 敗退していないチームをリストアップ 
                }
            }
            int targetTeamNo = 0;
            if (0 < attackTargetCandidates.Count)
            {
                targetTeamNo = attackTargetCandidates[Random.Range(0, attackTargetCandidates.Count)];
       
            }
            else
            {
                SetProg(Prog.Finish);
                yield break;
            }

            // 敵との距離を保つ
            int closestEnemy = -1;
            float minDistance = float.MaxValue;

            while (time < timeLimit)
            {
                for (int i = 1; i < GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE; ++i)
                {
                    if (!allTanksInfo[i].IsDefeated)
                    {
                        float distance = Vector3.Distance(tankPosition, allTanksInfo[i].Position);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestEnemy = i;
                        }
                    }
                }

                Vector3 direction = positionToMove - tankPosition;
                direction.y = 0;
                direction.Normalize();

                // 敵が近い場合の後退
                if (closestEnemy != -1 && minDistance < safeDistance)
                {
                    Vector3 awayFromEnemy = tankPosition - allTanksInfo[closestEnemy].Position;
                    awayFromEnemy.y = 0;
                    direction = awayFromEnemy.normalized;
                }

                // タイムリミットまで目標座標を目指して移動する 
                Vector3 tankFront = tankRotation * Vector3.forward;
                tankFront.y = 0;
                Vector3 tankFrontNormalized = tankFront.normalized;

                float leftTorque = 0;
                float rightTorque = 0;
                float dirDot = Vector3.Dot(direction, tankFrontNormalized);

                if (COS10 <= dirDot)
                {
                    leftTorque = 1;
                    rightTorque = 1;
                }
                else if (dirDot <= -COS10)
                {
                    leftTorque = -1;
                    rightTorque = -1;
                }
                else
                {
                    Vector3 dirCross = Vector3.Cross(direction, tankFrontNormalized);
                    if (COS45 <= dirDot)
                    {
                        leftTorque = Mathf.Clamp01(1.0f + dirCross.y * 0.5f);
                        rightTorque = Mathf.Clamp01(1.0f - dirCross.y * 0.5f);
                    }
                    else
                    {
                        if (dirCross.y < 0)
                        {
                            leftTorque = 1;
                            rightTorque = -1;
                        }
                        else
                        {
                            leftTorque = -1;
                            rightTorque = 1;
                        }
                    }
                }

                SXG_SetCaterpillarPower(leftTorque, rightTorque);

                // 射撃
                shootTimer -= Time.deltaTime;
                if (shootTimer <= 0)
                {
                    bool hasShot = false;
                    int turretCount = GetCountOfTurrets;

                    for (int i = 0; i < turretCount; i++)
                    {
                        int turretNo = m_shooterNo % turretCount;

                        if (SXG_CanShoot(turretNo) && shootableAngle <= transform.up.y)
                        {
                            SXG_Shoot(turretNo);
                            m_shooterNo++; // 次の砲塔へ
                            shootTimer = Random.Range(0.2f, 0.5f);
                            hasShot = true;
                            break;
                        }
                        else
                        {
                            // 撃てなかったら次の砲塔を試す
                            m_shooterNo++;
                        }
                    }

                    // どの砲塔も撃てなかった場合、少しだけ待って再試行
                    if (!hasShot)
                    {
                        shootTimer = 0.1f;
                    }
                }

                // 常に攻撃目標に砲塔を向ける 
                allTanksInfo = SXG_GetAllTanksInfo();
                Vector3 targetPosition = allTanksInfo[targetTeamNo].Position;
                for (int i = 0; i < GetCountOfTurrets; ++i)
                {
                    SXG_RotateTurretToImpactPoint(i, targetPosition);

                    // 攻撃対象が敗退していたら行動中断 
                    if (allTanksInfo[targetTeamNo].IsDefeated)
                    {
                        break;
                    }
                }


                // 時間経過 
                time += Time.deltaTime;
                yield return null;
            }

            // 次の行動 
            SetProg(Prog.RandomMove);
        }
    }
}