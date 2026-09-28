using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SXG2025
{
    public class OutOfScreenTankIndicatorItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform m_rootRectTransform = null;
        [SerializeField] private RectTransform m_arrowRootRectTransform = null;
        [SerializeField] private RawImage m_tankImage = null;
        [SerializeField] private TMP_Text m_distanceText = null;
        [SerializeField] private RectTransform m_textTransform = null;
        [SerializeField] private Graphic[] m_teamColorImages = null;

        public RectTransform RootRectTransform => m_rootRectTransform;

        private Material m_distanceTextMaterialInstance = null;

        private void Reset()
        {
            m_rootRectTransform = transform as RectTransform;
        }

        private void Awake()
        {
            if (m_distanceText != null)
            {
                m_distanceTextMaterialInstance = Instantiate(m_distanceText.fontSharedMaterial);
                m_distanceText.fontMaterial = m_distanceTextMaterialInstance;
            }
        }

        private void OnDestroy()
        {
            if (m_distanceTextMaterialInstance != null)
            {
                Destroy(m_distanceTextMaterialInstance);
                m_distanceTextMaterialInstance = null;
            }
        }

        public void SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
        }

        public void SetScreenPosition(Vector2 anchoredPosition)
        {
            if (m_rootRectTransform != null)
            {
                m_rootRectTransform.anchoredPosition = anchoredPosition;
            }
        }

        public void SetArrowAngle(float angleDeg)
        {
            float angle = angleDeg - 90.0f;

            if (m_arrowRootRectTransform != null)
            {
                m_arrowRootRectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            if (m_textTransform != null)
            {
                m_textTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
            }
            if (m_tankImage != null)
            {
                m_tankImage.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0f, 0f, -angle);
            }
        }

        public void SetTexture(Texture texture)
        {
            if (m_tankImage != null)
            {
                m_tankImage.texture = texture;
            }
        }

        public void SetDistanceAndHeight(float horizontalDistance, float height)
        {
            if (m_distanceText != null)
            {
                m_distanceText.text = $"距離：{horizontalDistance:F1}m\n高度：{height:+0.0;-0.0;0.0}m";
            }
        }

        public void SetTeamColor(Color newColor)
        {
            if (m_teamColorImages != null)
            {
                foreach (var image in m_teamColorImages)
                {
                    if (image != null)
                    {
                        image.color = newColor;
                    }
                }
            }

            if (m_distanceTextMaterialInstance != null)
            {
                m_distanceTextMaterialInstance.SetColor(ShaderUtilities.ID_OutlineColor, newColor);
            }
        }
    }
}