using SXG2025.Effect;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;



namespace SXG2025
{

    public class BaseTank : MonoBehaviour
    {
        public delegate CannonShell GetNewCannonShellDelegate();
        public delegate void ReleaseCannonShellDelegate(CannonShell shell);

        public delegate void DestroiedTankDelegate(int teamNo, int attackTeamNo);

        public delegate bool TankPartDamagedDelegate(int teamNo, Collider collidedCollider, Vector3 contactPoint);


        const float DESTROIED_EXPLOSION_SCALE = 3.0f;   // 破壊時の爆発エフェクト 



        [SerializeField] private float m_maxWheelTorque = 2000;
        [SerializeField] private GameObject m_playerCharaObj = null;

        private int m_teamNo = 0;

        private TurretPart[] m_turrets = null;      // 砲塔 
        private RotJointPart[] m_joints = null;     // 回転ジョイント 

        private List<CaterpillarObj> m_caterpillarsList = new();
        class CaterpillarObj
        {
            public CaterpillarPart m_partObj = null;
            public float m_torqueScale = 1;
        }

        private GetNewCannonShellDelegate m_getCannonShellFunc = null;
        private ReleaseCannonShellDelegate m_releaseCannonShellFunc = null;

        private DestroiedTankDelegate m_destroiedTankFunc = null;

        private TankPartDamagedDelegate m_tankPartDamagedDelegate = null;

        private Rigidbody m_rigidbody = null;
        private bool m_isDestroied = false;

        private Vector3 m_battleAreaCenter = Vector3.zero;
        private float m_battleAreaRadius = 0.0f;
        private float m_battleAreaReturnForce = 0.0f;
        private float m_battleAreaSphereCenterY = 0;
        private float m_battleAreaCoeffY = 1.0f;

        private Material m_materialInstance = null;
        private Material m_materialInstance2 = null;

        private Vector3 m_lastDamageContactPoint = Vector3.zero;
        private float m_lastDamageRadius = 0;


        /// <summary>
        /// 砲塔の数 
        /// </summary>
        public int TurretCount => m_turrets.Length;


        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody>();
        }


        internal void SetBattleArea(DataFormatGame data)
        {
            m_battleAreaCenter = data.m_battleAreaCenter;
            m_battleAreaRadius = Mathf.Max(0.0f, data.m_battleAreaRadius);
            m_battleAreaSphereCenterY = data.m_battleAreaSphereCenterY;
            m_battleAreaCoeffY = data.m_battleAreaCoeffY;
            m_battleAreaReturnForce = Mathf.Max(0.0f, data.m_battleAreaPushForceAcc);
        }



        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // キャタピラを設置 
            SetCaterpillarPart(new Vector3(-1, 0, 0), Quaternion.identity, 1.0f, animDir: 1.0f);   // 左 
            SetCaterpillarPart(new Vector3(+1, 0, 0), Quaternion.AngleAxis(180,Vector3.up), 1.0f, animDir: -1.0f);   // 右 (WheelCollider回転してても回転方向の考慮しなくて良いみたい)

            // パラメータ設定 
            DataFormatTank dataTank = GameDataHolder.Instance.DataTank;
            m_rigidbody.maxAngularVelocity = dataTank.m_tankMaxAngularVelocity;
        }

        /// <summary>
        /// キャタピラ作成
        /// </summary>
        /// <param name="localPosition"></param>
        /// <param name="localRotation"></param>
        /// <param name="torqueScale"></param>
        private void SetCaterpillarPart(Vector3 localPosition, Quaternion localRotation, float torqueScale, float animDir)
        {
            CaterpillarObj obj = new();

            obj.m_partObj = Instantiate(PrefabHolder.Instance.CaterpillarPartPrefab, this.transform);
            obj.m_partObj.SetAnimationDir(animDir);
            obj.m_partObj.transform.SetLocalPositionAndRotation(localPosition, localRotation);
            obj.m_torqueScale = torqueScale;

            m_caterpillarsList.Add(obj);
        }

        internal void SetTeam(int teamNo, Color baseColor)
        {
            m_teamNo = teamNo;

            // 本体にカラーリング 
            var bodies = GetComponentsInChildren<MeshRenderer>();
            if (0 < bodies.Length)
            {
                m_materialInstance = Instantiate(bodies[0].material);
                m_materialInstance.color = baseColor;
                m_materialInstance2 = Instantiate(bodies[0].material);
                m_materialInstance2.color = baseColor * 0.6f;   // Bodyは少し暗い色にしておく 

                foreach (var body in bodies)
                {
                    body.material = m_materialInstance2;
                }
            }
            // 運転手にカラーリング 
            var humans = m_playerCharaObj.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var hu in humans)
            {
                hu.material = m_materialInstance;
            }
        }

        public int TeamNo => m_teamNo;

        internal Material GetTeamColorMaterial()
        {
            return m_materialInstance;
        }


        /// <summary>
        /// 砲弾を扱うデリゲートを登録 
        /// </summary>
        /// <param name="getFunc"></param>
        /// <param name="releaseFunc"></param>
        internal void SetCannonShellDelegate(GetNewCannonShellDelegate getFunc, ReleaseCannonShellDelegate releaseFunc)
        {
            m_getCannonShellFunc = getFunc;
            m_releaseCannonShellFunc = releaseFunc;
        }

        /// <summary>
        /// 破壊された時のデリゲートを登録 
        /// </summary>
        /// <param name="destroiedFunc"></param>
        internal void SetDestroiedTankDelegate(DestroiedTankDelegate destroiedFunc)
        {
            m_destroiedTankFunc = destroiedFunc;
        }

        /// <summary>
        /// 戦車の部位がダメージを受けた時のコールバックを登録 
        /// </summary>
        /// <param name="damagedFunc"></param>
        internal void SetTankPartDamagedDelegate(TankPartDamagedDelegate damagedFunc)
        {
            m_tankPartDamagedDelegate = damagedFunc;
        }



        internal void ControlInDemo(float leftCaterpillarPower, float rightCaterpillarPower)
        {
            // キャタピラ制御  
            SetCaterpillarTorque(
                m_maxWheelTorque * Mathf.Clamp(leftCaterpillarPower, -1.0f, 1.0f),
                m_maxWheelTorque * Mathf.Clamp(rightCaterpillarPower, -1.0f, 1.0f),
                withPhysics: false);
        }


        /// <summary>
        /// ComBehaviorDataを使って制御する 
        /// </summary>
        /// <param name="behaviorData"></param>
        /// <param name="canShootFlag">砲弾を発射できる状況か？</param>
        internal void Control(ComBehaviorData behaviorData, bool canShootFlag, BattleTanksManager.TankWork tankWork)
        {
            // キャタピラ制御  
            SetCaterpillarTorque(
                m_maxWheelTorque * Mathf.Clamp(behaviorData.m_leftCaterpillarPower, -1.0f, 1.0f),
                m_maxWheelTorque * Mathf.Clamp(behaviorData.m_rightCaterpillarPower, -1.0f, 1.0f));

            // 砲塔制御 
            for (int i=0; i < behaviorData.m_turretData.Length; ++i)
            {
                var turretData = behaviorData.m_turretData[i];
                var turretPart = m_turrets[i];

                // null or 非アクティブなら無視
                if (turretPart == null || !turretPart.isActiveAndEnabled)
                    continue;

                // 旋回 
                switch (turretData.m_controlMode)
                {
                    case TurretControlMode.Direction:
                        turretPart.ControlDirectDirection(turretData.m_targetYawAngle, turretData.m_targetPitchAngle);
                        break;
                    case TurretControlMode.TargetAngle:
                        turretPart.ControlTargetLocalAngle(turretData.m_targetYawAngle, turretData.m_targetPitchAngle);
                        break;
                    case TurretControlMode.TargetPoint:
                        turretPart.ControlTargetPoint(turretData.m_targetPoint);
                        break;
                }

                // 発射 
                if (canShootFlag)
                {
                    // 発射フラグ確認＆クールタイム確認
                    if (turretData.m_shootTrigger)
                    {
                        if (tankWork.IsShootable(i))
                        {
                            // 実弾発射処理 
                            ShootShell(turretPart);
                            // 発射後のクールタイム設定 
                            tankWork.Shooted(i);
                        }
                    }
                }
            }

            // 回転ジョイント制御 
            for (int i=0; i < behaviorData.m_jointData.Length; ++i)
            {
                var jointData = behaviorData.m_jointData[i];
                var jointPart = m_joints[i];

                if (jointPart == null) continue;
                // 非アクティブなら無視
                if (!jointPart.isActiveAndEnabled)
                    continue;

                // 旋回 
                switch (jointData.m_controlMode)
                {
                    case JointControlMode.Direction:
                        jointPart.ControlDirectDirection(jointData.m_targetAngle);
                        break;
                    case JointControlMode.TargetAngle:
                        jointPart.ControlTargetAngle(jointData.m_targetAngle);
                        break;
                }
            }
        }


        internal void ResetControl()
        {
            // キャタピラ制御  
            SetCaterpillarTorque(0, 0);

        }

        /// <summary>
        /// 砲塔を紐づける 
        /// </summary>
        /// <param name="turrets"></param>
        internal void LinkTurrets(TurretPart [] turrets)
        {
            m_turrets = new TurretPart[turrets.Length];
            for (int i=0; i < m_turrets.Length; ++i)
            {
                m_turrets[i] = turrets[i];
            }
        }

        /// <summary>
        /// 回転ジョイントを紐づける 
        /// </summary>
        /// <param name="joints"></param>
        internal void LinkRotJoints(RotJointPart [] joints)
        {
            m_joints = new RotJointPart[joints.Length];
            for (int i=0; i < m_joints.Length; ++i)
            {
                m_joints[i] = joints[i];
            }
        }


        // Update is called once per frame
        void Update()
        {
            // 奈落に落ちた時の判定 
            if (transform.position.y < GameConstants.ABYSS_POSITION_Y)
            {
                Finish(-1);
            }
        }


        private float m_angularMax = 0;
        private void FixedUpdate()
        {
            float yawRad = Vector3.Dot(m_rigidbody.angularVelocity, transform.up);
            m_angularMax = Mathf.Max(Mathf.Abs(yawRad), m_angularMax);
            //Debug.Log("[Angular] " + this.name + " / " + yawRad + "(" + m_angularMax + ") | T=" + Time.frameCount);

            // 奈落に落ちた戦車は、落下判定を優先して救済しない。
            if (!m_isDestroied
                && m_battleAreaReturnForce > 0.0f)
            {
                CheckBattleArea();
            }
        }

        private void CheckBattleArea()
        {
            // 高度チェック
            if (transform.position.y < m_battleAreaSphereCenterY)
            {
                return;
            }

            // バトルエリア判定 
            Vector3 toBase = (m_battleAreaCenter + Vector3.up * m_battleAreaSphereCenterY) - transform.position;
            toBase.y *= m_battleAreaCoeffY;   // Y方向は範囲を狭くする 
            float radiusSqr = m_battleAreaRadius * m_battleAreaRadius;
            if (toBase.sqrMagnitude > radiusSqr)
            {
                Vector3 toCenter = m_battleAreaCenter - transform.position;
                m_rigidbody.AddForce(
                    toCenter.normalized * m_battleAreaReturnForce,
                    ForceMode.Acceleration);
            }
        }



        /// <summary>
        /// 砲身から弾を発射 
        /// </summary>
        /// <param name="turret"></param>
        private void ShootShell(TurretPart turret)
        {
            if (turret != null && m_getCannonShellFunc != null)
            {
                // 砲身の先の場所 
                Vector3 worldPosition;
                Quaternion worldRotation;
                turret.GetTopPositionAndRotation(out worldPosition, out worldRotation);

                // 砲弾を作成＆発射 
                var newShell = m_getCannonShellFunc.Invoke();
                if (newShell != null)
                {
                    var data = GameDataHolder.Instance.DataTank;

                    // 発射 
                    newShell.Shoot(worldPosition, worldRotation, data.m_shootCannonShellVelocity, m_teamNo,
                        (shell)=>
                        {
                            if (m_releaseCannonShellFunc != null)
                            {
                                m_releaseCannonShellFunc.Invoke(shell);
                            }
                        });

                    SoundController.PlaySE(SoundController.SEType.Shot, Mathf.Clamp(worldPosition.x / 9f, -0.4f, 0.4f));

                    // 反動 
                    Vector3 recoilDir = worldRotation * Vector3.forward;
                    m_rigidbody.AddForceAtPosition(recoilDir * data.m_shootRecoilForce, worldPosition); // AtPositionの方が面白い挙動になった 
                }
            }
        }


        /// <summary>
        /// 左右のキャタピラのホイールにトルクを設定 
        /// </summary>
        /// <param name="leftTorque"></param>
        /// <param name="rightTorque"></param>
        private void SetCaterpillarTorque(float leftTorque, float rightTorque, bool withPhysics=true)
        {
            float[] torque = new float[] { leftTorque, rightTorque };

            for (int i=0; i < torque.Length; ++i)
            {
                var obj = m_caterpillarsList[i];
                obj.m_partObj.SetTorque(torque[i] * obj.m_torqueScale, withPhysics);
            }
        }



        private void Finish(int attackTeamNo)
        {
            if (!m_isDestroied)
            {
                m_isDestroied = true;
                if (m_destroiedTankFunc != null)
                {
                    m_destroiedTankFunc.Invoke(m_teamNo, attackTeamNo);
                }
                SoundController.PlaySE(SoundController.SEType.Dead, Mathf.Clamp(transform.position.x / 9f, -0.4f, 0.4f));
            }
        }



        /// <summary>
        /// ダメージを受ける 
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="collidedCollider"></param>
        /// <param name="contactPoint"></param>
        /// <param name="damageRadius"></param>
        /// <retval>なんらかダメージを与えたらtrue</retval>
        internal bool PutDamageByShell(int attackTeamNo, int damage, Collider collidedCollider, Vector3 contactPoint, float damageRadius)
        {
            DataFormatTank dataTank = GameDataHolder.Instance.DataTank;

            // 部位にダメージ 
            var collidedPart = m_tankPartsList.Find((a) => { return a.m_partTr == collidedCollider.transform; });
            if (collidedPart != null)
            {
                if (collidedPart.m_durability < 0)
                {
                    return true;    // 既に耐久力無なら部位破壊済み 
                }

                Debug.Log("[Damage] " + collidedPart.m_partTr.name + " damage=" + damage 
                    + " hp=" + collidedPart.m_durability + " => " + (collidedPart.m_durability-damage) + " | T=" + Time.frameCount);

                collidedPart.m_durability -= damage;
                if (0 < collidedPart.m_durability)
                {
                    return true;     // 部位が破壊されていない
                } else
                {
                    // 部位を外す 
                    collidedPart.m_partTr.parent = null;
                    collidedPart.m_partTr.gameObject.layer = Constants.OBJ_LAYER_DROPPED_PART;
                    Rigidbody rb = collidedPart.m_partTr.gameObject.AddComponent<Rigidbody>();
                    rb.mass = (float)collidedPart.m_cost * dataTank.m_tankCostToMassCoef;

                    // 落下確認コンポーネント 
                    collidedPart.m_partTr.gameObject.AddComponent<DroppedPart>();

                    // リストから外す 
                    m_tankPartsList.Remove(collidedPart);

                    // 質量を再計算 
                    int cost = 0;
                    foreach (var part in m_tankPartsList)
                    {
                        if (!part.m_partTr)
                            continue;

                        if (part.m_partTr.GetComponentInParent<BaseTank>() == this)
                        {
                            cost += part.m_cost;
                        }
                    }
                    m_rigidbody.mass = CalculateTankMass(cost);
                }
                return true;
            }

            // 砲塔は無敵にする 
            if (collidedCollider.gameObject.GetComponentInParent<TurretPart>() != null)
            {
                Debug.Log("[Damage] 砲塔は無敵 " + collidedCollider.name + " | T=" + Time.frameCount);
                return false;
            }

            Debug.Log("[Damage] 直撃!! " + collidedCollider.name + " | T=" + Time.frameCount);


            // 爆発エフェクト 
            GameObject explosionObj = Instantiate(PrefabHolder.Instance.VfxTankDestroiedPrefab,
                transform.position, Quaternion.identity);
            explosionObj.transform.localScale = Vector3.one * DESTROIED_EXPLOSION_SCALE;

            // パラメータ
            m_lastDamageContactPoint = contactPoint;
            m_lastDamageRadius = damageRadius;

            // 破壊 
            Finish(attackTeamNo);

            return true;
        }


        /// <summary>
        /// 敗北キャラクターを切り離す 
        /// </summary>
        /// <returns></returns>
        internal GameObject DropLoserCharacter()
        {
            const float HUMAN_WEIGHT = 60.0f;   // 人間の体重
            const float HUMAN_DRAG = 0.1f;
            const float HUMAN_ANGULAR_DRAG = 0.12f;

            // プレイヤーキャラを剥がす 
            m_playerCharaObj.transform.parent = null;
            m_playerCharaObj.layer = Constants.OBJ_LAYER_DROPPED_PART;

            // RBを付ける 
            Rigidbody rb = m_playerCharaObj.AddComponent<Rigidbody>();
            rb.mass = HUMAN_WEIGHT;
            rb.linearDamping = HUMAN_DRAG;
            rb.angularDamping = HUMAN_ANGULAR_DRAG;

            // 落下確認コンポーネント 
            m_playerCharaObj.AddComponent<DroppedPart>();

            return m_playerCharaObj;
        }

        /// <summary>
        /// 敗北時に残っている部品を全部剥がす 
        /// </summary>
        internal void DropAllParts()
        {
            const float DEBRIS_DRAG = 0.1f;
            const float DEBRIS_ANGULAR_DRAG = 0.12f;

            DataFormatTank dataTank = GameDataHolder.Instance.DataTank;
            float volumeToMassCoef = dataTank.m_tankVolumeToCostCoef * dataTank.m_tankCostToMassCoef;

            var allMeshes = GetComponentsInChildren<MeshRenderer>();
            foreach (var mesh in allMeshes)
            {
                if (mesh.gameObject.activeSelf == false)
                {
                    continue;
                }
                if (mesh.gameObject.GetComponent<NotSubjectToMeasurement>() != null)
                {
                    continue;
                }

                // 剥がす 
                mesh.transform.parent = null;
                mesh.gameObject.layer = Constants.OBJ_LAYER_DROPPED_PART;

                // RBを付ける 
                Rigidbody rb = mesh.gameObject.AddComponent<Rigidbody>();
                float volume = CalcVolumeFromMesh(mesh, mesh.transform);
                rb.mass = volume * volumeToMassCoef;
                rb.linearDamping = DEBRIS_DRAG;
                rb.angularDamping = DEBRIS_ANGULAR_DRAG;
                //Debug.Log("[Mesh]" + mesh.name + " vol=" + volume + " | T=" + Time.frameCount);

                // 破裂 
                rb.AddExplosionForce(dataTank.m_shellCollidedExplosionForce*0.5f, m_lastDamageContactPoint, m_lastDamageRadius);

                // 落下確認コンポーネント 
                mesh.gameObject.AddComponent<DroppedPart>();
            }
        }



        #region コリジョン 

        private void OnTriggerEnter(Collider other)
        {
            // 奈落に落ちたら終了 
            if (other.gameObject.layer == Constants.OBJ_LAYER_VIRTUALWALL)
            {
                // ただし本体が奈落に接触した時のみ 
                if (transform.position.y < (GameConstants.ABYSS_POSITION_Y*0.05f))
                {
                    Finish(-1);
                }
            }
        }

        #endregion




        #region コスト関連 

        class TankPart
        {
            public enum PartType
            {
                Turret, // 砲塔 
                Armor,  // 装甲
                Joint,  // 回転ジョイント
            }

            public Transform m_partTr = null;
            public float m_objectVolume = 0;
            public int m_cost = 0;
            public float m_durability = 0;      // 耐久力 
            public PartType m_partType = PartType.Armor;
        }

        private List<TankPart> m_tankPartsList = new();
        private List<GameObject> m_errorObjList = new();    // レギュレーションエラーのオブジェクトのリスト 


        /// <summary>
        /// レギュレーションエラーのオブジェクトのリストを取得 
        /// </summary>
        /// <returns></returns>
        internal IReadOnlyList<GameObject>  GetErrorObjectsList()
        {
            return m_errorObjList;
        }


        /// <summary>
        /// 戦車のコスト計算 
        /// </summary>
        /// <param name="comPlayerBase"></param>
        /// <returns></returns>
        internal int CalculateTankCost(ComPlayerBase comPlayerBase, 
            out int countOfTurrets, 
            out int countOfRotators,
            out int countOfArmors, 
            out float tankMass)
        {
            DataFormatTank dataTank = GetData();

            Bounds regulationBounds;
            if (comPlayerBase.transform != null)
            {
                regulationBounds = new Bounds(
                    comPlayerBase.transform.TransformPoint(dataTank.m_regulationBounds.center), dataTank.m_regulationBounds.size);
            } else
            {
                regulationBounds = dataTank.m_regulationBounds;
            }


            m_tankPartsList.Clear();
            m_errorObjList.Clear();
            CalculateCostRecursively(comPlayerBase.transform, m_tankPartsList, m_errorObjList, regulationBounds);

            countOfTurrets = 0;
            countOfRotators = 0;
            countOfArmors = 0;

            // 総コストを計算 
            int cost = dataTank.m_tankBasePartCost;
            foreach (var part in m_tankPartsList)
            {
                cost += part.m_cost;

                switch (part.m_partType)
                {
                    case TankPart.PartType.Turret:
                        countOfTurrets++;
                        break;
                    case TankPart.PartType.Joint:
                        countOfRotators++;
                        break;
                    case TankPart.PartType.Armor:
                        countOfArmors++;
                        break;
                }
            }

            // 物理の質量を計算 
            tankMass = CalculateTankMass(cost);
            if (m_rigidbody != null)
            {
                m_rigidbody.mass = tankMass;
            }

            return cost;
        }

        /// <summary>
        /// 外部から呼び出し用 
        /// </summary>
        /// <param name="comPlayerBase"></param>
        /// <param name="countOfTurrets"></param>
        /// <param name="countOfRotators"></param>
        /// <param name="countOfArmors"></param>
        /// <param name="tankMass"></param>
        /// <returns></returns>
        internal static int SystemCalculateTankCost(ComPlayerBase comPlayerBase,
            out int countOfTurrets,
            out int countOfRotators,
            out int countOfArmors,
            out float tankMass, 
            DataFormatTank dataTank,
            List<GameObject> errorObjList)
        {
            BaseTank baseTank = new();
            baseTank.SetData(dataTank);
            var retval = baseTank.CalculateTankCost(comPlayerBase, out countOfTurrets, out countOfRotators, out countOfArmors, out tankMass);
            errorObjList.Clear();
            errorObjList.AddRange(baseTank.GetErrorObjectsList());
            return retval;
        }





        private DataFormatTank m_dataTank = null;
        private void SetData(DataFormatTank dataTank)
        {
            m_dataTank = dataTank;
        }
        private DataFormatTank  GetData()
        {
            if (m_dataTank != null)
            {
                return m_dataTank;
            }
            return GameDataHolder.Instance.DataTank;
        }



        private void CalculateCostRecursively(Transform tankPartTr, List<TankPart> tankPartsList, 
            List<GameObject> errorObjList, Bounds regulationBounds)
        {
            DataFormatTank dataTank = GetData();

            for (int i = 0; i < tankPartTr.childCount; ++i)
            {
                var partTr = tankPartTr.GetChild(i);

                // 非アクティブオブジェクト(DestroyPartInGameなど)以下は計量しない 
                if (partTr.gameObject.activeSelf == false)
                {
                    continue;
                }

                if (partTr.GetComponent<DestroyPartInGame>() != null)
                {
                    // 計量対象外 
                    continue;
                }
                else if (partTr.GetComponent<DebugPartInGame>() != null
                    || partTr.GetComponent<NotSubjectToMeasurement>() != null)
                {
                    // 計量対象外 
                }
                else if (partTr.GetComponent<TurretPart>() != null)
                {
                    // 砲塔
                    TankPart part = new();
                    part.m_partTr = partTr;
                    part.m_objectVolume = dataTank.m_turretPartObjectVolume;
                    part.m_cost = dataTank.m_turretPartCost;
                    part.m_durability = float.MaxValue;     // 砲塔は壊れない 
                    part.m_partType = TankPart.PartType.Turret;
                    tankPartsList.Add(part);

                    // レギュレーションチェック 
                    MeshRenderer meshRend = partTr.GetComponentInChildren<MeshRenderer>();
                    if (meshRend != null)
                    {
                        if (CheckTankSizeRegulation(meshRend, regulationBounds) == false)
                        {
                            errorObjList.Add(partTr.gameObject);
                        }
                    }
                    // リストに登録されていない砲塔はエラーオブジェクト扱い
                    if (m_turrets != null)
                    {
                        if (!m_turrets.Contains(partTr.GetComponent<TurretPart>()))
                        {
                            errorObjList.Add(partTr.gameObject);
                        }
                    }
                }
                else if (partTr.GetComponent<RotJointPart>() != null)
                {
                    // 回転ジョイント 
                    TankPart part = new();
                    part.m_partTr = partTr;
                    part.m_objectVolume = dataTank.m_rotJointPartObjectVolume;
                    part.m_cost = dataTank.m_rotJointPartCost;
                    part.m_durability = float.MaxValue;     // Jointは壊れない 
                    part.m_partType = TankPart.PartType.Joint;
                    tankPartsList.Add(part);

                    // レギュレーションチェック 
                    MeshRenderer meshRend = partTr.GetComponentInChildren<MeshRenderer>();
                    if (meshRend != null)
                    {
                        if (CheckTankSizeRegulation(meshRend, regulationBounds) == false)
                        {
                            errorObjList.Add(partTr.gameObject);
                        }
                    }
                    // リストに登録されていない回転ジョイントはエラーオブジェクト扱い
                    if (m_joints != null)
                    {
                        if (!m_joints.Contains(partTr.GetComponent<RotJointPart>()))
                        {
                            errorObjList.Add(partTr.gameObject);
                        }
                    }
                }
                // それ以外
                else
                {
                    // 計量対象 
                    MeshRenderer meshRend = partTr.GetComponent<MeshRenderer>();
                    if (meshRend != null)
                    {
                        TankPart part = new();
                        // Boundsで体積計算 
                        part.m_partTr = partTr;
                        part.m_objectVolume = CalcVolumeFromMesh(meshRend, partTr);
                        // コライダーが付いていなかったら付ける 
                        if (partTr.GetComponent<Collider>() == null)
                        {
                            var boxCollider = partTr.gameObject.AddComponent<BoxCollider>();
                            Bounds localBounds = meshRend.localBounds;
                            boxCollider.center = localBounds.center;
                            boxCollider.size = localBounds.size;
                        }
                        // コスト計算 (最低でも1)
                        part.m_cost = Mathf.Max(1, (int)(part.m_objectVolume * dataTank.m_tankVolumeToCostCoef));
                        // 耐久力計算 
                        part.m_durability = Mathf.Max(1.0f, part.m_objectVolume * dataTank.m_tankVolumeToDurability);
                        part.m_partType = TankPart.PartType.Armor;
                        tankPartsList.Add(part);

                        // レギュレーションチェック 
                        if (CheckTankSizeRegulation(meshRend, regulationBounds) == false)
                        {
                            errorObjList.Add(partTr.gameObject);
                        }
                    }
                    // それ以外でコライダーのついているオブジェクトはレギュレーション違反 
                    else if (partTr.GetComponent<Collider>() != null)
                    {
                        errorObjList.Add(partTr.gameObject);
                    }
                }

                // 再帰計量 
                CalculateCostRecursively(partTr, tankPartsList, errorObjList, regulationBounds);
            }
        }

        static float CalcVolumeFromMesh(MeshRenderer meshRend, Transform partTr)
        {
            Bounds boundsG = meshRend.bounds;
            float volumeGlobal = boundsG.size.x * boundsG.size.y * boundsG.size.z;
            Bounds boundsL = meshRend.localBounds;
            float boundsX = boundsL.size.x * Mathf.Abs(partTr.lossyScale.x);
            float boundsY = boundsL.size.y * Mathf.Abs(partTr.lossyScale.y);
            float boundsZ = boundsL.size.z * Mathf.Abs(partTr.lossyScale.z);
            float volumeLocal = boundsX * boundsY * boundsZ;
            return Mathf.Min(volumeGlobal, volumeLocal);
        }


        /// <summary>
        /// レギュレーションチェック 
        /// </summary>
        /// <param name="meshRend"></param>
        /// <param name="partTr"></param>
        /// <returns></returns>
        static bool CheckTankSizeRegulation(MeshRenderer meshRend, Bounds regulationBounds)
        {
            Bounds boundsG = meshRend.bounds;
            return regulationBounds.Contains(boundsG.min) && regulationBounds.Contains(boundsG.max);
        }



        /// <summary>
        /// 戦車のコストから質量を計算 
        /// </summary>
        /// <param name="cost"></param>
        /// <returns></returns>
        private float CalculateTankMass(int cost)
        {
            DataFormatTank dataTank = GetData();

            int baseCost = dataTank.m_turretPartCost + dataTank.m_tankBasePartCost;
            float totalMass = dataTank.m_tankBaseMass + dataTank.m_tankCostToMassCoef * (cost - baseCost);
            //float totalMass = dataTank.m_tankBaseMass + dataTank.m_tankCostToMassCoef * (GameConstants.DEFAULT_PLAYER_ENERGY/2 - baseCost);
            return totalMass;
        }





        #endregion


        private bool m_isPauseSubSystem = false;

        internal void PauseSubSystem(bool isPause)
        {
            if (m_isPauseSubSystem != isPause)
            {
                m_isPauseSubSystem = isPause;

                //// パーティクル停止 
                //if (isPause)
                //{
                //    PauseSmokeSfx();
                //}
            }
        }





        #region キャタピラ痕

        const float CATERPILLAR_HEIGHT_FROM_CENTER = 0.65f;
        readonly Vector3[] m_localCaterpillarPoint = { new Vector3(0, 0, 1.5f), new Vector3(0, 0, 0), new Vector3(0, 0, -1.5f) };

        [Header("Phase(Pitch)")]
        [SerializeField] private float m_trackPitch = 1.8f; // ブロック柄の周期（stampWorldLengthと揃えると良い）
        [SerializeField] private float m_minMoveToAccumulate = 0.01f;   // ノイズ除去 
        [SerializeField] private int m_maxStapsPerFrame = 8;            // 保険（ワープ・急加速時）

        [Header("Strength")]
        [SerializeField] private float m_strengthMin = 0.5f;
        [SerializeField] private float m_strengthMax = 1.0f;

        [Header("Strength (Speed/Turn Boost)")]
        [SerializeField] private float m_speedForMaxStrength = 12.0f;      // この速度で最大（m/s）
        [SerializeField] private float m_speedStrengthMinMul = 0.6f;        // 低速時の倍率
        [SerializeField] private float m_speedStrengthMaxMul = 1.3f;        // 高速時の倍率

        [SerializeField] private float m_yawRateForMaxStrength = 3.0f;      // この角速度で最大（rad/s目安）
        [SerializeField] private float m_turnStrengthMaxMul = 1.6f;         // 旋回でどれだけ増やすか

        [SerializeField] private float m_outerTrackExtraMul = 1.15f;        // 外側履帯を少し濃く（任意）


        struct CaterpillarAccum
        {
            public float m_accum;
            public Vector3 m_prevPoint;
            public bool m_hasPrev;
        }
        private CaterpillarAccum[] m_caterpillarAccums = new CaterpillarAccum[2];


        private void LateUpdate()
        {
            var manager = TrackMarks.TrackMarkManager.Instance;
            if (manager == null) return;
            if (m_rigidbody == null) return;
            if (m_isPauseSubSystem) return;

            Vector3 velocity = m_rigidbody.linearVelocity;
            float speed = velocity.magnitude;

            // 速度補正（0..1）
            float speed01 = Mathf.InverseLerp(0.0f, m_speedForMaxStrength, speed);
            float speedMul = Mathf.Lerp(m_speedStrengthMinMul, m_speedStrengthMaxMul, speed01);

            // 旋回補正（角速度Y成分：yaw）
            float yawRate = m_rigidbody.angularVelocity.y;          // UnityのRigidbodyは基本これでOK
            float turn01 = Mathf.InverseLerp(0.0f, m_yawRateForMaxStrength, Mathf.Abs(yawRate));
            float turnMul = Mathf.Lerp(1.0f, m_turnStrengthMaxMul, turn01);

            // 前進・後退判定 
            float forwardSign = 1.0f;
            var kind = TrackMarks.TrackMarkManager.TrackStampKind.Block;
            float slip = 0.0f;
            if (0.01f < velocity.sqrMagnitude)
            {
                // 前後判定 
                float dot = Vector3.Dot(velocity.normalized, transform.forward);
                forwardSign = (0 <= dot) ? 1.0f : -1.0f;

                // ドリフト判定 
                float d = Mathf.Abs(dot);
                slip = 1.0f - d;    // 0=直進 / 1=真横 
                if (0.35f <= slip)
                {
                    kind = TrackMarks.TrackMarkManager.TrackStampKind.Smear;    // 滑ってるから汚れテクスチャにする 
                }
            }

            // 混ぜウェイト 
            float blockW = Mathf.Lerp(1.0f, 0.2f, slip);
            float smearW = Mathf.Lerp(0.0f, 1.0f, slip);

            // キャタピラごとに処理 
            for (int i = 0; i < m_caterpillarsList.Count; ++i)
            {
                var caterpillar = m_caterpillarsList[i];
                if (caterpillar == null || caterpillar.m_partObj == null) continue;

                Transform tr = caterpillar.m_partObj.transform;

                // キャタピラの向きに合わせて符号反転 
                float laneForwardSign = forwardSign;
                if (i == 1)
                {
                    laneForwardSign = -1.0f * laneForwardSign;
                }

                bool isTurning = turn01 > 0.1f;

                // 例：i==0を左、i==1を右と仮定
                bool isOuter =
                    isTurning && (
                        (yawRate > 0.0f && i == 1) ||   // 左旋回なら右が外
                        (yawRate < 0.0f && i == 0)      // 右旋回なら左が外
                    );
                float outerMul = isOuter ? m_outerTrackExtraMul : 1.0f;

                // 接地点の平均をとる 
                float hitCount = 0.0f;
                Vector3 markPoint = Vector3.zero;
                for (int j = 0; j < m_localCaterpillarPoint.Length; ++j)
                {
                    Vector3 origin = tr.TransformPoint(m_localCaterpillarPoint[j]);
                    if (Physics.Raycast(origin, -tr.up, out RaycastHit hit,
                        CATERPILLAR_HEIGHT_FROM_CENTER,
                        1 << Constants.OBJ_LAYER_GROUND))
                    {
                        markPoint += hit.point;
                        hitCount += 1.0f;
                    }
                }
                if (hitCount <= 0.0f)
                {
                    m_caterpillarAccums[i].m_hasPrev = false; // 接地していない 
                    continue;
                }
                markPoint /= hitCount;

                // 接地が少ないほど強く
                float t = (hitCount - 1.0f) / 2.0f;
                float markStrength = Mathf.Lerp(m_strengthMax, m_strengthMin, t);
                markStrength *= speedMul;
                markStrength *= turnMul;
                markStrength *= outerMul;

                // 移動固定：prev->nowの線分上でpitchごとにスタンプする
                if (!m_caterpillarAccums[i].m_hasPrev)
                {
                    m_caterpillarAccums[i].m_hasPrev = true;
                    m_caterpillarAccums[i].m_prevPoint = markPoint;
                    m_caterpillarAccums[i].m_accum = 0;

                    // 初回スタンプ 
                    Vector3 forward = transform.forward * laneForwardSign;
                    manager.StampOne(markPoint, forward, markStrength, kind);
                    continue;
                }

                Vector3 prev = m_caterpillarAccums[i].m_prevPoint;
                Vector3 delta = markPoint - prev;
                float moved = delta.magnitude;
                if (moved < m_minMoveToAccumulate)
                {
                    // 距離が無くても滑るときはスタンプする 
                    if (0.2f < smearW)
                    {
                        float skidBoost = Mathf.Lerp(1.0f, 1.6f, smearW); // 強スリップほど増やす
                        manager.StampOne(markPoint, transform.forward * laneForwardSign, markStrength * smearW * skidBoost,
                            TrackMarks.TrackMarkManager.TrackStampKind.Smear);
                    }
                    continue;
                }

                float accum = m_caterpillarAccums[i].m_accum;
                accum += moved;
                int safety = 0;

                // 推すべき次の距離を計算して線分上で補間 
                while (m_trackPitch <= accum && safety++ < m_maxStapsPerFrame)
                {
                    accum -= m_trackPitch;
                    float back = accum;
                    float lerpT = 1.0f - Mathf.Clamp01(back / moved);
                    Vector3 stampPos = Vector3.Lerp(prev, markPoint, lerpT);
                    Vector3 forward = transform.forward * laneForwardSign;
                    //manager.StampOne(stampPos, forward, markStrength, kind);

                    if (0.001f < blockW)
                    {
                        manager.StampOne(stampPos, forward, markStrength * blockW,
                            TrackMarks.TrackMarkManager.TrackStampKind.Block);
                    }
                    if (0.001f < smearW)
                    {
                        manager.StampOne(stampPos, forward, markStrength * smearW,
                            TrackMarks.TrackMarkManager.TrackStampKind.Smear);
                    }
                }

                // 更新 
                m_caterpillarAccums[i].m_accum = accum;
                m_caterpillarAccums[i].m_prevPoint = markPoint;

            }
        }


        #endregion


    }


}

