using System;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// ScriptableObject that stores a GUID for unique identification. The population of this field is implemented
    /// inside an Editor script.
    /// </summary>
    [Serializable]
    public abstract class GuidScriptableObject : ScriptableObject
    {
        [HideInInspector]
        [SerializeField]
        byte[] m_Guid;

        public Guid Guid => new Guid(m_Guid);

        void OnValidate()
        {
            if (m_Guid.Length == 0)
            {
                m_Guid = Guid.NewGuid().ToByteArray();
            }
        }
    }
}
