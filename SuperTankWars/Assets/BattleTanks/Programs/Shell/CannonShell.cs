using SXG2025.Effect;
using System.Collections;
using UnityEngine;


namespace SXG2025
{

    public class CannonShell : MonoBehaviour
    {
        [SerializeField] private TrailRenderer m_trailRenderer;
        [SerializeField] private Color[] m_trailColors;

        public delegate void OnEraseDelegate(CannonShell shell);    // 削除時のデリゲート 


        private Rigidbody m_rigidbody = null;
        private OnEraseDelegate m_eraseFunc = null;

        private bool m_isCollided = false;
        private int m_teamNo = 0;
        private bool m_isDamagedTank = false;   // 戦車にダメージを与え済み


        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody>();
            m_rigidbody.useGravity = false;
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        private void OnEnable()
        {
            // 物理リセット 
            m_rigidbody.linearVelocity = Vector3.zero;
            m_rigidbody.angularVelocity = Vector3.zero;
            m_rigidbody.Sleep();
            m_rigidbody.WakeUp();

            // 初期化 
            m_isCollided = false;
            m_isDamagedTank = false;
        }


        /// <summary>
        /// 砲弾を発射
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="worldRotation"></param>
        /// <param name="velocity"></param>
        /// <param name="teamNo"></param>
        public void Shoot(Vector3 worldPosition, Quaternion worldRotation, float velocity, int teamNo, OnEraseDelegate eraseFunc)
        {
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            m_rigidbody.AddForce(transform.forward * velocity, ForceMode.VelocityChange);
            m_eraseFunc = eraseFunc;
            m_teamNo = teamNo;

            if (m_trailRenderer != null)
            {
                if (0 <= teamNo && teamNo < m_trailColors.Length)
                {
                    m_trailRenderer.startColor = m_trailColors[teamNo];
                    m_trailRenderer.endColor = m_trailColors[teamNo];
                }
                m_trailRenderer.Clear();
            }
        }


        private void Update()
        {
            // コリジョンを抜けて奈落の底まで落ちたチェック 
            if (transform.position.y < GameConstants.ABYSS_POSITION_Y)
            {
                Finish();
            }
            else
            {
                // 一回当たるまでは速度方向に姿勢制御する 
                if (!m_isCollided)
                {
                    if ((0.1f*0.1f) < m_rigidbody.linearVelocity.sqrMagnitude)
                    {
                        transform.rotation = Quaternion.LookRotation(m_rigidbody.linearVelocity);
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            m_rigidbody.AddForce(Physics.gravity * GameConstants.CANNON_SHELL_GRAVITY_SCALE);
        }


        private void Finish()
        {
            if (m_eraseFunc != null && gameObject.activeSelf)
            {
                m_eraseFunc.Invoke(this);
            }
        }



        #region コリジョン


        /// <summary>
        /// コリジョンコールバック 
        /// </summary>
        /// <param name="collision"></param>
        public void OnCollisionEnter(Collision collision)
        {
            // 自分自身にヒットした場合はスルーしてあげる 
            if (collision.rigidbody != null)
            {
                BaseTank tank = collision.rigidbody.GetComponent<BaseTank>();
                if (tank != null && tank.TeamNo == m_teamNo)
                {
                    return;
                }
            }

            if (!m_isCollided)
            {
                m_isCollided = true;

                // 爆発エフェクト 
                var explosionObj = Instantiate(PrefabHolder.Instance.VfxExplosionPrefab, transform.position, transform.rotation);
                SoundController.PlaySE(SoundController.SEType.Hit, Mathf.Clamp(transform.position.x / 9f, -0.4f, 0.4f));

                // 数フレーム後に破棄 
                if (gameObject.activeSelf)
                {
                    StartCoroutine(CoDelete());
                }
            }

            // まだ戦車にダメージを与えていないなら 
            if (!m_isDamagedTank)
            {
                // 衝突した対象にダメージ
                if (collision.rigidbody != null)
                {
                    var data = GameDataHolder.Instance.DataTank;

                    // 物理の衝撃を与える 
                    collision.rigidbody.AddExplosionForce(
                        data.m_shellCollidedExplosionForce,
                        collision.contacts[0].point,
                        data.m_shellCollidedExplosionRadius);

                    // 相手が戦車ならダメージを与える 
                    if (collision.rigidbody.gameObject.layer == Constants.OBJ_LAYER_TANK)
                    {
                        BaseTank tank = collision.rigidbody.GetComponent<BaseTank>();
                        if (tank != null)
                        {
                            Vector3 contactsPosition = Vector3.zero;
                            foreach (var contact in collision.contacts)
                            {
                                contactsPosition += contact.point;
                            }

                            tank.PutDamageByShell(
                                m_teamNo,
                                data.m_cannonShellGamePower,
                                collision.collider,
                                contactsPosition / (float)collision.contactCount,
                                data.m_cannonShellGameDamageRadius);

                            // 戦車にダメージを与え済みフラグ 
                            m_isDamagedTank = true;
                        }
                    }
                    // 相手が岩柱なら沈める 
                    if (collision.rigidbody.gameObject.layer == Constants.OBJ_LAYER_GROUND)
                    {
                        FieldColumn column = collision.rigidbody.GetComponent<FieldColumn>();
                        if (column != null)
                        {
                            column.PutDamageByShell();
                        }
                    }
                }
            }
        }

        private IEnumerator CoDelete()
        {
            yield return null;

            Finish();
        }


        public void OnTriggerEnter(Collider other)
        {
            // 場外 
            if (other.gameObject.layer == Constants.OBJ_LAYER_VIRTUALWALL)
            {
                Finish();
            }
        }

        #endregion
    }

}

