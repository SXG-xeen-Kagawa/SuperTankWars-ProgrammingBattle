using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace SXG2025
{

    public class NamePlateUI : MonoBehaviour
    {
        [SerializeField] private Image m_faceImage = null;
        [SerializeField] private TextMeshProUGUI m_textOrganization = null;
        [SerializeField] private TextMeshProUGUI m_textName = null;

        [SerializeField] private Sprite[] m_entryNameBaseSprites;
        [SerializeField] private Sprite[] m_playerIconBaseSprites;

        [SerializeField] private Image m_entryNameBaseImage;
        [SerializeField] private Image m_playerIconBaseImage;

        void Awake()
        {
            m_faceImage.sprite = null;
            m_textOrganization.text = string.Empty;
            m_textName.text = string.Empty;
        }

        /// <summary>
        /// セットアップ 
        /// </summary>
        /// <param name="comPlayer"></param>
        /// <param name="teamColor"></param>
        public void Setup(ComPlayerBase comPlayer, int teamNo, Color teamColor)
        {
            if (comPlayer != null)
            {
                m_textOrganization.text = comPlayer.Organization;
                m_textOrganization.color = teamColor;
                m_textName.text = comPlayer.YourName;
                m_textName.color = teamColor;
                m_faceImage.sprite = comPlayer.FaceImage;
            }
            m_entryNameBaseImage.sprite = m_entryNameBaseSprites[teamNo];
            m_playerIconBaseImage.sprite = m_playerIconBaseSprites[teamNo];
        }

    }

}

