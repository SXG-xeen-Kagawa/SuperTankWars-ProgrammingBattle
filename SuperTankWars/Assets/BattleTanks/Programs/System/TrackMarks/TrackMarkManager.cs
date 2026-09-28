using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SXG2025.TrackMarks
{
    /// <summary>
    /// ステージ全体の履帯痕を RenderTexture へ蓄積し、Decal で地面へ投影するための管理クラスです。
    ///
    /// 重要：
    /// - Decalが参照するRenderTexture（m_rtOut）は固定します（毎フレーム差し替えない）
    /// - 内部更新は m_rtOut -> m_rtTmp -> m_rtOut で行います（GPUのread/write競合回避）
    /// </summary>
    public sealed class TrackMarkManager : MonoBehaviour
    {
        [Header("Field Mapping (World -> UV)")]
        [Tooltip("ステージ中心（Yは無視してXZのみ使用）")]
        public Vector3 fieldCenter = Vector3.zero;

        [Tooltip("ステージ半径（例：SXG2025.GameConstants.ABOUT_GAME_FIELD_RADIUS）")]
        public float fieldRadius = 40.0f;

        [Tooltip("Decal/見た目半径と定数半径がズレる場合の微調整倍率（例：1.00～1.08）")]
        public float radiusTweak = 1.00f;

        [Tooltip("UV中心ズレの微調整（m単位、XZ）。必要になったときだけ使ってください。")]
        public Vector2 centerOffsetXZ = Vector2.zero;

        [Header("RenderTexture")]
        [Tooltip("履帯痕RTの解像度。見た目優先なら 2048 推奨。")]
        public int rtSize = 2048;

        [Tooltip("互換性優先は ARGB32。マスクだけなら R8 も候補（環境依存あり）。")]
        public RenderTextureFormat rtFormat = RenderTextureFormat.ARGB32;

        [Tooltip("Wrapは基本Clamp推奨。")]
        public TextureWrapMode wrapMode = TextureWrapMode.Clamp;

        [Tooltip("FilterはBilinear推奨。")]
        public FilterMode filterMode = FilterMode.Bilinear;

        [Header("Fade (Half-Life)")]
        [Tooltip("痕が半分の濃さになるまでの秒数。数十秒で消すなら 10～15秒あたりから調整がおすすめです。")]
        public float fadeHalfLifeSeconds = 12.0f;

        [Header("Stamp")]
        [Tooltip("履帯スタンプ（アルファ付き推奨）。『泥の擦れ』っぽいブラシが相性良いです。")]
        [SerializeField] private Texture2D m_stampTexBlock;
        [SerializeField] private Texture2D m_stampTexSmear;

        [Tooltip("スタンプの世界幅（m）。履帯の幅相当。")]
        public float stampWorldWidth = 0.45f;

        [Tooltip("スタンプの世界長さ（m）。進行方向の長さ相当。")]
        public float stampWorldLength = 0.75f;

        [Tooltip("スタンプ強度（基準値）。呼び出し側で strength を掛け算できます。")]
        [Range(0f, 5f)]
        public float stampStrength = 1.0f;

        [Tooltip("この距離以上動いたらスタンプします（m）。スタンプ密度の制御。")]
        public float minStampDistance = 0.15f;

        [Header("Decal Output")]
        [Tooltip("Decal側のマテリアル（Shader GraphのDecal想定）。_TrackRT, _FieldCenterXZ, _FieldRadius を受け取れる必要があります。")]
        public Material decalMat;

        [Tooltip("decalMatへ毎フレーム自動適用するかどうか。基本ONでOKです。")]
        public bool autoApplyToDecal = true;

        //[Header("Debug (Optional)")]
        //[Tooltip("デバッグ用：RT表示用RawImage（0=Out, 1=Tmp）")]
        //[SerializeField] private UnityEngine.UI.RawImage[] m_debugRawImages = new UnityEngine.UI.RawImage[2];

        [Tooltip("DecalProjector（同一Materialを使っているか確認用）。未設定でも動きます。")]
        [SerializeField] private DecalProjector m_decalProjector = null;

        [Tooltip("起動時にRTを白で埋めてテストします（動作確認用）。確認後はOFF推奨。")]
        public bool debugFillWhiteOnEnable = false;

        [Tooltip("起動時に中心へ1発スタンプします（動作確認用）。確認後はOFF推奨。")]
        public bool debugStampOnceOnEnable = true;

        [Header("Stamp Strength Multipliears")]
        [Range(0.0f, 5.0f)] [SerializeField] private float m_stampStrengthBlock = 1.0f;
        [Range(0.0f, 5.0f)] [SerializeField] private float m_stampStrengthSmear = 1.0f;


        // スタンプの種類 
        public enum TrackStampKind
        {
            Block,
            Smear
        }


        // Hiddenシェーダ用Material
        private Material m_fadeMat;
        private Material m_stampMat;

        private Material m_copyMat;

        // RenderTexture（固定出力 + 作業用）
        private RenderTexture m_rtOut; // Decalが常に参照する
        private RenderTexture m_rtTmp; // 作業用

        // 間引き用（左右）
        private Vector3 m_lastLeftWS;
        private Vector3 m_lastRightWS;
        private bool m_hasLastLeft;
        private bool m_hasLastRight;

        // Shader property IDs
        private static readonly int s_propMul = Shader.PropertyToID("_Mul");
        private static readonly int s_propMainTex = Shader.PropertyToID("_MainTex");
        private static readonly int s_propStampTex = Shader.PropertyToID("_StampTex");
        private static readonly int s_propStampUV = Shader.PropertyToID("_StampUV");
        private static readonly int s_propAngle = Shader.PropertyToID("_Angle");
        private static readonly int s_propStrength = Shader.PropertyToID("_Strength");

        private static readonly int s_propTrackRT = Shader.PropertyToID("_TrackRT");
        private static readonly int s_propFieldCenterXZ = Shader.PropertyToID("_FieldCenterXZ");
        private static readonly int s_propFieldRadius = Shader.PropertyToID("_FieldRadius");

        private static readonly int s_propSourceTex = Shader.PropertyToID("_SourceTex");


        /// <summary>Decalへ投影すべき最新RT（読み取り専用として扱ってください）。</summary>
        public RenderTexture CurrentRT => m_rtOut;


        private static TrackMarkManager ms_instance = null;
        static internal TrackMarkManager Instance => ms_instance;



        private void Awake()
        {
            if (m_fadeMat == null)
            {
                var shader = Shader.Find("Hidden/TrackRT_Fade");
                if (shader != null) m_fadeMat = new Material(shader);
            }

            if (m_stampMat == null)
            {
                var shader = Shader.Find("Hidden/TrackRT_StampMask");
                if (shader != null) m_stampMat = new Material(shader);
            }

            if (m_copyMat == null)
            {
                var shader = Shader.Find("Hidden/TrackRT_Copy");
                if (shader != null) m_copyMat = new Material(shader);
            }

            //Debug.Log($"[TrackMarks] fadeMat={(m_fadeMat ? "OK" : "NULL")} stampMat={(m_stampMat ? "OK" : "NULL")} copyMat={(m_copyMat ? "OK" : "NULL")} stampTex={(stampTex ? stampTex.name : "NULL")}");

            ms_instance = this;
        }

        private void OnEnable()
        {
            AllocateRTs();
            ClearRT(m_rtOut);
            ClearRT(m_rtTmp);

            //DebugReadPixel(m_rtOut, "AfterClear Out");
            //DebugReadPixel(m_rtTmp, "AfterClear Tmp");

            //if (m_debugRawImages != null && m_debugRawImages.Length >= 2)
            //{
            //    if (m_debugRawImages[0] != null) m_debugRawImages[0].texture = m_rtOut;
            //    if (m_debugRawImages[1] != null) m_debugRawImages[1].texture = m_rtTmp;
            //}

            ApplyToDecal();

            if (debugFillWhiteOnEnable)
            {
                DebugFillRTWhite(m_rtOut);
                ApplyToDecal();
            }

            if (debugStampOnceOnEnable)
            {
                //StampOne(fieldCenter, Vector3.forward, 1.0f);
                StampOne(new Vector3(5, 0, -3), Vector3.forward, 1.0f);
                ApplyToDecal();
            }

        }

        private void OnDisable()
        {
            ReleaseRTs();
        }


        //private int m_debugCounter;

        private void Update()
        {
            if (m_rtOut == null || m_rtTmp == null) return;
            if (m_fadeMat == null || m_copyMat == null) return; // Copy入れている場合

            float dt = Time.deltaTime;
            if (dt < 0.001f) return;    // 時間が止まっている 
            float halfLife = Mathf.Max(0.001f, fadeHalfLifeSeconds);
            float mul = Mathf.Pow(0.5f, dt / halfLife);

            m_fadeMat.SetFloat(s_propMul, mul);

            // Out -> Tmp (Fade)
            m_fadeMat.SetTexture(s_propSourceTex, m_rtOut);
            m_fadeMat.SetFloat(s_propMul, mul);
            Graphics.Blit(null, m_rtTmp, m_fadeMat);

            // Tmp -> Out (Copy)
            m_copyMat.SetTexture(s_propSourceTex, m_rtTmp);
            Graphics.Blit(null, m_rtOut, m_copyMat);

            if (autoApplyToDecal) ApplyToDecal();
        }


        internal void StampPair(
            Vector3 leftWS, bool leftHit,
            Vector3 rightWS, bool rightHit,
            Vector3 forwardWS,
            float strength = 1.0f)
        {
            if (m_rtOut == null || m_rtTmp == null) return;
            if (m_stampMat == null || m_stampTexBlock == null) return;
            if (!leftHit && !rightHit) return;

            bool shouldStamp = false;

            if (leftHit)
            {
                if (!m_hasLastLeft) shouldStamp = true;
                else if (Vector3.Distance(leftWS, m_lastLeftWS) >= minStampDistance) shouldStamp = true;
            }

            if (rightHit)
            {
                if (!m_hasLastRight) shouldStamp = true;
                else if (Vector3.Distance(rightWS, m_lastRightWS) >= minStampDistance) shouldStamp = true;
            }

            if (!shouldStamp) return;

            float angleRad = CalcYawAngleRad(forwardWS);

            if (leftHit)
            {
                StampOneInternal(leftWS, angleRad, strength, m_stampTexBlock);
                m_lastLeftWS = leftWS;
                m_hasLastLeft = true;
            }

            if (rightHit)
            {
                StampOneInternal(rightWS, angleRad, strength, m_stampTexBlock);
                m_lastRightWS = rightWS;
                m_hasLastRight = true;
            }
        }

        internal void StampOne(Vector3 contactWS, Vector3 forwardWS, float strength = 1.0f)
        {
            if (m_rtOut == null || m_rtTmp == null) return;
            if (m_stampMat == null || m_stampTexBlock == null) return;

            float angleRad = CalcYawAngleRad(forwardWS);
            StampOneInternal(contactWS, angleRad, strength, m_stampTexBlock);
        }

        internal void StampOne(Vector3 contactWS, Vector3 forwardWS, float strength, TrackStampKind kind)
        {
            if (m_rtOut == null || m_rtTmp == null) return;
            if (m_stampMat == null) return;

            Texture2D tex = null;
            float baseStrength = 1.0f;

            switch (kind)
            {
                case TrackStampKind.Block:
                    tex = m_stampTexBlock;
                    baseStrength = m_stampStrengthBlock;
                    break;
                case TrackStampKind.Smear:
                    tex = m_stampTexSmear;
                    baseStrength = m_stampStrengthSmear;
                    break;
            }

            if (tex == null) return;

            float angleRad = CalcYawAngleRad(forwardWS);
            StampOneInternal(contactWS, angleRad, strength * baseStrength, tex);
        }


        internal void ApplyToDecal()
        {
            if (decalMat == null) return;

            decalMat.SetTexture(s_propTrackRT, m_rtOut);

            Vector2 c = new Vector2(fieldCenter.x, fieldCenter.z) + centerOffsetXZ;
            decalMat.SetVector(s_propFieldCenterXZ, new Vector4(c.x, c.y, 0, 0));
            decalMat.SetFloat(s_propFieldRadius, GetEffectiveRadius());
        }

        internal void ClearAll()
        {
            if (m_rtOut == null || m_rtTmp == null) return;
            ClearRT(m_rtOut);
            ClearRT(m_rtTmp);
        }

        private float GetEffectiveRadius()
        {
            return fieldRadius * Mathf.Max(0.0001f, radiusTweak);
        }

        private static float CalcYawAngleRad(Vector3 forwardWS)
        {
            Vector2 f = new Vector2(forwardWS.x, forwardWS.z);
            if (f.sqrMagnitude < 1e-8f) return 0f;
            f.Normalize();
            return Mathf.Atan2(f.x, f.y);
        }

        private void StampOneInternal(Vector3 posWS, float angleRad, float strengthMul, Texture2D stampTex)
        {
            float radius = GetEffectiveRadius();

            Vector2 centerXZ = new Vector2(fieldCenter.x, fieldCenter.z) + centerOffsetXZ;

            float u = (posWS.x - (centerXZ.x - radius)) / (radius * 2.0f);
            float v = (posWS.z - (centerXZ.y - radius)) / (radius * 2.0f);

            if (u < -0.25f || u > 1.25f || v < -0.25f || v > 1.25f) return;

            float wUV = stampWorldWidth / (radius * 2.0f);
            float lUV = stampWorldLength / (radius * 2.0f);

            //m_stampMat.SetTexture(s_propMainTex, m_rtOut);
            m_stampMat.SetTexture(s_propSourceTex, m_rtOut);
            m_stampMat.SetTexture(s_propStampTex, stampTex);
            m_stampMat.SetVector(s_propStampUV, new Vector4(u, v, wUV, lUV));
            m_stampMat.SetFloat(s_propAngle, angleRad);
            m_stampMat.SetFloat(s_propStrength, stampStrength * strengthMul);

            Graphics.Blit(null, m_rtTmp, m_stampMat);
            m_copyMat.SetTexture(s_propSourceTex, m_rtTmp);
            Graphics.Blit(null, m_rtOut, m_copyMat);
        }

        private void AllocateRTs()
        {
            ReleaseRTs();
            m_rtOut = CreateRT("TrackRT_Out");
            m_rtTmp = CreateRT("TrackRT_Tmp");
        }

        private RenderTexture CreateRT(string name)
        {
            var rt = new RenderTexture(rtSize, rtSize, 0, rtFormat);
            rt.name = name;
            rt.wrapMode = wrapMode;
            rt.filterMode = filterMode;
            rt.useMipMap = false;
            rt.autoGenerateMips = false;
            rt.Create();
            return rt;
        }

        private void ReleaseRTs()
        {
            if (m_rtOut != null)
            {
                m_rtOut.Release();
                Destroy(m_rtOut);
                m_rtOut = null;
            }

            if (m_rtTmp != null)
            {
                m_rtTmp.Release();
                Destroy(m_rtTmp);
                m_rtTmp = null;
            }

            m_hasLastLeft = false;
            m_hasLastRight = false;
        }

        private static void ClearRT(RenderTexture rt)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = prev;
        }

        private static void DebugFillRTWhite(RenderTexture rt)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(false, true, Color.white);
            RenderTexture.active = prev;
        }



        private Texture2D m_debugReadTex;

        private void EnsureDebugReadTex()
        {
            if (m_debugReadTex == null)
                m_debugReadTex = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
        }

        //private void DebugReadPixel(RenderTexture rt, string label)
        //{
        //    EnsureDebugReadTex();

        //    var prev = RenderTexture.active;
        //    RenderTexture.active = rt;

        //    // 左下1pxを読む（どこでも良い）
        //    m_debugReadTex.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
        //    m_debugReadTex.Apply(false);

        //    RenderTexture.active = prev;

        //    var c = m_debugReadTex.GetPixel(0, 0);
        //    Debug.Log($"[TrackMarks] {label} pixel RGBA=({c.r:F3},{c.g:F3},{c.b:F3},{c.a:F3}) rt={rt.name}");
        //}
    }
}