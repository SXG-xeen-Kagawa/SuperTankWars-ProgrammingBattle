// このファイルは「かんたんAI作成」により自動生成されました。 (2026-02-12 16:02:00)

using UnityEngine;
using SXG2025;

namespace SXG2025
{
    public class ComPlayerSample08 : ComPlayerBase
    {
        private Vector3 m_nowPosition;
        private Quaternion m_nowRotation;
        private int m_turretCount;
        private TankInfo[] m_allTanksInfo;

        private Vector3 m_targetPoint;

        private void Update()
        {
            // 自TANKの座標と角度を取得 
            SXG_GetPositionAndRotation(out m_nowPosition, out m_nowRotation);
            // 自TANKの砲塔の数を取得 
            m_turretCount = SXG_GetCountOfMyTurrets();
            // 全TANKの情報を取得（[0]は必ず自分）
            m_allTanksInfo = SXG_GetAllTanksInfo();

            // 移動処理 
            DoMove();

            // 狙う 
            DoAim();

            // 射撃 
            DoFire();

            // たこねこ 
            DoTakoneko();
        }


        private void DoTakoneko()
        {
            Matrix4x4 tankWorldMatrix = Matrix4x4.TRS(m_nowPosition, m_nowRotation, Vector3.one);
            Vector3 localTarget = tankWorldMatrix.inverse.MultiplyPoint(m_targetPoint);
            Vector3 localDir = new Vector3(localTarget.x, 0, localTarget.z).normalized;
            SXG_RotateJointToAngle(0, Mathf.Atan2(localDir.x, localDir.z) * 180.0f / Mathf.PI);
        }



        #region MOVE

        [Header("<Move (移動)>")]
        [SerializeField, Tooltip("基本となる移動の強さ"), Range(0.1f,1.0f)] float m_movePower = 1.0f;
        [SerializeField, Tooltip("目標移動半径"), Range(0.1f, 40.0f)] float m_targetRadius = 34.0f;
        [SerializeField, Tooltip("時計回り？反時計回り？")] bool m_isClockwise = true;


        void DoMove()
        {
            // 現在地から20度ずれた方向を目標座標として計算する
            Vector3 toNow = new Vector3(m_nowPosition.x, 0, m_nowPosition.z).normalized;
            float rotateAngle = m_isClockwise? 20.0f: -20.0f;
            Vector3 newDir = Quaternion.AngleAxis(rotateAngle, Vector3.up) * toNow;
            Vector3 targetPosition = newDir * m_targetRadius;

            // そちらへ移動 
            Vector3 toGoal = targetPosition - m_nowPosition;
            SetCaterpillarTowardWorldDir(m_nowRotation, toGoal.normalized, forwardPower: m_movePower);

            // 目標座標をデバッグ表示
            SXG_DebugDrawPositionMarker(targetPosition);
        }

#endregion

#region MOVE_Helpers

        /// <summary>
        /// 操舵：ワールド方向ベクトルへ向ける
        /// </summary>
        void SetCaterpillarTowardWorldDir(Quaternion tankRot, Vector3 desireDir, float forwardPower)
        {
            Vector3 front = tankRot * Vector3.forward;
            front.y = 0;
            front.Normalize();

            desireDir.y = 0;
            desireDir.Normalize();

            float dot = Vector3.Dot(front, desireDir);
            float crossY = Vector3.Cross(front, desireDir).y;   // 左右判定 

            // ざっくり旋回 
            float turn = Mathf.Clamp(crossY * 1.4f, -1.0f, +1.0f);

            float basePower = 0;
            if (0.5f < dot)
            {
                basePower = forwardPower;   // だいたい前を向いている
            }
            else if (dot < -0.5f)
            {
                basePower = -forwardPower;  // ほぼ反対を向いているので後退 
            }
            else
            {
                basePower = 0.2f;       // 旋回時は少し前進
            }

            // 左右のキャタピラの力 
            float left = Mathf.Clamp(basePower - turn, -1.0f, +1.0f);
            float right = Mathf.Clamp(basePower + turn, -1.0f, +1.0f);
            SXG_SetCaterpillarPower(left, right);
        }

        /// <summary>
        /// 最も近い戦車のIDを取得 
        /// </summary>
        /// <returns></returns>
        int FindTankIdWithMinDistance()
        {
            int nearestId = 0;
            float distance = float.MaxValue;

            for (int i=1; i < m_allTanksInfo.Length; ++i)
            {
                var tankInfo = m_allTanksInfo[i];
                if (tankInfo.IsDefeated) continue;  // 敗退済みは対象にしない
                Vector3 diff = tankInfo.Position - m_nowPosition;
                float d = diff.magnitude;
                if (d < distance)
                {
                    nearestId = i;
                    distance = d;
                }
            }
            return nearestId;
        }

        /// <summary>
        /// 残りエナジーが一番多い戦車のIDを取得 
        /// </summary>
        /// <returns></returns>
        int FindTankIdWithMaxEnergy()
        {
            int id = 0;
            int maxEnergy = 0;

            for (int i = 1; i < m_allTanksInfo.Length; ++i)
            {
                var tankInfo = m_allTanksInfo[i];
                if (tankInfo.IsDefeated) continue;  // 敗退済みは対象にしない 
                if (maxEnergy < tankInfo.Energy)
                {
                    id = i;
                    maxEnergy = tankInfo.Energy;
                }
            }
            return id;
        }

        /// <summary>
        /// 残りエナジーが一番少ない戦車のIDを取得 
        /// </summary>
        /// <returns></returns>
        int FindTankIdWithMinEnergy()
        {
            int id = 0;
            int minEnergy = int.MaxValue;

            for (int i = 1; i < m_allTanksInfo.Length; ++i)
            {
                var tankInfo = m_allTanksInfo[i];
                if (tankInfo.IsDefeated) continue;  // 敗退済みは対象にしない 
                if (tankInfo.Energy < minEnergy)
                {
                    id = i;
                    minEnergy = tankInfo.Energy;
                }
            }
            return id;
        }

        /// <summary>
        /// 正面に近い戦車を探す 
        /// </summary>
        /// <returns></returns>
        int FindTankIdInFront()
        {
            int id = 0;
            float maxDotProduct = 0.0f;

            for (int i=1; i < m_allTanksInfo.Length; ++i)
            {
                var tankInfo = m_allTanksInfo[i];
                if (tankInfo.IsDefeated) continue;  // 敗退済みは対象にしない 

                var toTank = (tankInfo.Position - m_nowPosition).normalized;
                float dotProduct = Vector3.Dot(transform.forward, toTank);
                if (maxDotProduct < dotProduct)
                {
                    id = i;
                    maxDotProduct = dotProduct;
                }
            }
            return id;
        }

        /// <summary>
        /// 背面に近い戦車を探す 
        /// </summary>
        /// <returns></returns>
        int FindTankIdBehind()
        {
            int id = 0;
            float minDotProduct = 0.0f;

            for (int i = 1; i < m_allTanksInfo.Length; ++i)
            {
                var tankInfo = m_allTanksInfo[i];
                if (tankInfo.IsDefeated) continue;  // 敗退済みは対象にしない 

                var toTank = (tankInfo.Position - m_nowPosition).normalized;
                float dotProduct = Vector3.Dot(transform.forward, toTank);
                if (dotProduct < minDotProduct)
                {
                    id = i;
                    minDotProduct = dotProduct;
                }
            }
            return id;
        }

#endregion
#region AIM

        [Header("<Aim (狙い)>")]
        [SerializeField] EasyAI.TargetRule[] m_targetRules;


        /// <summary>
        /// 射撃処理 
        /// </summary>
        void DoAim()
        {
            // 砲台が無いなら何もしない 
            if (m_turretCount <= 0) return;

            // 狙う 
            for (int i = 0; i < m_turretCount; ++i)
            {
                EasyAI.TargetRule rule = (m_targetRules != null && i < m_targetRules.Length)
                    ? m_targetRules[i]
                    : EasyAI.TargetRule.MinDistance;

                int targetTankId = SelectTarget(rule);
                if (0 < targetTankId)
                {
                    m_targetPoint = m_allTanksInfo[targetTankId].Position;
                    SXG_RotateTurretToImpactPoint(i, m_targetPoint);
                }
            }

        }

        private int SelectTarget(EasyAI.TargetRule rule)
        {
            switch (rule)
            {
                case EasyAI.TargetRule.MinDistance:
                    return FindTankIdWithMinDistance();
                case EasyAI.TargetRule.MinEnergy:
                    return FindTankIdWithMinEnergy();
                case EasyAI.TargetRule.MaxEnergy:
                    return FindTankIdWithMaxEnergy();
                case EasyAI.TargetRule.InFront:
                    return FindTankIdInFront();
                case EasyAI.TargetRule.Behind:
                    return FindTankIdBehind();
                default:
                    return FindTankIdWithMinDistance();
            }
        }

#endregion

#region FIRE

        [Header("<Fire (射撃)>")]
        [SerializeField] float m_fireInterval = 0.2f;       // 1基ずつの砲台の射撃インターバル

        [SerializeField] bool m_dontFireNearEdge = false;   // 外周付近では反動による落下を恐れて射撃しない
        [SerializeField] bool m_dontFireWhenUnstable = false;   // 戦車本体の姿勢が悪いときは射撃しない

        private int m_nextTurretId = 0;
        private float m_fireIntervalTime = 0;


        void DoFire()
        { 
            m_fireIntervalTime += Time.deltaTime;

            // 安定している時だけ撃つ？ 
            if (m_dontFireWhenUnstable)
            {
                if (transform.up.y < 0.9f)
                {
                    return;
                }
            }

            // 落下間近なら打たない？ 
            if (m_dontFireWhenUnstable)
            {
                float distanceFromCenter = new Vector3(m_nowPosition.x, 0, m_nowPosition.z).magnitude;
                if (GameConstants.ABOUT_GAME_FIELD_RADIUS * 0.9f < distanceFromCenter)
                {
                    return;
                }
            }

            // インターバル確認 
            if (m_fireIntervalTime < m_fireInterval)
            {
                return;
            }

            // 撃てるなら全て撃つ
            for (int i=0; i < m_turretCount; ++i)
            {
                int turretNo = (m_nextTurretId + i) % m_turretCount;

                if (SXG_CanShoot(turretNo))
                {
                    SXG_Shoot(turretNo);
                    m_nextTurretId = turretNo;
                    m_fireIntervalTime = 0;
                    break;
                }
            }
        }

#endregion

    }
}
