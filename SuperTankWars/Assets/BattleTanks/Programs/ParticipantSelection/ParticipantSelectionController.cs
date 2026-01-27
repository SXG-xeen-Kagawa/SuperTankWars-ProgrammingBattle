using UnityEngine;

namespace SXG2025
{
    public class ParticipantSelectionController : MonoBehaviour
    {
        [SerializeField]
        ParticipantSelectionNamePlateUI[] m_namePlates = null;

        public int m_currentIndex { get; private set; } = 0;

        private void Awake()
        {
            // ボタン押下イベント登録
            for (var i = 0; i < m_namePlates.Length; i++)
            {
                var tmpI = i;
                m_namePlates[i].AddClicknEvent = () => UpdateSelectionNamePlate(tmpI);
            }
        }

        public void UpdateSelectionNamePlate(int nextIndex)
        {
            m_currentIndex = Mathf.Clamp(nextIndex, 0, m_namePlates.Length - 1);

            for (var i = 0; i < m_namePlates.Length; i++)
            {
                m_namePlates[i].SetSelection(m_currentIndex == i);
            }
        }

        public void SetCurrentNamePlateData(string organizationText, string nameText, Sprite faceImageSprite)
        {
            m_namePlates[m_currentIndex].SetData(organizationText, nameText, faceImageSprite);
        }

        /// <summary>
        /// 全ての挑戦者をセット済みか
        /// </summary>
        /// <returns></returns>
        public bool IsSetAllNamePlateData()
        {
            foreach (var namePlate in m_namePlates)
            {
                if (!namePlate.isSetData)
                    return false;
            }
            return true;
        }
    }
}
