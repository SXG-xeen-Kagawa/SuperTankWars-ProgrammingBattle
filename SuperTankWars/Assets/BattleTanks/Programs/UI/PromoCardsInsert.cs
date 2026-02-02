using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace SXG2025
{

    public class PromoCardsInsert : MonoBehaviour
    {
        [SerializeField] private Image[] m_promoCardBases;
        [SerializeField] private Image[] m_promoCardImages;

        private List<RectTransform> m_cardsTrList = new();
        private List<Vector2> m_cardsBasePosList = new();

        const float CARD_IMAGE_WIDTH = 1920 / 2;
        const float CARD_IMAGE_HEIGHT = 1080 / 2;

        [SerializeField] private float m_enterMoveLength = 800;
        [SerializeField] private float m_enterMoveTime = 0.5f;
        [SerializeField] private float m_enterDelayTime = 1.0f;
        [SerializeField] private float m_leaveMoveTime = 0.8f;
        [SerializeField] private AnimationCurve m_leaveCurve = new();
        [SerializeField] private AnimationCurve m_winnerScaleCurve = new();

        private CanvasGroup m_canvasGroup = null;

        private static PromoCardsInsert ms_instance = null;

        private bool m_isScreenCovered = false;     // 画面全体を覆い、かつ、プロモカードの表示演出も完了 
        private bool m_fadeInFlag = false;     // フェードイン命令


        static public PromoCardsInsert  Instance
        {
            get {
                if (ms_instance == null)
                {
                    if (PrefabHolder.Instance != null)
                    {
                        ms_instance = Instantiate(PrefabHolder.Instance.PromoCardsInsertPrefab, FadeCanvas.Instance.transform);
                        GameObject.DontDestroyOnLoad(ms_instance.gameObject);
                    }
                }
                return ms_instance;
            }
        }

        static public bool Check
        {
            get { return ms_instance != null; }
        }

        private void Awake()
        {
            m_canvasGroup = GetComponent<CanvasGroup>();

            foreach (var card in m_promoCardBases)
            {
                m_cardsTrList.Add(card.GetComponent<RectTransform>());
            }
        }

        public void FadeIn()
        {
            m_fadeInFlag = true;
        }



        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

            //StartCoroutine(CoEnter0());
            //StartCoroutine(CoEnter1(GetDebugRandomRanking()));
        }


        private void ResetCardsBasePosition(float positionScale=1.0f)
        {
            m_cardsBasePosList.Clear();
            int offset = Random.Range(0, 4);
            float rad = Mathf.PI / 4.0f + offset * (Mathf.PI / 2.0f);
            for (int i = 0; i < m_promoCardBases.Length; ++i)
            {
                Vector2 basePos = Vector2.zero;
                basePos.x = (0 < Mathf.Cos(rad)) ? (CARD_IMAGE_WIDTH * 0.5f) : -(CARD_IMAGE_WIDTH * 0.5f);
                basePos.y = (0 < Mathf.Sin(rad)) ? (CARD_IMAGE_HEIGHT * 0.5f) : -(CARD_IMAGE_HEIGHT * 0.5f);
                basePos *= positionScale;
                m_cardsBasePosList.Add(basePos);
                var tr = m_cardsTrList[i];
                tr.anchoredPosition = basePos;
                tr.localRotation = Quaternion.identity;
                tr.localScale = Vector3.one;
                rad += (Mathf.PI / 2.0f);
            }
        }


        /// <summary>
        /// プロモカードの画像割り当て 
        /// </summary>
        /// <param name="promoCardSprites"></param>
        private void SetPromoCardSprites(Sprite[] promoCardSprites)
        {
            for (int i = 0; i < promoCardSprites.Length; ++i)
            {
                if (i < m_promoCardImages.Length)
                {
                    m_promoCardImages[i].sprite = promoCardSprites[i];
                    m_promoCardImages[i].preserveAspect = true;
                }
            }
        }


        /// <summary>
        /// 画面を全部覆って、かつ、プロモーションも終了したかどうか 
        /// </summary>
        public bool IsScreenCovered => m_isScreenCovered;



        #region ---------- 均等な入場 ----------


        /// <summary>
        /// 均等モードで入場 
        /// </summary>
        /// <param name="promoCardSprites"></param>
        internal void EnterEqualityMode(Sprite[] promoCardSprites)
        {
            gameObject.SetActive(true);
            m_isScreenCovered = false;
            m_fadeInFlag = false;

            // 画像設定 
            SetPromoCardSprites(promoCardSprites);

            // 入場スタート 
            StartCoroutine(CoEnter0());
        }



        private IEnumerator CoEnter0()
        {
            m_isScreenCovered = false;

            // 基本位置決定 
            ResetCardsBasePosition(positionScale:1.0f);

            // 入場 
            m_canvasGroup.alpha = 1;
            float time = 0.0f;
            while (time < m_enterMoveTime)
            {
                time += Time.deltaTime;
                float a = Mathf.Lerp(1.0f, 0.0f, time / m_enterMoveTime);
                for (int i=0; i < m_cardsTrList.Count; ++i)
                {
                    Vector2 dir = m_cardsBasePosList[i].normalized;
                    m_cardsTrList[i].anchoredPosition = m_cardsBasePosList[i] + dir * (m_enterMoveLength * a);
                }

                yield return null;
            }

            // 待機 
            yield return new WaitForSeconds(m_enterDelayTime);

            // プロモーション終了 
            m_isScreenCovered = true;

            // フェードイン命令待ち 
            while (!m_fadeInFlag)
            {
                yield return null;
            }

            // 退場 
            time = 0.0f;
            while (time < m_leaveMoveTime)
            {
                time += Time.deltaTime;
                //float a = Mathf.Lerp(0.0f, 1.0f, time / m_leaveMoveTime);
                float a = m_leaveCurve.Evaluate(Mathf.Clamp01(time / m_leaveMoveTime));
                Quaternion rot = Quaternion.Euler(0, 0, -60 * a);
                for (int i = 0; i < m_cardsTrList.Count; ++i)
                {
                    //Vector2 dir = m_cardsBasePosList[(i+ m_cardsBasePosList.Count - 1)% m_cardsBasePosList.Count].normalized;
                    //m_cardsTrList[i].anchoredPosition = m_cardsBasePosList[i] + dir * (m_enterMoveLength * a);
                    Vector3 dir = rot * m_cardsBasePosList[i].normalized;
                    m_cardsTrList[i].anchoredPosition = m_cardsBasePosList[i] + ((Vector2)dir) * (m_enterMoveLength * a);
                }

                yield return null;
            }

            // 行動終了
            gameObject.SetActive(false);
        }

        #endregion



        #region ---------- 優勝決定後の入退場 ----------

        internal void EnterChampionMode(Sprite[] promoCardSprites, int[] ranking)
        {
            m_isScreenCovered = false;
            m_fadeInFlag = false;
            gameObject.SetActive(true);

            // 画像設定 
            SetPromoCardSprites(promoCardSprites);

            // 入場スタート 
            StartCoroutine(CoEnter1(ranking));
        }




        private int [] GetDebugRandomRanking()
        {
            int[] ranking = new int[GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE];
            for (int i=0; i < ranking.Length; ++i)
            {
                ranking[i] = i;
            }
            for (int i=10+Random.Range(0,5); 0 <= i; --i)
            {
                int id0 = Random.Range(0, GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE);
                int id1 = (id0 + Random.Range(1, GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE - 1))% GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE;
                int temp = ranking[id0];
                ranking[id0] = ranking[id1];
                ranking[id1] = temp;
            }
            return ranking;
        }




        private IEnumerator CoEnter1(int [] ranking)
        {
            const float WAIT_TIME = 1.0f;       // 待機時間 
            const float FADEOUT_TIME = 0.35f;

            m_isScreenCovered = false;

            // 基本位置決定(四隅、見えない位置)
            ResetCardsBasePosition(positionScale:3.0f);

            // 初期位置 
            List<BattleCard> battleCardsList = new();
            for (int i=0; i < m_cardsTrList.Count; ++i)
            {
                battleCardsList.Add(new BattleCard(i, ranking[i], m_cardsTrList[i], m_cardsBasePosList[i]));
            }

            // カードの行動更新  
            m_canvasGroup.alpha = 1;
            int end = 0;
            float waitTime = 0.0f;
            while (waitTime < WAIT_TIME)
            {
                // 移動 
                foreach (var card in battleCardsList)
                {
                    card.Update(m_winnerScaleCurve);

                    if (card.IsEnd())
                    {
                        end++;
                    }
                }
                // コリジョン判定 
                for (int i=0; i < battleCardsList.Count; ++i)
                {
                    var cardA = battleCardsList[i];
                    if (!cardA.IsCollidable()) continue;
                    for (int j=i+1; j < battleCardsList.Count; ++j)
                    {
                        var cardB = battleCardsList[j];
                        if (!cardB.IsCollidable()) continue;

                        // 衝突判定 
                        int winnerSide = 0;
                        if (BattleCard.CheckCollided(cardA, cardB, ref winnerSide))
                        {
                            if (winnerSide == 0)
                            {
                                cardA.SetWinner();  // Aの勝ち 
                                cardB.SetLoser();
                            } else
                            {
                                cardA.SetLoser();
                                cardB.SetWinner();  // Bの勝ち 
                            }
                            // １フレームに１つしか衝突しないようにする
                            break;
                        }
                    }
                }
                // 待機時間処理 
                if (0 < end)
                {
                    waitTime += Time.deltaTime;
                }

                yield return null;
            }

            // プロモーション終了 
            m_isScreenCovered = true;

            // フェードイン命令待ち 
            while (!m_fadeInFlag)
            {
                yield return null;
            }

            // フェードイン （カード退場）
            float time = 0;
            while (time < FADEOUT_TIME)
            {
                time += Time.deltaTime;
                m_canvasGroup.alpha = 1.0f - Mathf.Clamp01(time / FADEOUT_TIME);
                yield return null;
            }

            // 行動終了 
            gameObject.SetActive(false);
        }


        #endregion





        #region BattleCard

        public class BattleCard
        {
            const float ENTER_ANIM_TIME = 1.0f;
            const float SCALEUP_TIME = 0.25f;
            const float START_SCALE = 0.65f;
            const float LOSER_SPEED = 2.0f;

            private RectTransform m_rectTr = null;
            private int m_id = 0;
            private int m_power = 0;
            private float m_delay = 0;
            private bool m_isDefeated = false;
            private float m_rotSpeed = 0;
            private Vector2 m_startPosition;
            private float m_time = 0;
            private float m_rotZ = 0;
            private Vector3[] m_worldCorners = new Vector3[4];
            private Quaternion m_loserSpinSpeed = Quaternion.identity;

            enum Phase
            {
                Delay,
                Enter,
                BiggerAtCenter,
                Defeated,
                Ended,
            }
            private Phase m_phase = Phase.Delay;

            public BattleCard(int id, int ranking, RectTransform rectTr, Vector2 startPos)
            {
                m_id = id;
                m_power = GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE - ranking;
                m_startPosition = startPos;
                m_rotZ = Random.Range(0, 360);
                m_rectTr = rectTr;
                m_rectTr.anchoredPosition = startPos;
                m_rectTr.localRotation = Quaternion.AngleAxis(m_rotZ, Vector3.forward);
                m_rectTr.localScale = Vector3.one * START_SCALE;

                // 遅延 
                m_delay = Random.Range(0.0f, 0.5f);
                // 回転速度
                m_rotSpeed = Random.Range(360.0f * 0.5f, 360.0f * 1.0f);
            }

            public void Update(AnimationCurve winnerScaleCurve)
            {
                switch (m_phase)
                {
                    case Phase.Delay:   // 入場前遅延
                        {
                            m_delay -= Time.deltaTime;
                            if (m_delay <= 0.0f)
                            {
                                m_phase = Phase.Enter;
                            }
                        }
                        break;
                    case Phase.Enter:   // 入場 
                        {
                            if (0 < m_delay)
                            {
                                m_delay -= Time.deltaTime;
                            }
                            else
                            {
                                m_time += Time.deltaTime;
                                float a = Mathf.Clamp01(m_time / ENTER_ANIM_TIME);
                                m_rectTr.anchoredPosition = Vector2.Lerp(m_startPosition, Vector2.zero, a);
                                m_rotZ += m_rotSpeed * Time.deltaTime;
                                m_rectTr.localRotation = Quaternion.AngleAxis(m_rotZ, Vector3.forward);
                                if (ENTER_ANIM_TIME <= m_time)
                                {
                                    m_phase = Phase.BiggerAtCenter;
                                    m_time = 0;
                                }
                            }
                        }
                        break;
                    case Phase.BiggerAtCenter:  // 中央で大きくなる 
                        {
                            // 回転 
                            if (m_rotSpeed != 0)
                            {
                                int lastRotated = (int)(m_rotZ / 360.0f);
                                m_rotSpeed += (360.0f*4.0f) * Time.deltaTime;   // 回転加速 
                                m_rotZ += m_rotSpeed * Time.deltaTime;
                                int currRotated = (int)(m_rotZ / 360.0f);
                                if (lastRotated < currRotated)
                                {
                                    m_rotZ = 0;
                                    m_rotSpeed = 0;
                                }
                                m_rectTr.localRotation = Quaternion.AngleAxis(m_rotZ, Vector3.forward);
                            }
                            // スケール 
                            m_time += Time.deltaTime;
                            float scale = 1.0f + winnerScaleCurve.Evaluate(Mathf.Clamp01(m_time / SCALEUP_TIME));
                            m_rectTr.localScale = new Vector3(scale, scale, 1.0f);
                            // 終了判定 
                            if (m_rotSpeed == 0 && SCALEUP_TIME <= m_time)
                            {
                                m_phase = Phase.Ended;
                                m_time = 0;
                            }
                        }
                        break;
                    case Phase.Defeated:    // 敗北して弾き飛ばされる 
                        {
                            // 退場 
                            m_time -= Time.deltaTime * LOSER_SPEED;
                            float a = m_time / ENTER_ANIM_TIME;
                            m_rectTr.anchoredPosition = Vector2.Lerp(m_startPosition, Vector2.zero, a);
                            // 回転 
                            Quaternion deltaRotation = Quaternion.Slerp(Quaternion.identity, m_loserSpinSpeed, Time.deltaTime);
                            m_rectTr.localRotation = m_rectTr.localRotation * deltaRotation;
                        }
                        break;
                }

                // 四隅の座標 
                m_rectTr.GetWorldCorners(m_worldCorners);
            }

            public bool IsEnd()
            {
                return m_phase == Phase.Ended;
            }

            public bool IsDefeated()
            {
                return m_phase == Phase.Defeated;
            }

            public bool IsEntering()
            {
                return m_phase == Phase.Enter;
            }

            public bool IsCollidable()
            {
                return m_phase == Phase.Enter || m_phase == Phase.BiggerAtCenter;
            }

            public void SetLoser()
            {
                if (m_phase == Phase.Enter || m_phase == Phase.BiggerAtCenter)
                {
                    if (m_phase == Phase.BiggerAtCenter)
                    {
                        m_time = ENTER_ANIM_TIME;   // 中央から退場するようにパッチ 
                    }
                    m_phase = Phase.Defeated;
                    m_loserSpinSpeed = Quaternion.Euler(Random.Range(-180.0f, 180.0f), Random.Range(-180.0f, 180.0f), Random.Range(-180.0f, 180.0f));
                }
            }
            public void SetWinner()
            {
                if (m_phase == Phase.Enter)
                {
                    m_delay = 0.03f;    // ヒットストップ
                }
            }


            /// <summary>
            /// コリジョンチェック 
            /// </summary>
            /// <param name="card0"></param>
            /// <param name="card1"></param>
            /// <param name="winnerSide"></param>
            /// <returns></returns>
            static public bool CheckCollided(BattleCard card0, BattleCard card1, ref int winnerSide)
            {
                // テストする軸を集める 
                Vector3[] axes = new Vector3[4];
                int axisCount = 0;
                AddUniqueAxis(axes, ref axisCount, GetNormal(card0.m_worldCorners[1] - card0.m_worldCorners[0]));
                AddUniqueAxis(axes, ref axisCount, GetNormal(card0.m_worldCorners[2] - card0.m_worldCorners[1]));
                AddUniqueAxis(axes, ref axisCount, GetNormal(card1.m_worldCorners[1] - card1.m_worldCorners[0]));
                AddUniqueAxis(axes, ref axisCount, GetNormal(card1.m_worldCorners[2] - card1.m_worldCorners[1]));

                // 各軸で投影範囲チェック 
                for (int i=0; i < axisCount; ++i)
                {
                    if (!ProjectionsOverlap(card0.m_worldCorners, card1.m_worldCorners, axes[i]))
                    {
                        return false;   // 分離軸が見つかった → 交差していない
                    }
                }

                winnerSide = (card0.m_power < card1.m_power) ? 1 : 0;
                return true;    // 全ての軸で重なった → 交差（または接している)
            }

            static private Vector3 GetNormal(Vector3 v)
            {
                var n = new Vector3(-v.y, v.x, 0).normalized;
                return (0.0001f < n.sqrMagnitude) ? n : Vector3.zero;
            }

            static private void AddUniqueAxis(Vector3 [] axes, ref int count, Vector3 axis)
            {
                if (axis.sqrMagnitude < 0.0001f) return;
                for (int i=0; i < count; ++i)
                {
                    if (Vector3.Angle(axes[i], axis) < 1f || Vector3.Angle(axes[i], -axis) < 1f) return;
                }
                axes[count++] = axis;
            }

            static private bool ProjectionsOverlap(Vector3[] a, Vector3[] b, Vector3 axis)
            {
                float minA = float.MaxValue, maxA = float.MinValue;
                float minB = float.MaxValue, maxB = float.MinValue;

                foreach (var p in a)
                {
                    float proj = Vector3.Dot(p, axis);
                    minA = Mathf.Min(minA, proj);
                    maxA = Mathf.Max(maxA, proj);
                }
                foreach (var p in b)
                {
                    float proj = Vector3.Dot(p, axis);
                    minB = Mathf.Min(minB, proj);
                    maxB = Mathf.Max(maxB, proj);
                }

                // 接している場合も「重なっている」とみなす（必要なら < → <= に変更）
                return maxA >= minB && maxB >= minA;
            }


        }



        #endregion


    }

}

