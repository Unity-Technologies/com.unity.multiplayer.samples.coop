using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



/// <summary>
/// Provides common functionality for server/client movmement, and is the MonoBehaviour that hosts 
/// movement logic on server and client. Figures out whether it is running as a server or client
/// on startup, and creates the appropriate internal class. 
/// </summary>
public class MovementHost : MLAPI.NetworkedBehaviour
{
    private SharedMovement m_moveLogic;

    public static ulong ServerRecvdMoves = 0;

    public struct MovementMessage
    {
        public Vector3 m_pos;
        public float m_face; //in radians, in the XZ plane. 0 degrees faces along the +Z axis. 
        public float m_timeorspeed; //client->server, this is a speed (in m/s). In all other scenarios, it is a time.

        public MovementMessage(MLAPI.Serialization.Pooled.PooledBitReader reader)
        {
            m_pos = reader.ReadVector3();
            m_face = reader.ReadSingle();
            m_timeorspeed = reader.ReadSingle();
        }
        public void Write(MLAPI.Serialization.Pooled.PooledBitWriter writer)
        {
            writer.WriteVector3(m_pos);
            writer.WriteSingle(m_face);
            writer.WriteSingle(m_timeorspeed);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        m_moveLogic = ProgramState.Instance.IsHost ? (SharedMovement)(new ServerMovement(this)) : //server representation. 
                      this.IsOwner ?                 (SharedMovement)(new ClientMovement(this)) : //client-owner, with forward predicted movement. 
                                                     (SharedMovement)(new ReplayMovement(this));  //client-replay (shadow), that just replays movement from server.
        m_moveLogic.Start();
    }

    // Update is called once per frame
    void Update()
    {
        m_moveLogic.Update();
    }


    [MLAPI.Messaging.ServerRPC]
    private void ProcessMovement(ulong clientId, System.IO.Stream stream)
    {
        using (MLAPI.Serialization.Pooled.PooledBitReader reader = MLAPI.Serialization.Pooled.PooledBitReader.Get(stream))
        {
            MovementMessage msg = new MovementMessage(reader);
            m_moveLogic.ProcessMovement(ref msg);
        }
    }

    [MLAPI.Messaging.ClientRPC]
    private void ProcessMovementClient(ulong clientId, System.IO.Stream stream)
    {
        using (MLAPI.Serialization.Pooled.PooledBitReader reader = MLAPI.Serialization.Pooled.PooledBitReader.Get(stream))
        {
            MovementMessage msg = new MovementMessage(reader);
            m_moveLogic.ProcessMovement(ref msg);
        }
    }

    public override void NetworkStart()
    {
        System.Console.WriteLine("Player spawned");
    }
}
