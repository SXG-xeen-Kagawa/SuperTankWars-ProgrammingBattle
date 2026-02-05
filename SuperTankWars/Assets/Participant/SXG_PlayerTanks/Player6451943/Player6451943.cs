using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SXG2025;

namespace Player6451943
{
    public class Player6451943 : ComPlayerBase
    {
        [SerializeField] Transform playerPotision = null;
       

        private Vector3 targetPosition;                     // ターゲットの座標
        private float fireDelay = 0.1f;                     // 弾の発射間隔
        private Coroutine shootCoroutine = null;            // 弾の発射遅延用コルーチン

        [SerializeField] private float patrolRadius = 30f;  // ランダム移動先の範囲
        private Vector3 patrolTarget;                       // 移動先の座標
        private float obstaclesRadius = 10f;                 // 障害物検知範囲

        [SerializeField] private float avoidAngle = 0.8f;   // 障害物回避の正面判定
        [SerializeField] private float avoidDistance = 5f;  // 障害物回避の距離判定

        private Vector3 lastPosition;                       // 前回のプレイヤの座標
        private float stuckCheckInterval = 3f;              // スタックを判定する時間
        private float stuckThreshold = 1f;                  // スタック判定を行う距離

        private Vector3 initialPosition;                    // スタート時点の座標
        private Quaternion initialRotation;                 // スタート時点の角度

        private float targetUpdateTarget = 10f;             // ターゲットを更新する秒数

        private void Start()
        {
            // スタート時に基準となるフィールドの中心点を保存
            SXG_GetPositionAndRotation(out Vector3 startPos, out Quaternion startRot);
            initialPosition = startPos;
            initialRotation = startRot;
            lastPosition = startPos;

            SetRandomPatrolTarget();

            StartCoroutine(CheckForStuck());
        }


        private void Update()
        {
            // 自身の現在座標と角度を取得
            SXG_GetPositionAndRotation(out Vector3 currentPos, out Quaternion currentRot);

            // 最も近い敵をロックオン
            // 一定時間ごとにターゲットを更新
            StartCoroutine(UpdateTargetRoutine());

            // 現在の targetPosition へ移動
            if (Vector3.Distance(currentPos, patrolTarget) < 5f)
            {
                SetRandomPatrolTarget();
            }

            MoveTowardsPosition(patrolTarget); // 巡回目標に移動

            // 発射間隔をずらして発射
            if (shootCoroutine == null)
            {
                shootCoroutine = StartCoroutine(AllTurretsAtDelayTarget(fireDelay));
            }
        }
        private void OnDrawGizmos()
        {
            // 実行中かつ初期化されている場合にのみ描画
            if (!Application.isPlaying)
                return;

            // 初期向きに対して前方30m
            Vector3 forward = initialRotation * Vector3.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 center = initialPosition + forward * patrolRadius;

            // 移動範囲を半透明の青色で描画
            Gizmos.color = new Color(0, 0, 1, 0.3f);
            Gizmos.DrawSphere(center, patrolRadius);

            // 円の輪郭を緑で描画
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(center, patrolRadius);

            // 中心点に小さい赤い球を描く
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(center, 1f);

            // プレイヤの現在位置を黄色で表示
            SXG_GetPositionAndRotation(out Vector3 currentPos, out _);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(currentPos, 1f);
        }



        /// <summary>
        /// フィールドの中心点を基準に指定した範囲内の目標地点をランダム生成
        /// </summary>
        private void SetRandomPatrolTarget()
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;

            // 初期向きに対して前方30mを中心に設定
            Vector3 forward = initialRotation * Vector3.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 center = initialPosition + forward * 30f;
            patrolTarget = center + new Vector3(randomCircle.x, 0, randomCircle.y);
        }



        /// <summary>
        /// 一番近い相手をターゲットに設定
        /// </summary>
        private void UpdateTarget()
        {
            // 全戦車の情報を取得
            TankInfo[] allTanksInfo = SXG_GetAllTanksInfo();
            float distance = float.MaxValue;
            int targetTeamNo = 1;

            for (int i = 1; i < GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE; ++i)
            {
                if (allTanksInfo[i].IsDefeated) // 対象が敗退していたら
                {
                    continue;
                }
                float d = Vector3.Distance(allTanksInfo[0].Position, allTanksInfo[i].Position);

                if (d < distance)   // 過去のターゲットと次の戦車の距離を比較(近ければ最新のターゲットに設定)
                {
                    distance = d;
                    targetTeamNo = i;
                }
            }

            // 目標座標設定
            targetPosition = allTanksInfo[targetTeamNo].Position;
        }

        /// <summary>
        /// 指定した時間ごとにターゲットを更新
        /// </summary>
        /// <returns></returns>
        private IEnumerator UpdateTargetRoutine()
        {
            while (true)
            {
                UpdateTarget();
                yield return new WaitForSeconds(targetUpdateTarget);
            }
        }


        /// <summary>
        ///  プレイヤの移動
        /// </summary>
        /// <param name="targetPos">移動先の座標(的もしくはランダムの移動先)</param>
        private void MoveTowardsPosition(Vector3 targetPos)
        {
            // 自身の現在座標と角度を取得（transform.positionは使用しない）
            SXG_GetPositionAndRotation(out Vector3 currentPos, out Quaternion currentRot);

            // 高さは無視した目標方向のベクトルを計算
            Vector3 toTarget = targetPos - currentPos;
            toTarget.y = 0;

            // 目標までの距離と正規化した方向ベクトルを計算
            float distance = toTarget.magnitude;
            Vector3 forward = currentRot * Vector3.forward;
            forward.y = 0;
            forward.Normalize();

            // 現在の向きと目標方向の内積と外積を取得
            float dot = Vector3.Dot(forward, toTarget.normalized);
            Vector3 cross = Vector3.Cross(forward, toTarget.normalized);

            // ----------障害物回避処理---------
            bool shouldAvoid = CheckObstacle(currentPos, forward, avoidAngle, avoidDistance);


            float leftTorque = 0f;
            float rightTorque = 0f;

            if (shouldAvoid)    //回避フラグがたっていたら
            {
                leftTorque = -1f;
                rightTorque = 1f;
            }
            else
            {
                if (dot > 0.95f)    // ほぼ正面なら直進
                {
                    leftTorque = rightTorque = 1f;
                }
                else if (cross.y > 0)   // 目標が左なら
                {
                    leftTorque = 1f;
                    rightTorque = -1f;
                }
                else
                {
                    leftTorque = -1f;
                    rightTorque = 1f;
                }
            }

            SXG_SetCaterpillarPower(leftTorque, rightTorque);


            for (int i = 0; i < GetCountOfTurrets; ++i)
            {
                SXG_RotateTurretToImpactPoint(i, targetPosition); // 目標に向ける
            }
        }

        /// <summary>
        /// プレイヤを中心に、指定された範囲内に障害物があるか判定
        /// </summary>
        /// <param name="currentPos">プレイヤの現在位置</param>
        /// <param name="forward">プレイヤの前方ベクトル</param>
        /// <param name="avoidDistance">障害物とみなす距離の閾値</param>
        /// <param name="avoidAngle">障害物とみなす角度の閾値</param>
        /// <returns>回避すべき障害物がある場合は true、そうでなければ false</returns>
        private bool CheckObstacle(Vector3 currentPos, Vector3 forward, float avoidDistance, float avoidAngle)
        {
            // 範囲内の障害物の座標を取得
            Vector3[] obstacles = SXG_GetHitObstacles(obstaclesRadius);

            // 各障害物ごとに距離を比較
            foreach (Vector3 obstaclePos in obstacles)
            {
                // 高さは無視した自分から障害物までの方向ベクトルを計算
                Vector3 directionObstacle = obstaclePos - currentPos;
                directionObstacle.y = 0;

                float dotObstacle = Vector3.Dot(forward, directionObstacle.normalized);
                float distanceObstacle = directionObstacle.magnitude;

                // 障害物が前方にあり、かつ距離が近ければ回避フラグを立てる
                if (dotObstacle > avoidAngle && distanceObstacle < avoidDistance)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 各砲台の発射処理
        /// </summary>
        /// <param name="delay">複数砲台の発射間隔をずらす秒数</param>
        /// <returns></returns>
        private IEnumerator AllTurretsAtDelayTarget(float delay)
        {
            for (int i = 0; i < GetCountOfTurrets; ++i)
            {
                if (SXG_CanShoot(i))
                {
                    SXG_Shoot(i);
                }

                yield return new WaitForSeconds(delay);
            }
            shootCoroutine = null;
        }
        

        /// <summary>
        /// プレイヤがスタックしているかの判定
        /// </summary>
        /// <returns></returns>
        private IEnumerator CheckForStuck()
        {
            while(true)
            {
                SXG_GetPositionAndRotation(out Vector3 currentPos, out _);

                // 前回からの移動距離をチェック
                float moveDistance = Vector3.Distance(currentPos, lastPosition);

                if(moveDistance < stuckThreshold)
                {
                    SetRandomPatrolTarget(); // 目標を再設定
                }

                lastPosition = currentPos;

                yield return new WaitForSeconds(stuckCheckInterval);
            }
        }
    }
}
