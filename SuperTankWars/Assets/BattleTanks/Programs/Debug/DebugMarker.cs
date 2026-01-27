#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SXG2025
{
    [ExecuteAlways]
    public class DebugMarker : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] DecalProjector m_projector;

        [Header("Visual")]
        [SerializeField, Range(0f, 1f)] float m_alpha = 0.25f;
        [SerializeField] float m_surfaceOffset = 0.02f; // 地面から少し浮かせる

        [Header("Raycast")]
        [SerializeField] LayerMask m_groundMask = 0; // Ground推奨（未設定なら全判定でもOK）


        [SerializeField] private Material[] m_teamMaterials = new Material[4];

        // 外部から更新される想定（生成AIが持っているgoalを渡す）
        //public Vector3 GoalWorldPos { get; set; }
        //public Color TeamColor { get; set; } = Color.green;

        MaterialPropertyBlock m_mpb;

        void OnEnable()
        {
            if (m_projector == null) m_projector = GetComponent<DecalProjector>();
            if (m_mpb == null) m_mpb = new MaterialPropertyBlock();

            // お好み：Editorで邪魔なら最初は非表示にするなど
        }

        private void Start()
        {
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        public void SetTeamNo(int teamNo)
        {
            if (m_projector != null)
            {
                if (0 <= teamNo && teamNo < m_teamMaterials.Length)
                {
                    m_projector.material = m_teamMaterials[teamNo];
                }
            }
        }

    }
}
#endif