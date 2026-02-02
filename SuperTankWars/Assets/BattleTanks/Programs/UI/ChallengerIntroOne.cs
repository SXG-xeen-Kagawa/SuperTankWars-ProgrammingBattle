using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace SXG2025
{

    public class ChallengerIntroOne : MonoBehaviour
    {
        [SerializeField] private RawImage m_renderTextureImage = null;
        [SerializeField] private TextMeshProUGUI m_textTankSpec = null;
        [SerializeField] private TextMeshProUGUI m_textLifeCount = null;

        [SerializeField] private Image m_entryBaseImage = null;
        [SerializeField] private Sprite[] m_enterBaseSprites;

        [SerializeField] private TextMeshProUGUI m_textOrganization = null;
        [SerializeField] private TextMeshProUGUI m_textPlayerName = null;
        [SerializeField] private Image m_iconImage = null;

        [SerializeField] private Sprite[] m_tankNameBaseSprites;
        [SerializeField] private Image m_tankNameBase = null;
        [SerializeField] private TextMeshProUGUI m_textTankName = null;


        public struct TankSpecInfo
        {
            public int m_cost;  // 総コスト
            public int m_turretCount;   // 砲塔の数 
            public int m_rotatorCount;  // 回転部位の数 
            public int m_armorCount;    // 装甲の数 
            public int m_sortieCount;   // 出撃回数 
        }


        /// <summary>
        /// セットアップ 
        /// </summary>
        /// <param name="comPlayer"></param>
        /// <param name="teamColor"></param>
        public void Setup(ComPlayerBase comPlayer, int teamNo, Color teamColor, Texture charaTexture, TankSpecInfo specInfo)
        {
            m_renderTextureImage.texture = charaTexture;

            // 参加者 
            m_textOrganization.text = comPlayer.Organization;
            m_textOrganization.color = teamColor;
            m_textPlayerName.text = comPlayer.YourName;
            m_textPlayerName.color = teamColor;
            m_iconImage.sprite = comPlayer.FaceImage;

            // チームカラー設定 
            m_entryBaseImage.sprite = m_enterBaseSprites[teamNo];

            // 戦車のスペック 
            //m_textTankSpec.text = string.Format("<color=#FFC010>コスト：{0}pt</color>\n砲塔：{1}基\n回転部：{2}基\n装甲：{3}部\n<color=#FF2080>出撃可能回数：{4}回</color>",
            //    specInfo.m_cost, specInfo.m_turretCount, specInfo.m_rotatorCount, specInfo.m_armorCount, specInfo.m_sortieCount);
            m_textTankSpec.text = string.Format("コスト：{0}pt\n砲塔：{1}基\n回転部：{2}基 / 装甲：{3}部",
                specInfo.m_cost, specInfo.m_turretCount, specInfo.m_rotatorCount, specInfo.m_armorCount);
            m_textLifeCount.text = string.Format("出撃可能回数：<size=40>{0}</size>回",
                 specInfo.m_sortieCount);

            // 戦車名 
            m_tankNameBase.sprite = m_tankNameBaseSprites[teamNo];
            m_textTankName.text = comPlayer.TankName;
        }

    }


}

