using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SXG2025
{

    public class ResultScreen : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_roundText = null;
        [SerializeField] private ResultPlayerOne m_resultPlayerOnePrefab = null;
        [SerializeField] private RectTransform[] m_playerOneLocationTr = null;
        [SerializeField] private Image m_backImage = null;

        private List<ResultPlayerOne> m_resultPlayersList = new();

        private Animator m_animator = null;


        private void Awake()
        {
            m_animator = GetComponent<Animator>();
            gameObject.SetActive(false);
        }

        internal void StartScreen(ComPlayerBase[] challengers,
            Color[] teamColors, Texture[] charaTextures, IList<int> rankingPlayerIdList, int roundCount)
        {
            // 起こす 
            gameObject.SetActive(true);

            // 参加者を作る 
            for (int i=0; i < m_playerOneLocationTr.Length; ++i)
            {
                var resultOne = Instantiate(m_resultPlayerOnePrefab, m_playerOneLocationTr[i]);
                int playerId = rankingPlayerIdList[i];
                resultOne.Setup(i, challengers[playerId], playerId, teamColors[playerId], charaTextures[playerId]);
                m_resultPlayersList.Add(resultOne);
            }

            // カメラをぼかす 
            CameraDoF.Instance.Change(true);

            // 「第○回戦の結果」表示
            SetRoundCount(roundCount);

            // 背景を優勝者のカラーにする 
            Color winnerColor = teamColors[rankingPlayerIdList[0]];
            winnerColor.a = m_backImage.color.a;
            m_backImage.color = winnerColor;

            // 入場アニメーション 
            m_animator.SetTrigger("Enter");

        }


        /// <summary>
        /// 「第〇回戦の結果」のテキスト表示
        /// 1～99回戦まで表示可能
        /// </summary>
        /// <param name="roundCount">第○回</param>
        private void SetRoundCount(int roundCount)
        {
            roundCount = Mathf.Clamp(roundCount, 1, 99);

            var units = new string[] { "", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
            var tens = new string[] { "", "十", "二十", "三十", "四十", "五十", "六十", "七十", "八十", "九十" };

            int ten = roundCount / 10;
            int unit = roundCount % 10;

            var kansuji = string.Empty;
            if (ten == 0)
                kansuji = units[unit];
            else if (unit == 0)
                kansuji = tens[ten];
            else
                kansuji = tens[ten] + units[unit];

            m_roundText.text = $"第{kansuji}回戦の結果";
        }


    }


}

