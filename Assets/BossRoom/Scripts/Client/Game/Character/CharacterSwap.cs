using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BossRoom.Client
{

    //DEBUG SCRIPT: Meant to serve as a placeholder to allow artists to quickly cycle through player models.
    public class CharacterSwap : MonoBehaviour
    {
        [System.SerializableAttribute]
        public class CharacterModelSet
        {
            public GameObject ears;
            public GameObject head;
            public GameObject mouth;
            public GameObject hair;
            public GameObject eyes;
            public GameObject torso;
            public GameObject gear_right_hand;
            public GameObject gear_left_hand;
            public GameObject hand_right;
            public GameObject hand_left;
            public GameObject shoulder_right;
            public GameObject shoulder_left;


            public void setFullActive(bool isActive)
            {
                ears.SetActive(isActive);
                head.SetActive(isActive);
                mouth.SetActive(isActive);
                hair.SetActive(isActive);
                eyes.SetActive(isActive);
                torso.SetActive(isActive);
                gear_left_hand.SetActive(isActive);
                hand_right.SetActive(isActive);
                hand_left.SetActive(isActive);
                shoulder_right.SetActive(isActive);
                shoulder_left.SetActive(isActive);
                gear_left_hand.SetActive(isActive);
                gear_right_hand.SetActive(isActive);
            }
        }

        [SerializeField]
        int m_ModelIndex;
        int m_LastModelIndex;

        [SerializeField]
        CharacterModelSet[] m_CharacterModels;
        void Awake()
        {
            if (m_ModelIndex >= m_CharacterModels.Length)
            {
                m_ModelIndex = 0;
            }
            SwapToModel(m_ModelIndex);
        }


        public void SwapToModel(int idx)
        {
            if (idx >= m_CharacterModels.Length)
            {
                print("Index out of bounds");
                return;
            }

            for (int x = 0; x < m_CharacterModels.Length; x++)
            {
                m_CharacterModels[x].setFullActive(x == idx);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (m_LastModelIndex != m_ModelIndex)
            {
                m_LastModelIndex = m_ModelIndex;
                SwapToModel(m_ModelIndex);
            }
        }
    }

}
