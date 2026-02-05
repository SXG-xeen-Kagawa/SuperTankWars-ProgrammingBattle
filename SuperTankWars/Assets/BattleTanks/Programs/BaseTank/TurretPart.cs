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

        internal void ControlTargetLocalAngle(float yawDeg, float pitchDegSigned)
        {
            m_currentControlMode = ControlMode.RotateToLocalAngle;
            m_controlAngle = new Vector3(pitchDegSigned, yawDeg, 0);
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
            var data = GameDataHolder.Instance.DataTank;

            // 現在角 
            float currYaw = GetLocalYawDeg();           // 0～360
            float currPitch = GetLocalPitchDegSigned(); // -180～180

            // 目標角
            float targetYaw = currYaw;
            float targetPitch = currPitch;

            switch (m_currentControlMode)
            {
                case ControlMode.DirectDirection:
                    {
                        targetYaw = currYaw + m_controlAngle.y * data.m_turretRotSpeedYaw * Time.deltaTime;
                        targetPitch = currPitch + m_controlAngle.x * data.m_turretRotSpeedPitch * Time.deltaTime;
                    }
                    break;
                case ControlMode.RotateToLocalAngle:
                    {
                        targetYaw = m_controlAngle.y;
                        targetPitch = m_controlAngle.x;
                    }
                    break;
                case ControlMode.AimAtImpactPoint:
                    {
                        ComputeAimAnglesFromImpactPoint(out targetYaw, out targetPitch);
                    }
                    break;
            }

            // 寛容な入力処理 
            targetYaw = NormalizeYawDeg(targetYaw);
            targetPitch = Mathf.Clamp(targetPitch, data.m_turretRotPitchLimitUp, data.m_turretRotPitchLimitDown);

            // 実際の角度適用
            ApplyAngleDrive(targetYaw, targetPitch, data);

        }


        /// <summary>
        /// 着弾地点から、目標Yaw/Pitch角（ローカル角・度）を算出する
        /// yaw：正面0、右回り（時計回り）がプラス
        /// pitch：上がマイナス、下がプラス
        /// </summary>
        private void ComputeAimAnglesFromImpactPoint(out float targetYawDeg, out float targetPitchDegSigned)
        {
            const float AIM_OFFSET_Y = 0.5f; // 着弾目標座標の少しだけ上を狙う（見た目調整）

            var data = GameDataHolder.Instance.DataTank;

            // 着弾目標座標を砲塔空間のローカル座標にする
            Vector3 local = transform.InverseTransformPoint(m_impactPoint + Vector3.up * AIM_OFFSET_Y);

            // 簡易的に弾道を補正（現行仕様踏襲）
            float distance = local.magnitude;
            float impactTime = distance / data.m_shootCannonShellVelocity;
            local -= (Physics.gravity * GameConstants.CANNON_SHELL_GRAVITY_SCALE) * (0.5f * impactTime * impactTime);

            // yaw：x（右）, z（前）から算出。右（+x）方向で＋になる
            targetYawDeg = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;

            // pitch：上（+y）でマイナス、下（-y）でプラスにしたいので符号を反転する
            float distanceXZ = Mathf.Sqrt(local.x * local.x + local.z * local.z);
            targetPitchDegSigned = -Mathf.Atan2(local.y, distanceXZ) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// 目標Yaw/Pitch角へ、速度制限つきで追従させる（全モード共通）
        /// </summary>
        private void ApplyAngleDrive(float targetYawDeg, float targetPitchDegSigned, DataFormatTank data)
        {
            // Yaw：0..360表現で MoveTowardsAngle
            float curYaw = GetLocalYawDeg();
            float newYaw = Mathf.MoveTowardsAngle(curYaw, targetYawDeg, data.m_turretRotSpeedYaw * Time.deltaTime);
            SetLocalYawDeg(newYaw);

            // Pitch：signed表現で MoveTowardsAngle
            float curPitch = GetLocalPitchDegSigned();
            float newPitch = Mathf.MoveTowardsAngle(curPitch, targetPitchDegSigned, data.m_turretRotSpeedPitch * Time.deltaTime);
            SetLocalPitchDeg(newPitch);
        }

        /// <summary>
        /// yaw角を 0..360 に正規化する（-720 や 9999 など、どんな値でもOKにする）
        /// </summary>
        private float NormalizeYawDeg(float yawDeg)
        {
            return Mathf.Repeat(yawDeg, 360f);
        }

        /// <summary>
        /// 砲塔のヨー角度（ローカル・度）を取得（0..360）
        /// </summary>
        private float GetLocalYawDeg()
        {
            return m_yawRotTr.localEulerAngles.y;
        }

        /// <summary>
        /// 砲身のピッチ角度（ローカル・度）を取得（-180..180）
        /// </summary>
        private float GetLocalPitchDegSigned()
        {
            float x = m_pitchRotTr.localEulerAngles.x; // 0..360
            return (x <= 180f) ? x : x - 360f;         // -180..180
        }

        /// <summary>
        /// 砲塔のヨー角度（ローカル・度）を設定
        /// </summary>
        private void SetLocalYawDeg(float yawDeg)
        {
            m_yawRotTr.localRotation = Quaternion.Euler(0f, yawDeg, 0f);
        }

        /// <summary>
        /// 砲身のピッチ角度（ローカル・度）を設定（signed想定：上がマイナス）
        /// </summary>
        private void SetLocalPitchDeg(float pitchDegSigned)
        {
            m_pitchRotTr.localRotation = Quaternion.Euler(pitchDegSigned, 0f, 0f);
        }

        /// <summary>
        /// 砲身先端のワールド座標と回転を取得する
        /// </summary>
        internal void GetTopPositionAndRotation(out Vector3 worldPosition, out Quaternion worldRotation)
        {
            worldPosition = m_barrelTopTr.position;
            worldRotation = m_barrelTopTr.rotation;
        }


     }


}

