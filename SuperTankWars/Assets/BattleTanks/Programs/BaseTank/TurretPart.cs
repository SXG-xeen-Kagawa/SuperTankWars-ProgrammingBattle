using UnityEngine;


namespace SXG2025
{

    public class TurretPart : MonoBehaviour
    {
        [SerializeField] private Transform m_yawRotTr = null;       // 砲塔のヨー回転Transform 
        [SerializeField] private Transform m_pitchRotTr = null;     // 砲身のピッチ回転Transform
        [SerializeField] private Transform m_barrelTopTr = null;    // 砲身の先Transform

        public enum ControlMode
        {
            DirectDirection,     // 直接的に旋回したい方向とパワーを指定する 
            RotateToLocalAngle,    // 指定したローカルのヨー角、ピッチ角に向けて移動させる 
            AimAtImpactPoint,   // 着弾地点を指定してそこに向ける 
        }

        private ControlMode m_currentControlMode = ControlMode.DirectDirection;
        public ControlMode CurrentControlMode => m_currentControlMode;

        private Vector3 m_controlAngle = Vector3.zero;
        private Vector3 m_impactPoint = Vector3.zero;


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        /// <summary>
        /// 制御：DirectDirection：ヨー角とピッチ角の旋回力を指定する 
        /// </summary>
        /// <param name="yawPower"></param>
        /// <param name="pitchPower"></param>
        internal void ControlDirectDirection(float yawPower, float pitchPower)
        {
            m_currentControlMode = ControlMode.DirectDirection;
            m_controlAngle = new Vector3(pitchPower, yawPower, 0);
        }


        /// <summary>
        /// 制御：AimAtImpactPoint：着弾地点を指定する
        /// </summary>
        /// <param name="targetPoint"></param>
        internal void ControlTargetPoint(Vector3 targetPoint)
        {
            m_currentControlMode = ControlMode.AimAtImpactPoint;
            m_impactPoint = targetPoint;
        }



        private void Update()
        {
            switch (m_currentControlMode)
            {
                case ControlMode.DirectDirection:
                    UpdateControl_DirectDirection();
                    break;
                case ControlMode.AimAtImpactPoint:
                    UpdateControl_AimAtImpactPoint();
                    break;
            }
        }


        private void UpdateControl_DirectDirection()
        {
            var data = GameDataHolder.Instance.DataTank;

            // 砲塔のヨー角度 
            if (m_controlAngle.y != 0)
            {
                Vector3 eulerAngles = m_yawRotTr.localRotation.eulerAngles;
                float yawAngle = eulerAngles.y + m_controlAngle.y * (data.m_turretRotSpeedYaw * Time.deltaTime);
                m_yawRotTr.localRotation = Quaternion.Euler(0, yawAngle, 0);
            }

            // 砲身のピッチ角度 
            if (m_controlAngle.x != 0)
            {
                Vector3 eulerAngles = m_pitchRotTr.localRotation.eulerAngles;    // 常に 0～360で値が返ってくる仕様 
                float pitchAngle = (eulerAngles.x < 180.0f) ? eulerAngles.x : eulerAngles.x - 360;
                pitchAngle = pitchAngle + m_controlAngle.x * (data.m_turretRotSpeedPitch * Time.deltaTime);
                pitchAngle = Mathf.Clamp(pitchAngle, data.m_turretRotPitchLimitUp, data.m_turretRotPitchLimitDown);
                m_pitchRotTr.localRotation = Quaternion.Euler(pitchAngle, 0, 0);
            }

        }

        private void UpdateControl_AimAtImpactPoint()
        {
            const float AIM_OFFSET_Y = 0.5f;    // 着弾目標座標の少しだけ上を狙う 

            var data = GameDataHolder.Instance.DataTank;

            // 着弾目標座標を砲塔空間のローカル座標にする 
            Vector3 localImpactPoint = transform.InverseTransformPoint(m_impactPoint+Vector3.up*AIM_OFFSET_Y);
            // 簡易的に平面で考えて上方へ補正する 
            float distance = localImpactPoint.magnitude;
            float impactTime = distance / data.m_shootCannonShellVelocity;
            localImpactPoint -= (Physics.gravity*GameConstants.CANNON_SHELL_GRAVITY_SCALE) * (0.5f * impactTime * impactTime);

            // ヨー回転
            Quaternion lookXZ = Quaternion.LookRotation(new Vector3(localImpactPoint.x, 0, localImpactPoint.z));
            m_yawRotTr.localRotation = Quaternion.RotateTowards(m_yawRotTr.localRotation, lookXZ, data.m_turretRotSpeedYaw * Time.deltaTime);

            // ピッチ回転 
            float distanceXZ = Mathf.Sqrt(localImpactPoint.x * localImpactPoint.x + localImpactPoint.z * localImpactPoint.z);
            Quaternion lookZY = Quaternion.LookRotation(
                new Vector3(0, localImpactPoint.y, distanceXZ));
            Quaternion targetPitchRot = Quaternion.RotateTowards(m_pitchRotTr.localRotation, lookZY, data.m_turretRotSpeedPitch * Time.deltaTime);
            Vector3 frontDir = targetPitchRot * Vector3.forward;
            float pitchAngle = Mathf.Acos(frontDir.z);
            if (0 < frontDir.y)
            {
                pitchAngle = -pitchAngle;
            }
            m_pitchRotTr.localRotation = Quaternion.Euler(pitchAngle * 180.0f / Mathf.PI, 0, 0);
        }



        /// <summary>
        /// 砲身の先の座標と角度を取得 
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="worldRotatoin"></param>
        internal void GetTopPositionAndRotation(out Vector3 worldPosition, out Quaternion worldRotation)
        {
            worldPosition = m_barrelTopTr.position;
            worldRotation = m_barrelTopTr.rotation;
        }
    }


}

