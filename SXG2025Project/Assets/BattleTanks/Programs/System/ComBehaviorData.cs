using UnityEngine;


namespace SXG2025
{

    public enum TurretControlMode
    {
        None,
        Direction,      // 旋回方向を指定
        TargetAngle,    // 目標ローカル角度を指定
        TargetPoint,    // 目標着弾座標を指定
    }

    public enum JointControlMode
    {
        None,
        Direction,      // 旋回方向を指定 
        TargetAngle,    // 目標ローカル角度を指定 
    }


    public struct ComBehaviorData
    {
        public struct TurretData
        {
            public bool m_shootTrigger;     // 大砲発射フラグ 
            public TurretControlMode m_controlMode;
            public float m_targetYawAngle;  // 砲塔のヨー角
            public float m_targetPitchAngle;    // 砲塔のピッチ角 
            public Vector3 m_targetPoint;   // 着弾目標地 
        }

        public struct JointData
        {
            public JointControlMode m_controlMode;
            public float m_targetAngle; // ジョイントのロール角 
        }

        public float m_leftCaterpillarPower;
        public float m_rightCaterpillarPower;
        public TurretData[] m_turretData;
        public JointData[] m_jointData;


        public void Reset(int turretCount, int jointCount)
        {
            m_leftCaterpillarPower = 0;
            m_rightCaterpillarPower = 0;

            if (m_turretData == null || m_turretData.Length != turretCount)
            {
                m_turretData = new TurretData[turretCount];
            }
            for (int i=0; i < m_turretData.Length; ++i)
            {
                m_turretData[i] = new();
            }

            if (m_jointData == null || m_jointData.Length != jointCount)
            {
                m_jointData = new JointData[jointCount];
            }
            for (int i=0; i < m_jointData.Length; ++i)
            {
                m_jointData[i] = new();
            }
        }
    }


}

