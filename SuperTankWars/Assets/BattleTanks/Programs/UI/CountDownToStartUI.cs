using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Events;
using SXG2025.Effect;

namespace SXG2025
{

    public class CountDownToStartUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_text = null;
        private CanvasGroup m_canvasGrp = null;

        public class LabelData
        {
            public string m_label;
            public float m_delayTime;
            public bool m_endFlag = false;
        }
        private LabelData[] m_labelData =
        {
            new LabelData
            {
                m_label = "３",
                m_delayTime = 1
            },
            new LabelData
            {
                m_label = "２",
                m_delayTime = 1
            },
            new LabelData
            {
                m_label = "１",
                m_delayTime = 1
            },
            new LabelData
            {
                m_label = "FIGHT!",
                m_delayTime = 1.2f,
                m_endFlag = true
            },
        };

        private void Awake()
        {
            m_canvasGrp = GetComponent<CanvasGroup>();
            m_canvasGrp.alpha = 0;
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        /// <summary>
        /// カウントダウン開始 
        /// </summary>
        /// <param name="OnFinish"></param>
        public void StartCountDown(UnityAction OnFinish)
        {
            StartCoroutine(CoCountDown(OnFinish));
        }

        private IEnumerator CoCountDown(UnityAction OnFinish)
        {
            m_canvasGrp.alpha = 1;

            // SE
            SoundController.PlaySE(SoundController.SEType.CountDown);

            foreach (var label in m_labelData)
            {
                m_text.text = label.m_label;
                if (label.m_endFlag)
                {
                    if (OnFinish != null)
                    {
                        OnFinish.Invoke();
                    }
                }
                yield return new WaitForSeconds(label.m_delayTime);
            }
            m_canvasGrp.alpha = 0;
        }
    }

}
