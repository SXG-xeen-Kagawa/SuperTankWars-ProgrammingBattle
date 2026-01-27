using System.Collections.Generic;
using UnityEngine;

namespace SXG2025
{

    [CreateAssetMenu(menuName = "SXG2025/Create ParticipantList")]
    public class ParticipantList : ScriptableObject
    {
        public List<ComPlayerBase> m_comPlayers;
    }


}

