using UnityEngine;

namespace BossRoom.Client
{
    /// <summary>
    /// Generic movement object that updates transforms based on the state of an INetMovement source.
    /// This is part of a temporary movement system that will be replaced once MLAPI can drive movement
    /// internally.
    /// </summary>
    public class ClientGenericMovement : MLAPI.NetworkBehaviour
    {
        private INetMovement m_MovementSource;
        private Rigidbody m_Rigidbody;
        private bool m_Initialized;


        // Start is called before the first frame update
        void Start()
        {
            m_MovementSource = GetComponent<INetMovement>();
            if (!IsClient)
            {
                m_MovementSource.InitNetworkPositionAndRotationY(transform.position, transform.rotation.eulerAngles.y);
            }

            m_Rigidbody = GetComponent<Rigidbody>(); //this may be null.
        }

        public override void NetworkStart()
        {
            if (IsServer)
            {
                //this component is not needed on the host (or dedicated server), because ServerCharacterMovement will directly
                //update the character's position.
                this.enabled = false;
            }
            m_Initialized = true;
        }

        // Update is called once per frame
        void Update()
        {
            if (!m_Initialized) { return; }

            transform.position = m_MovementSource.NetworkPosition.Value;
            transform.rotation = Quaternion.Euler(0, m_MovementSource.NetworkRotationY.Value, 0);

            if (m_Rigidbody != null)
            {
                m_Rigidbody.position = transform.position;
                m_Rigidbody.rotation = transform.rotation;
            }
        }
    }
}

