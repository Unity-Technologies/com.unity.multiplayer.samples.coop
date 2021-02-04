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
        [SerializeField]
        private ModelSwap[] m_BodyParts;

        [SerializeField]
        private Animator m_Animator;

        private void Awake()
        {
            //m_BodyParts = GetComponents<ModelSwap>();
            //m_Animator = GetComponent<Animator>();
        }

        public void SetModel(CharacterTypeEnum characterClass, bool isMale)
        {
            int newModelIndex = MapToModelIdx(characterClass, isMale);
            for (int i = 0; i < m_BodyParts.Length; ++i)
            {
                if (m_BodyParts[i])
                    m_BodyParts[i].SetModel(newModelIndex);
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

        private int MapToModelIdx(CharacterTypeEnum characterClass, bool isMale)
        {
            switch (characterClass)
            {
            case CharacterTypeEnum.ARCHER:
                return isMale ? 0 : 1;
            case CharacterTypeEnum.MAGE:
                return isMale ? 2 : 3;
            case CharacterTypeEnum.ROGUE:
                return isMale ? 4 : 5;
            case CharacterTypeEnum.TANK:
                return isMale ? 6 : 7;
            default:
                throw new System.Exception($"Cannot map {characterClass} male={isMale} to a model index!");
            }
        }

    }
}
