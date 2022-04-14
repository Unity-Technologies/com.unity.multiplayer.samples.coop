using System.Collections;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    public class UIMessageSlot : MonoBehaviour
    {
        [SerializeField]
        Animator m_Animator;

        [SerializeField]
        TMPro.TextMeshProUGUI m_TextLabel;

        [SerializeField]
        float m_HideDelay = 10;
        public bool IsDisplaying { get; private set; }

        public void Display(string text)
        {
            if (!IsDisplaying)
            {
                IsDisplaying = true;
                m_Animator.SetTrigger("Display");
                StartCoroutine(HideCoroutine());
                m_TextLabel.text = text;
            }
        }

        IEnumerator HideCoroutine()
        {
            yield return new WaitForSeconds(m_HideDelay);
            Hide();
        }

        void Hide()
        {
            if (IsDisplaying)
            {
                IsDisplaying = false;
                m_Animator.SetTrigger("Hide");
            }
        }
    }
}
