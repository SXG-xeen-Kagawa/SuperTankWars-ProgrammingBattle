using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace SXG2025
{
    namespace UI
    {

        public class ResultRanking : MonoBehaviour
        {
            [SerializeField] private Sprite[] m_rankingSprites = null;

            private Image m_image = null;
            private int m_lastLank = -1;

            private void Awake()
            {
                m_image = GetComponent<Image>();
                m_image.enabled = false;
            }

            /// <summary>
            /// ランク設定 
            /// </summary>
            /// <param name="newRank"></param>
            public void SetRank(int newRank)
            {
                if (m_lastLank != newRank)
                {
                    m_image.enabled = true;
                    m_image.sprite = m_rankingSprites[newRank];
                    m_lastLank = newRank;
                }
            }


        }

    }
}

