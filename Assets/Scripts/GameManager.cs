using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PanicBuying
{
    public class GameManager : MonoBehaviour
    {
        static GameManager instance;
        public static GameManager Instance { get => instance; }

        private void Awake()
        {
            if (instance != null)
                Destroy(instance);
            instance = this;
        }

        public ItemSO GetItemSO(int inId)
        {
            if (itemSOs != null && inId >= 0 && inId < itemSOs.Length)
            {
                return itemSOs[inId];
            }
            return null;
        }


        [SerializeField]
        ItemSO[] itemSOs;
    }
}
