using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.PanicBuying.Character
{
    public class ItemSpawner : MonoBehaviour
    {
        private ItemPositioner[] itemSpawners;

        void Start()
        {
            itemSpawners = GetComponentsInChildren<ItemPositioner>();
        }

        public void SpawnItems(Dictionary<GameObject, int> itemSpawnDict)
        {
            foreach (KeyValuePair<GameObject, int> item in itemSpawnDict)
            {
                for (int i = 0; i < item.Value; i++)
                {
                    int spawnerIdx = Random.Range(0, itemSpawners.Length);
                    itemSpawners[spawnerIdx].spawnItem(item.Key);
                }
            }
        }
    }
}
