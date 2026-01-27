using UnityEngine;

namespace SXG2025
{

    public class DestroyPartInGame : MonoBehaviour
    {
        public void Delete()
        {
            gameObject.SetActive(false);
        }
    }


}

