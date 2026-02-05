using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SXG2025;

namespace Player6407639
{
	public class Player6407639 : ComPlayerBase
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

            SXG_RotateJointToDirection(0, 1f);

            switch (m_prog)
            {
                case Prog.RandomMove:
                    StartCoroutine(CoMoveToNearestEnemy());
                    break;
            }
            m_prog = Prog.None;
        }


        private void SetProg(Prog newProg)
        {
            m_prog = newProg;
        }

        /// <summary>
        /// 高角弾道のヨー・ピッチ角度を計算する
        /// </summary>
        /// <param name="from">発射位置</param>
        /// <param name="to">ターゲット位置</param>
        /// <param name="initialSpeed">砲弾初速</param>
        /// <param name="gravity">重力加速度（例: 9.81f）</param>
        /// <param name="useHighAngle">trueで高角、falseで低角</param>
        /// <param name="tankForward">戦車の正面方向（通常はtransform.forward）</param>
        /// <returns>ヨー角度（deg）, ピッチ角度（deg）</returns>
        public static (float yawAngle, float pitchAngle) CalcBallisticAngle(
            Vector3 from, Vector3 to, float initialSpeed, float gravity, bool useHighAngle, Vector3 tankForward)
        {
            Vector3 diff = to - from;
            Vector3 diffXZ = new Vector3(diff.x, 0, diff.z);
            float distance = diffXZ.magnitude;
            float y = diff.y;

            // ピッチ角度計算（弾道公式）
            float v2 = initialSpeed * initialSpeed;
            float g = gravity;
            float underSqrt = v2 * v2 - g * (g * distance * distance + 2 * y * v2);

            if (underSqrt < 0)
            {
                // 到達不能
                return (0f, 45f); // 適当な値
            }

            float sqrt = Mathf.Sqrt(underSqrt);
            float angleRad;
            if (useHighAngle)
                angleRad = Mathf.Atan2(v2 + sqrt, g * distance);
            else
                angleRad = Mathf.Atan2(v2 - sqrt, g * distance);

            float pitchAngle = angleRad * Mathf.Rad2Deg;

            // ヨー角度計算（XZ平面で戦車正面からの角度）
            float yawAngle = Vector3.SignedAngle(tankForward, diffXZ.normalized, Vector3.up);

            return (yawAngle, pitchAngle);
        }

        public static (float yawAngle, float pitchAngle) CalcBallisticAngleLocal(
            Vector3 from, Vector3 to, float initialSpeed, float gravity, bool useHighAngle, Quaternion tankRotation)
        {
            Vector3 diff = to - from;
            Vector3 localTargetDir = Quaternion.Inverse(tankRotation) * diff;

            float distanceXZ = new Vector2(localTargetDir.x, localTargetDir.z).magnitude;
            float y = localTargetDir.y;

            // ピッチ角度計算（弾道公式）
            float v2 = initialSpeed * initialSpeed;
            float g = gravity;
            float underSqrt = v2 * v2 - g * (g * distanceXZ * distanceXZ + 2 * y * v2);

            if (underSqrt < 0)
            {
                return (0f, 45f);
            }

            float sqrt = Mathf.Sqrt(underSqrt);
            float angleRad;
            if (useHighAngle)
                angleRad = Mathf.Atan2(v2 + sqrt, g * distanceXZ);
            else
                angleRad = Mathf.Atan2(v2 - sqrt, g * distanceXZ);

            float pitchAngle = angleRad * Mathf.Rad2Deg;

            float yawAngle = Mathf.Atan2(localTargetDir.x, localTargetDir.z) * Mathf.Rad2Deg;

            return (yawAngle, pitchAngle);
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
                if (m_debugSphereTr != null)
                {
                    m_debugSphereTr.position = positionToMove;
                }

                // ほとんどゴールに到達していたら中断 
                if (dirToGoal.magnitude < 1.0f)
                {
                    break;
                }
                // 角度的に通り過ぎていたら中断 
                Vector3 cross = Vector3.Cross(positionToMove, tankPosition);
                if (cross.y * startCross.y < 0.0f)
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
                    shootTimer = 0.505f;
                    for (int i=0; i < GetCountOfTurrets; ++i)
                    {
                        SXG_Shoot(i);
                    }

                    /*
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
                    */
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

        private IEnumerator CoMoveToNearestEnemy()
        {
            float COS15 = Mathf.Cos(15.0f * Mathf.PI / 180.0f);
            float COS45 = Mathf.Cos(45.0f * Mathf.PI / 180.0f);

            float shootableAngle = Mathf.Cos(m_shootableTiltAngleOfBody * Mathf.PI / 180.0f);

            float shootTimer = 0f;
            int nextShoterNo = 0;

            while (true)
            {
                // 全戦車情報取得
                TankInfo[] allTanksInfo = SXG_GetAllTanksInfo();
                Vector3 myPosition = allTanksInfo[0].Position;
                Quaternion myRotation = allTanksInfo[0].Rotation;

                // 最寄りの敵戦車を探す
                float minDistance = float.MaxValue;
                int nearestEnemyIndex = -1;
                for (int i = 1; i < allTanksInfo.Length; ++i)
                {
                    if (allTanksInfo[i].IsDefeated) continue;
                    float d = Vector3.Distance(myPosition, allTanksInfo[i].Position);
                    if (d < minDistance)
                    {
                        minDistance = d;
                        nearestEnemyIndex = i;
                    }
                }
                if (nearestEnemyIndex == -1)
                {
                    SXG_SetCaterpillarPower(0, 0);
                    yield return null;
                    continue;
                }

                Vector3 targetPosition = allTanksInfo[nearestEnemyIndex].Position;

                Vector3 from = myPosition; // 砲身先端座標
                Vector3 to = targetPosition;   // 狙いたい着弾座標
                float shellSpeed = 30.0f; // 砲弾初速
                float gravity = 9.81f;    // 重力加速度


                for (int i = 0; i < GetCountOfTurrets; ++i)
                {
                    Vector3 apex = CalcBallisticApexHighAngle(from, to, shellSpeed, gravity);

                    // 砲塔を最高到達点に向ける
                    //SXG_RotateTurretToImpactPoint(i, apex);
                }

                // 移動処理
                Vector3 dirToTarget = targetPosition - myPosition;
                dirToTarget.y = 0;
                Vector3 direction = dirToTarget.normalized;

                // 戦車の正面方向
                Vector3 tankFront = myRotation * Vector3.forward;
                tankFront.y = 0;
                Vector3 tankFrontNormalized = tankFront.normalized;

                // 方向の差異を計算
                float leftTorque = 0;
                float rightTorque = 0;
                float dirDot = Vector3.Dot(direction, tankFrontNormalized);
                if (COS15 <= dirDot)
                {
                    leftTorque = 1;
                    rightTorque = 1;
                }
                else if (dirDot <= -COS15)
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
                    if (SXG_CanShoot(nextShoterNo))
                    {
                        shootTimer = 1f;
                        nextShoterNo++;
                        if (nextShoterNo == GetCountOfTurrets) nextShoterNo = 0;

                        SXG_Shoot(nextShoterNo);
                    }

                }

                /*
                for (int i = 0; i < GetCountOfTurrets; ++i)
                {
                    if (shootableAngle <= transform.up.y)
                    {
                        if (SXG_CanShoot(i)) SXG_Shoot(i);

                    }
                }
                */


                // 砲塔を敵に向ける
                for (int i = 0; i < GetCountOfTurrets; ++i)
                {
                    if (Vector3.Dot(transform.up, Vector3.up) < -0.5f)
                    {
                        SXG_RotateTurretToImpactPoint(i, new Vector3(0f, -100f, 0f));
                    }
                    else
                    {
                        Vector3 tmp = targetPosition;
                        //tmp.y += 1f;   // 高さ調整

                        SXG_RotateTurretToImpactPoint(i, tmp);
                    }
                }
                




                yield return null;
            }
        }

        /// <summary>
        /// 指定したターゲット位置に高角弾道で砲撃した場合の砲弾の最高到達点（頂点座標）を返す
        /// </summary>
        /// <param name="from">発射位置（戦車の砲身先端など）</param>
        /// <param name="to">ターゲット位置（着弾させたい座標）</param>
        /// <param name="initialSpeed">砲弾初速</param>
        /// <param name="gravity">重力加速度（例: 9.81f）</param>
        /// <returns>最高到達点（ワールド座標）</returns>
        public static Vector3 CalcBallisticApexHighAngle(Vector3 from, Vector3 to, float initialSpeed, float gravity)
        {
            Vector3 diff = to - from;
            Vector3 diffXZ = new Vector3(diff.x, 0, diff.z);
            float distance = diffXZ.magnitude;
            float y = diff.y;

            // 弾道公式（高角）
            float v2 = initialSpeed * initialSpeed;
            float g = gravity;
            float underSqrt = v2 * v2 - g * (g * distance * distance + 2 * y * v2);

            if (underSqrt < 0)
            {
                // 到達不能
                return from;
            }

            float sqrt = Mathf.Sqrt(underSqrt);
            // 高角
            float angleRad = Mathf.Atan2(v2 + sqrt, g * distance);

            // 速度ベクトル
            Vector3 dirXZ = diffXZ.normalized;
            Vector3 velocity = dirXZ * initialSpeed * Mathf.Cos(angleRad) + Vector3.up * initialSpeed * Mathf.Sin(angleRad);

            // 最高到達点までの時間
            float t_apex = velocity.y / gravity;

            // 最高到達点座標
            Vector3 apex = from + velocity * t_apex + 0.5f * Vector3.down * gravity * t_apex * t_apex;

            return apex;
        }
    }
}