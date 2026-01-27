using UnityEngine;



namespace SXG2025
{

    public class FieldColumn : MonoBehaviour
    {
        public void PutDamageByShell()
        {
            Vector3 pos = transform.position;
            pos.y += GameDataHolder.Instance.DataGame.m_hitSinkDepthOfColumn;
            transform.position = pos;
        }
    }


}


