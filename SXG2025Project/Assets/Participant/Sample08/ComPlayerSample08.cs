using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SXG2025
{
	public class ComPlayerSample08 : ComPlayerBase
	{
        float m_time = 0; // 時間カウント用

		private void Start()
		{

		}

		private void Update()
		{
#if true
            UpdateComPlayer();
#else
            // コントローラ操作の検証用コードです
            // 動作検証が終わったらコメントアウトしてください
            SXG_TestPlayByGamepad();
#endif
        }

        /// <summary>
        /// COMプレイヤー更新
        /// </summary>
        private void UpdateComPlayer()
		{
            // 情報取得
            SXG_GetPositionAndRotation(out var position, out var rotation);
            var allTanksInfo = SXG_GetAllTanksInfo();

            UpdateCaterpillar();
            UpdateTurret(0, position, allTanksInfo);
        }

        /// <summary>
        /// キャタピラ更新
        /// </summary>
        private void UpdateCaterpillar()
        {
            // 左右のキャタピラのパワー更新 くねくね動く
            m_time += Time.deltaTime * 0.5f;
            var caterpillarPowerL = Mathf.Abs(Mathf.Cos(m_time)) + 0.25f;
            var caterpillarPowerR = Mathf.Abs(Mathf.Sin(m_time)) + 0.25f;

            // 左右のキャタピラを回転させる
            SXG_SetCaterpillarPower(caterpillarPowerL, caterpillarPowerR);
        }

        /// <summary>
        /// 砲台更新
        /// </summary>
        private void UpdateTurret(int turretNo, Vector3 position, TankInfo[] allTanksInfo)
        {
            // 生存プレイヤーのインデックスを取得
            var aliveTankIndexes = new List<int>();
            for (var i = 1; i < allTanksInfo.Length; i++) // 0番目は自分なので1から
            {
                var info = allTanksInfo[i];

                if (info.IsDefeated) // 敗退済みならスルー
                    continue;

                if (info.Position.y < -1.0f) // 落下していたらスルー
                    continue;

                aliveTankIndexes.Add(i);
            }

            // 最も近くにいるプレイヤーに砲塔を向ける
            var minDistance = Mathf.Infinity;
            for (var i = 0; i < aliveTankIndexes.Count; i++)
            {
                var idx = aliveTankIndexes[i];
                var info = allTanksInfo[idx];

                var distance = Vector3.Distance(position, info.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    // 砲台を回転させる
                    SXG_RotateTurretToImpactPoint(turretNo, info.Position);
                }
            }

            // 砲弾を撃てるなら発射
            if (SXG_CanShoot(turretNo))
            {
                SXG_Shoot(turretNo);
            }
        }
	}
}