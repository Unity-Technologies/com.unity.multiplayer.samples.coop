using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace PanicBuying
{
    [CreateAssetMenu(fileName = "ItemSO", menuName = "ScriptableObjects/ItemSO")]
    public class ItemSO : ScriptableObject
    {
        [SerializeField]
        Sprite image2D;
        public Sprite Image2D { get => image2D; }

        [SerializeField]
        GameObject holdingPrefab;
        public GameObject HoldingPrefab { get => holdingPrefab; }
        
        [SerializeField]
        NetworkObject droppedPrefab;
        public NetworkObject DroppedPrefab { get => droppedPrefab; }

        [SerializeField]
        int maxCount = 1;
        public int MaxCount { get => maxCount; }

        [SerializeField]
        int maxDuration = 0;
        public int MaxDuration { get => maxDuration; }

        [SerializeField]
        int weight = 1;
        public int Weight { get => weight; }

        [SerializeField]
        bool canCarry = true;
        public bool CanCarry { get => canCarry; }
    }
}
