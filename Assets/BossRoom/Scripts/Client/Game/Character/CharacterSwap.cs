using UnityEngine.Assertions;
using UnityEngine;

namespace BossRoom.Client
{
    /// <summary>
    /// Responsible for storing of all of the pieces of a character, and swapping the pieces out en masse when asked to.
    /// Debug: logic to allow designers to easily swap models in editor
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
        /// Swap the visuals of the character to the index passed in. 
        /// </summary>
        /// <param name="modelIndex"></param>
        public void SwapToModel(int modelIndex)
        {
            Assert.IsTrue(modelIndex < m_CharacterModels.Length);

            for (int i = 0; i < m_CharacterModels.Length; i++)
            {
                m_CharacterModels[i].SetFullActive(i == modelIndex);
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
            }
        }

    }

}
