using System;
using System.IO;
using Unity.Netcode;
using UnityEngine;

namespace BossRoom
{
    public class NetworkVariableGUID : NetworkVariableBase
    {
        [SerializeField]
        private Guid m_InternalValue;

        public Guid Value
        {
            get => m_InternalValue;
            set => Set(value);
        }

        private void Set(Guid value)
        {
            if (value == m_InternalValue)
            {
                return;
            }

            SetDirty(true);
            Guid previousValue = m_InternalValue;
            m_InternalValue = value;
            OnValueChanged?.Invoke(previousValue, m_InternalValue);
        }

        public delegate void OnValueChangedDelegate(Guid oldGuid, Guid newGuid);

        public event OnValueChangedDelegate OnValueChanged;

        public override void WriteDelta(Stream stream)
        {
            throw new System.NotImplementedException();
        }
        public override void WriteField(Stream stream)
        {
            throw new System.NotImplementedException();
        }
        public override void ReadField(Stream stream)
        {
            throw new System.NotImplementedException();
        }
        public override void ReadDelta(Stream stream, bool keepDirtyDelta)
        {
            throw new System.NotImplementedException();
        }
    }
}
