using UnityEngine;
using UnityEngine.UI;


namespace SXG2025
{
    namespace UI
    {


        public class DestroiedTankUI : MonoBehaviour
        {
            private Camera m_camera = null;
            private Vector3 m_targetPosition;
            private RectTransform m_rectTr = null;

            // Start is called once before the first execution of Update after the MonoBehaviour is created
            void Start()
            {
                m_rectTr = GetComponent<RectTransform>();
            }


            public void AnimEvent_Finished()
            {
                Destroy(gameObject);
            }

            /// <summary>
            /// 対象のオブジェクトに紐づけ 
            /// </summary>
            /// <param name="targetTankObj"></param>
            internal void Link(Camera camera, Vector3 targetPosition)
            {
                m_camera = camera;
                m_targetPosition = targetPosition;
            }

            private void Update()
            {
                if (m_camera != null)
                {
                    Vector2 viewp = m_camera.WorldToViewportPoint(m_targetPosition);
                    m_rectTr.anchoredPosition = new Vector2(
                        (float)Constants.CANVAS_WIDTH * viewp.x,
                        (float)Constants.CANVAS_HEIGHT * viewp.y);
                }
            }
        }


    }


}

