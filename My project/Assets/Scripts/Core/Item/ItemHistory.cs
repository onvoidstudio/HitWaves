using System;
using System.Collections.Generic;
using UnityEngine;

namespace HitWaves.Core.Item
{
    public class ItemHistory : MonoBehaviour
    {
        private const string LOG_TAG = "ItemHistory";

        private List<ItemMaker> _absorbedItems;

        public IReadOnlyList<ItemMaker> AbsorbedItems => _absorbedItems;

        public event Action<ItemMaker> OnItemAbsorbed;

        private void Awake()
        {
            _absorbedItems = new List<ItemMaker>();
        }

        public void AddAbsorbed(ItemMaker item)
        {
            if (item == null)
            {
                DebugLogger.LogWarning(LOG_TAG, $"{gameObject.name}: null ItemMaker 흡수 기록 시도.", this);
                return;
            }

            _absorbedItems.Add(item);
            DebugLogger.Log(LOG_TAG, $"{gameObject.name}: '{item.ItemName}' 흡수 기록 추가. 총 {_absorbedItems.Count}개.", this);
            OnItemAbsorbed?.Invoke(item);
        }
    }
}
