using UnityEngine;
using UnityEngine.UI;

namespace SXG2025
{

    public class ResultPlayerOne : MonoBehaviour
    {
        [SerializeField] private RawImage m_captureImage = null;
        [SerializeField] private NamePlateUI m_namePlateUI = null;


        /// <summary>
        /// セットアップ 
        /// </summary>
        /// <param name="rank"></param>
        /// <param name="comPlayer"></param>
        /// <param name="teamColor"></param>
        /// <param name="charaTexture"></param>
        internal void Setup(int rank, ComPlayerBase comPlayer, int teamNo, Color teamColor, Texture charaTexture)
        {
            // RenderTexture割り当て 
            m_captureImage.texture = charaTexture;

            // 名前 
            m_namePlateUI.Setup(comPlayer, teamNo, teamColor);
        }

    }


}

