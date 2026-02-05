using UnityEngine;
using UnityEngine.UI;


namespace SXG2025
{

    public class PromoCardOne : MonoBehaviour
    {
        [SerializeField] private Image m_spriteImage;
        [SerializeField] private RawImage m_textureImage;
        private Image m_baseImage;

        private void Awake()
        {
            m_baseImage = GetComponent<Image>();
        }

        /// <summary>
        /// 画像割り当て 
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="texture"></param>
        public void SetImage(Sprite sprite, Texture texture, Color teamColor)
        {
            // 枠の色設定 
            m_baseImage.color = teamColor;

            // Spriteの場合 
            if (sprite != null)
            {
                m_spriteImage.sprite = sprite;
                m_spriteImage.preserveAspect = true;
                m_spriteImage.gameObject.SetActive(true);
                m_textureImage.gameObject.SetActive(false);
            } else
            {
                m_textureImage.texture = texture;
                m_textureImage.gameObject.SetActive(true);
                m_spriteImage.gameObject.SetActive(false);
            }
        }
    }


}

