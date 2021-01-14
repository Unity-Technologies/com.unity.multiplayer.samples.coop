using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BossRoom.Visual
{
    public class ModelSwap : MonoBehaviour
    {
        //!! DEBUG SCRIPT! 

        public GameObject[] modelArray;
        public int m_ModelIndex = 0;

        //debug; used for changing assets at runtime. 
        private int m_lastModelIndex = 0;

        // Use this for initialization
        void Start()
        {
            SetModel(m_ModelIndex);
        }

        public void SetModel(int index)
        {
            index = System.Math.Min(index, modelArray.Length - 1);
            m_ModelIndex = index;
            for (int x = 0; x < modelArray.Length; x++)
            {
                modelArray[x].SetActive(x == m_ModelIndex);
            }
        }

        // Update is called once per frame
        public void Update()
        {
			//DEBUG: This allows you to change models at runtime by tweaking the ModelIndex in the editor. 
            if( m_lastModelIndex != m_ModelIndex )
            {
                m_lastModelIndex = m_ModelIndex;
                SetModel(m_ModelIndex);
            }
        }
    }
}
