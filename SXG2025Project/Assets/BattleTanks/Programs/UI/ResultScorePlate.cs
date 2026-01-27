using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace SXG2025
{
    namespace UI
    {

        public class ResultScorePlate : MonoBehaviour
        {
            [SerializeField] private TextMeshProUGUI m_text = null;
            [SerializeField] private Sprite[] m_plateSprites = null;

            private Image m_plateImage = null;
            private int m_totalScore = 0;
            private float m_targetScore = 0;
            private float m_displayScore = 0;

            private void Awake()
            {
                m_plateImage = GetComponent<Image>();
            }

            // Update is called once per frame
            void Update()
            {
                if (m_displayScore < m_targetScore)
                {
                    m_displayScore = Mathf.Lerp(m_displayScore, m_targetScore, 1.0f / 0.1f * Time.deltaTime);
                    SetDisplayScore(Mathf.RoundToInt(m_displayScore));    // floatの四捨五入
                }
            }

            public void Setup(int teamNo, Color teamColor)
            {
                m_plateImage.sprite = m_plateSprites[teamNo];
                m_text.color = teamColor;

                m_totalScore = 0;
                m_targetScore = 0;
                m_displayScore = 0;

                SetDisplayScore(0);
            }

            /// <summary>
            /// スコア加算 
            /// </summary>
            /// <param name="score"></param>
            public void AddScore(int score)
            {
                m_totalScore += score;
                m_targetScore = m_totalScore;
            }

            private void SetDisplayScore(int newScore)
            {
                m_text.text = string.Format("{0}Pts.", newScore.ToString("N0"));
            }

        }

    }

}

