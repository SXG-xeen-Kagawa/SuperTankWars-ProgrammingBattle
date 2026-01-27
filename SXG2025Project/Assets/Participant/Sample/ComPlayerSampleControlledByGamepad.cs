using UnityEngine;

namespace nsSample
{

    public class ComPlayerSampleControlledByGamepad : SXG2025.ComPlayerBase
    {
        private void Update()
        {
            // テストプレイ用：ゲームパッドで操作する (AI作ったらコメントアウトしてね)
            SXG_TestPlayByGamepad();
        }

    }


}

