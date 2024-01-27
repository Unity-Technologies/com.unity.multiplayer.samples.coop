using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace PanicBuying
{
    public struct ItemData : INetworkSerializeByMemcpy
    {
        public ItemData(int itemId = -1)
        {
            var itemSO = GameManager.Instance?.GetItemSO(itemId);

            id = itemId;
            count = 1;
            duration = itemSO != null ? itemSO.MaxDuration : 0;
        }


        public int Count { get => count; set => count = value; }
        public int Duration { get => duration; set => duration = value; }
        public ItemSO SO { get => GameManager.Instance?.GetItemSO(id); }
        public int Id { get => id; }

        public bool UseOnce()
        {
            if (Duration > 0)
            {
                Duration--;
                return true;
            }
            else if (Count > 0)
            {
                Count--;
                Duration = SO.MaxDuration;
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


        public bool Accumulate(ref ItemData Other)
        {
            if (Other.SO != SO ||
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
            return SO.MaxCount;
        }

        public int GetExtraCount()
        {
            return Math.Max(GetMaxCount() - Count, 0);
        }

        int id;
        int count;
        int duration;
    }
}
