using UnityEngine;


namespace SXG2025
{

    public class CanvasController : MonoBehaviour
    {
        private CanvasGroup m_canvasGroup = null;

        [SerializeField] private RectTransform m_3dRootTr = null;

        private void Awake()
        {
            m_canvasGroup = GetComponent<CanvasGroup>();
        }

        internal void SetAlpha(float alpha)
        {
            m_canvasGroup.alpha = alpha;
        }

        internal RectTransform  Get3dRootTr()
        {
            return m_3dRootTr;
        }
    }

}


