using UnityEngine;
using UnityEngine.UI;

namespace SXG2025
{
    namespace UI
    {

        public class TankScoreLifeUI : MonoBehaviour
        {
            [SerializeField] private Image m_bodyImage = null;
            [SerializeField] private Sprite[] m_battleLifeSprites = null;


            //internal void SetColor(Color teamColor)
            //{
            //    m_bodyImage.color = teamColor;
            //}

            internal void SetTeamNo(int teamNo)
            {
                m_bodyImage.sprite = m_battleLifeSprites[teamNo];
            }


            internal void LostLife()
            {
                m_bodyImage.enabled = false;
            }
        }

    }



}

