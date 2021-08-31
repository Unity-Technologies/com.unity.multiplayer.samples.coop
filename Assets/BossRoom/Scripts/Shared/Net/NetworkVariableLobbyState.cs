using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

namespace BossRoom
{
    [Serializable]
    public class NetworkVariableLobbyState : NetworkVariableBase, IEnumerable, IEnumerator
    {
        /// <summary>
        /// Delegate type for lobby changed event
        /// </summary>
        /// <param name="lobby">The latest lobby version</param>
        public delegate void OnLobbyChangedDelegate(ArraySegment<CharSelectData.LobbyPlayerState> lobby);

        /// <summary>
        /// The callback to be invoked when the lobby changes
        /// </summary>
        public event OnLobbyChangedDelegate OnLobbyChanged;

        /// <summary>
        /// Creates a NetworkVariable of LobbyStates with a specific size.
        /// </summary>
        /// <param name="lobbySize">The size of the lobby</param>
        public NetworkVariableLobbyState(int lobbySize)
        {
            m_PlayerStates = new CharSelectData.LobbyPlayerState[lobbySize];
            m_HasChangedValue = new bool[lobbySize];
            for (int i = 0; i < lobbySize; ++i)
            {
                m_HasChangedValue[i] = true;
            }
        }

        [SerializeField]
        private int m_PlayerCount = 0;

        private readonly bool[] m_HasChangedValue;

        private CharSelectData.LobbyPlayerState[] m_PlayerStates;

        private int m_Position = -1;

        /// <summary>
        /// Get the number of player in the lobby
        /// </summary>
        public int PlayerCount => m_PlayerCount;

        /// <summary>
        /// Get a specific LobbyPlasyerState base on its ID in the array
        /// </summary>
        /// <param name="i">The id where to find the player</param>
        public CharSelectData.LobbyPlayerState this[int i]
        {
            get => Get(i);
            set => Set(i, value);
        }

        private CharSelectData.LobbyPlayerState Get(int i)
        {
            SetDirty(true);
            m_HasChangedValue[i] = true;
            return m_PlayerStates[i];
        }

        private void Set(int i, CharSelectData.LobbyPlayerState state)
        {
            SetDirty(true);
            m_HasChangedValue[i] = true;
            m_PlayerStates[i] = state;
            OnLobbyChanged?.Invoke(new ArraySegment<CharSelectData.LobbyPlayerState>(m_PlayerStates, 0, m_PlayerCount));
        }

        /// <summary>
        /// Add a LobbyPlayerState to the lobby
        /// </summary>
        /// <param name="state">The state to be added</param>
        public void Add(CharSelectData.LobbyPlayerState state)
        {
            Interlocked.Increment(ref m_PlayerCount);
            m_PlayerStates[m_PlayerCount - 1] = state;
            m_HasChangedValue[m_PlayerCount - 1] = true;
            SetDirty(true);
            OnLobbyChanged?.Invoke(new ArraySegment<CharSelectData.LobbyPlayerState>(m_PlayerStates, 0, m_PlayerCount));
        }

        /// <summary>
        /// Remove the PlayerLobbyState at the specific index
        /// </summary>
        /// <param name="index">THe specific index to remove</param>
        /// <exception cref="IndexOutOfRangeException">Fired in case you are out of lobby bounds</exception>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= m_PlayerCount)
            {
                throw new IndexOutOfRangeException();
            }

            //In any case decrement the player count
            Interlocked.Decrement(ref m_PlayerCount);

            //If i is the latest element we don't care about its value
            if (index != m_PlayerCount)
            {
                //Resize the array
                Array.Copy(m_PlayerStates, index + 1, m_PlayerStates, index, m_PlayerCount - index);
            }

            SetDirty(true);
            for (int i = index; index < m_PlayerCount; ++i)
            {
                m_HasChangedValue[i] = true;
            }

            OnLobbyChanged?.Invoke(new ArraySegment<CharSelectData.LobbyPlayerState>(m_PlayerStates, 0, m_PlayerCount));
        }

        /// <summary>
        /// Write the modifications in the stream
        /// </summary>
        /// <param name="stream">The targeted stream</param>
        public override void WriteDelta(Stream stream)
        {
            using var writer = PooledNetworkWriter.Get(stream);
            writer.WriteInt32Packed(m_PlayerCount);

            for (int i = 0; i < m_PlayerCount; ++i)
            {
                writer.WriteBool(m_HasChangedValue[i]);
                if (m_HasChangedValue[i])
                {
                    m_PlayerStates[i].NetworkSerialize(writer.Serializer);
                }
            }
        }

        /// <summary>
        /// Write the entire state in the stream
        /// </summary>
        /// <param name="stream">The targeted stream</param>
        public override void WriteField(Stream stream)
        {
            using var writer = PooledNetworkWriter.Get(stream);
            writer.WriteInt32Packed(m_PlayerCount);
            for (int i = 0; i < m_PlayerCount; ++i)
            {
                m_PlayerStates[i].NetworkSerialize(writer.Serializer);
            }
        }

        /// <summary>
        /// Read the entire state from the stream
        /// </summary>
        /// <param name="stream">The targeted stream</param>
        public override void ReadField(Stream stream)
        {
            using var reader = PooledNetworkReader.Get(stream);
            m_PlayerCount = reader.ReadInt32Packed();
            for (int i = 0; i < m_PlayerCount; ++i)
            {
                m_PlayerStates[i].NetworkSerialize(reader.Serializer);
            }

            OnLobbyChanged?.Invoke(new ArraySegment<CharSelectData.LobbyPlayerState>(m_PlayerStates, 0, m_PlayerCount));
        }

        /// <summary>
        /// Read the modifications on the state from the stream
        /// </summary>
        /// <param name="stream">The targeted stream</param>
        /// <param name="keepDirtyDelta">A flag to say if we keep the dirty state</param>
        public override void ReadDelta(Stream stream, bool keepDirtyDelta)
        {
            using var reader = PooledNetworkReader.Get(stream);
            m_PlayerCount = reader.ReadInt32Packed();

            for (int i = 0; i < m_PlayerCount; ++i)
            {
                if (reader.ReadBool())
                {
                    m_PlayerStates[i].NetworkSerialize(reader.Serializer);
                }
            }

            if (keepDirtyDelta)
            {
                SetDirty(true);
            }

            OnLobbyChanged?.Invoke(new ArraySegment<CharSelectData.LobbyPlayerState>(m_PlayerStates, 0, m_PlayerCount));
        }

        /// <summary>
        /// Retrieve an enumerator from the array
        /// </summary>
        /// <returns>An enumerator on the array</returns>
        public IEnumerator GetEnumerator()
        {
            Reset();
            return this;
        }

        /// <summary>
        /// Move to the next position
        /// </summary>
        /// <returns>False if out of bounds, true otherwise</returns>
        public bool MoveNext()
        {
            m_Position++;
            return (m_Position < m_PlayerCount);
        }

        /// <summary>
        /// Reset the enumerator to the first position
        /// </summary>
        public void Reset()
        {
            m_Position = -1;
        }

        /// <summary>
        /// Get the Current value pointed by the Iterator
        /// </summary>
        public object Current => m_PlayerStates[m_Position];
    }
}
