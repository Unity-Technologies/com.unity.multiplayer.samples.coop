namespace LobbyRelaySample
{
    /// <summary>
    /// Just for displaying the anonymous Relay IP.
    /// </summary>
    public class ServerAddress
    {
        string m_IP;
        int m_Port;

        public string IP => m_IP;
        public int Port => m_Port;

        public ServerAddress(string ip, int port)
        {
            m_IP = ip;
            m_Port = port;
        }

        public override string ToString()
        {
            return $"{m_IP}:{m_Port}";
        }
    }
}
