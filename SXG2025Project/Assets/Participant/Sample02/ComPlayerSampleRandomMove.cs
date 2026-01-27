using SXG2025;
using System.Collections;
using UnityEngine;

namespace nsSample
{

    public class ComPlayerSampleRandomMove : SXG2025.ComPlayerBase
    {
        [SerializeField] Transform m_debugObjTr = null;

        enum Prog
        {
            None,
            RandomMove,     // ランダム移動 
        }
        private Prog m_prog = Prog.None;

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

            // ランダムな座標を決定 
            float randomRadius = Random.Range(GameConstants.ABOUT_GAME_FIELD_RADIUS*0.4f, GameConstants.ABOUT_GAME_FIELD_RADIUS*0.9f);
            float randomAngle = Random.Range(-Mathf.PI, Mathf.PI);
            Vector3 randomPosition = new Vector3(randomRadius * Mathf.Cos(randomAngle), 0, randomRadius * Mathf.Sin(randomAngle));

            // ランダムなタイムリミット 
            float timeLimit = Random.Range(3.0f, 15.0f);

            // 現在座標と角度を取得  
            SXG_GetPositionAndRotation(out tankPosition, out tankRotation);

            // 移動できているかチェック 
            float cantMoveTime = 0;
            Vector3 lastPosition = tankPosition;

            // 発砲タイマー 
            float shootTimer = Random.Range(1.0f, 3.0f);

            // 攻撃対象をランダム選択 
            TankInfo[] allTanksInfo = SXG_GetAllTanksInfo();
            int targetTeamNo = Random.Range(1,GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE);
            for (int i=0; i < 3; ++i)
            {
                targetTeamNo %= GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE;
                if (targetTeamNo == 0)
                {
                    targetTeamNo = 1;
                }
                if (!allTanksInfo[targetTeamNo].IsDefeated)
                {
                    break;  // 倒されていない対象を見つけた 
                }
                targetTeamNo++;
            }
            // 攻撃対象が居なくなったら辞める 
            if (targetTeamNo == GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE)
            {
                SetProg(Prog.None);
                yield break;
            }

            // タイムリミットまで目標座標を目指して移動する 
            float time = 0;
            while (time < timeLimit)
            {
                // 現在座標と角度を取得  
                SXG_GetPositionAndRotation(out tankPosition, out tankRotation);

                // 現在地から目的地の方向 
                Vector3 dirToGoal = randomPosition - tankPosition;
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
                } else
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
                if (COS10 <= dirDot)
                {
                    leftTorque = 1; // 全力前身
                    rightTorque = 1;
                } else if (dirDot <= -COS10)
                {
                    leftTorque = -1;    // 全力後退 
                    rightTorque = -1;
                } else
                {
                    Vector3 dirCross = Vector3.Cross(direction, tankFrontNormalized);
                    if (COS45 <= dirDot)
                    {
                        leftTorque = Mathf.Clamp01(1.0f + dirCross.y * 0.5f);
                        rightTorque = Mathf.Clamp01(1.0f - dirCross.y * 0.5f);
                    } else
                    {
                        if (dirCross.y < 0)
                        {
                            leftTorque = 1;
                            rightTorque = -1;
                        } else
                        {
                            leftTorque = -1;
                            rightTorque = 1;
                        }
                    }
                }
                // キャタピラのパワー設定 
                SXG_SetCaterpillarPower(leftTorque, rightTorque);

                m_debugObjTr.position = randomPosition;

                // 射撃 
                shootTimer -= Time.deltaTime;
                if (shootTimer <= 0)
                {
                    shootTimer = Random.Range(2.0f, 4.0f);
                    SXG_Shoot(0);
                }

                // 常に攻撃目標に砲塔を向ける 
                allTanksInfo = SXG_GetAllTanksInfo();
                SXG_RotateTurretToImpactPoint(0, allTanksInfo[targetTeamNo].Position);

                // 攻撃対象が破壊されていたら行動変更 
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

