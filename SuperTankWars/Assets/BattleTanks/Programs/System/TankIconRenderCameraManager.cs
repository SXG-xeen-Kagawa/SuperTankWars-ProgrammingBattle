using UnityEngine;

namespace SXG2025
{
    public class TankIconRenderCameraManager : MonoBehaviour
    {
        [System.Serializable]
        public class SlotData
        {
            private TankIconRenderCamera m_renderCamera = null;

            private Transform m_currentTargetTr = null;

            public TankIconRenderCamera RenderCamera => m_renderCamera;
            public Transform CurrentTargetTr => m_currentTargetTr;

            public void SetCamera(TankIconRenderCamera renderCamera)
            {
                m_renderCamera = renderCamera;
            }

            public void SetTarget(Transform targetTr)
            {
                m_currentTargetTr = targetTr;
            }

            public void ClearTarget()
            {
                m_currentTargetTr = null;
            }
        }

        [Header("Player Slot Fixed Cameras")]
        private SlotData[] m_slots = new SlotData[GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE];

        [SerializeField] private TankIconRenderCamera m_tankIconRenderCameraPrefab = null;

        private void Awake()
        {
            ValidateSlots();
            StopAllRendering();
            
            // ４つインスタンスを作る 
            for (int i=0; i < m_slots.Length; ++i)
            {
                var iconCamera = Instantiate(m_tankIconRenderCameraPrefab, this.transform);
                iconCamera.name = string.Format("TankIconRenderCamera_{0}", i);

                m_slots[i] = new();
                m_slots[i].SetCamera(iconCamera);
            }
        }

        /// <summary>
        /// プレイヤースロットに対応する戦車を登録する
        /// </summary>
        public void SetTarget(int playerIndex, Transform targetTankTr, bool startRenderingImmediately = false)
        {
            if (!IsValidPlayerIndex(playerIndex))
            {
                Debug.LogError($"TankIconRenderCameraManager.SetTarget : playerIndex が不正です。 playerIndex={playerIndex}", this);
                return;
            }

            var slot = m_slots[playerIndex];
            if (slot == null || slot.RenderCamera == null)
            {
                Debug.LogError($"TankIconRenderCameraManager.SetTarget : Slot または Camera が未設定です。 playerIndex={playerIndex}", this);
                return;
            }

            slot.SetTarget(targetTankTr);

            if (targetTankTr == null)
            {
                slot.RenderCamera.StopRendering();
                return;
            }

            if (startRenderingImmediately)
            {
                slot.RenderCamera.StartRendering(targetTankTr);
            }
            else
            {
                slot.RenderCamera.StopRendering();
            }
        }

        /// <summary>
        /// プレイヤースロットの現在ターゲットを取得する
        /// </summary>
        public Transform GetTarget(int playerIndex)
        {
            if (!IsValidPlayerIndex(playerIndex))
            {
                return null;
            }

            return m_slots[playerIndex]?.CurrentTargetTr;
        }

        /// <summary>
        /// プレイヤースロットの RenderTexture を取得する
        /// </summary>
        public RenderTexture GetTexture(int playerIndex)
        {
            if (!IsValidPlayerIndex(playerIndex))
            {
                return null;
            }

            var camera = m_slots[playerIndex]?.RenderCamera;
            return camera != null ? camera.Texture : null;
        }

        /// <summary>
        /// プレイヤースロットの Camera コンポーネントを取得する
        /// </summary>
        public TankIconRenderCamera GetRenderCamera(int playerIndex)
        {
            if (!IsValidPlayerIndex(playerIndex))
            {
                return null;
            }

            return m_slots[playerIndex]?.RenderCamera;
        }

        /// <summary>
        /// このプレイヤースロットの描画を開始する
        /// </summary>
        public void StartRendering(int playerIndex)
        {
            if (!IsValidPlayerIndex(playerIndex))
            {
                return;
            }

            var slot = m_slots[playerIndex];
            if (slot == null || slot.RenderCamera == null)
            {
                return;
            }

            if (slot.CurrentTargetTr == null)
            {
                return;
            }

            slot.RenderCamera.StartRendering(slot.CurrentTargetTr);
        }

        /// <summary>
        /// このプレイヤースロットの描画を停止する
        /// </summary>
        public void StopRendering(int playerIndex)
        {
            if (!IsValidPlayerIndex(playerIndex))
            {
                return;
            }

            var slot = m_slots[playerIndex];
            if (slot == null || slot.RenderCamera == null)
            {
                return;
            }

            slot.RenderCamera.StopRendering();
        }

        /// <summary>
        /// 現在のターゲットで即時再描画する
        /// </summary>
        public void RenderNow(int playerIndex)
        {
            if (!IsValidPlayerIndex(playerIndex))
            {
                return;
            }

            var slot = m_slots[playerIndex];
            if (slot == null || slot.RenderCamera == null)
            {
                return;
            }

            slot.RenderCamera.RenderNow();
        }

        /// <summary>
        /// 登録済みターゲットは残したまま、全スロットの描画だけ止める
        /// </summary>
        public void StopAllRendering()
        {
            for (int i = 0; i < m_slots.Length; i++)
            {
                var slot = m_slots[i];
                if (slot == null || slot.RenderCamera == null)
                {
                    continue;
                }

                slot.RenderCamera.StopRendering();
            }
        }

        /// <summary>
        /// ターゲットも含めて全スロットをクリアする
        /// </summary>
        public void ClearAllTargets()
        {
            for (int i = 0; i < m_slots.Length; i++)
            {
                var slot = m_slots[i];
                if (slot == null)
                {
                    continue;
                }

                if (slot.RenderCamera != null)
                {
                    slot.RenderCamera.StopRendering();
                }

                slot.ClearTarget();
            }
        }

        /// <summary>
        /// 4人分まとめてターゲット登録する
        /// 配列長は 4 想定
        /// </summary>
        public void SetTargets(Transform[] targetTankArray, bool startRenderingImmediately = false)
        {
            if (targetTankArray == null)
            {
                Debug.LogError("TankIconRenderCameraManager.SetTargets : targetTankArray が null です。", this);
                return;
            }

            int count = Mathf.Min(targetTankArray.Length, GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE);
            for (int i = 0; i < count; i++)
            {
                SetTarget(i, targetTankArray[i], startRenderingImmediately);
            }

            for (int i = count; i < GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE; i++)
            {
                SetTarget(i, null, false);
            }
        }

        /// <summary>
        /// すでに登録済みターゲットを全スロットで描画開始する
        /// </summary>
        public void StartRenderingAllRegisteredTargets()
        {
            for (int i = 0; i < GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE; i++)
            {
                StartRendering(i);
            }
        }

        private void ValidateSlots()
        {
            if (m_slots == null || m_slots.Length != GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE)
            {
                var newSlots = new SlotData[GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE];

                for (int i = 0; i < GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE; i++)
                {
                    if (m_slots != null && i < m_slots.Length && m_slots[i] != null)
                    {
                        newSlots[i] = m_slots[i];
                    }
                    else
                    {
                        newSlots[i] = new SlotData();
                    }
                }

                m_slots = newSlots;
            }

            for (int i = 0; i < m_slots.Length; i++)
            {
                if (m_slots[i] == null)
                {
                    m_slots[i] = new SlotData();
                }
            }
        }

        private bool IsValidPlayerIndex(int playerIndex)
        {
            return playerIndex >= 0 && playerIndex < GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE;
        }
    }
}