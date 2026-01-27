using UnityEngine;


namespace SXG2025
{

    public class DroppedPart : MonoBehaviour
    {
        void Update()
        {
            // 奈落に落ちた時の判定 
            if (transform.position.y < GameConstants.ABYSS_POSITION_Y)
            {
                Destroy(gameObject);
            }
        }


        private void OnTriggerEnter(Collider other)
        {
            // 奈落に落ちたら消滅させる 
            if (other.gameObject.layer == Constants.OBJ_LAYER_VIRTUALWALL)
            {
                Destroy(gameObject);
            }
        }

    }


}

