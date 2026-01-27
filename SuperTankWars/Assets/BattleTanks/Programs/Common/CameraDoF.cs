using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace SXG2025
{

    public class CameraDoF : MonoBehaviour
    {
        private static CameraDoF ms_instance = null;
        private Volume m_volume = null;
        private Camera m_camera = null;


        internal static CameraDoF Instance => ms_instance;

        private void Awake()
        {
            ms_instance = this;
            m_volume = GetComponent<Volume>();
            m_camera = GetComponent<Camera>();
        }


        /// <summary>
        /// 状態変更 
        /// </summary>
        /// <param name="isActive"></param>
        internal void Change(bool isActive)
        {
            if (m_volume != null)
            {
                DepthOfField dof = null;
                m_volume.profile.TryGet(out dof);
                if (dof != null)
                {
                    dof.active = isActive;
                }
            }
        }

    }

}

