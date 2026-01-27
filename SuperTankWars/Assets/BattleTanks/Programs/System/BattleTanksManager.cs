// リザルト検証 
//#define RESULT_TEST


using SXG2025.Effect;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

namespace SXG2025
{

    public class BattleTanksManager : MonoBehaviour
    {
        [SerializeField] private ParticipantList m_participantList = null;      // 参加者AIリスト 

        [SerializeField] Transform[] m_popPoints;

        [SerializeField] private CountDownToStartUI m_countDownToStartUI = null;
        [SerializeField] private RemainingTimeUI m_remainingTimeUI = null;
        [SerializeField] private TankScoreRootUI m_tankScoreRootUI = null;
        [SerializeField] private ChallengersIntroScreen m_challengersIntroScreenUI = null;
        [SerializeField] private GameSetUI m_gameSetUI = null;
        [SerializeField] private ResultScreen m_resultScreenUI = null;
        [SerializeField] private UI.ResultScreen2 m_resultScreen2UI = null;
        [SerializeField] private CanvasController m_mainCanvasController = null;

        [SerializeField] private Color[] m_gameTeamColors = new Color[4];   // チームカラー 

        [SerializeField] private Camera m_mainCamera = null;

        [SerializeField] private GameObject m_turretPrefab = null; // 砲塔作り直し用

#if RESULT_TEST
        const float GAME_PLAYING_TIME = 5.0f;
#else
        const float GAME_PLAYING_TIME = 2.0f * 60.0f;
#endif
        private float m_gamePlayingTime = GAME_PLAYING_TIME;

        public enum SceneFlow
        {
            Initialize,
            ChallengersIntro,
            CountDown,
            Playing,
            Result,
            Result2,    // 撃破スコアで競うリザルト 
            Finish,

            None
        }
        private SceneFlow m_sceneFlow = SceneFlow.None;
        public SceneFlow sceneFlow => m_sceneFlow;

        private CannonShellPool m_cannonShellPool = new();

        private Transform m_gameWorldTr = null;


        public struct TankBaseSpec
        {
            public int m_cost;
            public float m_mass;
            public int m_countOfTurrets;    // 砲塔の数 
            public int m_countOfRotators;   // 回転部位の数 
            public int m_countOfArmors;     // 装甲部位の数
        }


        public enum ScoreReason
        {
            Attack, // 攻撃によって倒した 
            MySelf, // 落下して自爆した 
            Survived,   // 時間切れまで生き残った 
            Delay,  // 表示なし、遅延 
        }
        public class BattleRecord
        {
            public int m_attackTeamNo;
            public int m_losedTeamNo;
            public ScoreReason m_reason;
            public int m_score;
            public int m_param;
        }


        /// <summary>
        /// 敵を倒した記録 
        /// </summary>
        public class DefeatRecord
        {
            public int m_teamNo;
            public int m_score;     // score=costとする 
        }

        public class TankWork
        {
            private float[] m_turretCoolTime;    // 大砲のクールタイム 

            /// <summary>
            /// セットアップ 
            /// </summary>
            /// <param name="countOfTurrets"></param>
            public void Setup(int countOfTurrets)
            {
                m_turretCoolTime = new float[countOfTurrets];
            }

            /// <summary>
            /// 更新 
            /// </summary>
            public void Update()
            {
                // 砲塔のクールタイム管理 
                for (int i = 0; i < m_turretCoolTime.Length; ++i)
                {
                    m_turretCoolTime[i] = Mathf.Max(0, m_turretCoolTime[i] - Time.deltaTime);
                }
            }

            /// <summary>
            /// クールタイムを考慮したうえで発射可能か？ 
            /// </summary>
            /// <param name="turretNo"></param>
            /// <returns></returns>
            public bool IsShootable(int turretNo)
            {
                if (0 <= turretNo && turretNo < m_turretCoolTime.Length)
                {
                    return m_turretCoolTime[turretNo] <= 0;
                }
                return false;
            }

            public void Reset()
            {
                for (int i=0; i < m_turretCoolTime.Length; ++i)
                {
                    m_turretCoolTime[i] = 0;
                }
            }

            /// <summary>
            /// 発射した！クールタイムよろしく。
            /// </summary>
            /// <param name="turretNo"></param>
            public void Shooted(int turretNo)
            {
                if (0 <= turretNo && turretNo < m_turretCoolTime.Length)
                {
                    DataFormatTank dataTank = GameDataHolder.Instance.DataTank;
                    m_turretCoolTime[turretNo] = dataTank.m_shotCooldownTime;
                }
            }
        }


        public class PlayerEntrySheet
        {
            public int m_id = 0;
            public Vector3 m_initialPosition;       // 初期座標 
            public Quaternion m_initialRotation;    // 初期向き 
            public BaseTank m_baseTank = null;  // 見た目の戦車の駆動基礎部分：インスタンス 
            public ComPlayerBase m_comPlayer = null;    // AI処理：インスタンス 
            public ComPlayerBase m_comPlayerPrefab = null;  // AI処理のプレハブ 
            public int m_energy = 0;        // チームエナジー 
            public int m_currentCost = 0;     // 戦車の現在状態のコスト 
            public bool m_canShootFlag = false;     // 攻撃可能フラグ
            public bool m_isInvincible = true;   // 無敵フラグ
            public int m_ranking = -1;      // ランキング成立したら 0, 1, 2, 3 のいずれかになる 
            public int m_lives = 0;         // 残機数 

            public TankBaseSpec m_tankBaseSpec;

            public TankWork m_tankWork;

            public List<GameObject> m_loserCharaObjList = new();    // 負けたキャラのオブジェクト 
            public List<DefeatRecord> m_defeatRecordsList = new();  // 撃破記録 
        }
        private List<PlayerEntrySheet> m_playerEntrySheetList = new();

        private bool m_isUpdateComs = false;    // COMの更新フラグ
        private bool m_isWinnerDecided = false;     // 決着がついてるフラグ 
        private int m_countOfLoser = 0;     // 敗退者の数 

        private List<CharaRenderCamera> m_charaRenderCameraList = new();

        private List<BattleRecord> m_battleRecordList = new();



        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            ChangeSceneFlow(SceneFlow.Initialize);

            // 乱数初期化 
            Random.InitState((int)System.DateTime.Now.Ticks);
        }


        private void Update()
        {
            // COMの更新 
            if (m_isUpdateComs)
            {
                foreach (var playerSheet in m_playerEntrySheetList)
                {
                    if (playerSheet.m_comPlayer != null && playerSheet.m_baseTank != null)
                    {
                        // 振る舞いデータを取得 
                        var behaviorData = playerSheet.m_comPlayer.GetComBehaviorDataAndReset();

                        // Work更新 
                        playerSheet.m_tankWork.Update();

                        // 戦車の駆動部に指示 
                        playerSheet.m_baseTank.Control(behaviorData, playerSheet.m_canShootFlag, playerSheet.m_tankWork);
                    }
                }
            }
        }


        /// <summary>
        /// シーンフローを変更 
        /// </summary>
        /// <param name="nextSceneFlow"></param>
        private void ChangeSceneFlow(SceneFlow nextSceneFlow)
        {
            if (m_sceneFlow != nextSceneFlow)
            {
                m_sceneFlow = nextSceneFlow;
                switch (m_sceneFlow)
                {
                    case SceneFlow.Initialize:
                        StartCoroutine(CoSceneInit());
                        break;
                    case SceneFlow.ChallengersIntro:
                        StartCoroutine(CoSceneChallengersIntro());
                        break;
                    case SceneFlow.CountDown:
                        StartCoroutine(CoSceneCountDown());
                        break;
                    case SceneFlow.Playing:
                        StartCoroutine(CoScenePlaying());
                        break;
                    //case SceneFlow.Result:
                    //    StartCoroutine(CoSceneResult());
                    //    break;
                    case SceneFlow.Result2:
                        StartCoroutine(CoSceneResult2());
                        break;
                    case SceneFlow.Finish:
                        StartCoroutine(CoSceneFinish());
                        break;
                }
            }
        }



#region シーン遷移管理 

        private IEnumerator CoSceneInit()
        {
            const float DELAY_TIME = 1.0f;
            SoundController.FadeOutBGM();

            // 初期化 
            m_isUpdateComs = false;

            // 物理処理をリセット 
            Physics.simulationMode = SimulationMode.FixedUpdate;

            // ゲーム用オブジェクトの配置ルートを作成 
            GameObject worldObj = new GameObject("GameWorld");
            m_gameWorldTr = worldObj.transform;

            // プールに設定 
            m_cannonShellPool.SetObjectRootTr(m_gameWorldTr);

            // プレイヤー生成 
            for (int i=0; i < GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE; ++i)
            {
                PlayerEntrySheet entrySheet = new();
                entrySheet.m_id = i;
                entrySheet.m_energy = GameConstants.DEFAULT_PLAYER_ENERGY;

                // 初期位置 
                entrySheet.m_initialPosition = m_popPoints[i].position;
                entrySheet.m_initialRotation = m_popPoints[i].rotation;

                // AI選択
                int entryId = GameDataHolder.Instance.ParticipantIndexes[i];
                if (entryId== -1)
                    entryId = i % m_participantList.m_comPlayers.Count;
                entrySheet.m_comPlayerPrefab = m_participantList.m_comPlayers[entryId];

                // 戦車生成 (コスト計算含む)
                RemakePlayerTank(entrySheet);

                // 戦車ワーク
                entrySheet.m_tankWork = new();
                entrySheet.m_tankWork.Setup(entrySheet.m_baseTank.TurretCount);

                // 残機数 
                entrySheet.m_lives = GameConstants.DEFAULT_PLAYER_ENERGY / entrySheet.m_currentCost;

                // 登録 
                m_playerEntrySheetList.Add(entrySheet);

                // スコアUIに登録 
                m_tankScoreRootUI.Entry(i, entrySheet.m_comPlayer, m_gameTeamColors[i], entrySheet.m_lives);
            }

            yield return null;

            // 残り時間 
            m_remainingTimeUI.SetTime(GAME_PLAYING_TIME);

            yield return null;

            // CharaRenderCamera を作る 
            for (int i=0; i < GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE; ++i)
            {
                var charaCamera = Instantiate(PrefabHolder.Instance.CharaRenderCameraPrefab, this.transform);
                m_charaRenderCameraList.Add(charaCamera);
            }


            yield return new WaitForSeconds(DELAY_TIME);

            // シーン遷移 
            ChangeSceneFlow(SceneFlow.ChallengersIntro);
        }


        /// <summary>
        /// 挑戦者紹介画面 
        /// </summary>
        /// <returns></returns>
        private IEnumerator CoSceneChallengersIntro()
        {
            const float TANK_HUMAN_CENTER_OFFSET_Y = 0.8f;

            // キャラテクスチャを描画開始 
            for (int i=0; i < GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE; ++i)
            {
                var charaTexture = m_charaRenderCameraList[i];
                var playerSheet = m_playerEntrySheetList[i];
                charaTexture.StartRendering(playerSheet.m_baseTank.transform, 
                    Vector3.up * TANK_HUMAN_CENTER_OFFSET_Y, CharaRenderCamera.CameraMode.ChallengerIntro);
            }
            yield return null;

            // mainキャンバスを非表示に 
            m_mainCanvasController.SetAlpha(0);

            // 挑戦者紹介画面を開始 
            ComPlayerBase[] comPlayers = new ComPlayerBase[m_playerEntrySheetList.Count];
            for (int i = 0; i < m_playerEntrySheetList.Count; ++i)
            {
                comPlayers[i] = m_playerEntrySheetList[i].m_comPlayer;
            }
            Texture[] charaTextures = new Texture[m_charaRenderCameraList.Count];
            for (int i = 0; i < m_charaRenderCameraList.Count; ++i)
            {
                charaTextures[i] = m_charaRenderCameraList[i].Texture;
            }
            ChallengerIntroOne.TankSpecInfo[] tankSpecInfo = new ChallengerIntroOne.TankSpecInfo[m_charaRenderCameraList.Count];
            for (int i=0; i < m_charaRenderCameraList.Count; ++i)
            {
                var entrySheet = m_playerEntrySheetList[i];
                ChallengerIntroOne.TankSpecInfo info = new();
                info.m_cost = entrySheet.m_currentCost;
                info.m_turretCount = entrySheet.m_tankBaseSpec.m_countOfTurrets;
                info.m_rotatorCount = entrySheet.m_tankBaseSpec.m_countOfRotators;
                info.m_armorCount = entrySheet.m_tankBaseSpec.m_countOfArmors;
                info.m_sortieCount = (0 < info.m_cost) ? GameConstants.DEFAULT_PLAYER_ENERGY / info.m_cost : 0;
                tankSpecInfo[i] = info;
            }
            m_challengersIntroScreenUI.StartScreen(
                comPlayers, m_gameTeamColors, charaTextures, tankSpecInfo);

            yield return null;

            // フェードイン 
            FadeCanvas.Instance.FadeIn();

            // キー入力待ち 
            while (!WasPressedKey())
            {
                // デモ 
                UpdateDemoTanksChallengersIntro();
                yield return null;
            }

            // キャラカメラを止める
            foreach (var charaCamera in m_charaRenderCameraList)
            {
                charaCamera.StopRendering();
            }

            // 挑戦者紹介画面を閉じる 
            m_challengersIntroScreenUI.CloseScreen();
            SoundController.PlaySE(SoundController.SEType.GameStart);
            yield return new WaitForSeconds(0.1f);

            // カウントダウンへ 
            ChangeSceneFlow(SceneFlow.CountDown);
        }

        /// <summary>
        /// デモ挙動 
        /// </summary>
        private void UpdateDemoTanksChallengersIntro()
        {
            foreach (var playerSheet in m_playerEntrySheetList)
            {
                if (playerSheet.m_comPlayer != null && playerSheet.m_baseTank != null)
                {
                    // 戦車の駆動部に指示 
                    playerSheet.m_baseTank.ControlInDemo(
                        leftCaterpillarPower: 1.0f,
                        rightCaterpillarPower: 1.0f);
                }
            }
        }




        /// <summary>
        /// カウントダウン 
        /// </summary>
        /// <returns></returns>
        private IEnumerator CoSceneCountDown()
        {
            // 戦車出撃による初期のゲージ
            for (int i=0; i < m_playerEntrySheetList.Count; ++i)
            {
                var entrySheet = m_playerEntrySheetList[i];
                entrySheet.m_energy -= entrySheet.m_currentCost;

                // UIに伝達 
                m_tankScoreRootUI.SetEnergyGauge(i, 
                    gaugeRate: (float)entrySheet.m_energy / (float)GameConstants.DEFAULT_PLAYER_ENERGY,
                    withDamageAnim: false);
            }

            // mainキャンバスを表示に 
            m_mainCanvasController.SetAlpha(1);


            // キー入力待ち 
            while (!WasPressedKey())
            {
                yield return null;
            }


            // カウントダウン開始 
            bool isDone = false;

            bool isActive = m_countDownToStartUI.gameObject.activeSelf;

            m_countDownToStartUI.StartCountDown(() =>
            {
                isDone = true;
            });

            // カウントダウン待ち 
            while (!isDone)
            {
                yield return null;
            }

            // シーン遷移 
            ChangeSceneFlow(SceneFlow.Playing);
        }

        /// <summary>
        /// ゲームプレイ中 
        /// </summary>
        /// <returns></returns>
        private IEnumerator CoScenePlaying()
        {
            SoundController.PlayBGM(SoundController.BGMType.MainBGM);

            // 戦車に操作許可命令 
            m_isUpdateComs = true;

            // 決着がついていない 
            m_isWinnerDecided = false;
            m_countOfLoser = 0;

            // COMを起こす 
            // ただし、最大コストを超えていれば自爆
            foreach (var playerSheet in m_playerEntrySheetList)
            {
                if (GameConstants.DEFAULT_PLAYER_ENERGY < playerSheet.m_currentCost)
                {
                    DestroiedTankCallback(playerSheet.m_id, -1);
                }
                else
                {
                    playerSheet.m_comPlayer.enabled = true;
                }
            }

            // 制限時間管理 
            m_gamePlayingTime = GAME_PLAYING_TIME;
            while (0 < m_gamePlayingTime        // 制限時間が残っている
                && m_isWinnerDecided == false)      // 決着がついていない 
            {
                // 制限時間UI 
                m_gamePlayingTime = Mathf.Max(m_gamePlayingTime-Time.deltaTime, 0);
                m_remainingTimeUI.SetTime(m_gamePlayingTime);

                yield return null;
            }

            // 戦車のコントロール余韻を消す 
            foreach (var playerSheet in m_playerEntrySheetList)
            {
                if (playerSheet.m_baseTank != null)
                {
                    playerSheet.m_baseTank.ResetControl();
                    playerSheet.m_comPlayer.enabled = false;    // スクリプトも止めておく
                }
            }

            // 物理処理を停止（Physics.Simulateを呼ばない限り物理計算は実行されなくなる）
            Physics.simulationMode = SimulationMode.Script;

            // It's Decided! 表示 
            m_gameSetUI.Enter();
            SoundController.StopBGM();
            SoundController.PlaySE(SoundController.SEType.TimeUp);

            // COMの更新停止 
            m_isUpdateComs = false;

            yield return null;

            // キー入力待ち 
            while (!WasPressedKey())
            {
                yield return null;
            }
            yield return null;

            // リザルト開始 
            //ChangeSceneFlow(SceneFlow.Result);
            ChangeSceneFlow(SceneFlow.Result2);
        }

        /*
        /// <summary>
        /// リザルト画面 
        /// </summary>
        /// <returns></returns>
        private IEnumerator CoSceneResult()
        {
            const float FADEOUT_TIME = 0.5f;
            const float TANK_HUMAN_CENTER_OFFSET_Y = 0.8f;
            const float LOSER_HUMAN_CENTER_OFFSET_Y = 0.3f;

            // 生き残り
            {
                List<int> survivedList = new();
                foreach (var entrySheet in m_playerEntrySheetList)
                {
                    if (entrySheet.m_baseTank != null)
                    {
                        survivedList.Add(entrySheet.m_id);
                    }
                }
                if (0 < survivedList.Count)
                {
                    int bonus = GameDataHolder.Instance.DataGame.m_survivedBonusScore / survivedList.Count;
                    foreach (var id in survivedList)
                    {
                        m_battleRecordList.Add(new BattleRecord
                        {
                            m_attackTeamNo = id,
                            m_losedTeamNo = id,
                            m_reason = ScoreReason.Survived,
                            m_score = bonus,
                        });
                    }
                }
            }

            // フェードアウト 
            FadeCanvas.Instance.FadeOut(FADEOUT_TIME);
            yield return new WaitForSeconds(FADEOUT_TIME);

            // キャラテクスチャを描画開始 
            for (int i = 0; i < GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE; ++i)
            {
                var charaTexture = m_charaRenderCameraList[i];
                var playerSheet = m_playerEntrySheetList[i];
                if (playerSheet.m_baseTank != null)
                {
                    charaTexture.StartRendering(playerSheet.m_baseTank.transform,
                        Vector3.up * TANK_HUMAN_CENTER_OFFSET_Y, CharaRenderCamera.CameraMode.ChallengerIntro);
                } else
                {
                    GameObject humanObj = null;
                    foreach (var obj in playerSheet.m_loserCharaObjList)
                    {
                        if (obj != null)
                        {
                            humanObj = obj;
                        }
                    }
                    if (humanObj != null)
                    {
                        charaTexture.StartRendering(humanObj.transform,
                            humanObj.transform.TransformDirection(Vector3.up*LOSER_HUMAN_CENTER_OFFSET_Y), CharaRenderCamera.CameraMode.Loser);
                    } else
                    {
                        charaTexture.StartRendering(m_popPoints[i],
                            Vector3.zero, CharaRenderCamera.CameraMode.Loser);
                    }
                }
            }
            yield return null;

            // mainキャンバスを非表示に 
            m_mainCanvasController.SetAlpha(0);

            // 順位を再精査 
            List<int> rankingPlayerIdList = new();
            for (int i = 0; i < GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE; ++i)
            {
                rankingPlayerIdList.Add(i);
            }
            rankingPlayerIdList.Sort((a, b) =>
            {
                var p0 = m_playerEntrySheetList[a];
                var p1 = m_playerEntrySheetList[b];
                if (p0.m_ranking < 0 && p1.m_ranking < 0)
                {
                    return p1.m_energy - p0.m_energy;
                }
                else
                {
                    return p0.m_ranking - p1.m_ranking;
                }
            });

            // リザルト画面を開始 
            ComPlayerBase[] comPlayers = new ComPlayerBase[m_playerEntrySheetList.Count];
            Texture[] charaTextures = new Texture[m_charaRenderCameraList.Count];
            for (int i=0; i < comPlayers.Length; ++i)
            {
                comPlayers[i] = m_playerEntrySheetList[i].m_comPlayerPrefab;
            }
            for (int i=0; i < m_charaRenderCameraList.Count; ++i)
            {
                charaTextures[i] = m_charaRenderCameraList[i].Texture;
            }
            m_resultScreenUI.StartScreen(
                comPlayers, m_gameTeamColors, charaTextures, rankingPlayerIdList, GameConfigSetting.Instance.RoundCount);


            yield return null;

            // フェードイン 
            FadeCanvas.Instance.FadeIn();

            // 待ち 
            yield return new WaitForSeconds(1.0f);

            // キー入力待ち 
            while (!WasPressedKey())
            {
                yield return null;
            }

            // 終了へ 
            ChangeSceneFlow(SceneFlow.Finish);
        }
        */

        /// <summary>
        /// リザルト画面 
        /// </summary>
        /// <returns></returns>
        private IEnumerator CoSceneResult2()
        {
            const float FADEOUT_TIME = 0.5f;
            const float TANK_HUMAN_CENTER_OFFSET_Y = 0.8f;
            const float LOSER_HUMAN_CENTER_OFFSET_Y = 0.3f;

#if RESULT_TEST
            // リザルト検証用ダミーデータ 
            {
                int battleCount = Random.Range(5, 10);
                for (int i=0; i < battleCount; ++i)
                {
                    int attackerTeamNo = Random.Range(0, GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE);
                    int loserTeamNo = (attackerTeamNo + Random.Range(1, GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE)) 
                        % GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE;
                    AddBattleRecord(attackerTeamNo, loserTeamNo);
                }
            }
#endif


            {
                System.Text.StringBuilder sb = new();
                sb.AppendLine("[Record]");

                int[] totalScore = new int[GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE];
                for (int i = 0; i < GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE; ++i)
                {
                    sb.AppendFormat("team{0}: ", i);
                    var entrySheet = m_playerEntrySheetList[i];
                    foreach (var record in entrySheet.m_defeatRecordsList)
                    {
                        totalScore[i] += record.m_score;
                        sb.AppendFormat("({0}, {1})", record.m_teamNo, record.m_score);
                    }
                    sb.AppendFormat(" | totalScore={0}\n", totalScore[i]);
                }
                Debug.Log(sb.ToString());
            }

            // 生き残りスコアを記録 
            //{
            //    List<int> survivedList = new();
            //    foreach (var entrySheet in m_playerEntrySheetList)
            //    {
            //        if (entrySheet.m_baseTank != null)
            //        {
            //            survivedList.Add(entrySheet.m_id);
            //        }
            //    }
            //    if (0 < survivedList.Count)
            //    {
            //        int bonus = GameDataHolder.Instance.DataGame.m_survivedBonusScore / survivedList.Count;
            //        foreach (var id in survivedList)
            //        {
            //            m_battleRecordList.Add(new BattleRecord
            //            {
            //                m_attackTeamNo = id,
            //                m_losedTeamNo = id,
            //                m_reason = ScoreReason.Survived,
            //                m_score = bonus,
            //            });
            //        }
            //    }
            //}

            // 残機ボーナス
            {
                // 残機ボーナス表示前遅延 
                m_battleRecordList.Add(new BattleRecord
                {
                    m_reason = ScoreReason.Delay
                });
                // 残機ボーナスを積み上げ
                foreach (var entrySheet in m_playerEntrySheetList)
                {
                    // まだ生存している 
                    for (int i=0; i < entrySheet.m_lives; ++i)
                    {
                        int bonus = entrySheet.m_currentCost;
                        m_battleRecordList.Add(new BattleRecord
                        {
                            m_attackTeamNo = entrySheet.m_id,
                            m_losedTeamNo = entrySheet.m_id,
                            m_reason = ScoreReason.Survived,
                            m_score = bonus,
                            m_param = entrySheet.m_lives,
                        });
                    }
                }
            }


            // フェードアウト 
            FadeCanvas.Instance.FadeOut(FADEOUT_TIME);
            yield return new WaitForSeconds(FADEOUT_TIME);

            // 戦車を再生成 
            for (int i = 0; i < GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE; ++i)
            {
                PlayerEntrySheet playerSheet = m_playerEntrySheetList[i];

                // 戦車がまだ生存しているなら削除 
                if (playerSheet.m_baseTank != null)
                {
                    Destroy(playerSheet.m_baseTank.gameObject);
                }

                // 戦死者は全て撤去 
                foreach (var chara in playerSheet.m_loserCharaObjList)
                {
                    Destroy(chara);
                }

                // 戦車生成 (コスト計算不要 )
                RemakePlayerTank(playerSheet, false, false);
            }
            yield return null;

            // 戦車の行動を止める 
            for (int i=0; i < GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE; ++i)
            {
                var playerSheet = m_playerEntrySheetList[i];
                if (playerSheet.m_comPlayer != null)
                {
                    playerSheet.m_comPlayer.enabled = false;
                    playerSheet.m_comPlayer.DeleteDebugDraw();
                }
            }

            // キャラテクスチャを描画開始 
            for (int i = 0; i < GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE; ++i)
            {
                var charaTexture = m_charaRenderCameraList[i];
                var playerSheet = m_playerEntrySheetList[i];
                if (playerSheet.m_baseTank != null)
                {
                    charaTexture.StartRendering(playerSheet.m_baseTank.transform,
                        Vector3.up * TANK_HUMAN_CENTER_OFFSET_Y, CharaRenderCamera.CameraMode.ChallengerIntro);
                }
            }
            yield return null;

            // mainキャンバスを非表示に 
            m_mainCanvasController.SetAlpha(0);

            // リザルト画面を開始 
            bool isFinishResultAnimation = false;
            ComPlayerBase[] comPlayers = new ComPlayerBase[m_playerEntrySheetList.Count];
            Texture[] charaTextures = new Texture[m_charaRenderCameraList.Count];
            for (int i = 0; i < comPlayers.Length; ++i)
            {
                comPlayers[i] = m_playerEntrySheetList[i].m_comPlayerPrefab;
            }
            for (int i = 0; i < m_charaRenderCameraList.Count; ++i)
            {
                charaTextures[i] = m_charaRenderCameraList[i].Texture;
            }
            m_resultScreen2UI.StartScreen(
                comPlayers, m_gameTeamColors, charaTextures, m_battleRecordList, GameConfigSetting.Instance.RoundCount,
                (winnerTeamIdList)=>
                {
                    // リザルト終了 
                    isFinishResultAnimation = true;
                    // 優勝チーム以外のカメラは止める 
                    for (int i=0; i < GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE; ++i)
                    {
                        if (!winnerTeamIdList.Contains(i))
                        {
                            m_charaRenderCameraList[i].StopCameraRotation();
                        }
                    }
                });

            yield return null;

            // フェードイン 
            FadeCanvas.Instance.FadeIn();

            // 待ち 
            yield return new WaitForSeconds(0.1f);

            // キー入力待ち 
            while (!isFinishResultAnimation || !WasPressedKey())
            {
                // デモ 
                UpdateDemoTanksChallengersIntro();
                yield return null;
            }

            // 終了へ 
            ChangeSceneFlow(SceneFlow.Finish);
        }




        /// <summary>
        /// 終了 
        /// </summary>
        /// <returns></returns>
        private IEnumerator CoSceneFinish()
        {
            // フェードアウト 
            FadeCanvas.Instance.FadeOut();
            yield return new WaitForSeconds(0.5f);

            // 物理処理をリセット 
            Physics.simulationMode = SimulationMode.FixedUpdate;

            // 再びゲーム開始 
            SceneManager.LoadSceneAsync(GameDataHolder.GameExitSceneName);
        }



        private bool WasPressedKey()
        {
            return GameInputManager.Instance.WasPressed(GameInputManager.Type.Decide);
        }


#endregion



#region 戦車管理 

        // 戦車生成 
        private void RemakePlayerTank(PlayerEntrySheet entrySheet, bool withCalculateCost=true, bool withShield=true)
        {
            // リセット 
            entrySheet.m_canShootFlag = false;

            // 初期位置 
            Vector3 initialPos = entrySheet.m_initialPosition + new Vector3(Random.Range(-4.0f, 4.0f), 0.0f, Random.Range(-4.0f, 4.0f));

            // 戦車駆動部分を生成 
            entrySheet.m_baseTank = CreateNewTankObject(entrySheet.m_id, initialPos, entrySheet.m_initialRotation);
            entrySheet.m_baseTank.name = Utility.MakeRandomString(12);

            // AI＆戦車生成 
            entrySheet.m_comPlayer = Instantiate(entrySheet.m_comPlayerPrefab, entrySheet.m_baseTank.transform);
            entrySheet.m_comPlayer.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            entrySheet.m_comPlayer.Setup(entrySheet.m_id, this);
            entrySheet.m_comPlayer.enabled = false;

            // 戦車のレイヤーを変更する 
            ChangeTankLayer(entrySheet.m_comPlayer.gameObject);

            // 戦車の有効な砲塔にカラーを設定する 
            entrySheet.m_comPlayer.SetTeamColor(entrySheet.m_baseTank.GetTeamColorMaterial());

            // 戦車の砲塔を駆動部に紐づける 
            entrySheet.m_comPlayer.GetTurrets((turrets) =>
            {
                entrySheet.m_baseTank.LinkTurrets(turrets);
            });
            // 戦車の回転ジョイントを駆動物紐づける 
            entrySheet.m_comPlayer.GetRotJoints((joints) =>
            {
                entrySheet.m_baseTank.LinkRotJoints(joints);
            });

            // 戦車のコストを計量する 
            if (withCalculateCost)
            {
                entrySheet.m_tankBaseSpec.m_cost = entrySheet.m_baseTank.CalculateTankCost(entrySheet.m_comPlayer,
                    out entrySheet.m_tankBaseSpec.m_countOfTurrets,
                    out entrySheet.m_tankBaseSpec.m_countOfRotators,
                    out entrySheet.m_tankBaseSpec.m_countOfArmors,
                    out entrySheet.m_tankBaseSpec.m_mass);
                entrySheet.m_currentCost = entrySheet.m_tankBaseSpec.m_cost;
            }

            var errorObjects = entrySheet.m_baseTank.GetErrorObjectsList();

            // 戦車生成直後はシールドを張る（最大コストを超えていなければ）
            if (withShield && entrySheet.m_currentCost <= GameConstants.DEFAULT_PLAYER_ENERGY)
            {
                // 戦車の全体の大きさを計算 
                Bounds tankBounds = new();
                var meshes = entrySheet.m_baseTank.GetComponentsInChildren<MeshRenderer>(false).ToList();
                if (0 < meshes.Count)
                {
                    // body（もしくはfullBody）オブジェクトを基準にする
                    var bodyMeshRenderer = meshes.Find(_ => (_.gameObject.name == "body" || _.gameObject.name == "fullBody"));
                    if (tankBounds != null)
                    {
                        tankBounds = bodyMeshRenderer.bounds;
                    }

                    for (int i = 0; i < meshes.Count; ++i)
                    {
                        // レギュレーション違反や削除予定対象のオブジェクトは無視
                        if (errorObjects.Contains(meshes[i].gameObject)
                            || errorObjects.Contains(meshes[i].GetComponentInParent<TurretPart>()?.gameObject)
                            || errorObjects.Contains(meshes[i].GetComponentInParent<RotJointPart>()?.gameObject)
                            || meshes[i].GetComponentInParent<DestroyPartInGame>())
                            continue;

                        tankBounds.Encapsulate(meshes[i].bounds);
                    }
                }

                // シールド表示 
                entrySheet.m_isInvincible = true;
                var shieldObj = Instantiate(PrefabHolder.Instance.SphereShieldPrefab, m_gameWorldTr);
                shieldObj.Setup(entrySheet.m_baseTank.transform, tankBounds,
                    canReduceFunc: () => { return m_isUpdateComs; },
                    finishedShieldFunc: () =>
                    {
                        entrySheet.m_canShootFlag = true;
                        entrySheet.m_isInvincible = false;
                    });
            }

            // 不要パートを削除 
            var destroyList = entrySheet.m_comPlayer.GetComponentsInChildren<DestroyPartInGame>();
            foreach (var part in destroyList)
            {
                part.Delete();
            }

            // レギュレーション違反のパーツがあれば削除 
            if (0 < errorObjects.Count)
            {
                foreach (var errorObj in errorObjects)
                {
                    if (errorObj != null)
                    {
                        Destroy(errorObj);
                    }
                }
            }

            // 砲塔を作り直す（スケール変更対策）
            // ※リストに入っていない砲塔は対象外
            // ※砲塔内に装甲パーツなど入れていれば消える
            // ※もしレギュレーション違反となる位置になっても削除されない
            entrySheet.m_comPlayer.GetTurrets((turrets) =>
            {
                var posCashes = new Vector3[turrets.Length];
                var rotCashes = new Quaternion[turrets.Length];
                var parentCashes = new Transform[turrets.Length];
                for (int i = 0; i < turrets.Length; i++)
                {
                    var turretPart = turrets[i];
                    if (turretPart != null)
                    {
                        posCashes[i] = turretPart.transform.position;
                        rotCashes[i] = turretPart.transform.rotation;
                        parentCashes[i] = turretPart.transform.parent;

                        // 親が砲塔だった場合はComPlayer直下に置く
                        if (parentCashes[i].GetComponentInParent<TurretPart>() != null)
                            parentCashes[i] = entrySheet.m_comPlayer.transform;

                        Destroy(turretPart.gameObject);

                        turrets[i] = Instantiate(m_turretPrefab, posCashes[i], rotCashes[i], parentCashes[i])
                            .GetComponent<TurretPart>();
                    }
                }
            });
            // 戦車の有効な砲塔にカラーを設定する 
            entrySheet.m_comPlayer.SetTeamColor(entrySheet.m_baseTank.GetTeamColorMaterial());
            // 戦車の砲塔を駆動部に紐づける 
            entrySheet.m_comPlayer.GetTurrets((turrets) =>
            {
                entrySheet.m_baseTank.LinkTurrets(turrets);
            });

            // MeshRendererがColliderを持っていれば、MeshColliderに差し替えてMeshRendererの形状に合わせる
            var renderers = entrySheet.m_comPlayer.transform.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                // 砲塔は無視
                if (renderer.GetComponentInParent<TurretPart>())
                    continue;

                if (renderer.TryGetComponent<Collider>(out var col))
                {
                    Destroy(col);

                    var mc = renderer.gameObject.AddComponent<MeshCollider>();
                    var mf = renderer.GetComponent<MeshFilter>();
                    mc.convex = true;
                    if (mf != null && mf.sharedMesh != null)
                        mc.sharedMesh = mf.sharedMesh;
                }
            }
        }





        /// <summary>
        /// 
        /// </summary>
        /// <param name="tankObj"></param>
        private static void ChangeTankLayer(GameObject tankObj)
        {
            if (tankObj.layer != Constants.OBJ_LAYER_BARREL && tankObj.layer == Constants.OBJ_LAYER_SHELL)
            {
                tankObj.layer = Constants.OBJ_LAYER_TANK;
            }
            // 子階層を探す 
            for (int i=0; i < tankObj.transform.childCount; ++i)
            {
                var childTr = tankObj.transform.GetChild(i);
                if (childTr != null)
                {
                    ChangeTankLayer(childTr.gameObject);
                }
            }
        }


        /// <summary>
        /// 新規の戦車オブジェクトを作る 
        /// </summary>
        /// <param name="teamNo"></param>
        /// <returns></returns>
        private BaseTank    CreateNewTankObject(int teamNo, Vector3 worldPosition, Quaternion worldRotation)
        {
            var tank = Instantiate(PrefabHolder.Instance.BaseTankPrefab, m_gameWorldTr);
            tank.transform.SetPositionAndRotation(worldPosition, worldRotation);
            tank.SetTeam(teamNo, m_gameTeamColors[teamNo]);
            tank.SetCannonShellDelegate(GetNewCannonShell, ReleaseCannonShell);
            tank.SetDestroiedTankDelegate(DestroiedTankCallback);
            tank.SetTankPartDamagedDelegate(TankPartDamagedCallback);

            return tank;
        }


        /// <summary>
        /// 戦車が破壊された時のコールバック 
        /// </summary>
        /// <param name="teamNo"></param>
        private void DestroiedTankCallback(int teamNo, int attackTeamNo)
        {
            // ゲームが継続しているか判定 
            if (m_isUpdateComs == false)
            {
                return;
            }

            // 破棄 
            var entrySheet = m_playerEntrySheetList[teamNo];
            if (entrySheet.m_baseTank != null)
            {
                // キャラを剥がす 
                entrySheet.m_loserCharaObjList.Add(entrySheet.m_baseTank.DropLoserCharacter());

                // その他の残っている部品を剥がす
                entrySheet.m_baseTank.DropAllParts();

                // 撃破UI 
                var destroiedUi = Instantiate(PrefabHolder.Instance.DestroiedTankUiPrefab, m_mainCanvasController.Get3dRootTr());
                destroiedUi.Link(m_mainCamera, entrySheet.m_baseTank.transform.position);

                // 破棄 
                Destroy(entrySheet.m_baseTank.gameObject);  // comPlayerはbaseTankの子階層なので連動して削除される 
                entrySheet.m_baseTank = null;
                entrySheet.m_comPlayer = null;
            }

            // 撃破記録をつける 
            if (0 <= attackTeamNo && teamNo != attackTeamNo)
            {
                AddBattleRecord(attackTeamNo, teamNo);

                // UI表示 
                m_tankScoreRootUI.SetDefeatUI(attackTeamNo, teamNo);

            } else
            {
                // 戦績を残す(自爆)
                m_battleRecordList.Add(new BattleRecord
                {
                    m_attackTeamNo = teamNo,
                    m_losedTeamNo = teamNo,
                    m_reason = ScoreReason.MySelf,
                    m_score = 0,
                    m_param = 0,
                });
            }

            // 残機数を減らす 
            entrySheet.m_lives--;

            // 戦車を新規生成するエナジーが残っていたら新規作成
            int cost = entrySheet.m_currentCost;
            if (cost <= entrySheet.m_energy)
            {
                // コスト減少 
                entrySheet.m_energy -= cost;
                // 戦車の再生成 
                RemakePlayerTank(entrySheet);

                // 即座にCOMは動作開始 
                entrySheet.m_comPlayer.enabled = true;

                // ワークをリセット 
                entrySheet.m_tankWork.Reset();

                // UIに伝達 
                m_tankScoreRootUI.DestroyOneLife(teamNo, true);
                //m_tankScoreRootUI.SetEnergyGauge(teamNo, (float)entrySheet.m_energy / (float)GameConstants.DEFAULT_PLAYER_ENERGY, true);

            }
            else
            {
                // 敗退 
                m_countOfLoser++;

                // 順位決定 
                entrySheet.m_ranking = GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE - m_countOfLoser;

                // UIに伝達 
                m_tankScoreRootUI.DestroyOneLife(teamNo, false);
                m_tankScoreRootUI.LoseByGaugeDepletion(teamNo);

                // 生存者が一人だけになったら決着 
                if (GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE-1 <= m_countOfLoser)
                {
                    m_isWinnerDecided = true;   // 決着 
                }
            }
        }

        /// <summary>
        /// 勝敗記録を付ける 
        /// </summary>
        /// <param name="attackTeamNo"></param>
        /// <param name="defeatTeamNo"></param>
        private void AddBattleRecord(int attackTeamNo, int defeatTeamNo)
        {
            // 撃破記録をつける 
            if (0 <= attackTeamNo && defeatTeamNo != attackTeamNo)
            {
                var loserEntrySheet = m_playerEntrySheetList[defeatTeamNo];

                var attackerEntrySheet = m_playerEntrySheetList[attackTeamNo];
                attackerEntrySheet.m_defeatRecordsList.Add(new DefeatRecord
                {
                    m_teamNo = defeatTeamNo,
                    m_score = loserEntrySheet.m_tankBaseSpec.m_cost,
                });

                // 戦績を残す(攻撃) 
                m_battleRecordList.Add(new BattleRecord
                {
                    m_attackTeamNo = attackTeamNo,
                    m_losedTeamNo = defeatTeamNo,
                    m_reason = ScoreReason.Attack,
                    m_score = loserEntrySheet.m_tankBaseSpec.m_cost,
                    m_param = 0,
                });
            }
        }


        private bool TankPartDamagedCallback(int teamNo, Collider collidedCollider, Vector3 contactPoint)
        {
            return false;
        }





#endregion




#region 砲弾管理


        /// <summary>
        /// 新しい砲弾のインスタンスを取得 
        /// </summary>
        /// <returns></returns>
        internal CannonShell GetNewCannonShell()
        {
            return m_cannonShellPool.GetData();
        }

        /// <summary>
        /// 使用済みの砲弾を返却 
        /// </summary>
        /// <param name="shell"></param>
        internal void ReleaseCannonShell(CannonShell shell)
        {
            m_cannonShellPool.ReleaseData(shell);
        }

#endregion



#region ComPlayerBaseから呼ばれる 


        /// <summary>
        /// 戦車のワールド座標と角度を返す 
        /// </summary>
        /// <param name="teamNo"></param>
        /// <param name="worldPosition"></param>
        /// <param name="worldRotation"></param>
        internal void GetTankPositionAndRotation(int teamNo, out Vector3 worldPosition, out Quaternion worldRotation)
        {
            var playerSheet = m_playerEntrySheetList[teamNo];
            if (playerSheet.m_baseTank != null)
            {
                worldPosition = playerSheet.m_baseTank.transform.position;
                worldRotation = playerSheet.m_baseTank.transform.rotation;
            } else
            {
                //worldPosition = Vector3.zero;
                worldPosition = Vector3.up * 100;   // ダミーで上空にいる事にする
                worldRotation = Quaternion.identity;
            }
        }


        /// <summary>
        /// 戦車チームの情報を取得
        /// </summary>
        /// <param name="teamNo"></param>
        /// <param name=""></param>
        /// <param name=""></param>
        /// <param name="costOfOneTank"></param>
        internal void GetTankInfo(int teamNo, out int remainingEnergy, out int costOfOneTank, out bool isDefeated, out bool isInvincible)
        {
            var playerSheet = m_playerEntrySheetList[teamNo];
            remainingEnergy = playerSheet.m_energy;
            costOfOneTank = playerSheet.m_currentCost;
            isDefeated = (playerSheet.m_energy < playerSheet.m_currentCost && playerSheet.m_comPlayer == null);
            isInvincible = playerSheet.m_isInvincible;
        }

        /// <summary>
        /// 戦車が砲塔から砲弾を発射可能か確認する関数 
        /// </summary>
        /// <param name="teamNo"></param>
        /// <param name="turretNo"></param>
        /// <returns></returns>
        internal bool CanShootTheTank(int teamNo, int turretNo)
        {
            if (m_isUpdateComs)
            {
                var playerSheet = m_playerEntrySheetList[teamNo];
                if (playerSheet.m_canShootFlag && playerSheet.m_tankWork.IsShootable(turretNo))
                {
                    return true;
                }
            }
            return false;
        }


#endregion

    }


}

