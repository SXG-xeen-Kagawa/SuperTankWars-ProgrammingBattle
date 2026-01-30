using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace SXG2025
{
    namespace UI
    {
        public delegate void OnGaugeStopDelegate(int playerId);

        public delegate void OnFinishResultAnimation(List<int> winnerTeamIdList);


        public class ResultScreen2 : MonoBehaviour
        {

            [SerializeField] private RectTransform m_dividedRootTr = null;
            [SerializeField] private ResultPlayerOne2 m_resultPlayerPrefab = null;
            [SerializeField] private TextMeshProUGUI m_roundText = null;

            private List<ResultPlayerOne2> m_resultPlayersList = new();

            private OnGaugeStopDelegate m_onGaugeStopCallback = null;
            private int[] m_teamScore = new int[GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE];      // 事前に計算された結果の合計スコア 
            private int[] m_totalScoreInDrama = new int[GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE];  // 演出上の次々に加算される合計スコア 

            public delegate void OnGetPriceDelegate(int teamNo, int price, int step);

            public bool IsEndResult { get; private set; } = false;
            public int[] TeamScore { get => m_teamScore; }

            private OnFinishResultAnimation m_onFinishResultAnimationCallback = null;



            private void Awake()
            {
                gameObject.SetActive(false);
            }


            /// <summary>
            /// スクリーン表示開始 
            /// </summary>
            /// <param name="challegers"></param>
            public void StartScreen(ComPlayerBase[] challengers,
                Color[] teamColors, Texture[] charaTextures, List<BattleTanksManager.BattleRecord> battleRecordList, int roundCount,
                OnFinishResultAnimation onFinishCallback)
            {
                IsEndResult = false;
                m_onFinishResultAnimationCallback = onFinishCallback;

                // 起こす 
                gameObject.SetActive(true);

                // スコア初期化 
                for (int i = 0; i < m_teamScore.Length; ++i)
                {
                    m_teamScore[i] = 0;
                    m_totalScoreInDrama[i] = 0;
                }

                // チームスコアを集計 
                foreach (var record in battleRecordList)
                {
                    if (0 <= record.m_attackTeamNo)
                    {
                        m_teamScore[record.m_attackTeamNo] += record.m_score;
                    }
                }

                // 参加者を作る 
                for (int i = 0; i < challengers.Length; ++i)
                {
                    var challger = challengers[i];
                    var resultOne = Instantiate(m_resultPlayerPrefab, m_dividedRootTr);
                    resultOne.Setup(challger, i, teamColors[i], charaTextures[i]);
                    m_resultPlayersList.Add(resultOne);
                }

                // カメラをぼかす 
                //CameraDoF.Instance.Change(true);

                // 「第○回戦の結果」表示
                SetRoundCount(roundCount);

                // 集計開始 
                StartCoroutine(CoStartOfTally(battleRecordList, challengers, teamColors));
            }

            public void SetOnGaugeStopDelegate(OnGaugeStopDelegate callback)
            {
                m_onGaugeStopCallback = callback;
            }


            
            /// <summary>
            /// 集計開始 
            /// </summary>
            /// <returns></returns>
            private IEnumerator CoStartOfTally(List<BattleTanksManager.BattleRecord> battleRecordList, ComPlayerBase[] challengers, Color[] teamColors)
            {
                float gaugeStopEfWaitTime = 0.1f;
                IEnumerator GaugeStopCallback(int i)
                {
                    yield return new WaitForSeconds(gaugeStopEfWaitTime);
                    gaugeStopEfWaitTime += 0.15f;
                    m_onGaugeStopCallback.Invoke(i);
                }
                const float DRAWABLE_HEIGHT = 540;   // ゲージの表示可能幅 
                const int BASE_TOTAL_SCORE = 2000;  // 基準となる合計スコア

                const float GAUGE_DELAY_TIME = 0.5f;
                const float GAUGE_EACH_PLAYER_DELAY = 0.15f;

                const float BORDER_HEIGHT_ONE_LINE = 50.0f;

                // 少し遅延 
                yield return new WaitForSeconds(0.5f);

                // BGM再生
                Effect.SoundController.PlayBGM(Effect.SoundController.BGMType.Roll);

                // 最大値を求める 
                int maxScore = 0;
                foreach (var score in m_teamScore)
                {
                    maxScore = Mathf.Max(maxScore, score);
                }

                // 1pts.の高さを計算する 
                float totalPrice = Mathf.Max(BASE_TOTAL_SCORE, maxScore);
                float heightPerYen = DRAWABLE_HEIGHT / totalPrice;

                // リザルトゲージ 
                int[] step = new int[GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE];
                float delayTime = GAUGE_DELAY_TIME;
                foreach (var record in battleRecordList)
                {
                    string message = null;
                    int targetTeamNo = record.m_attackTeamNo;
                    Color gaugeColor = Color.white;
                    switch (record.m_reason)
                    {
                        case BattleTanksManager.ScoreReason.Attack:
                            message = string.Format("{0} を撃破\n{1}pts.", challengers[record.m_losedTeamNo].YourName, record.m_score);
                            targetTeamNo = record.m_losedTeamNo;
                            gaugeColor = teamColors[record.m_losedTeamNo];
                            break;
                        case BattleTanksManager.ScoreReason.Survived:
                            //message = string.Format("生存BONUS {0}pts.", record.m_score);
                            message = string.Format("残機BONUS\n{0}pts.", record.m_score);
                            targetTeamNo = record.m_attackTeamNo;
                            gaugeColor = teamColors[record.m_attackTeamNo];
                            break;
                        case BattleTanksManager.ScoreReason.Delay:
                            // 残機ボーナス前に遅延 
                            yield return new WaitForSeconds(1.5f);
                            break;
                        default:
                            continue;
                    }
                    
                    // ゲージ降らせる 
                    int teamNo = record.m_attackTeamNo;
                    float gaugeHeight = heightPerYen * record.m_score;
                    m_resultPlayersList[teamNo].AddResultGaugeOneChip(message,
                        targetTeamNo, teamColors[targetTeamNo], gaugeHeight, true, record.m_score, step[teamNo],
                        faceImage: challengers[targetTeamNo].FaceImage,
                        onGetCallback: OnGetPriceCallback);

                    // delay
                    yield return new WaitForSeconds(GAUGE_EACH_PLAYER_DELAY);

                    // 遅延。徐々に短く 
                    yield return new WaitForSeconds(delayTime);
                    delayTime = Mathf.Max(delayTime * 0.8f, GAUGE_EACH_PLAYER_DELAY);
                }


                /*
                int stopFlags = 0;
                float delayTime = GAUGE_DELAY_TIME;
                for (int step = 0; step < 100; ++step)
                {
                    bool available = false;
                    bool isSE = true;
                    for (int i = 0; i < m_resultPlayersList.Count; ++i)
                    {
                        int price = 0;
                        string menuName = null;
                        int madePlayerId = 0;
                        if (resultBooks[i].GetData(step, out price, out menuName, out madePlayerId, true))
                        {
                            available = true;

                            // ゲージ降らせる 
                            float gaugeHeight = heightPerYen * price;
                            string message = (BORDER_HEIGHT_ONE_LINE < gaugeHeight) ?
                                string.Format("{0}\n{1}円", menuName, price) :
                                string.Format("{0} {1}円", menuName, price);
                            m_resultPlayersList[i].AddResultGaugeOneChip(message,
                                teamColors[madePlayerId], gaugeHeight, isSE, price, step, OnGetPriceCallback);

                            isSE = false;

                            // delay
                            yield return new WaitForSeconds(GAUGE_EACH_PLAYER_DELAY);
                        }
                        else
                        {
                            if (m_onGaugeStopCallback != null)
                            {
                                if ((stopFlags & (1 << i)) == 0)
                                {
                                    stopFlags |= (1 << i);
                                    m_gaugeFinishedSteps[i] = step;
                                    StartCoroutine(GaugeStopCallback(i));
                                }
                            }
                        }
                    }
                    if (!available) break;

                    // 遅延。徐々に短く 
                    yield return new WaitForSeconds(delayTime);
                    delayTime = Mathf.Max(delayTime * 0.8f, GAUGE_EACH_PLAYER_DELAY);
                }
                */

                // 完了 
                //if (m_onGaugeStopCallback != null)
                //{
                //    for (int i = 0; i < m_resultPlayersList.Count; ++i)
                //    {
                //        if ((stopFlags & (1 << i)) == 0)
                //        {
                //            m_onGaugeStopCallback.Invoke(i);
                //            break;
                //        }
                //    }
                //}

                // ゲージ完了遅延 
                yield return new WaitForSeconds(2);

                // 優勝おめでとう 
                List<int> winnerTeamIdList = new();
                for (int i=0; i < m_resultPlayersList.Count; ++i)
                {
                    if (m_resultPlayersList[i].StartDecided())
                    {
                        winnerTeamIdList.Add(i);
                    }
                }
                if (m_onFinishResultAnimationCallback != null)
                {
                    m_onFinishResultAnimationCallback.Invoke(winnerTeamIdList);
                }


                //// 生産者別結果
                //for (int i = 0; i < m_resultPlayersList.Count; ++i)
                //{
                //    var resultOne = m_resultPlayersList[i];
                //    int price = totalPriceByPlayer[i];
                //    float gaugeHeight = heightPerYen * (float)price;
                //    resultOne.SetScoreForEachProducer(gaugeHeight);
                //}

                // BGM再生
                Effect.SoundController.StopBGM();
                // SE
                Effect.SoundController.PlaySE(Effect.SoundController.SEType.RollEnd);

                // 遅延 
                yield return new WaitForSeconds(1);
                // SE
                Effect.SoundController.PlaySE(Effect.SoundController.SEType.Waaa);

                // 遅延 
                yield return new WaitForSeconds(1.5f);
                // BGM再生
                Effect.SoundController.FadeInBGM(Effect.SoundController.BGMType.Title);

                IsEndResult = true;
            }
            


            /// <summary>
            /// 閉じる 
            /// </summary>
            public void CloseScreen()
            {
                Animator animator = GetComponent<Animator>();
                animator.SetTrigger("Leave");
            }



            public void AnimEvent_Finish()
            {
                gameObject.SetActive(false);

                // ぼかし解除 
                CameraDoF.Instance.Change(false);
            }

            /// <summary>
            /// 「第〇回戦の結果」のテキスト表示
            /// 1～99回戦まで表示可能
            /// </summary>
            /// <param name="roundCount">第○回</param>
            private void SetRoundCount(int roundCount)
            {
                roundCount = Mathf.Clamp(roundCount, 1, 99);

                var units = new string[] { "", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
                var tens = new string[] { "", "十", "二十", "三十", "四十", "五十", "六十", "七十", "八十", "九十" };

                int ten = roundCount / 10;
                int unit = roundCount % 10;

                var kansuji = string.Empty;
                if (ten == 0)
                    kansuji = units[unit];
                else if (unit == 0)
                    kansuji = tens[ten];
                else
                    kansuji = tens[ten] + units[unit];

                m_roundText.text = $"第{kansuji}回戦の結果";
            }


            class ScoreChip
            {
                public int m_teamNo = 0;
                public int m_score = 0;
            }

            
            /// <summary>
            /// プライス加算のコールバック 
            /// </summary>
            /// <param name="teamNo"></param>
            /// <param name="score"></param>
            private void OnGetPriceCallback(int teamNo, int score, int step)
            {
                // 加算 
                m_totalScoreInDrama[teamNo] += score;

                // 順位をソートして、結果を伝える 
                List<ScoreChip> sortScoreChips = new();
                for (int i = 0; i < m_resultPlayersList.Count; ++i)
                {
                    sortScoreChips.Add(new ScoreChip
                    {
                        m_teamNo = i,
                        m_score = m_totalScoreInDrama[i],
                    });
                }
                sortScoreChips.Sort((a, b) => { return b.m_score - a.m_score; });

                int currentRank = 0;
                int lastScore = 0;
                for (int i = 0; i < sortScoreChips.Count; ++i)
                {
                    var scoreChip = sortScoreChips[i];
                    if (scoreChip.m_score == lastScore)
                    {
                        // ランクはそのまま 
                    }
                    else
                    {
                        // ランクを更新 
                        currentRank = i;
                        lastScore = scoreChip.m_score;
                    }

                    // チームの現在ランキングを設定する 
                    m_resultPlayersList[scoreChip.m_teamNo].SetRank(currentRank);

                    // リザルトにも格納しておく 
                    GameConfigSetting.Instance.Ranking[scoreChip.m_teamNo] = currentRank + 1;

                }
            }
            

        }

    }
}

