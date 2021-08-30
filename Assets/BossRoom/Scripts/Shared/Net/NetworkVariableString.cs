using System;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;

namespace BossRoom
{
    public class NetworkVariableString : NetworkVariableBase, IComparable<string>, IEquatable<string>
    {
        [SerializeField]
        private string m_InternalValue;

        public delegate void OnValueChangedDelegate(string oldValue, string newValue);

        public event OnValueChangedDelegate OnValueChanged;

        /// <summary>
        /// The value of the NetworkVariable container
        /// </summary>
        public virtual string Value
        {
            get => m_InternalValue;
            set
            {
                Set(value);
            }
        }

        private void Set(string value)
        {
            if (EqualityComparer<string>.Default.Equals(m_InternalValue, value))
            {
                return;
            }

            SetDirty(true);
            string previousValue = m_InternalValue;
            m_InternalValue = value;
            OnValueChanged?.Invoke(previousValue, m_InternalValue);
        }

        public int CompareTo(string other)
        {
            return String.Compare(m_InternalValue, other, StringComparison.Ordinal);
        }

        public bool Equals(string other)
        {
            return m_InternalValue.Equals(other);
        }

        public override void WriteDelta(Stream stream)
        {
            using var writer = PooledNetworkWriter.Get(stream);
            writer.WriteStringPacked(m_InternalValue);
        }

        public override void WriteField(Stream stream)
        {
            WriteDelta(stream);
        }

        public override void ReadField(Stream stream)
        {
            ReadDelta(stream, false);
        }

        public override void ReadDelta(Stream stream, bool keepDirtyDelta)
        {
            using var reader = PooledNetworkReader.Get(stream);
            string previousValue = m_InternalValue;
            m_InternalValue = reader.ReadStringPacked();

            if (keepDirtyDelta)
            {
                SetDirty(true);
            }

            OnValueChanged?.Invoke(previousValue, m_InternalValue);
        }
    }
}
