using UnityEngine;


namespace SXG2025
{

    public class Constants
    {


        #region ObjectLayer

        public const int OBJ_LAYER_TANK = 6;    // Tank (戦車)
        public const int OBJ_LAYER_SHELL = 7;   // Shell (砲弾)
        public const int OBJ_LAYER_BARREL = 8;  // Barrel (砲身：砲弾が自身に当たらないようにするためのレイヤー)
        public const int OBJ_LAYER_VIRTUALWALL = 9; // VirtualWall (仮想の透明壁)
        public const int OBJ_LAYER_SHIELD = 10; // Shield（無敵シールド。戦車出現後数秒のみシールドをまとう）
        public const int OBJ_LAYER_DROPPED_PART = 11;   // DroppedPart （被弾して落下したパーツ）
        public const int OBJ_LAYER_GROUND = 12; // Ground（地形）

        #endregion


        public const int CANVAS_WIDTH = 1920;
        public const int CANVAS_HEIGHT = 1080;

    }


}

