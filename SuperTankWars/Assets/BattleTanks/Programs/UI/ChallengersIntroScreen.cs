using System.Collections.Generic;
using UnityEngine;

namespace SXG2025
{

    public class ChallengersIntroScreen : MonoBehaviour
    {
        [SerializeField] private RectTransform m_dividedRootTr = null;
        [SerializeField] private ChallengerIntroOne m_challengerIntroPrefab = null;

        private List<ChallengerIntroOne> m_challengersIntroList = new();


        private void Awake()
        {
            gameObject.SetActive(false);
        }


        /// <summary>
        /// スクリーン表示開始 
        /// </summary>
        /// <param name="challegers"></param>
        public void StartScreen(ComPlayerBase[] challengers,
            Color[] teamColors, Texture[] charaTextures, ChallengerIntroOne.TankSpecInfo[] tankSpecInfo)
        {
            // 起こす 
            gameObject.SetActive(true);

            // チャレンジャーを作る 
            for (int i = 0; i < challengers.Length; ++i)
            {
                var challger = challengers[i];
                var introOne = Instantiate(m_challengerIntroPrefab, m_dividedRootTr);
                introOne.Setup(challger, i, teamColors[i], charaTextures[i], tankSpecInfo[i]);
                m_challengersIntroList.Add(introOne);
            }

            // カメラをぼかす 
            CameraDoF.Instance.Change(true);
        }


        /// <summary>
        /// 閉じる 
        /// </summary>
        public void CloseScreen()
        {
            Animator animator = GetComponent<Animator>();
            animator.SetTrigger("Leave");
        }



        public void AnimEvent_Finish()
        {
            // カメラをぼかす 
            CameraDoF.Instance.Change(false);

            gameObject.SetActive(false);
        }
    }


}

