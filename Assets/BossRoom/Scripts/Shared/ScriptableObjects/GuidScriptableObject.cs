using System;
using UnityEngine;

namespace BossRoom
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
    }
}
