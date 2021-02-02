using System.IO;

namespace BossRoom
{
    /// <summary>
    /// Describes a set of actions to be performed, in order.
    /// (The server-side ActionPlayer will decide if they get to perform these actions, and when!)
    /// </summary>
    public class ActionSequence : MLAPI.Serialization.IBitWritable
    {
        public const int k_MaxActionsInSequence = 3;

        // Because ActionRequestData is a struct that we want to pass by-ref, we use an array instead of a List.
        // Fortunately we know the max-possible-size of the list: it's set by a constant
        private ActionRequestData[] m_Actions = new ActionRequestData[k_MaxActionsInSequence];
        private byte m_NumActions = 0;

        public void Add(ref ActionRequestData data)
        {
            UnityEngine.Debug.Assert(m_NumActions < k_MaxActionsInSequence);
            m_Actions[m_NumActions++] = data;
        }

        public int Count { get { return m_NumActions; } }

        public ref ActionRequestData Get(int idx)
        {
            return ref m_Actions[idx];
        }

        public void Read(Stream stream)
        {
            m_NumActions = (byte)stream.ReadByte();
            UnityEngine.Debug.Assert(m_NumActions < k_MaxActionsInSequence);
            for (int i = 0; i < m_NumActions; ++i)
            {
                m_Actions[i].Read(stream);
            }
        }

        public void Write(Stream stream)
        {
            stream.WriteByte(m_NumActions);
            for (int i = 0; i < m_NumActions; ++i)
            {
                m_Actions[i].Write(stream);
            }
        }
    }
}
