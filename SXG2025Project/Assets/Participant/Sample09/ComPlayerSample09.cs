using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SXG2025;
using System.Security.Cryptography;

namespace ComPlayerSample09
{
	public class ComPlayerSample09 : ComPlayerBase
	{
        TankInfo myTank;
        public float minRad = 10f;
        public float maxRad = 20f;

        private void Start()
		{
            StartCoroutine(Co_Attack());
        }

		private void Update()
		{
            UpdateTarget();
            Move();

        }
        void UpdateTarget()
        {
            TankInfo[] allTanksInfo = SXG_GetAllTanksInfo();
            myTank = allTanksInfo[0];
            Vector3 center = myTank.Position;
            int turretCount = SXG_GetCountOfMyTurrets();

            if (turretCount == 0) return;

            // 敵の中で一番残基が少ない敵を探す（0は自分なので1から）
            TankInfo weakestEnemy = allTanksInfo[1];
            float minEna = float.MaxValue;
            for (int i = 1; i < allTanksInfo.Length; i++)
            {
                if (allTanksInfo[i].IsDefeated) continue;
                if (allTanksInfo[i].Energy < minEna)
                {
                    minEna = allTanksInfo[i].Energy;
                    weakestEnemy = allTanksInfo[i];
                }
            }


            // 自機から敵への方向と距離
            Vector3 baseDir = weakestEnemy.Position - center;
            baseDir.y = 0f;

            float distance = baseDir.magnitude;
            if (distance < 0.001f)
            {
                baseDir = Vector3.forward;
                distance = 10f; // 適当な距離に補正
            }
            baseDir.Normalize();

            // 1砲台あたりの角度間隔
            float stepAngle = 360f / turretCount;

            for (int i = 0; i < turretCount; i++)
            {
                Vector3 impactPoint;

                if (i == 0)
                {
                    impactPoint = weakestEnemy.Position;
                }
                else
                {
                    Quaternion rot = Quaternion.AngleAxis(stepAngle * i, Vector3.up);
                    Vector3 dir = rot * baseDir;

                    impactPoint = center + dir * distance;
                }

                SXG_RotateTurretToImpactPoint(i, impactPoint);
            }
        }


        // 移動
        void Move()
        {
            var target = GetOrbitPosition(Vector3.zero, 1, 30);
            MoveTowardsTarget(myTank.Position, myTank.Rotation, target, out float leftPower, out float rightPower);
            SXG_SetCaterpillarPower(leftPower, rightPower);
        }
        // 半径が周期的に変化しながら、指定秒でZ軸360度回転する位置を返す
        Vector3 GetOrbitPosition(Vector3 center, float radiusPeriod, float rotTime)
        {
            // 経過時間
            float t = Time.time;

            float radius = Mathf.Lerp(minRad, maxRad, (Mathf.Sin(t / radiusPeriod * Mathf.PI * 2f) + 1f) * 0.5f);
            float angle = (t / rotTime) * 360f;

            float rad = angle * Mathf.Deg2Rad;
            float x = Mathf.Cos(rad) * radius;
            float z = Mathf.Sin(rad) * radius;

            return center + new Vector3(x, 0f, z);
        }
        void MoveTowardsTarget(Vector3 tankPosition, Quaternion tankRotation, Vector3 targetPos, out float leftPower, out float rightPower)
        {
            // 戦車の前方向
            Vector3 tankForward = tankRotation * Vector3.forward;
            tankForward.y = 0f;
            tankForward.Normalize();

            // 目標への方向
            Vector3 toTarget = targetPos - tankPosition;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance < 0.1f)
            {
                // ほぼ到着
                leftPower = 0f;
                rightPower = 0f;
                return;
            }

            Vector3 dir = toTarget.normalized;

            float angle = Vector3.SignedAngle(tankForward, dir, Vector3.up);
            float forward = (Mathf.Abs(angle) <= 90f) ? 1f : -1f;
            float turn = Mathf.Clamp(angle / 90f, -1f, 1f);
            float speed = Mathf.Clamp01(distance / 10f); // 10mで全速、近いとゆっくり

            // キャタピラ出力計算
            leftPower = Mathf.Clamp(forward * speed - turn, -1f, 1f);
            rightPower = Mathf.Clamp(forward * speed + turn, -1f, 1f);
        }


        IEnumerator Co_Attack()
        {
            while (true)
            {
                for (int i = 0; i < GetCountOfTurrets; ++i)
                {
                    SXG_Shoot(i);
                }
                yield return new WaitWhile(() => SXG_CanShoot(0));
            }
        }
    }
}