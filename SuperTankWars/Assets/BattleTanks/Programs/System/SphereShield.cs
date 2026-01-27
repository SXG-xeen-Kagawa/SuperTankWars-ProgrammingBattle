using UnityEngine;


namespace SXG2025
{

    public class SphereShield : MonoBehaviour
    {
        public delegate bool CanReduceRemainingTimeDelegate();
        private CanReduceRemainingTimeDelegate m_canReduceRemainingTimeFunc = null;

        public delegate void FinishedShieldDelegate();
        private FinishedShieldDelegate m_finishedShieldFunc = null;

        private Transform m_targetTr = null;
        private float m_remainingTime = 0;
        private Vector3 m_offset = Vector3.zero;
        private float m_time = 0;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            m_time = Random.Range(0.0f, Mathf.PI*2.0f);
        }

        // Update is called once per frame
        void LateUpdate()
        {
            // 回転 
            Quaternion newRot = transform.rotation * Quaternion.AngleAxis(360.0f*Time.deltaTime,
                new Vector3(Mathf.Cos(m_time), Mathf.Sin(m_time), 0));
            m_time += Mathf.PI*Time.deltaTime;

            // 戦車の位置に併せる 
            if (m_targetTr != null)
            {
                Vector3 newPos = m_targetTr.TransformPoint(m_offset);
                transform.SetPositionAndRotation(newPos, newRot);
            }

            // バリア時間 
            if (m_canReduceRemainingTimeFunc != null)
            {
                if (m_canReduceRemainingTimeFunc.Invoke())
                {
                    m_remainingTime -= Time.deltaTime;
                }
                if (m_remainingTime <= 0)
                {
                    if (m_finishedShieldFunc != null)
                    {
                        m_finishedShieldFunc.Invoke();
                    }
                    Destroy(this.gameObject);
                }
            }
        }



        /// <summary>
        /// セットアップ 
        /// </summary>
        /// <param name="tankTr"></param>
        /// <param name="canReduceFunc"></param>
        public void Setup(Transform tankTr, Bounds tankBounds, 
            CanReduceRemainingTimeDelegate canReduceFunc,
            FinishedShieldDelegate finishedShieldFunc)
        {
            m_targetTr = tankTr;
            m_remainingTime = GameDataHolder.Instance.DataGame.m_invincibleTimeAfterSpawn;
            m_canReduceRemainingTimeFunc = canReduceFunc;
            m_finishedShieldFunc = finishedShieldFunc;

            // オフセット位置 
            float boundsSize = 0;
            if (tankBounds.size == Vector3.zero)
            {
                m_offset = Vector3.zero;
                boundsSize = 0;
            } else
            {
                m_offset = m_targetTr.InverseTransformPoint(tankBounds.center);
                m_offset.y = Mathf.Max(0, m_offset.y - 0.5f);   // 微調整 
                boundsSize = tankBounds.size.magnitude;
            }
            float radius = Mathf.Clamp(boundsSize,
                GameDataHolder.Instance.DataGame.m_minInvinsibleShieldRadius,
                GameDataHolder.Instance.DataGame.m_maxInvinsibleShieldRadius);
            this.transform.localScale = Vector3.one * radius;
        }
    }


}


