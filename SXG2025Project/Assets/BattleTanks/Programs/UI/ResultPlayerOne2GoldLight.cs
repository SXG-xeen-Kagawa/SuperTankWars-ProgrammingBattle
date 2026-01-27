using UnityEngine;


namespace SXG2025
{
    namespace UI
    {

        public class ResultPlayerOne2GoldLight : MonoBehaviour
        {
            [SerializeField] private float m_rotationSpeed = 360.0f;
            private RectTransform m_rectTr = null;
            private float m_timer = 0;

            // Start is called once before the first execution of Update after the MonoBehaviour is created
            void Start()
            {
                m_rectTr = GetComponent<RectTransform>();
            }

            // Update is called once per frame
            void Update()
            {
                m_timer += Time.deltaTime;
                m_rectTr.localRotation = Quaternion.AngleAxis(m_rotationSpeed * m_timer, Vector3.forward);
            }
        }

    }
}

