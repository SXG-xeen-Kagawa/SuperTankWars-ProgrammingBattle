using UnityEngine;
using UnityEngine.InputSystem;


namespace SXG2025
{

    public class TestTank : MonoBehaviour
    {
        [SerializeField] private WheelCollider[] m_leftWheels;
        [SerializeField] private WheelCollider[] m_rightWheels;

        [SerializeField] private float m_maxWheelTorque = 2000;
        [SerializeField] private float m_brakeTorque = 1000;

        private Rigidbody m_rigidbody = null;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            m_rigidbody = GetComponent<Rigidbody>();
        }

        // Update is called once per frame
        void Update()
        {
            //m_rigidbody.drag
        }


        private void FixedUpdate()
        {
            if (Gamepad.current != null)
            {
                Vector2 leftStick = Gamepad.current.leftStick.ReadValue();
                Vector2 rightStick = Gamepad.current.rightStick.ReadValue();

                SetTorqueToWheels(m_leftWheels, m_maxWheelTorque * leftStick.y);
                SetTorqueToWheels(m_rightWheels, m_maxWheelTorque * rightStick.y);

                //Debug.Log("Left=" + leftStick + " / Right=" + rightStick + " | T=" + Time.frameCount);
            }
        }

        private void SetTorqueToWheels(WheelCollider[] wheels, float torque)
        {
            const float ABOUT_STOP = 0.01f;

            float breakeTorque = 0;
            if (Mathf.Abs(torque) < ABOUT_STOP)
            {
                torque = 0;
                breakeTorque = m_brakeTorque;
            }

            foreach (var wheel in wheels)
            {
                wheel.motorTorque = torque;

                //wheel.brakeTorque = breakeTorque;
            }
        }
    }


}

