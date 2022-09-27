using System;
using System.Collections;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.UI
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
                transform.parent.SetAsLastSibling();
            }
        }

        IEnumerator HideCoroutine()
        {
            yield return new WaitForSeconds(m_HideDelay);
            m_Animator.SetTrigger("Hide");
        }

        public void Hide()
        {
            if (IsDisplaying)
            {
                IsDisplaying = false;
            }
        }
    }
}
