using System;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// NetworkVariableString represents a String on the network.
    /// A string being a managed type we cannot wrap it in a NetworkVariable<>.
    /// </summary>
    [Serializable]
    public class NetworkVariableString : NetworkVariableBase, IComparable<string>, IEquatable<string>
    {
        /// <summary>
        /// Delegate type for value changed event
        /// </summary>
        /// <param name="oldValue">The value before the change</param>
        /// <param name="newValue">The value after the change</param>
        public delegate void OnValueChangedDelegate(string oldValue, string newValue);

        /// <summary>
        /// The callback to be invoked when the value get changed
        /// </summary>
        public event OnValueChangedDelegate OnValueChanged;

        [SerializeField]
        private string m_InternalValue;

        /// <summary>
        /// The value of the string
        /// </summary>
        public virtual string Value
        {
            get => m_InternalValue;
            set => Set(value);
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

        /// <summary>
        /// Compare the underlying string with the other one
        /// </summary>
        /// <param name="other">The string to be compared with</param>
        /// <returns></returns>
        public int CompareTo(string other)
        {
            return String.Compare(m_InternalValue, other, StringComparison.Ordinal);
        }

        /// <summary>
        /// Check if a string is equal to the inner string
        /// </summary>
        /// <param name="other">The string to be compared with</param>
        /// <returns>True if the string are the same, false otherwise</returns>
        public bool Equals(string other)
        {
            return m_InternalValue.Equals(other);
        }

        /// <summary>
        /// Write the modifications in the stream
        /// </summary>
        /// <param name="stream">The targeted stream</param>
        public override void WriteDelta(Stream stream)
        {
            using var writer = PooledNetworkWriter.Get(stream);
            writer.WriteStringPacked(m_InternalValue);
        }

        /// <summary>
        /// Write the entire state in the stream
        /// </summary>
        /// <param name="stream">The targeted stream</param>
        public override void WriteField(Stream stream)
        {
            WriteDelta(stream);
        }

        /// <summary>
        /// Read the entire state from the stream
        /// </summary>
        /// <param name="stream">The targeted stream</param>
        public override void ReadField(Stream stream)
        {
            ReadDelta(stream, false);
        }

        /// <summary>
        /// Read the modifications from the stream
        /// </summary>
        /// <param name="stream">The targeted stream</param>
        /// <param name="keepDirtyDelta">A flag to say if we keep the dirty state</param>
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
