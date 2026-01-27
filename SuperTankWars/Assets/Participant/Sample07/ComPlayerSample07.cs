using System.Collections;
using UnityEngine;


namespace ComPlayerSample07
{

    public class ComPlayerSample07 : SXG2025.ComPlayerBase
    {
        [SerializeField] private float PatrolFieldRadius = 30.0f;

        private int[] m_shieldTargetIds;

        enum JointId
        {
            Shield01,
            Shield02,
            Shield03,
            Tail,
        }
        const int JOINT_COUNT_FOR_SHIELD = 3;


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // 盾の向き(最初は1対1)
            m_shieldTargetIds = new int[JOINT_COUNT_FOR_SHIELD];
            for (int i=0; i < m_shieldTargetIds.Length; ++i)
            {
                m_shieldTargetIds[i] = 1 + i;   // 盾を向ける攻撃対象 
            }

            // 砲塔制御 
            StartCoroutine(CoUpdateTurret());
        }

        // Update is called once per frame
        void Update()
        {
            // 移動行動 
            UpdateMove();

            // 盾を他の戦車の方に向ける 
            UpdateShieldWork();

            // しっぽを使って姿勢を安定させる 
            UpdateTailWork();
        }





        private void UpdateMove()
        {
            // 敵戦車の座標の平均値を求める
            var allTanksInfo = SXG_GetAllTanksInfo();
            int aliveCount = 0;
            Vector3 sumOfPosition = Vector3.zero;
            for (int i=1; i < allTanksInfo.Length; ++i)
            {
                if (!allTanksInfo[i].IsDefeated)
                {
                    sumOfPosition += allTanksInfo[i].Position;
                    aliveCount++;
                }
            }
            if (0 < aliveCount)
            {
                // 敵戦車の座標平均値の反対側がフリースペースと考える 
                Vector3 averageOfPosition = sumOfPosition / aliveCount;
                averageOfPosition.y = 0;
                Vector3 freeSpaceDir = averageOfPosition.normalized * (-1.0f);

                // 移動目標位置を決定 
                Vector3 targetPosition = freeSpaceDir * PatrolFieldRadius;
                MoveToTarget(targetPosition);
            }
        }

        private void MoveToTarget(Vector3 targetPosition)
        {
            const float COS20 = 0.9510f;
            const float COS45 = 0.7604f;

            // 移動する 
            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0;
            Vector3 toTargetNormalized = toTarget.normalized;
            float dotFront = Vector3.Dot(transform.forward, toTargetNormalized);
            float dotRight = Vector3.Dot(transform.right, toTargetNormalized);

            // ほぼ正面方向 
            if (COS20 < dotFront)
            {
                SXG_SetCaterpillarPower(1.0f, 1.0f);
            }
            // ほぼ真後ろ方向 
            else if (dotFront < -COS20)
            {
                SXG_SetCaterpillarPower(-1.0f, -1.0f);
            }
            // 正面付近 
            else if (COS45 < dotFront)
            {
                if (0 < dotRight)
                {
                    SXG_SetCaterpillarPower(1.0f, 0.4f);
                }
                else
                {
                    SXG_SetCaterpillarPower(0.4f, 1.0f);
                }
            }
            // 後ろ付近 
            else if (dotFront < -COS45)
            {
                if (0 < dotRight)
                {
                    SXG_SetCaterpillarPower(-1.0f, -0.4f);
                }
                else
                {
                    SXG_SetCaterpillarPower(-0.4f, -1.0f);
                }
            }
            // 横 
            else
            {
                if (0 < dotRight)
                {
                    SXG_SetCaterpillarPower(1.0f, -1.0f);
                }
                else
                {
                    SXG_SetCaterpillarPower(-1.0f, 1.0f);
                }
            }
        }



        /// <summary>
        /// 盾を他の戦車の方に向ける 
        /// </summary>
        private void UpdateShieldWork()
        {
            var allTanksInfo = SXG_GetAllTanksInfo();
            for (int i=0; i < m_shieldTargetIds.Length; ++i)
            {
                var targetTank = allTanksInfo[m_shieldTargetIds[i]];
                // ターゲットが敗退済みになったら他の戦車をターゲットにする 
                if (targetTank.IsDefeated)
                {
                    for (int j=1; j < SXG2025.GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE; ++j)
                    {
                        if (!allTanksInfo[j].IsDefeated)
                        {
                            m_shieldTargetIds[i] = j;
                            break;
                        }
                    }
                }
                // ターゲットが敗退していないならそちらに向ける 
                else
                {
                    Vector3 dirToTarget = targetTank.Position - allTanksInfo[0].Position;
                    Vector3 localDir = transform.InverseTransformDirection(dirToTarget);
                    SXG_RotateJointToAngle(i, Mathf.Atan2(localDir.x, localDir.z) * (180.0f / Mathf.PI));
                }
            }
        }

        /// <summary>
        /// 尻尾を使って姿勢を安定させる 
        /// </summary>
        private void UpdateTailWork()
        {
            const float COS10 = 0.987688f;
            //const float COS20 = 0.951056f;

            //Debug.Log("[Tail] Up=" + transform.up + " " + (transform.up.y < COS10) + " | T=" + Time.frameCount);

            // 本体が傾斜しているので反対側を支える  
            if (transform.up.y < COS10)
            {
                Vector3 dir = transform.up;
                dir.y = 0;
                dir = (-dir).normalized;
                SXG_RotateJointToAngle((int)JointId.Tail, Mathf.Atan2(dir.x, dir.z) * (180.0f / Mathf.PI));
            } else
            {

            }
        }


        private IEnumerator CoUpdateTurret()
        {
            while (true)
            {
                // 最も近い敵を攻撃対象にする 
                int targetId = 1;
                {
                    float nearestDistance = float.MaxValue;
                    var allTanksInfo = SXG_GetAllTanksInfo();
                    for (int i = 1; i < allTanksInfo.Length; ++i)
                    {
                        var tankInfo = allTanksInfo[i];
                        if (!tankInfo.IsDefeated)
                        {
                            Vector3 dir = tankInfo.Position - transform.position;
                            float distance = dir.magnitude;
                            if (distance < nearestDistance)
                            {
                                targetId = i;
                                nearestDistance = distance;
                            }
                        }
                    }
                }

                // ターゲットの戦車を狙う 
                while (!SXG_CanShoot(0))
                {
                    var allTanksInfo = SXG_GetAllTanksInfo();
                    var targetTank = allTanksInfo[targetId];
                    SXG_RotateTurretToImpactPoint(0, targetTank.Position);
                    yield return null;
                }
                // 発射 
                SXG_Shoot(0);
                yield return null;
            }

        }




    }


}

