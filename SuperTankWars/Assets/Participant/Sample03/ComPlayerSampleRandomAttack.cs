using UnityEngine;
using SXG2025;
using System.Collections;
using System.Collections.Generic;

namespace SXG2025
{

    public class ComPlayerSampleRandomAttack : ComPlayerBase
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

        /// <summary>
        /// ランダムな位置へ移動 
        /// </summary>
        /// <returns></returns>
        private IEnumerator CoMoveToRandomPosition()
        {
            float COS10 = Mathf.Cos(10.0f * Mathf.PI / 180.0f);
            float COS45 = Mathf.Cos(45.0f * Mathf.PI / 180.0f);

            Vector3 tankPosition;
            Quaternion tankRotation;

            // 現在座標と角度を取得  
            SXG_GetPositionAndRotation(out tankPosition, out tankRotation);

            // 45度の座標を決定
            Vector3 tankPoint = tankPosition;
            tankPoint.y = 0;
            Vector3 tankDir = tankPoint.normalized;
            Vector3 positionToMove = Quaternion.AngleAxis(45.0f, Vector3.up) * (tankDir* m_patrolDistance);

            // １回の行動のランダムなタイムリミット 
            float timeLimit = Random.Range(3.0f, 10.0f);

            // 移動できているかチェック 
            float cantMoveTime = 0;
            Vector3 lastPosition = tankPosition;

            // 攻撃対象をランダム選択 
            TankInfo[] allTanksInfo = SXG_GetAllTanksInfo();
            List<int> attackTargetCandidates = new();
            for (int i=1; i < GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE; ++i)
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
            } else
            {
                SetProg(Prog.Finish);
                yield break;
            }

            // タイムリミットまで目標座標を目指して移動する 
            float time = 0;
            while (time < timeLimit)
            {
                // 現在座標と角度を取得  
                SXG_GetPositionAndRotation(out tankPosition, out tankRotation);

                // 現在地から目的地の方向 
                Vector3 dirToGoal = positionToMove - tankPosition;
                dirToGoal.y = 0;    // (高低差は無視)
                Vector3 direction = dirToGoal.normalized;

                // ほとんどゴールに到達していたら中断 
                if (dirToGoal.magnitude < 1.0f)
                {
                    break;
                }

                // 移動できているかチェック（一定時間移動できないなら目的地を変更する）
                if (2.0f < time && Vector3.Distance(tankPosition, lastPosition) < 0.2f)
                {
                    cantMoveTime += Time.deltaTime;
                    if (0.5f < cantMoveTime)
                    {
                        // スタックしてると判断：発砲して反動で揺する 
                        SXG_Shoot(0);
                        break;
                    }
                }
                else
                {
                    lastPosition = tankPosition;
                    cantMoveTime = 0;
                }

                // 戦車の正面方向 
                Vector3 tankFront = tankRotation * Vector3.forward;
                tankFront.y = 0;    // (高低差は無視)
                Vector3 tankFrontNormalized = tankFront.normalized;

                // 方向の差異を計算 
                float leftTorque = 0;
                float rightTorque = 0;
                float dirDot = Vector3.Dot(direction, tankFrontNormalized);
                //System.Text.StringBuilder sb = new();
                //sb.AppendFormat("[Attacker] dirDot={0} ", dirDot);
                if (COS10 <= dirDot)
                {
                    leftTorque = 1; // 全力前身
                    rightTorque = 1;
                    //sb.AppendFormat("<Front>");
                }
                else if (dirDot <= -COS10)
                {
                    leftTorque = -1;    // 全力後退 
                    rightTorque = -1;
                    //sb.AppendFormat("<Back>");
                }
                else
                {
                    Vector3 dirCross = Vector3.Cross(direction, tankFrontNormalized);
                    if (COS45 <= dirDot)
                    {
                        leftTorque = Mathf.Clamp01(1.0f + dirCross.y * 0.5f);
                        rightTorque = Mathf.Clamp01(1.0f - dirCross.y * 0.5f);
                        //sb.AppendFormat("<45deg:({0}, {1})>", leftTorque, rightTorque);
                    }
                    else
                    {
                        if (dirCross.y < 0)
                        {
                            leftTorque = 1;
                            rightTorque = -1;
                            //sb.AppendFormat("<Left:({0}, {1})>", leftTorque, rightTorque);
                        }
                        else
                        {
                            leftTorque = -1;
                            rightTorque = 1;
                            //sb.AppendFormat("<Right:({0}, {1})>", leftTorque, rightTorque);
                        }
                    }
                }
                // キャタピラのパワー設定 
                SXG_SetCaterpillarPower(leftTorque, rightTorque);

                //sb.AppendFormat(" | T={0}", Time.frameCount);
                //Debug.Log(sb.ToString());

                // 射撃 
                int turretNo = m_shooterNo % GetCountOfTurrets;
                if (SXG_CanShoot(turretNo))
                {
                    SXG_Shoot(m_shooterNo % GetCountOfTurrets);
                    m_shooterNo++;
                }

                // 常に攻撃目標に砲塔を向ける 
                for (int i=0; i < GetCountOfTurrets; ++i)
                {
                    int targetNo = attackTargetCandidates[(targetTeamNo + i) % attackTargetCandidates.Count];
                    SXG_RotateTurretToImpactPoint(i, allTanksInfo[targetNo].Position);
                }

                // 攻撃対象が敗退していたら行動中断 
                if (allTanksInfo[targetTeamNo].IsDefeated)
                {
                    break;
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

