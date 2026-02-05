using TMPro;
using UnityEngine;


namespace SXG2025
{


    public class PromoCardRenderCamera : MonoBehaviour
    {
        [SerializeField] private Transform m_targetPoint;
        [SerializeField] private GameObject m_sampleTankObj;
        [SerializeField] private UI.ResultTankName m_tankNameUI;

        [SerializeField] private TextMeshProUGUI m_textName;
        [SerializeField] private TextMeshProUGUI m_textSpec;

        [SerializeField] private Transform m_concreteTr;

        [SerializeField, Range(1.01f, 2.0f)] private float m_framingMargin = 1.12f;
        [SerializeField] private float m_minCameraDistance = 2.5f;
        [SerializeField] private float m_maxCameraDistance = 12.0f;
        [SerializeField] private float m_cameraExtendBorderDistance = 20.0f;    // カメラを広げるかどうかの境界値 

        private Camera m_camera = null;
        private RenderTexture [] m_renderTexture = null;

        private Vector3 m_defaultTargetPointPosition = Vector3.zero;
        private float m_defaultTargetPointDistance = 0;
        private Vector3 m_defaultConcreteLocalPosition = Vector3.zero;

        private static PromoCardRenderCamera ms_instance = null;

        public static PromoCardRenderCamera Instance => ms_instance;


        public Texture GetTexture(int teamNo)
        {
            return m_renderTexture[teamNo];
        }

        private int m_teamNo = 0;


        private void Awake()
        {
            ms_instance = this;

            m_camera = GetComponent<Camera>();

            // RenderTextureを複製 
            m_renderTexture = new RenderTexture[GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE];
            for (int i=0; i < m_renderTexture.Length; ++i)
            {
                m_renderTexture[i] = Instantiate(m_camera.targetTexture);
                m_renderTexture[i].name = string.Format("PromoCardRenderTexture:{0}", i);
            }

            // 初期位置 
            m_defaultTargetPointPosition = m_targetPoint.localPosition;
            m_defaultTargetPointDistance = m_defaultTargetPointPosition.magnitude;
            m_defaultConcreteLocalPosition = m_concreteTr.localPosition;

            // 活動停止 
            gameObject.SetActive(false);

            // １つだけ 
            DontDestroyOnLoad(gameObject);
        }


        /// <summary>
        /// @memo MemoryProfilerで確認してもRenderTextureが溜まることは無かったが保険処理を入れておく
        /// </summary>
        private void OnDestroy()
        {
            if (m_camera != null)
            {
                m_camera.targetTexture = null;
            }

            if (m_renderTexture != null)
            {
                foreach (var renderTex in m_renderTexture)
                {
                    if (renderTex != null)
                    {
                        renderTex.Release();
                        Destroy(renderTex);
                    }
                }
                m_renderTexture = null;
            }
        }

        /// <summary>
        /// レンダリング開始 
        /// </summary>
        /// <param name="targetCharaTr"></param>
        /// <param name="cameraMode"></param>
        public void SetRendering(Transform targetCharaTr, ComPlayerBase comPlayer, BattleTanksManager.TankBaseSpec spec, int teamNo, Color teamColor)
        {
            // サンプルモデルは消す 
            if (m_sampleTankObj)
            {
                m_sampleTankObj.SetActive(false);
            }

            // TargetPointを戦車の場所に合わせる 
            m_targetPoint.localPosition = m_defaultTargetPointPosition;
            transform.rotation = targetCharaTr.rotation * Quaternion.Inverse(m_targetPoint.localRotation);
            transform.position = targetCharaTr.position - (transform.rotation * m_targetPoint.localPosition);

            // 戦車の大きさに合わせてカメラの距離だけ自動調整
            AdjustCameraDistanceToFit(targetCharaTr);
            
            // UIに設定 
            m_teamNo = teamNo;
            m_tankNameUI.Setup(comPlayer, m_teamNo);

            // 名前 
            string teamHex = ColorUtility.ToHtmlStringRGB(teamColor);

            m_textName.text =
                $"<size=80%><color=#E0E0E0>所属</color></size>\n" +
                $"<b><size=110%><color=#{teamHex}>{comPlayer.Organization}</color></size></b>\n" +
                $"<line-height=30%>\n</line-height>" +
                $"<size=80%><color=#E0E0E0>制作者名</color></size>\n" +
                $"<b><size=110%><color=#{teamHex}>{comPlayer.YourName}</color></size></b>";
            //m_textName.text = string.Format("所属：{0}\n名前：{1}", comPlayer.Organization, comPlayer.YourName);
            //m_textName.color = teamColor;
            // スペック 
            //m_textSpec.text = string.Format("コスト：{0}\n砲塔：{1}基\n回転部：{2}基\n装甲：{3}部\n重量：{4}kg",
            //    spec.m_cost, spec.m_countOfTurrets, spec.m_countOfRotators, spec.m_countOfArmors, spec.m_mass);
            m_textSpec.text =
                $"<size=80%><color=#CFCFCF>コスト</color></size> <b><size=115%>{spec.m_cost}</size></b><size=80%>pt</size>\n" +
                $"<size=80%><color=#CFCFCF>砲塔</color></size> <b><size=115%>{spec.m_countOfTurrets}</size></b><size=80%>基</size>\n" +
                $"<size=80%><color=#CFCFCF>回転部</color></size> <b><size=115%>{spec.m_countOfRotators}</size></b><size=80%>基</size>\n" +
                $"<size=80%><color=#CFCFCF>装甲</color></size> <b><size=115%>{spec.m_countOfArmors}</size></b><size=80%>部</size>\n" +
                $"<size=80%><color=#CFCFCF>質量</color></size> <b><size=115%>{spec.m_mass.ToString("N0")}</size></b>";

            // 描画 
            RenderOnce(teamNo);
        }

        private void RenderOnce(int teamNo)
        {
            // カメラを有効化 
            m_camera.enabled = true;
            gameObject.SetActive(true);

            // RenderTexture割り当て 
            m_camera.targetTexture = m_renderTexture[teamNo];

            // UIを即時反映 
            Canvas.ForceUpdateCanvases();

            // １回だけ描画 
            m_camera.Render();

            // 止める 
            m_camera.enabled = false;
            gameObject.SetActive(false);

            // 剥がす 
            m_camera.targetTexture = null;
        }

        private void AdjustCameraDistanceToFit(Transform tankRoot)
        {
            if (m_camera == null || tankRoot == null) return;

            var renderers = tankRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            // カメラは角度固定なので、距離だけ変える
            float fovY = m_camera.fieldOfView * Mathf.Deg2Rad;
            float fovX = 2f * Mathf.Atan(Mathf.Tan(fovY * 0.5f) * m_camera.aspect);

            // 球近似（安定・実装が簡単）
            //float radius = b.extents.magnitude * m_framingMargin;
            var extents = new Vector3(b.extents.x * 9.0f / 16.0f, b.extents.y, b.extents.z * 9.0f / 16.0f);
            float radius = extents.magnitude * m_framingMargin;

            // カメラを引く必要があれば対処 
            if (radius < m_cameraExtendBorderDistance)
            {
                // 中心：基本はBounds中心が「盛り」(牛)にも強い
                Vector3 center = b.center;
                center.y += b.size.y * 0.1f;    // 少しだけ上に寄せる 

                float distY = radius / Mathf.Tan(fovY * 0.5f);
                float distX = radius / Mathf.Tan(fovX * 0.5f);

                float dist = Mathf.Max(distX, distY);
                dist = Mathf.Clamp(dist, m_minCameraDistance, m_maxCameraDistance);

                // forwardは「カメラが向いている方向」なので、centerの後ろに下がる
                Vector3 forward = m_camera.transform.forward;
                m_camera.transform.position = center - forward * dist;

                // 念のため中心を見る（角度固定運用でも、中心が動くのでLookAtはした方が安定）
                m_camera.transform.LookAt(center);
            }

            // concrete位置調整 
            m_concreteTr.position = tankRoot.position + m_defaultConcreteLocalPosition;

        }



    }

}