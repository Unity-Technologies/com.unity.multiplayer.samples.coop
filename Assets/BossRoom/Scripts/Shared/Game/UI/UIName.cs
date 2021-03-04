using MLAPI.NetworkVariable;
using UnityEngine;
using UnityEngine.UI;

namespace BossRoom
{
    /// <summary>
    /// UI object that visually represents an object's name. Visuals are updated when NetworkVariable is modified.
    /// </summary>
    public class UIName : MonoBehaviour
    {
        [SerializeField]
        Text m_UINameText;

        NetworkVariableString m_NetworkedNameTag;

        public void Initialize(NetworkVariableString networkedName)
        {
            m_NetworkedNameTag = networkedName;

            m_UINameText.text = networkedName.Value;
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
