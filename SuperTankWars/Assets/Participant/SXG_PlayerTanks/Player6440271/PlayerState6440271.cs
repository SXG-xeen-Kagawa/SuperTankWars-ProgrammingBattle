using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
namespace Player6440271
{

    public class MovingTarget
    {
        public MovingTarget() { }


        /// <summary>
        /// 先頭を取得
        /// </summary>
        /// <returns>先頭の座標</returns>
        public Vector3 GetFront() { return targets_.Count != 1 ? targets_[0] : Vector3.zero; }

        /// <summary>
        /// 更新処理
        /// </summary>
        public Vector3 Update(Vector3 position)
        {
            // 現在の目的地が存在しなかったら
            if (targets_.Count == 0)
            {
                // プレイヤの座標を返す
                return position;
            }

            // 配列から最も近い場所を探す
            Int32 nearestIndex = -1;
            float nearestDistance = float.MaxValue;
            for (Int32 i = 0; i < targets_.Count; i++)
            {
                float distance = Vector3.Distance(position, targets_[i]);
                // 距離が最も近い場所を探す
                if (nearestDistance > distance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }

            // 最も近い場所が見つからなかったら
            if (nearestIndex == -1)
            {
                // プレイヤの座標を返す
                return position;
            }

            // 最も近い場所が見つかったら､その場所を先頭にしてリストを更新する
            targets_.RemoveRange(0, nearestIndex);

            // 先頭を返す
            return targets_[0];
        }

        /// <summary>
        /// 目標地点のリスト
        /// </summary>
        List<Vector3> targets_ = new();
    }


    /// <summary>
    /// プレイヤの行動状態
    /// </summary>
    public enum ActionState
    {
        kNormalState,   // 通常状態
        kRecoveryState, // 転覆からの復帰
        kAirRecoveryState,  // ステージ外からの復帰
        kArielState,    // 空中状態
        kOneOnOneState, // 1on1 状態


        Count           // 状態の数
    }

    public interface IPlayerState
    {
        // このクラスの状態を取得する
        ActionState State { get; }

        // 状態開始時に最初に実行される
        void Entry();

        // フレームごとに実行される
        void Update();

        // 状態終了時に実行される
        void Exit();
    }


    public class PlayerStateNormal : IPlayerState
    {
        private Player6440271 player_;
        public ActionState State => ActionState.kNormalState;
        public PlayerStateNormal(Player6440271 player) => player_ = player;
        public void Entry()
        {
        }
        public void Update()
        {
            // 敵に砲塔を向ける
            player_.FacingTurretForEnemy();
            // 砲塔を順次発射する
            player_.SequentiallyTurretFire();

            // 移動処理を行う
            player_.MoveForTarget();


            var position = player_.Position;
            position.y = 0;

            // もしステージ外にいるなら､復帰状態に遷移する
            if (Player6440271.kStageRadius_ + 3.0f < position.magnitude)
            {
                player_.stateContext_.ChangeState(ActionState.kAirRecoveryState);
                return;
            }

            // もし転覆していたら復帰状態に遷移する
            if (player_.GetIsUprightScore() < 0.0f || (Player6440271.kStageRadius_ - 1.5f) < position.magnitude)
            {
                player_.stateContext_.ChangeState(ActionState.kRecoveryState);
                return;
            }
        }
        public void Exit()
        {
        }
    }


    public class PlayerStateRecovery : IPlayerState
    {
        private Player6440271 player_;
        public ActionState State => ActionState.kRecoveryState;
        public PlayerStateRecovery(Player6440271 player) => player_ = player;
        public void Entry()
        {
        }
        public void Update()
        {
            // 転覆から復帰する
            player_.FacingTurretRecovery();
            // 砲塔を順次発射する
            player_.SequentiallyTurretFire();
            // もし起き上がったら通常状態に遷移する
            if (player_.GetIsUprightScore() > 0.0f)
            {
                player_.stateContext_.ChangeState(ActionState.kNormalState);
            }
        }
        public void Exit()
        {
        }
    }

    public class PlayerStateAirRecovery : IPlayerState
    {
        private Player6440271 player_;
        public ActionState State => ActionState.kAirRecoveryState;
        public PlayerStateAirRecovery(Player6440271 player) => player_ = player;
        public void Entry()
        {
        }
        public void Update()
        {
            // ステージ外から復帰する
            player_.FacingTurretStageOutSide();
            // 砲塔を順次発射する
            player_.SequentiallyTurretFire();

            var position = player_.Position;
            // もし空中にいたら､空中状態に遷移する
            if (30.0f < position.y)
            {
                player_.stateContext_.ChangeState(ActionState.kArielState);
                return;
            }

            position.y = 0;

            // もし帰ってこられたら通常状態に遷移する
            if (Player6440271.kStageRadius_ - 5.0f > position.magnitude)
            {
                player_.stateContext_.ChangeState(ActionState.kNormalState);
            }
        }
        public void Exit()
        {
        }
    }

    public class PlayerStateAriel : IPlayerState
    {
        private Player6440271 player_;
        public ActionState State => ActionState.kArielState;
        public PlayerStateAriel(Player6440271 player) => player_ = player;
        public void Entry()
        {
        }
        public void Update()
        {
            // 大砲を空中モードにする
            player_.FacingTurretArielMode();
            // 砲塔を順次発射する
            player_.SequentiallyTurretFire();

            var position = player_.Position;

            // もし上空から切れたら､復帰状態に遷移する
            if (position.y < 20.0f)
            {
                player_.stateContext_.ChangeState(ActionState.kAirRecoveryState);
            }
        }
        public void Exit()
        {
        }
    }

    public class PlayerStateOneOnOne : IPlayerState
    {
        private Player6440271 player_;
        public ActionState State => ActionState.kOneOnOneState;
        public PlayerStateOneOnOne(Player6440271 player) => player_ = player;
        public void Entry()
        {
        }
        public void Update()
        {
            // 砲塔を順次発射する
            player_.SequentiallyTurretFire();
        }
        public void Exit()
        {
        }
    }

    public class PlayerStateContext
    {
        // 現在の状態
        private IPlayerState currentState_ = null;

        // 状態のテーブル
        private Dictionary<ActionState, IPlayerState> stateTable_;


        public void Init(Player6440271 player, ActionState initState)
        {
            // 既に初期化されている場合は何もしない
            if (currentState_ != null) { return; }
            // 状態テーブルを初期化する
            Dictionary<ActionState, IPlayerState> table = new Dictionary<ActionState, IPlayerState>()
            {
                { ActionState.kNormalState, new PlayerStateNormal(player) },
                { ActionState.kRecoveryState, new PlayerStateRecovery(player) },
                { ActionState.kArielState, new PlayerStateAriel(player) },
                { ActionState.kAirRecoveryState, new PlayerStateAirRecovery(player) },
                { ActionState.kOneOnOneState, new PlayerStateOneOnOne(player) },
            };
            stateTable_ = table;
            // 初期状態に遷移する
            ChangeState(initState);
        }

        public void ChangeState(ActionState newState)
        {
            // もし現在の状態と同じなら何もしない
            if (currentState_ != null && currentState_.State == newState)
            {
                return;
            }

            // 現在の状態を終了する
            currentState_?.Exit();
            // 新しい状態に変更する
            currentState_ = stateTable_[newState];
            // 新しい状態を開始する
            currentState_.Entry();
        }

        // 現在の状態をUpdateする
        public void Update() => currentState_?.Update();
    }
}