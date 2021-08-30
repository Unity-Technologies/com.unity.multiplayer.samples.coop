using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;

namespace BossRoom
{
    public class NetworkVariableLobbyState : NetworkVariableBase, IEnumerable<CharSelectData.LobbyPlayerState>
    {
        private int m_PlayerCount = 0;

        private CharSelectData.LobbyPlayerState[] m_PlayerStates;

        //NetworkList<LobbyPlayerState>
        public int PlayerCount => m_PlayerCount;

        public NetworkVariableLobbyState(int lobbySize)
        {
            m_PlayerStates = new CharSelectData.LobbyPlayerState[lobbySize];
        }

        public CharSelectData.LobbyPlayerState this[int i]
        {
            get => m_PlayerStates[i];
            set => throw new Exception("Just to test");
        }

        public delegate void OnLobbyChangedDelegate(ArraySegment<CharSelectData.LobbyPlayerState> lobby);

        public event OnLobbyChangedDelegate OnLobbyChanged;

        public void Add(CharSelectData.LobbyPlayerState state)
        {

        }

        public void RemoveAt(int i)
        {
            
        }

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
        public IEnumerator<CharSelectData.LobbyPlayerState> GetEnumerator()
        {
            throw new NotImplementedException();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
