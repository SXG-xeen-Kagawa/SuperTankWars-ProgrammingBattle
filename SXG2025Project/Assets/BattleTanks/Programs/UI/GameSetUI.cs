using UnityEngine;


namespace SXG2025
{

    public class GameSetUI : MonoBehaviour
    {
        private Animator m_animator = null;

        private void Awake()
        {
            m_animator = GetComponent<Animator>();
        }

        public void Enter()
        {
            m_animator.SetTrigger("Enter");
        }

    }

}

