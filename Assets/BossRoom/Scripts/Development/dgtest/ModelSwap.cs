using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BossRoom.Viz
{
    public class ModelSwap : MonoBehaviour
    {
        //!! DEBUG SCRIPT! 

        public GameObject[] modelArray;
        private int m_ModelIndex;
        public Button yourButton;

        // Use this for initialization
        void Start()
        {

            m_ModelIndex = 0;
            ModelSwitch();
            GameObject hackyButtonGameObject = GameObject.Find("Button (1)");
            if (hackyButtonGameObject)
            {
                Button btn = hackyButtonGameObject.GetComponent<Button>();
                btn.onClick.AddListener(ModelSwitch);
            }
        }

        public void ModelSwitch()
        {
            for (int x = 0; x < modelArray.Length; x++)
            {
                modelArray[x].SetActive(x == m_ModelIndex);
            }
            m_ModelIndex += 1;
            if (m_ModelIndex > modelArray.Length - 1)
            {
                m_ModelIndex = 0;
            }
        }



        // Update is called once per frame
        public void Update()
        {

            if (Input.GetKeyDown(KeyCode.Return))
            {
                ModelSwitch();
            }

        }
    }
}
