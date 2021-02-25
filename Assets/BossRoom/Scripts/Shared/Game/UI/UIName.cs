using MLAPI.NetworkedVar;
using UnityEngine;
using UnityEngine.UI;

namespace BossRoom
{
    /// <summary>
    /// UI object that visually represents an object's name. Visuals are updated when NetworkedVar is modified.
    /// </summary>
    public class UIName : MonoBehaviour
    {
        [SerializeField]
        Text m_UINameText;

        NetworkedVarString m_NetworkedNameTag;

        public void Initialize(NetworkedVarString networkedName)
        {
            m_NetworkedNameTag = networkedName;

            NameUpdated(string.Empty, m_NetworkedNameTag.Value);
            networkedName.OnValueChanged += NameUpdated;
        }

        void NameUpdated(string previousValue, string newValue)
        {
            m_UINameText.text = newValue;
        }

        void OnDestroy()
        {
            m_NetworkedNameTag.OnValueChanged += NameUpdated;
        }
    }
}
