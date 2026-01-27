using UnityEngine;
using UnityEngine.InputSystem;

namespace SXG2025
{


    public partial class ComPlayerBase : MonoBehaviour
    {


        #region テスト用の機能 

        /// <summary>
        /// ゲームパッドを使用したテストプレイをするときは、Updateでこの関数を呼んで下さい
        /// </summary>
        protected void SXG_TestPlayByGamepad()
        {
            // 操作テスト 
            if (Gamepad.current != null)
            {
                Vector2 leftStick = Gamepad.current.leftStick.ReadValue();
                Vector2 rightStick = Gamepad.current.rightStick.ReadValue();
                float rightShoulder = Gamepad.current.rightShoulder.ReadValue();
                float leftShoulder = Gamepad.current.leftShoulder.ReadValue();

                // キャタピラ操作 
                SXG_SetCaterpillarPower(leftStick.y, rightStick.y);

                // 砲塔を旋回 
                {
                    float yawRotDir = 0;
                    float pitchRotDir = 0;
                    if (0.1f < rightShoulder)
                    {
                        yawRotDir += rightShoulder;
                    }
                    if (0.1f < leftShoulder)
                    {
                        yawRotDir += -leftShoulder;
                    }
                    if (leftStick.x < -0.7f && 0.7f < rightStick.x)
                    {
                        pitchRotDir = -1;
                    }
                    else if (0.7f < leftStick.x && rightStick.x < -0.7f)
                    {
                        pitchRotDir = 1;
                    }

                    for (int i = 0; i < SXG_GetCountOfMyTurrets(); ++i)
                    {
                        SXG_RotateTurretToDirection(i, yawRotDir, pitchRotDir);
                    }
                }

                // 砲弾を発射 
                if (Gamepad.current.rightTrigger.wasPressedThisFrame)
                {
                    for (int i = 0; i < SXG_GetCountOfMyTurrets(); ++i)
                    {
                        SXG_Shoot(i);
                    }
                }
            }

        }

        #endregion
    }

}
