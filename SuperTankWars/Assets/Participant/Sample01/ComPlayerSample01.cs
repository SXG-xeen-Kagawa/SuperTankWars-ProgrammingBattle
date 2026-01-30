using SXG2025;
using System.Collections;
using UnityEngine;


namespace SXG2025
{

    public class ComPlayerSample01 : ComPlayerBase
    {
        enum Prog
        {
            None,
            RandomMove,     // ランダム移動 
        }
        private Prog m_prog = Prog.None;

        private int m_shooterNo = 0;

        [SerializeField] private float m_patrolDistance = 20.0f;    // 巡回の距離(半径)

        [SerializeField] private Transform m_debugSphereTr = null;

        [SerializeField] private float m_shootableTiltAngleOfBody = 20.0f;  // 射撃可能な本体の姿勢角度 


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            SetProg(Prog.RandomMove);
        }

        private void Update()
        {
            // コントローラ操作の検証用コードです。動作検証が終わったら削除してください。
            //SXG_TestPlayByGamepad();


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
            float COS15 = Mathf.Cos(15.0f * Mathf.PI / 180.0f);
            float COS45 = Mathf.Cos(45.0f * Mathf.PI / 180.0f);

            Vector3 tankPosition;
            Quaternion tankRotation;
            float shootableAngle = Mathf.Cos(m_shootableTiltAngleOfBody * Mathf.PI / 180.0f);

            // 現在座標と角度を取得  
            SXG_GetPositionAndRotation(out tankPosition, out tankRotation);

            // 60度の座標を決定
            Vector3 tankDir = tankPosition.normalized;
            Vector3 positionToMove = Quaternion.AngleAxis(60.0f, Vector3.up) * (tankDir * m_patrolDistance);
            Vector3 startCross = Vector3.Cross(positionToMove, tankPosition);

            // ランダムなタイムリミット 
            float timeLimit = Random.Range(2.0f, 5.0f);

            // 発砲タイマー 
            float shootTimer = Random.Range(0.2f, 0.5f);

            // 攻撃対象を選択 
            TankInfo[] allTanksInfo = SXG_GetAllTanksInfo();
            float distance = float.MaxValue;
            int targetTeamNo = 1;   // 初期値は隣 
            for (int i = 1; i < GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE; ++i)
            {
                if (allTanksInfo[i].IsDefeated)
                {
                    continue;
                }
                float d = Vector3.Distance(allTanksInfo[0].Position, allTanksInfo[i].Position);
                if (d < distance)
                {
                    distance = d;
                    targetTeamNo = i;
                }
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

                // debug
                //m_debugSphereTr.position = positionToMove;

                // ほとんどゴールに到達していたら中断 
                if (dirToGoal.magnitude < 1.0f)
                {
                    break;
                }
                // 角度的に通り過ぎていたら中断 
                Vector3 cross = Vector3.Cross(positionToMove, tankPosition);
                if (cross.y* startCross.y < 0.0f)
                {
                    break;
                }

                // 戦車の正面方向 
                Vector3 tankFront = tankRotation * Vector3.forward;
                tankFront.y = 0;    // (高低差は無視)
                Vector3 tankFrontNormalized = tankFront.normalized;

                // 方向の差異を計算 
                float leftTorque = 0;
                float rightTorque = 0;
                float dirDot = Vector3.Dot(direction, tankFrontNormalized);
                if (COS15 <= dirDot)
                {
                    leftTorque = 1; // 全力前身
                    rightTorque = 1;
                }
                else if (dirDot <= -COS15)
                {
                    leftTorque = -1;    // 全力後退 
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
                // キャタピラのパワー設定 
                SXG_SetCaterpillarPower(leftTorque, rightTorque);

                // 射撃 
                shootTimer -= Time.deltaTime;
                if (shootTimer <= 0)
                {
                    int turretNo = m_shooterNo % GetCountOfTurrets;
                    if (SXG_CanShoot(turretNo))
                    {
                        shootTimer = Random.Range(0.2f, 0.5f);
                        // 姿勢を確認 
                        if (shootableAngle <= transform.up.y)
                        {
                            SXG_Shoot(turretNo);
                            m_shooterNo++;
                        }
                    }
                }

                // 常に攻撃目標に砲塔を向ける 
                allTanksInfo = SXG_GetAllTanksInfo();
                Vector3 targetPosition = allTanksInfo[targetTeamNo].Position;
                for (int i = 0; i < GetCountOfTurrets; ++i)
                {
                    SXG_RotateTurretToImpactPoint(i, targetPosition);
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

