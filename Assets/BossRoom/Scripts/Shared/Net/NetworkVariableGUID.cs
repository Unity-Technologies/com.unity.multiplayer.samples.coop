using System;
using System.IO;
using Unity.Netcode;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// NetworkVariableGUID represents a GUID on the Network.
    /// We cannot use NetworkVariable<GUID> or NetworkVariable<byte[]> because
    /// those two types are managed and thus cannot be used in a standard generic NetworkVariable.
    /// </summary>
    [Serializable]
    public class NetworkVariableGUID : NetworkVariableBase
    {
        /// <summary>
        /// Delegate type for value changed event
        /// </summary>
        public delegate void OnValueChangedDelegate(Guid oldGuid, Guid newGuid);

        /// <summary>
        /// The callback to be invoked when the value changes
        /// </summary>
        public event OnValueChangedDelegate OnValueChanged;

        [SerializeField]
        private Guid m_InternalValue;

        /// <summary>
        /// The Guid value of this NetVariable
        /// </summary>
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

        /// <summary>
        /// Write the modifications in the stream
        /// </summary>
        /// <param name="stream">The targeted stream</param>
        public override void WriteDelta(Stream stream)
        {
            WriteField(stream);
        }

        /// <summary>
        /// Write the whole state in the stream
        /// </summary>
        /// <param name="stream">The targeted stream</param>
        public override void WriteField(Stream stream)
        {
            using var writer = PooledNetworkWriter.Get(stream);
            writer.WriteArrayPacked(m_InternalValue.ToByteArray());
        }

        /// <summary>
        /// Read the whole state in the stream
        /// </summary>
        /// <param name="stream">The targeted stream</param>
        public override void ReadField(Stream stream)
        {
            using var reader = PooledNetworkReader.Get(stream);
            var previousGuid = m_InternalValue;
            m_InternalValue = new Guid(reader.ReadByteArray());
            OnValueChanged?.Invoke(previousGuid, m_InternalValue);
        }

        /// <summary>
        /// Read the modification in the stream
        /// </summary>
        /// <param name="stream">The targeted stream</param>
        /// <param name="keepDirtyDelta">A flag to say if we keep the dirty state</param>
        public override void ReadDelta(Stream stream, bool keepDirtyDelta)
        {
            ReadField(stream);

            if (keepDirtyDelta)
            {
                SetDirty(true);
            }
        }
    }
}
