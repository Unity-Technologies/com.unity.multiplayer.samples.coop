using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Server
{
    [RequireComponent(typeof(NetworkCharacterState))]
    public class ServerCharacter : MLAPI.NetworkedBehaviour
    {
        public NetworkCharacterState NetState { get; private set; }

        [SerializeField]
        [Tooltip("If enabled, this character has an AIBrain and behaves as an enemy")]
        public bool IsNPC;

        [SerializeField]
        [Tooltip("If IsNPC, this is how far the npc can detect others (in meters)")]
        public float DetectRange = 10;

        private ActionPlayer m_actionPlayer;
        private AIBrain m_aiBrain;

        /// <summary>
        /// Temp place to store all the active characters (to avoid having to
        /// perform insanely-expensive GameObject.Find operations during Update)
        /// </summary>
        private static List<ServerCharacter> g_activeServerCharacters = new List<ServerCharacter>();

        private void OnEnable()
        {
            g_activeServerCharacters.Add(this);
        }

        private void OnDisable()
        {
            g_activeServerCharacters.Remove(this);
        }

        public static List<ServerCharacter> GetAllActiveServerCharacters()
        {
            return g_activeServerCharacters;
        }

        // Start is called before the first frame update
        void Start()
        {
            NetState = GetComponent<NetworkCharacterState>();
            m_actionPlayer = new ActionPlayer(this);
            if (IsNPC)
            {
                m_aiBrain = new AIBrain(this, m_actionPlayer);
            }
        }

        public override void NetworkStart()
        {
            if (!IsServer) { this.enabled = false; }
            else
            {
                this.NetState = GetComponent<NetworkCharacterState>();
                this.NetState.DoActionEventServer += this.OnActionPlayRequest;
            }
        }

        /// <summary>
        /// Play an action!
        /// </summary>
        /// <param name="data">Contains all data necessary to create the action</param>
        public void PlayAction(ref ActionRequestData data )
        {
            this.m_actionPlayer.PlayAction(ref data);
        }

        /// <summary>
        /// Clear all active Actions. 
        /// </summary>
        public void ClearActions()
        {
            this.m_actionPlayer.ClearActions();
        }

        private void OnActionPlayRequest( ActionRequestData data )
        {
            this.PlayAction(ref data);
        }

        // Update is called once per frame
        void Update()
        {
            m_actionPlayer.Update();
            if (m_aiBrain != null)
            {
                m_aiBrain.Update();
            }
        }
    }
}
