using UnityEngine;

namespace SXG2025
{
    [RequireComponent(typeof(Camera))]
    public class TankIconRenderCamera : MonoBehaviour
    {
        private Camera m_camera = null;
        private RenderTexture m_renderTexture = null;
        private Transform m_targetObjTr = null;

        public RenderTexture Texture => m_renderTexture;
        public bool IsRendering => gameObject.activeSelf && m_targetObjTr != null;

        [System.Serializable]
        public class CameraViewData
        {
            [Header("Overrideが無い場合の注視点オフセット")]
            public Vector3 m_centerOffset = new Vector3(0.0f, 0.8f, 0.0f);

            [Header("参照カメラ未指定時の予備視線方向")]
            public Vector3 m_fallbackViewDirection = new Vector3(0.0f, -0.6f, 0.8f);

            [Header("焦点からの基本距離")]
            public float m_distance = 10.0f;
        }

        [Header("View")]
        [SerializeField] private CameraViewData m_viewData = new CameraViewData();
        [SerializeField] private Camera m_referenceCamera = null;
        [SerializeField] private Color m_cameraBackgroundColor = Color.black;

        [Header("RenderTexture")]
        [SerializeField] private int m_textureWidth = 256;
        [SerializeField] private int m_textureHeight = 256;
        [SerializeField] private int m_depthBuffer = 16;
        [SerializeField] private RenderTextureFormat m_renderTextureFormat = RenderTextureFormat.ARGB32;
        [SerializeField] private FilterMode m_filterMode = FilterMode.Bilinear;

        [Header("Culling")]
        [SerializeField] private LayerMask m_rendererLayerMask = ~0;

        [Header("Update")]
        [SerializeField, Range(1, 6)] private int m_updateIntervalFrames = 2;

        [Header("Auto Fit")]
        [SerializeField] private bool m_enableAutoFit = true;
        [SerializeField] private float m_baseFitRadius = 2.0f;
        [SerializeField] private float m_fitPadding = 1.15f;
        [SerializeField] private bool m_ignoreParticleRenderer = true;

        private int m_updateFrameOffset = 0;

        private void Awake()
        {
            m_camera = GetComponent<Camera>();

            SetupCamera();
            CreateRenderTexture();

            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (m_camera != null)
            {
                m_camera.enabled = true;
            }
        }

        private void OnDisable()
        {
            if (m_camera != null)
            {
                m_camera.enabled = false;
            }
        }

        private void OnDestroy()
        {
            ReleaseRenderTexture();
        }

        public void StartRendering(Transform targetTankTr)
        {
            m_targetObjTr = targetTankTr;
            m_updateFrameOffset = Random.Range(0, Mathf.Max(1, m_updateIntervalFrames));

            gameObject.SetActive(true);

            UpdateCameraPositionAndRotation();
            m_camera.Render();
        }

        public void StopRendering()
        {
            m_targetObjTr = null;
            gameObject.SetActive(false);
        }

        public void RenderNow()
        {
            if (m_camera == null || m_targetObjTr == null)
            {
                return;
            }

            UpdateCameraPositionAndRotation();
            m_camera.Render();
        }

        private void LateUpdate()
        {
            if (m_targetObjTr == null)
            {
                return;
            }

            if (!ShouldUpdateThisFrame())
            {
                return;
            }

            UpdateCameraPositionAndRotation();
        }

        private bool ShouldUpdateThisFrame()
        {
            int interval = Mathf.Max(1, m_updateIntervalFrames);
            return ((Time.frameCount + m_updateFrameOffset) % interval) == 0;
        }

        private void SetupCamera()
        {
            if (m_camera == null)
            {
                return;
            }

            m_camera.allowHDR = false;
            m_camera.allowMSAA = false;
            m_camera.useOcclusionCulling = false;
            m_camera.clearFlags = CameraClearFlags.SolidColor;
            m_camera.backgroundColor = m_cameraBackgroundColor;
            m_camera.cullingMask = m_rendererLayerMask;
            m_camera.enabled = false;
        }

        private void CreateRenderTexture()
        {
            ReleaseRenderTexture();

            m_renderTexture = new RenderTexture(
                m_textureWidth,
                m_textureHeight,
                m_depthBuffer,
                m_renderTextureFormat);

            m_renderTexture.name = $"TankIconRenderTexture_{gameObject.name}";
            m_renderTexture.filterMode = m_filterMode;
            m_renderTexture.wrapMode = TextureWrapMode.Clamp;
            m_renderTexture.useMipMap = false;
            m_renderTexture.autoGenerateMips = false;
            m_renderTexture.Create();

            if (m_camera != null)
            {
                m_camera.targetTexture = m_renderTexture;
            }
        }

        private void ReleaseRenderTexture()
        {
            if (m_camera != null && m_camera.targetTexture == m_renderTexture)
            {
                m_camera.targetTexture = null;
            }

            if (m_renderTexture != null)
            {
                if (m_renderTexture.IsCreated())
                {
                    m_renderTexture.Release();
                }

                Destroy(m_renderTexture);
                m_renderTexture = null;
            }
        }

        private void UpdateCameraPositionAndRotation()
        {
            if (m_targetObjTr == null)
            {
                return;
            }

            Vector3 centerPoint = GetCenterPoint();
            Vector3 forward = GetViewForward();

            float distance = Mathf.Max(0.01f, m_viewData.m_distance);
            Vector3 viewPoint = centerPoint - forward * distance;

            ApplyAutoFit(ref viewPoint, centerPoint);

            Quaternion lookRotation = Quaternion.LookRotation(centerPoint - viewPoint, Vector3.up);
            transform.SetPositionAndRotation(viewPoint, lookRotation);
        }

        private Vector3 GetViewForward()
        {
            if (m_referenceCamera != null)
            {
                Vector3 refForward = m_referenceCamera.transform.forward;
                if (refForward.sqrMagnitude > Mathf.Epsilon)
                {
                    return refForward.normalized;
                }
            }

            Vector3 fallback = m_viewData.m_fallbackViewDirection;
            if (fallback.sqrMagnitude <= Mathf.Epsilon)
            {
                fallback = new Vector3(0.0f, -0.6f, 0.8f);
            }

            return fallback.normalized;
        }

        private Vector3 GetCenterPoint()
        {
            if (m_targetObjTr == null)
            {
                return Vector3.zero;
            }

            var overrideComp = GetCameraOverride();
            if (overrideComp != null && overrideComp.UseOverrideBounds)
            {
                return overrideComp.transform.TransformPoint(overrideComp.BoundsCenter);
            }

            return m_targetObjTr.TransformPoint(m_viewData.m_centerOffset);
        }

        private void ApplyAutoFit(ref Vector3 viewPoint, Vector3 centerPoint)
        {
            if (!m_enableAutoFit)
            {
                return;
            }

            if (!TryGetTargetBounds(out Bounds bounds))
            {
                return;
            }

            float targetRadius = bounds.extents.magnitude * m_fitPadding;
            if (targetRadius <= m_baseFitRadius)
            {
                return;
            }

            Vector3 viewDir = viewPoint - centerPoint;
            float currentDistance = viewDir.magnitude;
            if (currentDistance <= Mathf.Epsilon)
            {
                return;
            }

            viewDir /= currentDistance;

            float addDistance = targetRadius - m_baseFitRadius;
            viewPoint = centerPoint + viewDir * (currentDistance + addDistance);
        }

        private bool TryGetTargetBounds(out Bounds bounds)
        {
            bounds = default;

            if (m_targetObjTr == null)
            {
                return false;
            }

            var overrideComp = GetCameraOverride();
            if (overrideComp != null && overrideComp.UseOverrideBounds)
            {
                Vector3 worldCenter = overrideComp.transform.TransformPoint(overrideComp.BoundsCenter);

                Vector3 lossyScale = overrideComp.transform.lossyScale;
                float maxScale = Mathf.Max(
                    Mathf.Abs(lossyScale.x),
                    Mathf.Abs(lossyScale.y),
                    Mathf.Abs(lossyScale.z));

                float worldRadius = overrideComp.BoundsRadius * maxScale;
                bounds = new Bounds(worldCenter, Vector3.one * worldRadius * 2.0f);
                return true;
            }

            Renderer[] renderers = m_targetObjTr.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;

            foreach (var renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (!renderer.enabled)
                {
                    continue;
                }

                if (m_ignoreParticleRenderer && renderer is ParticleSystemRenderer)
                {
                    continue;
                }

                if (((1 << renderer.gameObject.layer) & m_rendererLayerMask.value) == 0)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private CharaRenderCameraOverride GetCameraOverride()
        {
            if (m_targetObjTr == null)
            {
                return null;
            }

            return m_targetObjTr.GetComponentInChildren<CharaRenderCameraOverride>(true);
        }

#if UNITY_EDITOR
        [ContextMenu("Recreate RenderTexture")]
        private void RecreateRenderTextureInEditor()
        {
            if (!Application.isPlaying)
            {
                m_camera = GetComponent<Camera>();
                SetupCamera();
            }

            CreateRenderTexture();
        }
#endif
    }
}