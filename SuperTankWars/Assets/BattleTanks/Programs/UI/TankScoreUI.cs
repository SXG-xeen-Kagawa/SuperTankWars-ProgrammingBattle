using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using SXG2025.UI;

namespace SXG2025
{

    public class TankScoreUI : MonoBehaviour
    {
        [SerializeField] private Image m_faceIconImage = null;

        [SerializeField] private Sprite[] m_faceEdgeTeamColorSprites = null;    // 顔の周辺のチームカラー表現のスプライト 

        [SerializeField] private TextMeshProUGUI m_nameText = null;     // お名前 

        [SerializeField] private Image m_energyGaugeImage = null;

        [SerializeField] private TankScoreLifeUI m_tankLifeUI = null;

        [SerializeField] private Image m_battleNameBaseImage = null;
        [SerializeField] private Sprite[] m_battleNameBaseSprites = null;

        [SerializeField] private RectTransform m_battleDefeatRootTr = null;
        [SerializeField] private BattleDefeatUI m_battleDefeatUiPrefab = null;


        private int m_teamNo = 0;
        private RectTransform m_rectTr = null;
        private Animator m_animator = null;

        private float m_gaugeFillAmount = 1.0f;  // ゲージ状態 

        private List<TankScoreLifeUI> m_tankScoreLifesList = new();
        private int m_remainLives = 0;

        private readonly int ANIM_TRIGGER_DAMAGE = Animator.StringToHash("Damage");
        private readonly int ANIM_TRIGGER_LOSE = Animator.StringToHash("Lose");


        private void Awake()
        {
            m_rectTr = GetComponent<RectTransform>();
            m_animator = GetComponent<Animator>();
        }


        /// <summary>
        /// セットアップ 
        /// </summary>
        /// <param name="teamNo"></param>
        /// <param name="playerName"></param>
        /// <param name="faceIcon"></param>
        /// <param name="teamColor"></param>
        public void Setup(int teamNo, ComPlayerBase comPlayer, Color teamColor, float locationX, int lives)
        {
            // 設定 
            m_teamNo = teamNo;
            m_faceIconImage.sprite = comPlayer.FaceImage;
            m_energyGaugeImage.color = teamColor;

            // NameText
            m_nameText.text = comPlayer.YourName;
            m_nameText.color = teamColor;

            // BattleNameBase
            m_battleNameBaseImage.sprite = m_battleNameBaseSprites[teamNo];

            // 配置座標 
            Vector2 location = m_rectTr.anchoredPosition;
            location.x = locationX;
            m_rectTr.anchoredPosition = location;

            // ゲージリセット 
            m_gaugeFillAmount = 1.0f;
            m_energyGaugeImage.fillAmount = m_gaugeFillAmount;

            // 残機の色設定 
            m_tankLifeUI.SetTeamNo(teamNo);
            //m_tankLifeUI.SetColor(teamColor);

            // 残機の数に応じて僅かにスケールを変える 
            float lifeScale = Mathf.Lerp(1.4f, 1.0f, Mathf.Clamp01(((float)lives - 1) / 4.0f));
            m_tankLifeUI.transform.localScale = Vector3.one * lifeScale;

            // 残機のUIを作る 
            m_tankScoreLifesList.Add(m_tankLifeUI);
            for (int i=1; i < lives; ++i)
            {
                TankScoreLifeUI next = Instantiate(m_tankLifeUI, m_tankLifeUI.transform.parent);
                m_tankScoreLifesList.Add(next);
            }
            m_remainLives = lives;

            // グリッドサイズ調整 
            StartCoroutine(CoAdjustCellSize(lifeScale));
        }

        private IEnumerator CoAdjustCellSize(float lifeScale)
        {
            yield return null;
            yield return null;

            // Gridサイズ調整 
            GridLayoutGroup gridLayout = m_tankLifeUI.transform.parent.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {

                RectTransform tankLifeOneTr = m_tankLifeUI.GetComponent<RectTransform>();
                gridLayout.cellSize = new Vector2(tankLifeOneTr.sizeDelta.x * lifeScale, tankLifeOneTr.sizeDelta.y * lifeScale);
            }

        }


        /// <summary>
        /// ゲージの値を設定 
        /// </summary>
        /// <param name="newValue"></param>
        public void SetGauge(float newValue, bool withAnim)
        {
            m_gaugeFillAmount = newValue;

            StopAllCoroutines();
            StartCoroutine(CoUpdateGauge());

            // アニメーション 
            if (withAnim)
            {
                m_animator.SetTrigger(ANIM_TRIGGER_DAMAGE);
            }
        }

        internal void DestroyOneLife(bool withDamageAnim)
        {
            // ダメージアニメーション 
            if (withDamageAnim)
            {
                m_animator.SetTrigger(ANIM_TRIGGER_DAMAGE);
            }

            // １つ減らす 
            m_remainLives--;
            if (0 <= m_remainLives && m_remainLives < m_tankScoreLifesList.Count)
            {
                m_tankScoreLifesList[m_remainLives].LostLife();
            }
        }


        private IEnumerator CoUpdateGauge()
        {
            const float ANIM_MAX_TIME = 0.5f;

            float time = 0;
            float startValue = m_energyGaugeImage.fillAmount;
            while (time < ANIM_MAX_TIME)
            {
                time += Time.deltaTime;

                m_energyGaugeImage.fillAmount = Mathf.Lerp(startValue, m_gaugeFillAmount, Mathf.Sin(time * (Mathf.PI * 0.5f) / ANIM_MAX_TIME));

                yield return null;
            }
            m_energyGaugeImage.fillAmount = m_gaugeFillAmount;
        }


        /// <summary>
        /// 敗退 
        /// </summary>
        public void Lose()
        {
            m_animator.SetTrigger(ANIM_TRIGGER_LOSE);
        }



        internal void SetDefeated(int defeatedTeamNo)
        {
            var defeatedUi = Instantiate(m_battleDefeatUiPrefab, m_battleDefeatRootTr);
            defeatedUi.Setup(m_teamNo, defeatedTeamNo);
        }

    }


}

