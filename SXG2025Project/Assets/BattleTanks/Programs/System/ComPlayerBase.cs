using System.Collections.Generic;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace SXG2025
{

    public partial class ComPlayerBase : MonoBehaviour
    {
        // 所属組織 
        [SerializeField] private string m_yourOrganization = "Please input your organization.";
        public string Organization => m_yourOrganization;

        // 名前 
        [SerializeField] private string m_yourName = "Please input your name.";
        public string YourName => m_yourName;

        // 顔画像 
        [SerializeField] private Sprite m_faceImage = null;
        public Sprite FaceImage { 
            get
            {
                if(m_faceImage == null)
                {
                    Sprite sp = Resources.Load<Sprite>("Textures/noimage");
                    if (sp != null) return sp;
                    return null;
                }
                return m_faceImage;
            }
        }


        // 砲塔(Turret)を登録 
        [SerializeField] private TurretPart[] m_turrets = null;

        // 回転ジョイントを登録 
        [SerializeField] private RotJointPart[] m_rotJoints = null;


        protected int GetCountOfTurrets => (m_turrets == null) ? 0 : m_turrets.Length;


        private ComBehaviorData m_comBehaviorData = new();

        private BattleTanksManager m_gameManager = null;


        private int m_id = 0;

        public void SetPlayerData(string organization, string name, Sprite icon)
        {
            m_yourOrganization = organization;
            m_yourName = name;
            m_faceImage = icon;
        }

        public void Setup(int id, BattleTanksManager gameManager)
        {
            m_id = id;
            m_comBehaviorData.Reset(m_turrets.Length, m_rotJoints.Length);

            m_gameManager = gameManager;
        }


        /// <summary>
        /// [for System] ComBehaviourDataを取得、そしてリセット 
        /// </summary>
        /// <returns></returns>
        public ComBehaviorData GetComBehaviorDataAndReset()
        {
            var retval = m_comBehaviorData;
            // m_turretDataをディープコピー 
            if (m_comBehaviorData.m_turretData != null)
            {
                retval.m_turretData = (ComBehaviorData.TurretData[])m_comBehaviorData.m_turretData.Clone();
            }
            // m_jointDataをディープコピー 
            if (m_comBehaviorData.m_jointData != null)
            {
                retval.m_jointData = (ComBehaviorData.JointData[])m_comBehaviorData.m_jointData.Clone();
            }
            // リセット 
            m_comBehaviorData.Reset(m_turrets.Length, m_rotJoints.Length);

            return retval;
        }


        /// <summary>
        /// チームカラーを設定 
        /// </summary>
        /// <param name="colorMaterial"></param>
        public void SetTeamColor(Material colorMaterial)
        {
            // 砲塔にカラーマテリアルを設定 
            foreach (var turret in m_turrets)
            {
                var meshes = turret?.GetComponentsInChildren<MeshRenderer>();
                foreach (var mesh in meshes)
                {
                    mesh.material = colorMaterial;
                }
            }
        }


        public delegate void GetTurretsDelegate(TurretPart[] turrets);
        public void GetTurrets(GetTurretsDelegate callback)
        {
            if (callback!=null)
            {
                callback.Invoke(m_turrets);
            }
        }

        public delegate void GetRotJointsDelegate(RotJointPart[] joints);
        public void GetRotJoints(GetRotJointsDelegate callback)
        {
            if (callback != null)
            {
                callback.Invoke(m_rotJoints);
            }
        }

        private void OnDestroy()
        {
            DeleteDebugDraw();
        }


        #region 参照用の関数群

        /// <summary>
        /// 自分の戦車の砲台の数を取得 
        /// </summary>
        /// <returns></returns>
        protected int SXG_GetCountOfMyTurrets()
        {
            return m_turrets.Length;
        }

        /// <summary>
        /// 自分の戦車の回転ジョイントの数を取得 
        /// </summary>
        /// <returns></returns>
        protected int SXG_GetCountOfMyRotJoints()
        {
            return m_rotJoints.Length;
        }

        /// <summary>
        /// 周囲の障害物の座標を取得
        /// </summary>
        /// <returns></returns>
        protected Vector3[] SXG_GetHitObstacles(float searchLength)
        {
            // 座標取得
            SXG_GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
            List<Vector3> list = new List<Vector3>();

            // 自分の位置を中心に球のレイキャスト
            Collider[] hitColliders = Physics.OverlapSphere(position, searchLength, LayerMask.GetMask("Ground"));

            foreach (Collider hit in hitColliders)
            {
                Transform current = hit.transform;

                // root まで親をたどりながらチェック
                while (current != null)
                {
                    if (current.CompareTag("Obstacle"))
                    {
                        list.Add(hit.transform.position);
                        break;
                    }

                    // root まで行ったら終了
                    if (current == current.root)
                        break;

                    current = current.parent;
                }
            }

            return list.ToArray();
        }

        /// <summary>
        /// 左右のキャタピラを回転させる。それぞれ -1 ～ 1 で、マイナスは後進、プラスは前進 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        protected void SXG_SetCaterpillarPower(float left, float right)
        {
            m_comBehaviorData.m_leftCaterpillarPower = left;
            m_comBehaviorData.m_rightCaterpillarPower = right;
        }

        /// <summary>
        /// 砲塔を指定して砲弾を発射 
        /// </summary>
        /// <param name="m_turretNo"></param>
        protected void SXG_Shoot(int turretNo)
        {
            Assert.IsNotNull(m_comBehaviorData.m_turretData);
            Assert.IsTrue(0 <= turretNo && turretNo < m_comBehaviorData.m_turretData.Length);
            m_comBehaviorData.m_turretData[turretNo].m_shootTrigger = true;
        }

        /// <summary>
        /// クールタイムを考慮したうえで砲弾を発射可能か？ 
        /// </summary>
        /// <param name="turretNo"></param>
        /// <returns></returns>
        protected bool SXG_CanShoot(int turretNo)
        {
            Assert.IsNotNull(m_comBehaviorData.m_turretData);
            Assert.IsTrue(0 <= turretNo && turretNo < m_comBehaviorData.m_turretData.Length);
            return m_gameManager.CanShootTheTank(m_id, turretNo);
        }


        /// <summary>
        /// ヨー、ピッチの回転方法を指定して砲塔を旋回させる
        /// </summary>
        /// <param name="turretId">砲塔番号</param>
        /// <param name="yawDir">左右の旋回：-1 ～ +1：右方向がプラス、左方向がマイナス</param>
        /// <param name="pitchDir">上下の旋回：-1 ～ +1：下方向がプラス、上方向がマイナス</param>
        protected void SXG_RotateTurretToDirection(int turretId, float yawDir, float pitchDir)
        {
            Assert.IsNotNull(m_comBehaviorData.m_turretData);
            Assert.IsTrue(0 <= turretId && turretId < m_comBehaviorData.m_turretData.Length);

            m_comBehaviorData.m_turretData[turretId].m_controlMode = TurretControlMode.Direction;
            m_comBehaviorData.m_turretData[turretId].m_targetYawAngle = yawDir;
            m_comBehaviorData.m_turretData[turretId].m_targetPitchAngle = pitchDir;
        }


        /// <summary>
        /// ローカル角度(ヨー、ピッチ)を指定して砲塔を旋回させる 
        /// </summary>
        /// <param name="turretId">砲塔番号</param>
        /// <param name="yawPower">左右の旋回：戦車の正面が0度：右方向がプラス、左方向がマイナス</param>
        /// <param name="pitchPower">上下の旋回：戦車の正面が0度：下方向がプラス、上方向がマイナス</param>
        [System.Obsolete("この関数は使用しても何も起こりません。")]
        protected void SXG_RotateTurretToAngle(int turretId, float yawAngle, float pitchAngle)
        {
            Debug.LogError("SXG_RotateTurretToAngle関数は使用しないでください。");

            //Assert.IsNotNull(m_comBehaviorData.m_turretData);
            //Assert.IsTrue(0 <= turretId && turretId < m_comBehaviorData.m_turretData.Length);

            //m_comBehaviorData.m_turretData[turretId].m_controlMode = TurretControlMode.TargetAngle;
            //m_comBehaviorData.m_turretData[turretId].m_targetYawAngle = yawAngle;
            //m_comBehaviorData.m_turretData[turretId].m_targetPitchAngle = pitchAngle;
        }

        /// <summary>
        /// 着弾予定座標を指定して砲塔を旋回させる
        /// </summary>
        /// <param name="turretId"></param>
        /// <param name="impactPoint"></param>
        protected void SXG_RotateTurretToImpactPoint(int turretId, Vector3 impactPoint)
        {
            Assert.IsNotNull(m_comBehaviorData.m_turretData);
            Assert.IsTrue(0 <= turretId && turretId < m_comBehaviorData.m_turretData.Length);

            m_comBehaviorData.m_turretData[turretId].m_controlMode = TurretControlMode.TargetPoint;
            m_comBehaviorData.m_turretData[turretId].m_targetPoint = impactPoint;
        }


        /// <summary>
        /// ジョイント関節の回転方法を指定してジョイントを旋回させる
        /// </summary>
        /// <param name="jointId">ジョイント関節番号</param>
        /// <param name="yawDir">左右の旋回：-1 ～ +1：右方向がプラス、左方向がマイナス</param>
        protected void SXG_RotateJointToDirection(int jointId, float yawDir)
        {
            Assert.IsNotNull(m_comBehaviorData.m_jointData);
            Assert.IsTrue(0 <= jointId && jointId < m_comBehaviorData.m_jointData.Length);

            m_comBehaviorData.m_jointData[jointId].m_controlMode = JointControlMode.Direction;
            m_comBehaviorData.m_jointData[jointId].m_targetAngle = yawDir;
        }


        /// <summary>
        /// ローカル角度(ヨー)を指定して砲塔を旋回させる 
        /// </summary>
        /// <param name="jointId">ジョイント関節番号</param>
        /// <param name="yawPower">左右の旋回：戦車の正面が0度：右方向がプラス、左方向がマイナス</param>
        protected void SXG_RotateJointToAngle(int jointId, float yawAngle)
        {
            Assert.IsNotNull(m_comBehaviorData.m_jointData);
            Assert.IsTrue(0 <= jointId && jointId < m_comBehaviorData.m_jointData.Length);

            m_comBehaviorData.m_jointData[jointId].m_controlMode = JointControlMode.TargetAngle;
            m_comBehaviorData.m_jointData[jointId].m_targetAngle = yawAngle;
        }




        /// <summary>
        /// 自身(戦車)のワールド座標と角度を取得 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        protected void SXG_GetPositionAndRotation(out Vector3 position, out Quaternion rotation)
        {
            if (m_gameManager != null)
            {
                m_gameManager.GetTankPositionAndRotation(m_id, out position, out rotation);
            } else
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
            }
        }



        public struct TankInfo
        {
            public Vector3 Position;    // 戦車の座標 
            public Quaternion Rotation; // 戦車の向き
            public int Energy;          // チームの残りエナジー 
            public int CostOfOneTank;   // 戦車1台分のコスト 
            public bool IsDefeated;     // 敗退済みフラグ
            public bool IsInvincible;   // 無敵フラグ
        }

        /// <summary>
        /// 全チームの戦車の情報を取得
        /// </summary>
        /// <returns>返り値の配列の[0]は必ず自分の情報</returns>
        protected TankInfo []   SXG_GetAllTanksInfo()
        {
            TankInfo[] tanksInfo = new TankInfo[GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE];

            if (m_gameManager != null)
            {
                for (int i = 0; i < tanksInfo.Length; ++i)
                {
                    tanksInfo[i] = new();
                    int globalId = (m_id + i) % GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE;
                    m_gameManager.GetTankPositionAndRotation(globalId, out tanksInfo[i].Position, out tanksInfo[i].Rotation);
                    m_gameManager.GetTankInfo(globalId, out tanksInfo[i].Energy, out tanksInfo[i].CostOfOneTank,
                        out tanksInfo[i].IsDefeated, out tanksInfo[i].IsInvincible);
                }
            }

            return tanksInfo;
        }



        #endregion



        #region 各自でオーバーライドして実装が必要な関数群
        #endregion



    }


}

