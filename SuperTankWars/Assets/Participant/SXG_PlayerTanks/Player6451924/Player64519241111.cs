using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SXG2025;

namespace Player6451924
{
    public class Player6451924 : ComPlayerBase
    {
        float m_time = 0; // 時間カウント用
        private float m_walkTime = 0f; // 疑似足用の時間

        // --- 追加 ---
        private Vector3 m_lastPos;          // 前回位置
        private float m_stillTime = 0f;     // 静止していた時間
        private const float STOP_ROTATE_THRESHOLD = 2.0f; // 2秒静止で旋回開始
        private const float MOVE_EPSILON = 0.05f;         // これ以下の移動は静止とみなす
        // -------------

        private void Start()
        {
            SXG_GetPositionAndRotation(out m_lastPos, out _);
        }

        private void Update()
        {
#if true
            UpdateComPlayer();
            UpdateFakeLegs();
#else
            SXG_TestPlayByGamepad();
#endif
        }

        /// <summary>
        /// COMプレイヤー更新
        /// </summary>
        private void UpdateComPlayer()
        {
            SXG_GetPositionAndRotation(out var position, out var rotation);
            var allTanksInfo = SXG_GetAllTanksInfo();

            UpdateCaterpillar(position);
            UpdateTurret(0, position, allTanksInfo);
        }

        /// <summary>
        /// キャタピラ更新（最も近い敵に向かって突っ込む）
        /// </summary>
        private void UpdateCaterpillar(Vector3 myPos)
        {
            // 自分の位置と向きを取得
            SXG_GetPositionAndRotation(out myPos, out var myRot);
            var forward = myRot * -Vector3.forward;

            // ---- 位置変化の検出 ----
            float moveDistance = Vector3.Distance(myPos, m_lastPos);
            if (moveDistance < MOVE_EPSILON)
            {
                m_stillTime += Time.deltaTime;
            }
            else
            {
                m_stillTime = 0f;
                m_lastPos = myPos;
            }
            // ------------------------

            // 敵探索
            var allTanksInfo = SXG_GetAllTanksInfo();
            int nearestEnemyIndex = -1;
            float minDistance = Mathf.Infinity;

            for (int i = 1; i < allTanksInfo.Length; i++)
            {
                var info = allTanksInfo[i];
                if (info.IsDefeated || info.Position.y < -1.0f) continue;

                float distance = Vector3.Distance(myPos, info.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestEnemyIndex = i;
                }
            }

            // 追いかける敵がいない場合
            if (nearestEnemyIndex == -1)
            {
                SXG_SetCaterpillarPower(0, 0);
                return;
            }

            // 敵方向の角度計算
            Vector3 targetDir = (allTanksInfo[nearestEnemyIndex].Position - myPos).normalized;
            float angle = Vector3.SignedAngle(forward, targetDir, Vector3.up);

            float leftPower, rightPower;
            float basePower = 1.0f;
            float turnPower = Mathf.Clamp(angle / 45f, -1f, 1f);

            // ===== 停止検知時の旋回処理 =====
            if (m_stillTime > STOP_ROTATE_THRESHOLD)
            {
                // その場で旋回
                leftPower = 1f;
                rightPower = -1f;
            }
            else if (Mathf.Abs(angle) < 10f)
            {
                // 正面なら直進
                leftPower = 1f;
                rightPower = 1f;
            }
            else
            {
                // 通常旋回
                leftPower = Mathf.Clamp(basePower - turnPower, -1f, 1f);
                rightPower = Mathf.Clamp(basePower + turnPower, -1f, 1f);
            }

            SXG_SetCaterpillarPower(leftPower, rightPower);
        }

        /// <summary>
        /// 砲台更新
        /// </summary>
        private void UpdateTurret(int turretNo, Vector3 position, TankInfo[] allTanksInfo)
        {
            var aliveTankIndexes = new List<int>();
            for (var i = 1; i < allTanksInfo.Length; i++)
            {
                var info = allTanksInfo[i];
                if (info.IsDefeated || info.Position.y < -1.0f) continue;
                aliveTankIndexes.Add(i);
            }

            var minDistance = Mathf.Infinity;
            for (var i = 0; i < aliveTankIndexes.Count; i++)
            {
                var idx = aliveTankIndexes[i];
                var info = allTanksInfo[idx];

                var distance = Vector3.Distance(position, info.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    SXG_RotateTurretToImpactPoint(turretNo, info.Position);
                }
            }

            if (SXG_CanShoot(turretNo))
            {
                SXG_Shoot(turretNo);
            }
        }

        /// <summary>
        /// 疑似4足の足を動かす
        /// </summary>
        private void UpdateFakeLegs()
        {
            m_walkTime += Time.deltaTime;
            float walkSpeed = 15f;
            float walkAngle = 18f;
            float phaseStep = Mathf.PI / 4f;

            int[][] pairs = new int[][]
            {
                new int[] {0, 2},
                new int[] {1, 3},
                new int[] {4, 6},
                new int[] {5, 7}
            };

            for (int pairIndex = 0; pairIndex < pairs.Length; pairIndex++)
            {
                float phase = pairIndex * phaseStep;

                int jointA = pairs[pairIndex][0];
                int jointB = pairs[pairIndex][1];

                SXG_RotateJointToAngle(jointA, Mathf.Sin(m_walkTime * walkSpeed + phase) * walkAngle);
                SXG_RotateJointToAngle(jointB, Mathf.Sin(m_walkTime * walkSpeed + phase + Mathf.PI) * walkAngle);
            }
        }
    }
}
