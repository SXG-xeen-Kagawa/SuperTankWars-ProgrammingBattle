using UnityEngine;


namespace SXG2025
{

    public class PrefabHolder : MonoBehaviour
    {
        static PrefabHolder ms_instance;
        private void Awake()
        {
            ms_instance = this;
        }
        public static PrefabHolder Instance => ms_instance;


        /// <summary>
        /// 戦車のプレハブ 
        /// </summary>
        [SerializeField] private BaseTank m_baseTankPrefab = null;
        public BaseTank BaseTankPrefab => m_baseTankPrefab;


        /// <summary>
        /// 戦車のキャタピラの片側 
        /// </summary>
        [SerializeField] private CaterpillarPart m_caterpillarPartPrefab = null;
        public CaterpillarPart CaterpillarPartPrefab => m_caterpillarPartPrefab;


        /// <summary>
        /// 砲弾のプレハブ 
        /// </summary>
        [SerializeField] private CannonShell m_cannonShellPrefab = null;
        public CannonShell CannonShellPrefab => m_cannonShellPrefab;


        /// <summary>
        /// 爆発エフェクトのプレハブ 
        /// </summary>
        [SerializeField] private GameObject m_vfxExplosionPrefab = null;
        public GameObject VfxExplosionPrefab => m_vfxExplosionPrefab;


        /// <summary>
        /// 戦車が破壊された爆発エフェクトのプレハブ 
        /// </summary>
        [SerializeField] private GameObject m_vfxTankDestroiedPrefab = null;
        public GameObject VfxTankDestroiedPrefab => m_vfxTankDestroiedPrefab;


        /// <summary>
        /// 無敵シールド 
        /// </summary>
        [SerializeField] private SphereShield m_sphereShieldPrefab = null;
        public SphereShield SphereShieldPrefab => m_sphereShieldPrefab;


        [SerializeField] private CharaRenderCamera m_charaRenderCameraPrefab = null;
        public CharaRenderCamera CharaRenderCameraPrefab => m_charaRenderCameraPrefab;



        /// <summary>
        /// UI:撃破 
        /// </summary>
        [SerializeField] private UI.DestroiedTankUI m_destroiedTankUiPrefab = null;
        public UI.DestroiedTankUI DestroiedTankUiPrefab => m_destroiedTankUiPrefab;


        [SerializeField] private DebugMarker m_debugMarkerPrefab = null;
        public DebugMarker DebugMarkerPrefab => m_debugMarkerPrefab;


        /// <summary>
        /// PRカード表示 
        /// </summary>
        [SerializeField] private PromoCardsInsert m_promoCardsInsertPrefab = null;
        public PromoCardsInsert PromoCardsInsertPrefab => m_promoCardsInsertPrefab;

    }


}
