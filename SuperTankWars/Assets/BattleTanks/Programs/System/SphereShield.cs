using UnityEngine;

namespace SXG2025
{
    public class SphereShield : MonoBehaviour
    {
        private static readonly int ShieldColorId =
            Shader.PropertyToID("_ShieldColor");

        public delegate bool CanReduceRemainingTimeDelegate();
        private CanReduceRemainingTimeDelegate m_canReduceRemainingTimeFunc = null;

        public delegate void FinishedShieldDelegate();
        private FinishedShieldDelegate m_finishedShieldFunc = null;

        private Transform m_targetTr = null;
        private float m_remainingTime = 0;
        private Vector3 m_offset = Vector3.zero;
        private float m_time = 0;

        void Start()
        {
            m_time = Random.Range(0.0f, Mathf.PI * 2.0f);
        }

        void LateUpdate()
        {
            // バリアを回転させる
            Quaternion newRot = transform.rotation *
                Quaternion.AngleAxis(180.0f * Time.deltaTime, Vector3.up);
            m_time += Mathf.PI * Time.deltaTime;

            // 戦車の位置に合わせる
            if (m_targetTr != null)
            {
                Vector3 newPos = m_targetTr.TransformPoint(m_offset);
                transform.SetPositionAndRotation(newPos, newRot);
            }

            // バリアの残り時間を更新する
            if (m_canReduceRemainingTimeFunc != null)
            {
                if (m_canReduceRemainingTimeFunc.Invoke())
                {
                    m_remainingTime -= Time.deltaTime;
                }

                if (m_remainingTime <= 0)
                {
                    m_finishedShieldFunc?.Invoke();
                    Destroy(gameObject);
                }
            }
        }

        /// <summary>
        /// バリアをセットアップする。
        /// </summary>
        /// <param name="teamColor">バリアに適用するチームカラー。</param>
        public void Setup(
            Transform tankTr,
            Bounds tankBounds,
            CanReduceRemainingTimeDelegate canReduceFunc,
            FinishedShieldDelegate finishedShieldFunc,
            Color teamColor)
        {
            m_targetTr = tankTr;
            m_remainingTime =
                GameDataHolder.Instance.DataGame.m_invincibleTimeAfterSpawn;
            m_canReduceRemainingTimeFunc = canReduceFunc;
            m_finishedShieldFunc = finishedShieldFunc;

            // このSphereだけのシェーダーカラーを設定する。
            Renderer shieldRenderer = GetComponent<Renderer>();
            if (shieldRenderer != null)
            {
                MaterialPropertyBlock propertyBlock =
                    new MaterialPropertyBlock();

                // 既存の個別設定があれば維持する。
                shieldRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(ShieldColorId, teamColor);
                shieldRenderer.SetPropertyBlock(propertyBlock);
            }
            else
            {
                Debug.LogWarning(
                    "SphereShield: 同じGameObjectにRendererがありません。",
                    this);
            }

            // オフセット位置
            float boundsSize = 0;
            if (tankBounds.size == Vector3.zero)
            {
                m_offset = Vector3.zero;
            }
            else
            {
                m_offset = m_targetTr.InverseTransformPoint(tankBounds.center);
                m_offset.y = Mathf.Max(0, m_offset.y - 0.5f);
                boundsSize = tankBounds.size.magnitude;
            }

            float radius = Mathf.Clamp(
                boundsSize,
                GameDataHolder.Instance.DataGame.m_minInvinsibleShieldRadius,
                GameDataHolder.Instance.DataGame.m_maxInvinsibleShieldRadius);

            transform.localScale = Vector3.one * radius;
        }
    }
}