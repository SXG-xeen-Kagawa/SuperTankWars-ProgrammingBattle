using UnityEngine;

namespace SXG2025
{

    [CreateAssetMenu(menuName="SXG2025/DataFormatTank")]
    public class DataFormatTank : ScriptableObject
    {
        public float m_turretRotSpeedYaw = 180;     // 砲塔のヨー旋回角速度(degree/second)
        public float m_turretRotSpeedPitch = 90;    // 砲身のピッチ旋回角速度(degree/second)
        public float m_turretRotPitchLimitUp = -45;   // 砲身のピッチ角度限界 
        public float m_turretRotPitchLimitDown = 10;   // 砲身のピッチ角度限界 

        public float m_rotateJointRotSpeedYaw = 180;     // 回転ジョイントのヨー旋回角速度(degree/second)

        public float m_shootCannonShellVelocity = 100;  // 砲弾の発射速度 
        public float m_shootRecoilForce = -100;      // 砲弾を発射したことによる反動 

        public float m_shellCollidedExplosionForce = 1000000;  // 砲弾が当たった時の爆発力 
        public float m_shellCollidedExplosionRadius = 10.0f;    // 砲弾が当たった時の爆発半径 

        public int m_cannonShellGamePower = 100;  // ゲーム内での砲弾の攻撃力 
        public float m_cannonShellGameDamageRadius = 5.0f;  // ゲーム内でヒット時にダメージを与える半径 

        public float m_turretPartObjectVolume = 5.0f;   // 砲塔部分の共通オブジェクト体積 
        public int m_turretPartCost = 100;        // 砲塔部分の共通コスト 

        public float m_rotJointPartObjectVolume = 0.25f;    // 回転ジョイントの共通オブジェクト体積 
        public int m_rotJointPartCost = 2;          // 回転ジョイントの共通コスト(かなり安い)

        public int m_tankBasePartCost = 100;        // 戦車の基本駆動部位のコスト(最低コスト)
        public float m_tankVolumeToCostCoef = 1.0f; // 戦車の部位の体積からコストに変換する係数 
        public float m_tankCostToMassCoef = 4.0f;   // 戦車のコストから物理質量に変換する係数 
        public float m_tankBaseMass = 1000.0f;      // 戦車の最低限の質量（駆動部分＋１つの砲塔を想定）
        public float m_tankVolumeToDurability = 1.0f;   // 戦車の体積から耐久力に変換する係数 

        public float m_tankMaxAngularVelocity = 10.0f;  // 戦車のRigidbody.maxAngularVelocity最大角速度 

        public float m_shotCooldownTime = 1.5f;     // 弾を発射した後のクールタイム 

        public Bounds m_regulationBounds = new Bounds(new Vector3(0, 4, 0), new Vector3(5, 5, 5));  // 戦車の既定サイズ 

    }


}
