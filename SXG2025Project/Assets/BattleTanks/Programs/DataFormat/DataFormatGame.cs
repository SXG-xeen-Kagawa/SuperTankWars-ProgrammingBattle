using UnityEngine;

namespace SXG2025
{
    [CreateAssetMenu(fileName = "DataFormatGame", menuName = "SXG2025/DataFormatGame")]
    public class DataFormatGame : ScriptableObject
    {
        public float m_invincibleTimeAfterSpawn = 2.0f;     // 戦車生成直後の無敵時間 
        public float m_maxInvinsibleShieldRadius = 10.0f;   // 戦車をまとう無敵シールドの最大半径 
        public float m_minInvinsibleShieldRadius = 2.5f;    // 戦車をまとう無敵シールドの最小半径 

        public int m_survivedBonusScore = 100;          // 生き残りのボーナススコア(生存者で分け合う)

        public float m_hitSinkDepthOfColumn = -0.2f;     // フィールドの柱に砲弾が当たって沈む深さ 
    }
}
