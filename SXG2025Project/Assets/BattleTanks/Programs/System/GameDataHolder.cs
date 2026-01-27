using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif


namespace SXG2025
{

    public class GameDataHolder : MonoBehaviour
    {
        private static GameDataHolder ms_instance = null;

        internal static GameDataHolder Instance => ms_instance;


        private DataFormatTank m_dataTank = null;
        internal DataFormatTank DataTank => m_dataTank;


        private DataFormatGame m_dataGame = null;
        internal DataFormatGame DataGame => m_dataGame;


        /// <summary>
        /// Gameシーン後に遷移するシーン名
        /// </summary>
        internal static string GameExitSceneName { get; private set; } = "Game";
        /// <summary>
        /// 参加プレイヤーのリストインデックス
        /// </summary>
        internal int[] ParticipantIndexes { get; set; } = new int[GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE];


        private void Awake()
        {
            if (ms_instance == null)
            {
                ms_instance = this;
            } else
            {
                Destroy(this.gameObject);
                return;
            }
            DontDestroyOnLoad(this.gameObject);

            // データロード 
            LoadData();

            // 初期化
            for (var i = 0; i < ParticipantIndexes.Length; i++)
            {
                ParticipantIndexes[i] = -1;
            }
        }


        private void LoadData()
        {
            m_dataTank = Resources.Load<DataFormatTank>("DataTank");
            m_dataGame = Resources.Load<DataFormatGame>("DataGame");
        }



        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            if (Instance == null)
            {
                GameObject obj = new GameObject("GameDataHolder");
                obj.AddComponent<GameDataHolder>();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void GetStartSceneName()
        {
#if UNITY_EDITOR
            GameExitSceneName = EditorSceneManager.GetActiveScene().name;
#endif
            Debug.Log($"GameExitSceneName : {GameExitSceneName}");
        }

    }


}

