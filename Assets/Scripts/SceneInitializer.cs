using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.PanicBuying.Character
{
    public class NewBehaviourScript : MonoBehaviour
    {
        public ItemSpawner itemSpawner;
        public GameObject item;
        public int itemCounts;
        void Start()
        {
            itemSpawner.SpawnItems(new Dictionary<GameObject, int>(){ { item, itemCounts } });
        }
    }
}
