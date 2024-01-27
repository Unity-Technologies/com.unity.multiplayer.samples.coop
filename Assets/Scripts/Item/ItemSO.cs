using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PanicBuying
{
    [CreateAssetMenu(fileName = "ItemSO", menuName = "ScriptableObjects/ItemSO")]
    public class ItemSO : ScriptableObject
    {
        [SerializeField]
        Sprite image2D;
        public Sprite Image2D { get => image2D; }

        [SerializeField]
        Mesh mesh3D;
        public Mesh Mesh3D { get => mesh3D; }

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
