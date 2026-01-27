using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace SXG2025
{
    namespace UI
    {

        public class ResultPlayerOne2 : MonoBehaviour
        {
            [SerializeField] private RawImage m_renderTextureImage = null;
            [SerializeField] private RectTransform m_tankImageTr = null;
            [SerializeField] private NamePlateUI m_namePlate = null;
            [SerializeField] private RectTransform m_gaugeTr = null;
            [SerializeField] private ResultGaugeOneChip m_resultGaugeChipPrefab = null;
            [SerializeField] private GameObject m_winnerGoldLight = null;

            [SerializeField] private ResultScorePlate m_resultScorePlate = null;
            [SerializeField] private ResultRanking m_ranking = null;

            [SerializeField] private Sprite[] m_rankingSprites;

            private Vector2 m_gaugePosition = Vector2.zero;
            private Vector2 m_facePosition = Vector2.zero;
            private Vector2 m_faceTargetPosition = Vector2.zero;
            private int m_teamNo = 0;
            private int m_myRank = 0;


            /// <summary>
            /// セットアップ 
            /// </summary>
            /// <param name="comPlayer"></param>
            /// <param name="teamColor"></param>
            public void Setup(ComPlayerBase comPlayer, int teamNo, Color teamColor, Texture charaTexture)
            {
                m_renderTextureImage.texture = charaTexture;

                m_gaugePosition = Vector2.zero;

                m_facePosition = m_tankImageTr.anchoredPosition;
                m_faceTargetPosition = m_facePosition;

                m_teamNo = teamNo;

                m_namePlate.Setup(comPlayer, teamNo, teamColor);
                m_resultScorePlate.Setup(teamNo, teamColor);
            }


            
            /// <summary>
            /// ゲージを一つ追加 
            /// </summary>
            /// <param name="message"></param>
            /// <param name="baseColor"></param>
            /// <param name="height"></param>
            public void AddResultGaugeOneChip(string message, 
                int targetTeamNo, Color baseColor, float height, bool isSE,
                int price, int step,
                Sprite faceImage,
                ResultScreen2.OnGetPriceDelegate onGetCallback)
            {
                var gaugeOne = Instantiate(m_resultGaugeChipPrefab, m_gaugeTr);
                gaugeOne.Setup(message, targetTeamNo, baseColor, height, faceImage,
                    (count) =>
                    {
                        if (count == 1)
                        {
                            m_faceTargetPosition.y += height;
                            if (isSE)
                            {
                                // SE
                                Effect.SoundController.PlaySE(Effect.SoundController.SEType.ResultGauge);
                            }
                            // コールバック
                            if (onGetCallback != null)
                            {
                                onGetCallback.Invoke(m_teamNo, price, step);
                            }
                            // スコア加算 
                            m_resultScorePlate.AddScore(price);
                        }
                    });

                // 表示位置調整 
                RectTransform gaugeTr = gaugeOne.GetComponent<RectTransform>();
                gaugeTr.anchoredPosition = m_gaugePosition;

                // 位置をずらす 
                m_gaugePosition.y += height;
            }
            




            private void Update()
            {
                if (m_facePosition.y < m_faceTargetPosition.y)
                {
                    m_facePosition.y = Mathf.Lerp(m_facePosition.y, m_faceTargetPosition.y,
                        (1.0f / 0.25f) * Time.deltaTime);
                    m_tankImageTr.anchoredPosition = m_facePosition;
                }
            }

            
            public void SetRank(int newRank)
            {
                m_myRank = newRank;
                m_ranking.SetRank(newRank);
            }
            

            /// <summary>
            /// 勝敗が決した時の演出 
            /// </summary>
            public bool StartDecided()
            {
                if (m_myRank == 0)
                {
                    m_winnerGoldLight.SetActive(true);
                    return true;
                }
                return false;
            }

        }

    }
}

