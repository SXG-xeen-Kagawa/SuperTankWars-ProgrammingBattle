using SXG2025;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements.Experimental;

namespace Player6440271
{

    public class Player6440271 : ComPlayerBase
    {

        /// <summary>
        /// タンクが有効な状態か
        /// </summary>
        /// <param name="tank">戦車の情報</param>
        /// <returns>有効な状態ならtrue</returns>
        static bool IsTankActive(TankInfo tank)
        {
            return !(tank.IsDefeated || tank.IsInvincible);
        }




        private void Start()
        {
            // 状態コンテキストを初期化
            stateContext_.Init(this, ActionState.kNormalState);
            // すべてのタンクの情報を取得
            var tankInfos = SXG_GetAllTanksInfo();
            // 復帰用のスポーンポイントを保存
            spawnPoint_ = new Vector3[tankInfos.Length];
            for (Int32 i = 0; i < tankInfos.Length; i++)
            {
                spawnPoint_[i] = tankInfos[i].Position;
            }
            GetTanksInfo();

            var pos = Quaternion.AngleAxis(25, Vector3.up) * tankInfos[0].Position * 1.25f;

            for (Int32 i = 0; i < 10; i++)
            {
                prevPositionQueue_.Enqueue(pos);
            }
        }

        private void Update()
        {
            // 最新の状態を取得する
            PreUpdate();


            // 状態に応じた処理を実行
            stateContext_.Update();
        }

        public void SetSefetyMovePosition()
        {
            // 安全な位置から移動先を探す



        }

        /// <summary>
        /// 安全地帯を探す
        /// </summary>
        /// <returns>安全地帯の位置リスト</returns>
        public List<Vector3> FindSefetyArea()
        {
            List<Vector3> safetyAreas = new List<Vector3>();
            // とりあえず､エリアの指標となるベクトルを4つ登録
            Vector3[] vectors = new Vector3[]
            {
                Vector3.forward,
                Vector3.back,
                Vector3.left,
                Vector3.right
            };

            // まず自分のスポーン地点は安全地帯として登録

            safetyAreas.Add(FindNearestVector(spawnPoint_[0], vectors));

            // 全部の敵タンクについてループ
            for (Int32 i = 1; i < tankInfos_.Length; i++)
            {
                // 敵タンクが敗退していなければスキップ
                if (!tankInfos_[i].IsDefeated)
                {
                    continue;
                }
                // 敵タンクのスポーン地点を安全地帯として登録
                safetyAreas.Add(FindNearestVector(spawnPoint_[i], vectors));
            }


            return safetyAreas;

            // 与えられたベクトルが最も近いベクトルを返すような関数
            static Vector3 FindNearestVector(Vector3 target, Vector3[] vectors)
            {
                // 最も近いベクトルを探す
                Vector3 nearestVector = vectors[0];
                // とりあえず高さは無視して内積で比較
                target.y = 0;

                // 最も値の大きい内積を持つベクトルを探す
                float nearestDot = float.MinValue;  // 最小値で初期化
                for (Int32 i = 1; i < vectors.Length; i++)
                {
                    // 正規化して内積を計算
                    float dot = Vector3.Dot(target.normalized, vectors[i].normalized);
                    // 内積が最も大きいベクトルを保存
                    if (dot > nearestDot)
                    {
                        // 内積を更新
                        nearestDot = dot;
                        nearestVector = vectors[i];
                    }
                }
                return nearestVector;
            }
        }

        /// <summary>
        /// 自機が起立しているかのスコアを取得する
        /// </summary>
        /// <returns>直立しているほど1.0に近い値、倒れているほど-1.0に近い値</returns>
        public float GetIsUprightScore()
        {
            // 自機の姿勢を取得
            Quaternion selfRotation = selfInfo_.Rotation;
            // 自機の上方向ベクトルを取得
            Vector3 upVector = selfRotation * Vector3.up;
            // 上方向ベクトルとワールドの上方向ベクトルの内積を計算
            float uprightScore = Vector3.Dot(upVector, Vector3.up);
            return uprightScore;
        }

        /// <summary>
        /// 復帰用の砲塔操作を行う
        /// </summary>
        public void FacingTurretRecovery()
        {

            // 自機の姿勢を取得
            Quaternion selfRotation = selfInfo_.Rotation;
            // 自機の上方向ベクトルを取得
            Vector3 upVector = selfRotation * Vector3.up;

            // 砲台の目的座標
            Vector3 targetPos = selfInfo_.Position + upVector * 5.0f;

            Vector3 position = selfInfo_.Position;
            position.y = 0.0f;

            // もし､自分がステージの端なら
            // ターゲットをステージ外側に向ける
            if (kStageRadius_ - 1.5f < position.magnitude)
            {
                targetPos = position + position.normalized * 1.5f;
            }


            // 砲塔をその方向に向ける
            for (Int32 i = 0; i < SXG_GetCountOfMyTurrets(); i++)
            {
                SXG_RotateTurretToImpactPoint(i, targetPos);
            }

        }

        public void FacingTurretStageOutSide(float downDir = -15.0f)
        {
            var pos = selfInfo_.Position;
            pos.y = 0.0f;
            float distance = pos.magnitude;
            pos = pos.normalized;

            pos *= distance + 3.0f;
            pos.y = downDir;

            for (Int32 i = 0; i < SXG_GetCountOfMyTurrets(); i++)
            {
                SXG_RotateTurretToImpactPoint(i, pos);
            }
        }

        /// <summary>
        /// 砲塔を順次発射する
        /// </summary>
        public void SequentiallyTurretFire()
        {

            if (selfInfo_.IsInvincible) { return; }
            // 発射クールダウンを進める
            fireCooldown_ += Time.deltaTime;

            // 連続発射処理を for で実装
            Int32 turretCount = SXG_GetCountOfMyTurrets();
            // 砲塔が1つ以上ある場合
            if (turretCount > 0)
            {
                // 各砲塔について発射判定を行う
                for (Int32 t = 0; t < turretCount; t++)
                {
                    // 各砲塔の発射閾値を計算 (等分割)
                    float threshold = (kFireInterval_ * t) / turretCount;
                    // 閾値を超えていたら発射
                    if (fireCooldown_ >= threshold)
                    {
                        SXG_Shoot(t);
                    }
                }

                //if (!SXG_CanShoot(0) && !SXG_CanShoot(1))
                //{
                //    SXG_Shoot(2);
                //}

                // クールダウンが一周したらリセット
                if (fireCooldown_ >= kFireInterval_)
                {
                    fireCooldown_ = 0.0f;
                }
            }
        }

        /// <summary>
        /// 更新前の処理
        /// </summary>
        private void PreUpdate()
        {
            if (tankInfos_ != null && !tankInfos_[0].IsInvincible)
            {
                // タンクの前回位置を保存
                prevPositions_ = new Vector3[tankInfos_.Length];
                // もし過去の位置の配列に既定数の要素があれば､1つ吐き出す
                if (prevPositionQueue_.Count >= 15)
                {
                    prevPositionQueue_.Dequeue();
                }
                prevPositionQueue_.Enqueue(selfInfo_.Position);

                for (Int32 i = 0; i < tankInfos_.Length; i++)
                {
                    prevPositions_[i] = tankInfos_[i].Position;
                }
            }
            // すべてのタンクの情報を取得
            GetTanksInfo();
        }

        /// <summary>
        /// 目的位置へ移動する
        /// </summary>
        public void MoveForTarget()
        {
            /// 
            /// 移動処理の優先度
            /// 
            /// 1. ステージ外にいる場合､ステージ内に戻る
            /// 2. ステージ外を目指そうとした場合､ステージ内に寄せる
            /// 3. 自分自身が居た場所は､暫くの間避ける
            /// 4. 敵タンクの位置から離れる挙動を取る
            /// 5. 敵タンクの射線を外すために､壁があったら裏側に回る
            /// 6. 安全地帯を優先的に選ぶ
            /// 

            ///
            /// 移動は目標地点を決定し､壁を避けつつその地点に向かうようにする
            ///


            ///
            /// ポジティブ座標とネガティブ座標を用意する??
            /// とりあえず､平面座標と半径､減衰曲線をあわせた構造体で､ポジティブとネガティブを定義する
            /// 何かしらの乱数で座標を決定し､その座標の評価値の高い場所を目標地点とする
            /// n回のサンプリングを行い､最も評価値の高い座標を目標地点とする
            ///

            const Int32 kSampleCount = 20; // サンプリング回数

            // とりあえず､目標地点をランダムに決定する
            Tuple<Vector3, float>[] target = new Tuple<Vector3, float>[kSampleCount];

            for (Int32 i = 0; i < kSampleCount; i++)
            {
                // ステージ内のランダムな位置を生成
                float angle = UnityEngine.Random.Range(0.0f, Mathf.PI * 2.0f);
                float radius = Easing.OutQuad(UnityEngine.Random.Range(0.0f, 1.0f)) * 15.0f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;

                // 目標地点
                var pos = new Vector3(x, 0.0f, z) + selfInfo_.Position;

                // 範囲外であった場合､範囲内に戻るようClampする
                if (pos.magnitude > kStageRadius_)
                {
                    pos = pos.normalized * (kStageRadius_ - 2.5f);
                }

                // 目標地点と初期スコアを設定
                target[i] = new(pos, 0.0f);
            }

            // 評価値のための構造体リストを用意する
            List<PosNegArea> posNegArea = new();

            // 自分自身の位置をネガティブエリアとして登録する
            if (prevPositions_ != null)
            {
                var area = new PosNegArea(prevPositions_[0], 3.0f, -1.0f, (t) => (1.0f - Easing.InCirc(t)));

            }
            // 敵タンクの位置をネガティブエリアとして登録する
            for (Int32 i = 1; i < tankInfos_.Length; i++)
            {
                // 敵タンクが有効な状態であれば登録
                if (!tankInfos_[i].IsDefeated)
                {
                    posNegArea.Add(new(tankInfos_[i].Position, 35.0f, -30.0f, (t) => (1.0f - Easing.InCirc(t))));
                }
            }

            // 岩の位置を検索し､敵から隠れられる位置をポジティブエリアとして登録する
            var rocks = SXG_GetHitObstacles(15.0f);
            for (Int32 i = 0; i < rocks.Length; i++)
            {
                // 岩の位置
                Vector3 rockPos = rocks[i];
                // 敵タンクの位置から岩の反対側にオフセットした位置を計算
                for (Int32 j = 1; j < tankInfos_.Length; j++)
                {
                    // 敵タンクが有効な状態であれば登録
                    if (!tankInfos_[j].IsDefeated)
                    {
                        // 敵タンクの位置
                        Vector3 enemyPos = tankInfos_[j].Position;
                        // 敵タンクから岩への方向ベクトルを計算
                        Vector3 dirToRock = (rockPos - enemyPos).normalized;
                        // 岩の反対側にオフセットした位置を計算
                        Vector3 hidePos = rockPos + dirToRock * 3.0f;
                        Vector3 openPos = rockPos - dirToRock * 1.0f;
                        // ポジティブエリアとして登録
                        posNegArea.Add(new(new Vector2(hidePos.x, hidePos.z), 3.0f, 1.0f));
                        // ネガティブエリアとして登録
                        posNegArea.Add(new(new Vector2(openPos.x, openPos.z), 1.5f, -3.0f));
                    }
                }
            }

            // 敵の数が1体以下の場合､ステージ外に落ちるのを避けるために､ステージ外のネガティブエリアを強化する

            Int32 enemyCount = 0;
            for (Int32 i = 1; i < tankInfos_.Length; i++)
            {
                if (!tankInfos_[i].IsDefeated)
                {
                    enemyCount++;
                }
            }

            // ステージ外に向けて､ネガティブエリアを登録する｡(減衰曲線を反転したカスタムで設定)
            posNegArea.Add(new(
                new Vector2(0.0f, 0.0f),
                kStageRadius_ - 5.0f,
                -100.0f,
                (t) => Mathf.Pow(Easing.InCirc(t), 15) // 反転した線形減衰
            ));

            // 過去の位置キューの位置の補正をかけた総和
            var prevPos = Vector3.zero;
            for (Int32 i = 0; i < prevPositionQueue_.Count; i++)
            {
                prevPos += prevPositionQueue_.ToArray()[i] / prevPositionQueue_.Count;
            }

            Int32 bestScore = Int32.MinValue;
            Int32 bestIndex = -1;

            // サンプリングした目標地点に対して､評価値の計算を行う
            for (Int32 i = 0; i < kSampleCount; i++)
            {
                // 目標地点の座標
                Vector3 pos = target[i].Item1;
                float score = 0.0f;

                // ポジティブエリアの評価値を加算する
                foreach (var area in posNegArea)
                {
                    score += area.GetScore(pos);
                }

                // 前回の位置から進行方向を計算し､その方向に進むように評価値を加算する
                if (prevPositions_ != null)
                {
                    Vector3 moveDir = (selfInfo_.Position - prevPos).normalized;
                    Vector3 toTargetDir = (pos - selfInfo_.Position).normalized;
                    float forwardDot = Vector3.Dot(moveDir, toTargetDir);
                    score += forwardDot * 7; // 前方向に進むほど評価値が高くなる
                }
                // 半径nマス以内に敵がいる場合､その敵から遠ざかるように評価値を加算する
                for (Int32 j = 1; j < tankInfos_.Length; j++)
                {
                    // 敵タンクが有効な状態であれば登録
                    if (!tankInfos_[j].IsDefeated)
                    {
                        const float findDistance = 15.0f;
                        float distanceToEnemy = Vector3.Distance(selfInfo_.Position, tankInfos_[j].Position);
                        if (distanceToEnemy <= findDistance)
                        {
                            Vector3 dirToEnemy = (tankInfos_[j].Position - selfInfo_.Position).normalized;
                            Vector3 toTargetDir = (pos - selfInfo_.Position).normalized;
                            float awayDot = Mathf.Clamp(Vector3.Dot(-dirToEnemy, toTargetDir), -1, 0);
                            score += awayDot * 20.0f * Easing.OutSine(distanceToEnemy / findDistance); // 敵から遠ざかるほど評価値が高くなる
                        }
                    }
                }


                // 評価値が最も高い場合､目標地点として採用する
                if (score > bestScore)
                {
                    bestScore = (Int32)score;
                    bestIndex = i;
                }
            }

            // 目標地点を設定
            moveTargetPosition_ = target[bestIndex].Item1;

            // 目標地点に移動する処理を行う
            MoveForPosition(moveTargetPosition_);
        }

        // ネガポジの図形の構造体を用意する
        private class PosNegArea
        {
            public PosNegArea(Vector2 center, float radius, float power)
            {
                center_ = center;
                radius_ = radius;
                power_ = power;
            }
            public PosNegArea(Vector2 center, float radius, float power, Func<float, float> attenuationFunc)
            {
                center_ = center;
                radius_ = radius;
                power_ = power;
                attenuationFunc_ = attenuationFunc;
            }
            public Vector2 center_; // 中心座標
            public float radius_;   // 半径
            public float power_;    // 効果の強さ
            // 減衰曲線｡デフォルトは線形
            public Func<float, float> attenuationFunc_ = (t) => 1.0f - t;
            // 評価値を取得する
            public float GetScore(Vector3 position)
            {
                // 2D座標に変換
                Vector2 pos2D = new Vector2(position.x, position.z);
                // 中心からの距離を計算
                float distance = Vector2.Distance(pos2D, center_);
                // 半径内かどうかで評価値を計算
                if (distance <= radius_)
                {
                    // 半径内なら減衰関数を用いて評価値を計算
                    float t = Mathf.Clamp01(distance / radius_); // 0~1の範囲に正規化
                    return attenuationFunc_(t) * power_;
                }
                else
                {
                    return attenuationFunc_(1) * power_;
                }
            }
        }


        /// <summary>
        /// 敵タンクにタレットを向ける
        /// </summary>
        public void FacingTurretForEnemy()
        {
            // 近い順に敵タンクを探す
            var nearestEnemyIndex = SortNearestEnemyTank();
            if (nearestEnemyIndex != null && nearestEnemyIndex.Count != 0)
            {
                for (Int32 i = 0; i < SXG_GetCountOfMyTurrets(); i++)
                {
                    // タレットを敵に向ける
                    TargetTurretForEnemy(i, nearestEnemyIndex[i % nearestEnemyIndex.Count]);
                }
            }
        }

        /// <summary>
        /// 空中にいる時の砲塔操作を行う
        /// </summary>
        public void FacingTurretArielMode()
        {
            var position = selfInfo_.Position;
            float height = position.y;
            position.y = 0.0f;


            // まず､ステージ外側に砲塔を向ける
            FacingTurretStageOutSide(0);

            // ステージの上に来たら､砲塔を真上に向ける
            if (kStageRadius_ - 5.0f < position.magnitude && height > 30.0f)
            {
                for (Int32 i = 0; i < SXG_GetCountOfMyTurrets(); i++)
                {
                    SXG_RotateTurretToImpactPoint(i, position * (i + 2) + Vector3.up * 100.0f);
                }
            }

            else if (!SXG_CanShoot(3) && fireCooldown_ < kFireInterval_ / 4)
            {
                fireCooldown_ = -2.0f;
            }
        }

        /// <summary>
        /// 指定位置へ移動する
        /// </summary>
        /// <param name="position">目的位置</param>
        public void MoveForPosition(Vector3 position)
        {
            // 目的位置への方向ベクトルを計算
            Vector3 direction = position - selfInfo_.Position;
            direction.y = 0; // 水平方向のみ考慮
            direction.Normalize();

            // 戦車の前方向と､目的位置への方向の差を計算
            Vector3 tankForward = selfInfo_.Rotation * Vector3.forward;
            tankForward.y = 0; // 水平方向のみ考慮
            tankForward.Normalize();

            // 内積で進行方向の前後を計算
            float forwardDot = Vector3.Dot(tankForward, direction);

            // 外積で進行方向の左右を計算
            Vector3 cross = Vector3.Cross(tankForward, direction);
            float rightDot = cross.y; // y成分が上下方向の符号になる (平面のみ考慮しているため)

            // キャタピラのパワーを設定

            // まず､内積の値に応じて､内側と外側のパワーを設定
            float inSidePower = 0.0f;
            float outSidePower = 1.0f;  // 基本的に外側は全力で回転させる

            // とりあえず90度から0度までを(-1~1)で変化させる
            float t = Easing.InSine(Mathf.Abs(forwardDot));
            inSidePower = Mathf.Lerp(-1.0f, 1.0f, t); // -1 ~ 1 の範囲になる

            // 次に､内積の符号に応じて､前後を入れ替える
            inSidePower *= Mathf.Sign(forwardDot);
            outSidePower *= Mathf.Sign(forwardDot);

            // 外積の左右で､内側と外側を入れ替えてキャタピラのパワーを設定
            SXG_SetCaterpillarPower(
                rightDot >= 0 ? outSidePower : inSidePower,
                rightDot >= 0 ? inSidePower : outSidePower
            );
        }

        /// <summary>
        /// タンクの情報を取得する
        /// </summary>
        private void GetTanksInfo()
        {
            // すべてのタンクの情報を取得
            tankInfos_ = SXG_GetAllTanksInfo();
            // 自分のタンクの情報を取得 (0番目が自分)
            selfInfo_ = tankInfos_[0];
        }

        /// <summary>
        /// 敵に対してタレットを向ける (狙撃には向いてないことがわかった)
        /// </summary>
        /// <param name="turretIndex">タレットのIndex</param>
        /// <param name="enemyIndex">敵のIndex</param>
        /// <returns>有効な敵に照準が合っているか</returns>
        private bool TargetTurretForEnemy(Int32 turretIndex, Int32 enemyIndex)
        {
            // タレットが有効な値か
            if (turretIndex < 0 || turretIndex >= SXG_GetCountOfMyTurrets())
            {
                // 無効な値であったならfalseを返す
                return false;
            }

            // 敵が有効な値か
            if (enemyIndex < 0 || enemyIndex >= tankInfos_.Length)
            {
                // 無効な値であったならfalseを返す
                return false;
            }
            // 敵の情報を取得
            TankInfo enemyInfo = tankInfos_[enemyIndex];

            // 敵が無効な状態か
            if (enemyInfo.IsDefeated)
            {
                // 無効な状態であったならfalseを返す
                return false;
            }

            /// 敵の位置に､前回の位置からの移動量を加算して予測位置を計算

            // 前回位置が無い場合は現在位置を使用
            Vector3 enemyPrevPosition = prevPositions_ != null ? prevPositions_[enemyIndex] : enemyInfo.Position;
            // 過去位置と現在位置を用いて､秒間の移動量を計算
            Vector3 enemyVelocity = (enemyInfo.Position - enemyPrevPosition) / Time.deltaTime;

            // 相手の距離から､弾の到達時間を計算

            // 敵との距離
            float distanceToEnemy = Vector3.Distance(selfInfo_.Position, enemyInfo.Position);
            // 弾の速度
            const float kBulletSpeed = 38.0f;
            // 到達時間
            float timeToImpact = distanceToEnemy / kBulletSpeed;
            // 予測位置を計算
            Vector3 predictedPosition = enemyInfo.Position + enemyVelocity * timeToImpact;


            // 敵の位置にタレットを向ける
            SXG_RotateTurretToImpactPoint(turretIndex, predictedPosition);

            return true;
        }

        /// <summary>
        /// 最も近い敵タンクを探す
        /// </summary>
        /// <returns>最も近い敵のIndex</returns>
        private Int32 FindNearestEnemyTank()
        {
            // 最も近い敵タンクを探す
            Int32 nearestEnemyIndex = -1;
            // 最小距離の初期値を大きな値に設定
            float nearestDistanceSqr = float.MaxValue;
            // 敵タンクの情報は1番目以降に格納されているので基数を1から開始
            for (Int32 i = 1; i < tankInfos_.Length; i++)
            {
                // 敵タンクの情報を取得
                TankInfo enemyTank = tankInfos_[i];
                // 無敵状態でなく､撃破状態でない場合に処理を行う
                if (IsTankActive(enemyTank))
                {
                    // 自分のタンクと敵タンクの距離の二乗を計算
                    float distanceSqr = (enemyTank.Position - selfInfo_.Position).sqrMagnitude;
                    // 最小距離よりも近い場合に更新
                    if (distanceSqr < nearestDistanceSqr)
                    {
                        // 最小距離を更新
                        nearestDistanceSqr = distanceSqr;
                        // 最も近い敵タンクのインデックスを更新
                        nearestEnemyIndex = i;
                    }
                }
            }
            return nearestEnemyIndex;
        }

        /// <summary>
        /// 敵タンクを距離の近い順にソートしたインデックスリストを作成
        /// </summary>
        /// <returns>ソートされた敵タンクのインデックスリスト</returns>
        private List<Int32> SortNearestEnemyTank()
        {
            // 敵タンクを距離の近い順にソートしたインデックスリストを作成
            List<Int32> sortedEnemyIndices = new List<Int32>();
            // 敵タンクの距離とインデックスのマップを作成
            SortedList<float, Int32> distanceToIndexMap = new SortedList<float, Int32>();
            // 敵タンクの情報は1番目以降に格納されているので基数を1から開始
            for (Int32 i = 1; i < tankInfos_.Length; i++)
            {
                // 敵タンクの情報を取得
                TankInfo enemyTank = tankInfos_[i];
                // 無敵状態でなく､撃破状態でない場合に処理を行う
                if (IsTankActive(enemyTank))
                {
                    // 自分のタンクと敵タンクの距離の二乗を計算
                    float distanceSqr = (enemyTank.Position - selfInfo_.Position).sqrMagnitude;
                    // 重複する距離を避けるために微小な値を加算
                    while (distanceToIndexMap.ContainsKey(distanceSqr))
                    {
                        distanceSqr += 0.0001f; // 重複を避けるために微小な値を加算
                    }
                    // 距離とインデックスのマップに追加
                    distanceToIndexMap.Add(distanceSqr, i);
                }
            }
            // もし距離が1つしかなかったらそのインデックスを返す
            if (distanceToIndexMap.Count == 1)
            {
                sortedEnemyIndices.Add(distanceToIndexMap.Values[0]);
                return sortedEnemyIndices;
            }

            // ソートされたインデックスリストを作成
            foreach (var pair in distanceToIndexMap)
            {
                // インデックスをリストに追加
                sortedEnemyIndices.Add(pair.Value);
            }

            return sortedEnemyIndices;

        }

        /// <summary>
        /// すべてのタンクの情報
        /// </summary>
        private TankInfo[] tankInfos_ = null;

        /// <summary>
        /// 前回のタンクの位置
        /// </summary>
        private Vector3[] prevPositions_ = null;

        private Vector3[] spawnPoint_;

        /// <summary>
        /// プレイヤの状態コンテキスト
        /// </summary>
        public PlayerStateContext stateContext_ = new();

        private Queue<Vector3> prevPositionQueue_ = new();

        public Vector3 Position { get { return selfInfo_.Position; } }

        /// <summary>
        /// 自分のタンクの情報
        /// </summary>
        private TankInfo selfInfo_;

        private Vector3 moveTargetPosition_;

        private float fireCooldown_ = 0.0f; // 発射クールダウンタイム

        private MovingTarget movingTarget_ = new(); // 移動目標

        public const float kFireInterval_ = 1.5f; // 発射間隔

        public const float kStageRadius_ = 45.0f; // ステージの半径(目測)
    }
}