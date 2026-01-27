using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SXG2025
{

    public class GameInputManager : MonoBehaviour
    {
        public static GameInputManager ms_instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnGameStart()
        {
            if (ms_instance == null)
            {
                // プレハブをロードしてインスタンスとして保持
                GameObject prefab = Resources.Load<GameObject>("GameInputManager");
                ms_instance = Instantiate(prefab).GetComponent<GameInputManager>();

                // シーン遷移時に破棄されないように設定
                DontDestroyOnLoad(ms_instance.gameObject);
            }
        }


        internal static GameInputManager Instance => ms_instance;

        public delegate void OnPressedKeyDelegate();


        public enum Type
        {
            Decide,     // 決定 
            ShiftDecide,
            SpeedUp,
            SpeedDown,
            DebugFPS,   // FPS Graph
            //Replay,     // Replay開始 
            //ReplaySpeedUp,
            //ReplaySpeedDown,
            //ReplaySpeedStop,
            //CameraTarget0,
            //CameraTarget1,
            //CameraTarget2,
            //CameraTarget3,
            //CameraTargetReset,
        }
        private bool[] m_wasPressed;

        private Vector2 m_cameraMove = Vector2.zero;
        private float m_cameraDistance = 0;

        private OnPressedKeyDelegate OnPressedKeyCallback_Decide = null;
        private OnPressedKeyDelegate OnPressedKeyCallback_ShiftDecide = null;
        private OnPressedKeyDelegate OnPressedKeyCallback_SpeedUp = null;
        private OnPressedKeyDelegate OnPressedKeyCallback_SpeedDown = null;
        private OnPressedKeyDelegate OnPressedKeyCallback_DebugFPS = null;
        //private OnPressedKeyDelegate OnPressedKeyCallback_Replay = null;
        //private OnPressedKeyDelegate OnPressedKeyCallback_ReplaySpeedUp = null;
        //private OnPressedKeyDelegate OnPressedKeyCallback_ReplaySpeedDown = null;
        //private OnPressedKeyDelegate OnPressedKeyCallback_ReplaySpeedStop = null;
        //private OnPressedKeyDelegate OnPressedKeyCallback_CameraTarget0 = null;
        //private OnPressedKeyDelegate OnPressedKeyCallback_CameraTarget1 = null;
        //private OnPressedKeyDelegate OnPressedKeyCallback_CameraTarget2 = null;
        //private OnPressedKeyDelegate OnPressedKeyCallback_CameraTarget3 = null;
        //private OnPressedKeyDelegate OnPressedKeyCallback_CameraTargetReset = null;


        // Start is called before the first frame update
        void Start()
        {
            m_wasPressed = new bool[Enum.GetValues(typeof(Type)).Length];
            ResetAll();
        }

        private void ResetAll()
        {
            for (int i = 0; i < m_wasPressed.Length; ++i)
            {
                m_wasPressed[i] = false;
            }
        }

        // Update is called once per frame
        void LateUpdate()
        {
            ResetAll();
        }


        /// <summary>
        /// 押した？ 
        /// </summary>
        /// <param name="inputType"></param>
        /// <returns></returns>
        public bool WasPressed(Type inputType)
        {
            return m_wasPressed[(int)inputType];
        }


        public Vector2 GetCameraMoveVector()
        {
            return m_cameraMove;
        }
        public float GetCameraDistanceValue()
        {
            return m_cameraDistance;
        }



        public void SetCallback(Type type, OnPressedKeyDelegate callback)
        {
            switch (type)
            {
                case Type.Decide:
                    OnPressedKeyCallback_Decide += callback;
                    break;
                case Type.ShiftDecide:
                    OnPressedKeyCallback_ShiftDecide += callback;
                    break;
                case Type.SpeedUp:
                    OnPressedKeyCallback_SpeedUp += callback;
                    break;
                case Type.SpeedDown:
                    OnPressedKeyCallback_SpeedDown += callback;
                    break;
                case Type.DebugFPS:
                    OnPressedKeyCallback_DebugFPS += callback;
                    break;
                //case Type.Replay:
                //    OnPressedKeyCallback_Replay += callback;
                //    break;
                //case Type.ReplaySpeedUp:
                //    OnPressedKeyCallback_ReplaySpeedUp += callback;
                //    break;
                //case Type.ReplaySpeedDown:
                //    OnPressedKeyCallback_ReplaySpeedDown += callback;
                //    break;
                //case Type.ReplaySpeedStop:
                //    OnPressedKeyCallback_ReplaySpeedStop += callback;
                //    break;
                //case Type.CameraTarget0:
                //    OnPressedKeyCallback_CameraTarget0 += callback;
                //    break;
                //case Type.CameraTarget1:
                //    OnPressedKeyCallback_CameraTarget1 += callback;
                //    break;
                //case Type.CameraTarget2:
                //    OnPressedKeyCallback_CameraTarget2 += callback;
                //    break;
                //case Type.CameraTarget3:
                //    OnPressedKeyCallback_CameraTarget3 += callback;
                //    break;
                //case Type.CameraTargetReset:
                //    OnPressedKeyCallback_CameraTargetReset += callback;
                //    break;
            }
        }



        #region Callback

        public void OnDecide(InputAction.CallbackContext context)
        {
            //Debug.Log(string.Format("OnDecide: phase={0} time={1} | T={2}", 
            //    context.phase, context.time, Time.frameCount));
            OnInputCallback(context, Type.Decide, OnPressedKeyCallback_Decide);
        }

        public void OnShiftDecide(InputAction.CallbackContext context)
        {
            OnInputCallback(context, Type.ShiftDecide, OnPressedKeyCallback_ShiftDecide);
        }

        public void OnSpeedUp(InputAction.CallbackContext context)
        {
            OnInputCallback(context, Type.SpeedUp, OnPressedKeyCallback_SpeedUp);
        }

        public void OnSpeedDown(InputAction.CallbackContext context)
        {
            OnInputCallback(context, Type.SpeedDown, OnPressedKeyCallback_SpeedDown);
        }

        public void OnDebugFPS(InputAction.CallbackContext context)
        {
            OnInputCallback(context, Type.DebugFPS, OnPressedKeyCallback_DebugFPS);
        }

        //public void OnReplay(InputAction.CallbackContext context)
        //{
        //    OnInputCallback(context, Type.Replay, OnPressedKeyCallback_Replay);
        //}

        //public void OnReplaySpeedUp(InputAction.CallbackContext context)
        //{
        //    OnInputCallback(context, Type.ReplaySpeedUp, OnPressedKeyCallback_ReplaySpeedUp);
        //}
        //public void OnReplaySpeedDown(InputAction.CallbackContext context)
        //{
        //    OnInputCallback(context, Type.ReplaySpeedDown, OnPressedKeyCallback_ReplaySpeedDown);
        //}
        //public void OnReplaySpeedStop(InputAction.CallbackContext context)
        //{
        //    OnInputCallback(context, Type.ReplaySpeedStop, OnPressedKeyCallback_ReplaySpeedStop);
        //}
        //public void OnCameraTarget0(InputAction.CallbackContext context)
        //{
        //    OnInputCallback(context, Type.CameraTarget0, OnPressedKeyCallback_CameraTarget0);
        //}
        //public void OnCameraTarget1(InputAction.CallbackContext context)
        //{
        //    OnInputCallback(context, Type.CameraTarget1, OnPressedKeyCallback_CameraTarget1);
        //}
        //public void OnCameraTarget2(InputAction.CallbackContext context)
        //{
        //    OnInputCallback(context, Type.CameraTarget2, OnPressedKeyCallback_CameraTarget2);
        //}
        //public void OnCameraTarget3(InputAction.CallbackContext context)
        //{
        //    OnInputCallback(context, Type.CameraTarget3, OnPressedKeyCallback_CameraTarget3);
        //}

        //public void OnCameraTargetReset(InputAction.CallbackContext context)
        //{
        //    OnInputCallback(context, Type.CameraTargetReset, OnPressedKeyCallback_CameraTargetReset);
        //}

        private void OnInputCallback(InputAction.CallbackContext context, Type inputType, OnPressedKeyDelegate callback)
        {
            if (context.phase == InputActionPhase.Started)
            {
                m_wasPressed[(int)inputType] = true;

                if (callback != null)
                {
                    callback.Invoke();
                }
            }
        }



        public void OnCameraMove(InputAction.CallbackContext context)
        {
            m_cameraMove = context.ReadValue<Vector2>();
            //Debug.Log("[UDON] CameraMove = (" + m_cameraMove.x.ToString("0.000") + "," + m_cameraMove.y.ToString("0.000") + ")");
        }

        public void OnCameraDistance(InputAction.CallbackContext context)
        {
            m_cameraDistance = context.ReadValue<float>();
            //Debug.Log("[UDON] CameraDistance = " + m_cameraDistance);
        }


        #endregion


    }


}

