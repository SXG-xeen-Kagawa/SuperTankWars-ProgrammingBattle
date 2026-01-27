using UnityEngine;


namespace SXG2025
{

    public class GameConfigSetting
    {
        public static readonly string TournamentSceneName = "Tournament16";

        /// <summary>
        /// 現在トーナメント中か
        /// </summary>
        internal bool IsTournament { get; set; } = false;

        /// <summary>
        /// 第何回戦か
        /// </summary>
        internal int RoundCount { get; set; } = 1;

        /// <summary>
        /// 参加プレイヤーの番号
        /// </summary>
        internal int[] Participants { get; set; } = new int[4] { -1, -1, -1, -1 };

        /// <summary>
        /// 試合結果の順位
        /// </summary>
        internal int[] Ranking { get; set; } = new int[4] { -1, -1, -1, -1 };



        private static GameConfigSetting ms_instance = null;
        static internal GameConfigSetting  Instance
        {
            get
            {
                if (ms_instance == null)
                {
                    ms_instance = new();
                }
                return ms_instance;
            }
        }
    }


}

