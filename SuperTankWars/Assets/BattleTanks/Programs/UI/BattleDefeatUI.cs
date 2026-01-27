using UnityEngine;
using UnityEngine.UI;


namespace SXG2025
{
    namespace UI
    {

        public class BattleDefeatUI : MonoBehaviour
        {
            [SerializeField] private Image m_baseImage = null;
            [SerializeField] private Sprite[] m_battleDefeatBaseSprites;

            [SerializeField] private Image m_textImage = null;
            [SerializeField] private Sprite[] m_battleDefeatTxtSprites;


            // Start is called once before the first execution of Update after the MonoBehaviour is created
            void Start()
            {

            }


            public void AnimEvent_Finished()
            {
                Destroy(gameObject);
            }


            internal void Setup(int attackerTeamId, int defeatedTeamId)
            {
                m_baseImage.sprite = m_battleDefeatBaseSprites[attackerTeamId];
                m_textImage.sprite = m_battleDefeatTxtSprites[defeatedTeamId];
            }

        }

    }
}

