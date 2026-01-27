using UnityEngine;

namespace SXG2025
{

    public partial class ComPlayerBase : MonoBehaviour
    {
#if UNITY_EDITOR
        private DebugMarker m_marker = null;
#endif


        /// <summary>
        /// デバッグ用：座標マーカーを表示
        /// </summary>
        protected void SXG_DebugDrawPositionMarker(Vector3 targetPosition, bool isDraw=true)
        {
#if UNITY_EDITOR
            if (isDraw)
            {
                // 生成 
                if (m_marker == null)
                {
                    m_marker = Instantiate(PrefabHolder.Instance.DebugMarkerPrefab);
                    m_marker.SetTeamNo(m_id);
                } else
                {
                    if (m_marker.gameObject.activeSelf==false)
                    {
                        m_marker.gameObject.SetActive(true);
                    }
                }
                m_marker.transform.position = targetPosition;
            } else
            {
                if (m_marker != null)
                {
                    if (m_marker.gameObject.activeSelf)
                    {
                        m_marker.gameObject.SetActive(false);
                    }
                }
            }
#endif
        }

        /// <summary>
        /// デバッグ表示を削除 
        /// </summary>
        public void DeleteDebugDraw()
        {
#if UNITY_EDITOR
            if (m_marker != null)
            {
                Destroy(m_marker.gameObject);
            }
#endif
        }

    }

}