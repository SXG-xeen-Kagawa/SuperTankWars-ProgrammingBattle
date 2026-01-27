using UnityEngine;

namespace SXG2025
{

    public class GameConstants
    {
        public const int MAX_PLAYER_COUNT_IN_ONE_BATTLE = 4;    // １戦に参加するプレイヤー数

        public const float ABYSS_POSITION_Y = -100.0f;  // 共通奈落の底の高さ

        public const int DEFAULT_PLAYER_ENERGY = 1000;      // プレイヤー(チーム)の初期のエナジー 



        public const float ABOUT_GAME_FIELD_RADIUS = 40.0f;    // 大まかなゲームフィールドの半径 

        public const float CANNON_SHELL_GRAVITY_SCALE = 2.0f;   // 砲弾の重力スケール (砲弾のみ重力を大きくして山なり軌道にする)
    }


}
