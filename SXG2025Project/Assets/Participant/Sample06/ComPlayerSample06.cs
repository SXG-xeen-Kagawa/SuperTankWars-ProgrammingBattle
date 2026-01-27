using System.Collections;
using UnityEngine;


namespace ComPlayerSample06
{

    public class ComPlayerSample06 : SXG2025.ComPlayerBase
    {
        enum Prog
        {
            None,
            StandUp, 
            Attack,
        }
        private Prog m_prog = Prog.None;

        [SerializeField] private float[] m_standUpRotation;


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            SetProg(Prog.StandUp);
        }

        // Update is called once per frame
        void Update()
        {
            switch (m_prog)
            {
                case Prog.StandUp:
                    StartCoroutine(CoStandUp());
                    break;
                case Prog.Attack:
                    StartCoroutine(CoAttack());
                    break;
            }
            m_prog = Prog.None;
        }

        private void SetProg(Prog newProg)
        {
            m_prog = newProg;
        }


        /// <summary>
        /// 立ち上がる 
        /// </summary>
        /// <returns></returns>
        private IEnumerator CoStandUp()
        {
            // 無敵時間を使ってゆっくり立ち上がる 
            const float STANDUP_ANIM_TIME = 4.0f;

            // 立ち上がれ 
            float time = 0;

            while (time < STANDUP_ANIM_TIME)
            {
                time += Time.deltaTime;
                float a = Mathf.Clamp01(time / STANDUP_ANIM_TIME);

                // ジョイントを回転 
                if (m_standUpRotation.Length == SXG_GetCountOfMyRotJoints())
                {
                    for (int i=0; i < m_standUpRotation.Length; ++i)
                    {
                        SXG_RotateJointToAngle(i, m_standUpRotation[i]*a);
                    }
                }

                yield return null;
            }

            // 立ち上がったら攻撃 
            SetProg(Prog.Attack);
        }

        /// <summary>
        /// 適当に攻撃しまくる 
        /// </summary>
        /// <returns></returns>
        private IEnumerator CoAttack()
        {
            const float ATTACK_INTERVAL = 0.5f;

            // 平面的に見て一番近い戦車をターゲットに選ぶ
            int targetId = -1;
            {
                float nearedstDistance = float.MaxValue;
                var allTanksInfo = SXG_GetAllTanksInfo();
                var myTank = allTanksInfo[0];
                for (int i = 1; i < allTanksInfo.Length; ++i)
                {
                    var tank = allTanksInfo[i];
                    if (!tank.IsDefeated)
                    {
                        Vector3 dir = tank.Position - myTank.Position;
                        dir.y = 0;
                        float distance = dir.magnitude;
                        if (distance < nearedstDistance)
                        {
                            targetId = i;
                            nearedstDistance = distance;
                        }
                    }
                }
            }

            // 見つからないなら少し間を空けて再攻撃  
            if (targetId < 0)
            {
                yield return new WaitForSeconds(0.5f);
                SetProg(Prog.Attack);
                yield break;
            }

            // 狙う
            float time = 0;
            while (time < ATTACK_INTERVAL)
            {
                time += Time.deltaTime;

                // 攻撃対象 
                var allTanksInfo = SXG_GetAllTanksInfo();
                var targetTankInfo = allTanksInfo[targetId];
                if (targetTankInfo.IsDefeated)
                {
                    break;
                }

                // 左右どちらの砲台で狙うか？ 
                Vector3 dir = targetTankInfo.Position - transform.position;
                float dotRight = Vector3.Dot(transform.right, dir);
                int mainTurretNo = 0;
                if (0 < dotRight)
                {
                    // 右で狙う 
                    mainTurretNo = 0;
                } else
                {
                    // 左で狙う 
                    mainTurretNo = 1;
                }
                // 反対側の砲台で狙う場所を決める 
                Vector3 anotherPoint = new Vector3(
                    transform.position.x - dir.x,
                    targetTankInfo.Position.y,
                    transform.position.z - dir.z);

                // 狙う 
                SXG_RotateTurretToImpactPoint(mainTurretNo, targetTankInfo.Position);
                SXG_RotateTurretToImpactPoint(mainTurretNo ^ 1, anotherPoint);


                yield return null;
            }

            // 左右同時に撃つ(反動で姿勢を崩さないため)
            SXG_Shoot(0);
            SXG_Shoot(1);

            // 攻撃を繰り返す 
            SetProg(Prog.Attack);
        }
    }

}

