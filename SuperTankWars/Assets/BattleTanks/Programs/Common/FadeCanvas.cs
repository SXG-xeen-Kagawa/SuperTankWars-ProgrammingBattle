using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace SXG2025
{

    public class FadeCanvas : MonoBehaviour
    {
        private static FadeCanvas ms_instance = null;

        const float DEFAULT_FADE_TIME = 0.5f;

        internal static FadeCanvas Instance => ms_instance;

        [SerializeField] private Image m_fadeImage = null;


        [RuntimeInitializeOnLoadMethod()]
        static void OnLoadMethod()
        {
            if (ms_instance == null)
            {
                GameObject prefab = Resources.Load<GameObject>("FadeCanvas");
                if (prefab != null)
                {
                    Instantiate(prefab);
                }
            }
        }

        private void Awake()
        {
            ms_instance = this;
            DontDestroyOnLoad(this.gameObject);
        }


        /// <summary>
        /// フェードアウト 
        /// </summary>
        /// <param name="fadeTime"></param>
        internal void FadeOut(float fadeTime = DEFAULT_FADE_TIME)
        {
            StopAllCoroutines();
            StartCoroutine(CoFade(1.0f, fadeTime));
        }

        /// <summary>
        /// フェードイン 
        /// </summary>
        /// <param name="fadeTime"></param>
        internal void FadeIn(float fadeTime = DEFAULT_FADE_TIME)
        {
            StopAllCoroutines();
            StartCoroutine(CoFade(0.0f, fadeTime));
        }


        private IEnumerator CoFade(float targetAlpha, float time)
        {
            Color fadeColor = m_fadeImage.color;
            float startAlpha = fadeColor.a;
            float animTime = Mathf.Abs(targetAlpha - fadeColor.a) * time;
            float localTime = 0;
            m_fadeImage.enabled = true;
            while (localTime < animTime)
            {
                localTime += Time.deltaTime;
                fadeColor.a = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(localTime / animTime));
                m_fadeImage.color = fadeColor;

                yield return null;
            }

            // 完了 
            fadeColor.a = targetAlpha;
            m_fadeImage.color = fadeColor;

            // 非表示？ 
            if (targetAlpha <= 0)
            {
                m_fadeImage.enabled = false;
            }
        }


    }

}

