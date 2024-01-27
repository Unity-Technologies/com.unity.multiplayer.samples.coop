using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace PanicBuying
{
    public class ItemBehaviour : NetworkBehaviour
    {
        public int Count { get => count.Value; private set => count.Value = value; }
        public int Duration { get => duration.Value; private set => duration.Value = value; }

        virtual public bool Use()
        {
            if (Duration > 0)
            {
                Duration--;
                return true;
            }
            else if (Count > 0)
            {
                Count--;
                Duration = itemSO.MaxDuration;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool RemoveOne()
        {
            if (Count > 0)
            {
                Count--;
                return true;
            }
            else
            {
                return false;
            }
        }


        public bool Accumulate(ItemBehaviour Other)
        {
            if (Other.itemSO != itemSO ||
                GetExtraCount() <= 0 ||
                Other.Count <= 0)
            {
                return false;
            }
            else if (Other.Count <= GetExtraCount())
            {
                Count += Other.Count;
                Other.Count = 0;
                return true;
            }
            else
            {
                Other.Count -= GetExtraCount();
                Count = GetMaxCount();
                return true;
            }
        }

        public int GetMaxCount()
        {
            return itemSO.MaxCount;
        }

        public int GetExtraCount()
        {
            return Math.Max(GetMaxCount() - Count, 0);
        }

        private void Awake()
        {
            duration.Value = itemSO.MaxDuration;

            // subscribe
        }

        [SerializeField]
        protected ItemSO itemSO;
        protected NetworkVariable<int> count = new(1);
        protected NetworkVariable<int> duration;
    }
}
