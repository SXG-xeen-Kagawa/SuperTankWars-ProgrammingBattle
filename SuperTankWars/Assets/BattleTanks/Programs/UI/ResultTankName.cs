using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SXG2025
{
    namespace UI
    {

        public class ResultTankName : MonoBehaviour
        {
            [SerializeField] private Sprite[] m_tankNameBaseSprites;
            [SerializeField] private Image m_tankNameBase;
            [SerializeField] private TextMeshProUGUI m_textTankName;

            public void Setup(ComPlayerBase comPlayer, int teamNo)
            {
                m_tankNameBase.sprite = m_tankNameBaseSprites[teamNo];
                m_textTankName.text = comPlayer.TankName;
            }
        }


    }

}
