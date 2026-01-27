using UnityEngine;


namespace SXG2025
{
    public class RotJointPart : MonoBehaviour
    {
        public enum ControlMode
        {
            DirectDirection,     // 直接的に旋回したい方向とパワーを指定する 
            RotateToLocalAngle,    // 指定したローカルのヨー角に向けて移動させる 
        }

        private ControlMode m_currentControlMode = ControlMode.DirectDirection;
        public ControlMode CurrentControlMode => m_currentControlMode;

        private float m_controlAngle = 0;
        private Quaternion m_baseRotation = Quaternion.identity;
        private float m_localAngle = 0;


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // 初期値を記録 
            m_baseRotation = transform.localRotation;
        }

        // Update is called once per frame
        void Update()
        {
            switch (m_currentControlMode)
            {
                case ControlMode.DirectDirection:
                    UpdateControl_DirectDirection();
                    break;
                case ControlMode.RotateToLocalAngle:
                    UpdateControl_RotateToLocalAngle();
                    break;
            }
        }

        /// <summary>
        /// 回転方向を指定してジョイントを回転させる 
        /// </summary>
        /// <param name="angleDir"></param>
        internal void ControlDirectDirection(float angleDir)
        {
            m_currentControlMode = ControlMode.DirectDirection;
            m_controlAngle = angleDir;
        }

        /// <summary>
        /// 目標角度を指定してジョイントを回転させる 
        /// </summary>
        /// <param name="targetAngle"></param>
        internal void ControlTargetAngle(float targetAngle)
        {
            m_currentControlMode = ControlMode.RotateToLocalAngle;
            m_controlAngle = targetAngle;
        }



        private void UpdateControl_DirectDirection()
        {
            var data = GameDataHolder.Instance.DataTank;

            // 角度更新 
            m_localAngle += data.m_rotateJointRotSpeedYaw * Time.deltaTime * Mathf.Clamp(m_controlAngle, -1.0f, +1.0f );

            // 角度反映 
            transform.localRotation = m_baseRotation * Quaternion.AngleAxis(m_localAngle, Vector3.up);
        }

        private void UpdateControl_RotateToLocalAngle()
        {
            var data = GameDataHolder.Instance.DataTank;

            float last = m_localAngle;

            // 角度更新と反映 
            Quaternion lastRot = Quaternion.AngleAxis(m_localAngle, Vector3.up);
            Quaternion targetRot = Quaternion.AngleAxis(m_controlAngle, Vector3.up);
            Quaternion calculatedRot = Quaternion.RotateTowards(lastRot, targetRot, data.m_rotateJointRotSpeedYaw * Time.deltaTime);

            // 角度反映 
            transform.localRotation = m_baseRotation * calculatedRot;

            // 角度書き戻し 
            Vector3 rotatedDir = calculatedRot * Vector3.right;
            m_localAngle = Mathf.Atan2(-rotatedDir.z, rotatedDir.x) * (180.0f/Mathf.PI);

            //Debug.Log("[Rot]" + gameObject.name + " " + last + " -> " + m_localAngle + " | T=" + Time.frameCount);
        }

    }
}


