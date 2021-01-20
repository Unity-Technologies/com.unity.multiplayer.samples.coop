using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// Sets the visual representation of a player character to a
    /// specific class and gender. (Note: internals of this class will
    /// be replaced, but the front-facing interface will remain.)
    /// </summary>
    public class ModelAppearanceSetter : MonoBehaviour
    {
        private ModelSwap[] m_BodyParts;
        private Animator m_Animator;

        private void Awake()
        {
            m_BodyParts = GetComponents<ModelSwap>();
            m_Animator = GetComponent<Animator>();
        }

        public void SetModel(CharacterTypeEnum Class, bool IsMale)
        {
            int newModelIndex = MapToModelIdx(Class, IsMale);
            for (int x = 0; x < m_BodyParts.Length; ++x)
            {
                if (m_BodyParts[ x ])
                    m_BodyParts[ x ].SetModel(newModelIndex);
            }
        }

        public enum UIGesture
        {
            Selected,
            LockedIn,
        }

        /// <summary>
        /// A way for the lobby UI to tell a dummy character model
        /// to perform a "yay, you selected me!" type animation. This
        /// interface should not be used during actual gameplay.
        /// </summary>
        public void PerformUIGesture(UIGesture gesture)
        {
            // just uses existing triggers that happen to be handy.
            // Will be replaced with UI-specific triggers
            switch (gesture)
            {
            case UIGesture.Selected:
                m_Animator.SetTrigger("BeginRevive");
                break;
            case UIGesture.LockedIn:
                m_Animator.SetTrigger("BeginRevive");
                break;
            }
        }

        private int MapToModelIdx(CharacterTypeEnum Class, bool IsMale)
        {
            switch (Class)
            {
            case CharacterTypeEnum.ARCHER:
                return IsMale ? 0 : 1;
            case CharacterTypeEnum.MAGE:
                return IsMale ? 2 : 3;
            case CharacterTypeEnum.ROGUE:
                return IsMale ? 4 : 5;
            case CharacterTypeEnum.TANK:
                return IsMale ? 6 : 7;
            default:
                Debug.LogError("Cannot map " + Class + " " + (IsMale ? "male" : "female") + "!");
                return 0;
            }
        }

    }
}
