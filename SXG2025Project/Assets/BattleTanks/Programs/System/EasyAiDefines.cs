using UnityEngine;

namespace SXG2025
{

    public class EasyAI
    {
        public enum TargetRule
        {
            MinDistance = 0,    // 最も近い戦車を狙う 
            MaxEnergy = 1,      // 最もエネルギー残量の多い戦車を狙う 
            MinEnergy = 2,      // 最もエネルギー残量の少ない戦車を狙う
            InFront = 3,        // 正面方向に近い戦車を狙う 
            Behind = 4,         // 背面方向に近い戦車を狙う 
        }

    }

}