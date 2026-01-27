using UnityEngine;

namespace SXG2025
{

    public class TankScoreRootUI : MonoBehaviour
    {
        [SerializeField] private TankScoreUI m_tankScorePrefab = null;

        private TankScoreUI[] m_tankScoreUiList = new TankScoreUI[GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE];

        const float LOCATION_AREA_WIDTH = 1920 - 80;    // 配置エリア幅 


        private CanvasGroup m_canvasGroup = null;


        private void Awake()
        {
            m_canvasGroup = GetComponent<CanvasGroup>();
        }



        /// <summary>
        /// 登録 
        /// </summary>
        /// <param name="teamNo"></param>
        /// <param name="comPlayer"></param>
        /// <param name="teamColor"></param>
        public void Entry(int teamNo, ComPlayerBase comPlayer, Color teamColor, int tankLives)
        {
            var instance = Instantiate(m_tankScorePrefab, this.transform);

            // 配置座標 
            float oneWidth = LOCATION_AREA_WIDTH / GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE;
            float locationX = -LOCATION_AREA_WIDTH / 2.0f + oneWidth * 0.5f + oneWidth * teamNo;
            instance.Setup(teamNo, comPlayer, teamColor, locationX, tankLives);
            m_tankScoreUiList[teamNo] = instance;
        }

        /// <summary>
        /// ゲージの値変更 
        /// </summary>
        /// <param name="teamNo"></param>
        /// <param name="gaugeRate"></param>
        public void SetEnergyGauge(int teamNo, float gaugeRate, bool withDamageAnim)
        {
            m_tankScoreUiList[teamNo].SetGauge(Mathf.Clamp01(gaugeRate), withDamageAnim);
        }

        /// <summary>
        /// 残機1つ減らす 
        /// </summary>
        /// <param name="teamNo"></param>
        internal void DestroyOneLife(int teamNo, bool withDamageAnim)
        {
            m_tankScoreUiList[teamNo].DestroyOneLife(withDamageAnim);
        }


        /// <summary>
        /// ゲージが無くなって敗退 
        /// </summary>
        /// <param name="teamNo"></param>
        public void LoseByGaugeDepletion(int teamNo)
        {
            m_tankScoreUiList[teamNo].Lose();
        }


        /// <summary>
        /// 撃破UI 
        /// </summary>
        /// <param name="attackerTeamNo"></param>
        /// <param name="defeatedTeamNo"></param>
        internal void SetDefeatUI(int attackerTeamNo, int defeatedTeamNo)
        {
            m_tankScoreUiList[attackerTeamNo].SetDefeated(defeatedTeamNo);
        }

    }


}
