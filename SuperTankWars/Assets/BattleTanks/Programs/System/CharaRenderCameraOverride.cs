using UnityEngine;

namespace SXG2025
{
    public class CharaRenderCameraOverride : MonoBehaviour
    {
        [Header("有効な場合、このSphere設定をRenderTexture用の収まり判定に使用")]
        [SerializeField] private bool m_useOverrideBounds = true;

        [Header("このオブジェクトのローカル座標での中心")]
        [SerializeField] private Vector3 m_boundsCenter = Vector3.zero;

        [Header("映したい半径")]
        [SerializeField] private float m_boundsRadius = 2.0f;

        public bool UseOverrideBounds => m_useOverrideBounds;
        public Vector3 BoundsCenter => m_boundsCenter;
        public float BoundsRadius => m_boundsRadius;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!m_useOverrideBounds)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(m_boundsCenter, m_boundsRadius);
            Gizmos.matrix = oldMatrix;
        }
#endif
    }
}