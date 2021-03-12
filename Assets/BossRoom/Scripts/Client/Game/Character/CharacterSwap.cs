using UnityEngine.Assertions;
using UnityEngine;

namespace BossRoom.Client
{
    /// <summary>
    /// Responsible for storing of all of the pieces of a character, and swapping the pieces out en masse when asked to.
    /// </summary>
    public class CharacterSwap : MonoBehaviour
    {
        [System.Serializable]
        public class CharacterModelSet
        {
            public GameObject ears;
            public GameObject head;
            public GameObject mouth;
            public GameObject hair;
            public GameObject eyes;
            public GameObject torso;
            public GameObject gearRightHand;
            public GameObject gearLeftHand;
            public GameObject handRight;
            public GameObject handLeft;
            public GameObject shoulderRight;
            public GameObject shoulderLeft;
            public Visual.AnimatorTriggeredSpecialFX specialFx; // should be a component on the same GameObject as the Animator!
            public AnimatorOverrideController animatorOverrides; // references a separate stand-alone object in the project

            public void SetFullActive(bool isActive)
            {
                ears.SetActive(isActive);
                head.SetActive(isActive);
                mouth.SetActive(isActive);
                hair.SetActive(isActive);
                eyes.SetActive(isActive);
                torso.SetActive(isActive);
                gearLeftHand.SetActive(isActive);
                gearRightHand.SetActive(isActive);
                handRight.SetActive(isActive);
                handLeft.SetActive(isActive);
                shoulderRight.SetActive(isActive);
                shoulderLeft.SetActive(isActive);
            }
        }

        [SerializeField]
        private CharacterModelSet[] m_CharacterModels;

        /// <summary>
        /// Reference to our shared-characters' animator.
        /// Can be null, but if so, animator overrides are not supported!
        /// </summary>
        [SerializeField]
        private Animator m_Animator;

        /// <summary>
        /// Reference to the original controller in our Animator.
        /// We switch back to this whenever we don't have an Override.
        /// </summary>
        private RuntimeAnimatorController m_OriginalController;

        private void Awake()
        {
            if (m_Animator)
            {
                m_OriginalController = m_Animator.runtimeAnimatorController;
            }
        }

        /// <summary>
        /// Swap the visuals of the character to the index passed in. 
        /// </summary>
        /// <param name="modelIndex"></param>
        public void SwapToModel(int modelIndex)
        {
            Assert.IsTrue(modelIndex < m_CharacterModels.Length);

            for (int i = 0; i < m_CharacterModels.Length; i++)
            {
                m_CharacterModels[i].SetFullActive(i == modelIndex);
                if (m_CharacterModels[i].specialFx)
                {
                    // Disable all the specialFx; we'll turn the correct one on after the loop.
                    // "Why not just turn on the specialFx right here?" you ask.
                    // Because the same specialFx object is used for both the male and female
                    // version of each class, so if we tried to turn them on/off here, we'd end up
                    // turning it both on AND off each time through the loop! (With 50/50 odds
                    // that it ends in the state we want it to be.)
                    m_CharacterModels[i].specialFx.enabled = false;
                }
            }

            if (m_CharacterModels[modelIndex].specialFx)
            {
                m_CharacterModels[modelIndex].specialFx.enabled = true;
            }

            if (m_Animator)
            {
                // plug in the correct animator override... or plug the original non - overridden version back in!
                if (m_CharacterModels[modelIndex].animatorOverrides)
                {
                    m_Animator.runtimeAnimatorController = m_CharacterModels[modelIndex].animatorOverrides;
                }
                else
                {
                    m_Animator.runtimeAnimatorController = m_OriginalController;
                }
            }
        }

        /// <summary>
        /// Used by special effects where the character should be invisible.
        /// </summary>
        public void SwapAllOff()
        {
            for (int i = 0; i < m_CharacterModels.Length; i++)
            {
                m_CharacterModels[i].SetFullActive(false);
                if (m_CharacterModels[i].specialFx)
                {
                    m_CharacterModels[i].specialFx.enabled = false;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // if an Animator is on the same GameObject as us, assume that's the one we'll be using!
            if (!m_Animator)
                m_Animator = GetComponent<Animator>();
        }
#endif
    }
}
