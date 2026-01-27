using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;


namespace SXG2025
{
    namespace UI
    {

        public class ResultGaugeOneChip : MonoBehaviour
        {
            [SerializeField] private TextMeshProUGUI m_resultText = null;
            [SerializeField] private RectTransform m_chipTr = null;
            [SerializeField] private Image m_chipImage = null;
            [SerializeField] private Image m_facePlateImage = null;
            [SerializeField] private Image m_faceImage = null;

            [SerializeField] private float m_dropStartPosition = 1200;
            [SerializeField] private float m_dropGravity = 400;
            [SerializeField] private float m_dropStartSpeed = -200;
            [SerializeField] private float m_dropBounciness = 0.1f;

            [SerializeField] private Sprite[] m_gaugeChipSprites = null;
            [SerializeField] private Sprite[] m_facePlateSprites = null;

            private RectTransform m_rectTr = null;

            private void Awake()
            {
                m_rectTr = GetComponent<RectTransform>();
            }


            /// <summary>
            /// セットアップ 
            /// </summary>
            /// <param name="resultMessage"></param>
            /// <param name="baseColor"></param>
            /// <param name="gaugeHeight"></param>
            public void Setup(string resultMessage,
                int targetTeamNo, Color baseColor, float gaugeHeight, Sprite faceImage, 
                UnityAction<int> boundCallback)
            {
                m_resultText.text = resultMessage;
                m_resultText.color = baseColor;
                m_chipImage.sprite = m_gaugeChipSprites[targetTeamNo];
                m_facePlateImage.sprite = m_facePlateSprites[targetTeamNo];
                m_faceImage.sprite = faceImage;

                // サイズ設定 
                ChangeRectTrHeight(m_rectTr, gaugeHeight);
                ChangeRectTrHeight(m_chipTr, gaugeHeight);

                // 落下アニメーション 
                StartCoroutine(CoDropGauge(boundCallback));
            }

            private IEnumerator CoDropGauge(UnityAction<int> boundCallback)
            {
                const int BOUND_TIMES = 5;

                Vector2 localPosition = new Vector2(0, m_dropStartPosition);
                Vector2 localSpeed = Vector2.up * m_dropStartSpeed;
                m_chipTr.anchoredPosition = localPosition;

                // 落下 
                int boundCount = 0;
                while (boundCount < BOUND_TIMES)
                {
                    // 落下とバウンド 
                    localPosition.y += localSpeed.y * Time.deltaTime;
                    if (localPosition.y <= 0 && localSpeed.y <= 0)
                    {
                        localSpeed.y = -localSpeed.y * m_dropBounciness;
                        localPosition.y = 0;
                        boundCount++;
                        // コールバック 
                        if (boundCallback != null)
                        {
                            boundCallback.Invoke(boundCount);
                        }
                    }
                    m_chipTr.anchoredPosition = localPosition;

                    // 加速 
                    localSpeed.y += m_dropGravity * Time.deltaTime;

                    yield return null;
                }

                // 停止 
                m_chipTr.anchoredPosition = Vector2.zero;
            }


            private void ChangeRectTrHeight(RectTransform rectTr, float height)
            {
                Vector2 size = rectTr.sizeDelta;
                size.y = height;
                rectTr.sizeDelta = size;
            }

        }


    }
}

