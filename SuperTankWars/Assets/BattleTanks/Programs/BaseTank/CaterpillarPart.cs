using UnityEngine;


namespace SXG2025
{

    public class CaterpillarPart : MonoBehaviour
    {
        [SerializeField] private WheelCollider[] m_wheelColliders;
        [SerializeField] private MeshRenderer m_beltRenderer = null;

        [SerializeField] private float m_scrollByTorque = 0.4f / 2000.0f;

        private float m_torque = 0;
        private Material m_beltMaterialInstance = null;
        private Vector2 m_scrollOffset = Vector2.zero;
        private int m_shaderIdMainTex = Shader.PropertyToID("_BaseMap");
        private float m_animationDir = 1;


        private void Start()
        {
            // マテリアルインスタンス作成 
            m_beltMaterialInstance = Instantiate(m_beltRenderer.material);
            m_beltRenderer.material = m_beltMaterialInstance;
        }



        /// <summary>
        /// キャタピラのホイールにトルクを設定する(0にする場合は明示的にnewTorque=0で呼ぶ必要がある)
        /// </summary>
        /// <param name="newTorque"></param>
        internal void SetTorque(float newTorque, bool withPhysics=true)
        {
            m_torque = newTorque;

            if (withPhysics)
            {
                foreach (var wheel in m_wheelColliders)
                {
                    wheel.motorTorque = m_torque;
                }
            }

            // マテリアル更新 
            m_scrollOffset.y += (newTorque * m_scrollByTorque * m_animationDir) * Time.unscaledDeltaTime;
            m_beltMaterialInstance?.SetTextureOffset(m_shaderIdMainTex, m_scrollOffset);
        }

        internal void SetAnimationDir(float animDir)
        {
            m_animationDir = animDir;
        }
    }


}

