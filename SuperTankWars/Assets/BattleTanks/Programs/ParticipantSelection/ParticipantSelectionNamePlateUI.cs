using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SXG2025
{
    public class ParticipantSelectionNamePlateUI : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI m_organizationText = null;

        [SerializeField]
        TextMeshProUGUI m_nameText = null;

        [SerializeField]
        Image m_faceImage = null;

        [SerializeField]
        GameObject m_arrowObject = null;

        [SerializeField]
        Button m_button = null;

        public bool isSetData { get; private set; } = false;

        public UnityAction AddClicknEvent { set => m_button.onClick.AddListener(value); }

        void Awake()
        {
            m_organizationText.text = string.Empty;
            m_nameText.text = string.Empty;
            m_faceImage.enabled = false;
        }

        public void SetSelection(bool isSelection)
        {
            if (m_arrowObject)
                m_arrowObject.SetActive(isSelection);
        }

        public void SetData(string organizationText, string nameText, Sprite faceImageSprite)
        {
            m_organizationText.text = organizationText;
            m_nameText.text = nameText;
            m_faceImage.enabled = true;
            m_faceImage.sprite = faceImageSprite;

            isSetData = true;
        }
    }
}
