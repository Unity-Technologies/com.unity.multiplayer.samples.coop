using UnityEngine;
using TMPro;

namespace BossRoom
{
    /// <summary>
    /// UI object that visually represents an object's name. Visuals are updated when NetworkVariable is modified.
    /// </summary>
    public class UIName : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI m_UINameText;

        NetworkNameState m_NetworkNameState;

        public void Initialize(NetworkNameState networkedName)
        {
            m_NetworkNameState = networkedName;

            m_UINameText.text = networkedName.NetworkName;

            m_NetworkNameState.AddListener(NameUpdated);
        }

        void NameUpdated(string previousValue, string newValue)
        {
            m_UINameText.text = newValue;
        }

        void OnDestroy()
        {
            m_NetworkNameState.RemoveListener(NameUpdated);
        }
    }
}
