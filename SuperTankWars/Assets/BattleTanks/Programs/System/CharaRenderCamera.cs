using UnityEngine;


namespace SXG2025
{


    public class CharaRenderCamera : MonoBehaviour
    {
        private Camera m_camera = null;
        private RenderTexture m_renderTexture = null;

        public RenderTexture Texture => m_renderTexture;

        public enum CameraMode
        {
            ChallengerIntro,        // 挑戦者紹介 
            Loser,
        }

        [System.Serializable]
        public class CameraData
        {
            public Vector3 m_viewPoint = Vector3.zero;  // 視点 
            public Vector3 m_viewAngle = Vector3.zero;  // カメラ角度 
            public bool m_isRotate = false;
        }
        [SerializeField] private CameraData[] m_cameraData;

        private CameraMode m_cameraMode = CameraMode.ChallengerIntro;
        private Transform m_targetObjTr = null;
        private Vector3 m_centerOffset = Vector3.zero;
        private float m_rotationTimer = 0;
        private bool m_canCameraRotation = true;


        private void Awake()
        {
            m_camera = GetComponent<Camera>();

            // RenderTextureを複製 
            m_renderTexture = Instantiate(m_camera.targetTexture);
            m_renderTexture.name = "CharaRenderCameraTexture";
            m_camera.targetTexture = m_renderTexture;

            // 活動停止 
            gameObject.SetActive(false);
        }


        /// <summary>
        /// レンダリング開始 
        /// </summary>
        /// <param name="targetCharaTr"></param>
        /// <param name="cameraMode"></param>
        public void StartRendering(Transform targetCharaTr, Vector3 centerOffset, CameraMode cameraMode)
        {
            gameObject.SetActive(true);
            m_cameraMode = cameraMode;
            m_targetObjTr = targetCharaTr;
            m_centerOffset = centerOffset;
            m_rotationTimer = 0;

            // 一回座標更新しておく 
            UpdateCameraPositionAndRotation();
        }

        public void StopRendering()
        {
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            UpdateCameraPositionAndRotation();
        }

        private void UpdateCameraPositionAndRotation()
        {
            const float ROTATION_CYCLE = 20.0f;

            if (!m_canCameraRotation)
            {
                return;
            }

            if ((int)m_cameraMode < m_cameraData.Length && m_targetObjTr != null)
            {
                var data = m_cameraData[(int)m_cameraMode];
                Vector3 localPosition = data.m_viewPoint;
                Vector3 centerPoint = Vector3.zero;
                Vector3 upVector = Vector3.up;
                Vector3 viewPoint = Vector3.zero;

                if (data.m_isRotate)
                {
                    centerPoint = m_targetObjTr.TransformPoint(m_centerOffset);
                    m_rotationTimer += Time.deltaTime;
                    upVector = m_targetObjTr.up;
                    viewPoint = m_targetObjTr.position
                        + m_targetObjTr.rotation * Quaternion.AngleAxis(m_rotationTimer * 360.0f / ROTATION_CYCLE, Vector3.down) * localPosition;
                } else
                {
                    centerPoint = m_targetObjTr.position + m_centerOffset;
                    viewPoint = centerPoint + data.m_viewPoint;
                }
                Quaternion lookRotation = Quaternion.LookRotation(centerPoint - viewPoint, upVector);
                transform.SetPositionAndRotation(viewPoint, lookRotation);

                /*
                transform.SetPositionAndRotation(
                    m_targetObjTr.TransformPoint(localPosition),
                    m_targetObjTr.rotation * localRotation);
                */
            }
        }

        internal void StopCameraRotation()
        {
            m_canCameraRotation = false;
        }


    }



}


