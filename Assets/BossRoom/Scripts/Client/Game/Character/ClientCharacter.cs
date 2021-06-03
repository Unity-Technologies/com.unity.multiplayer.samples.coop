using System;
using UnityEngine;

namespace BossRoom.Client
{
    [RequireComponent(typeof(NetworkCharacterState))]
    public class ClientCharacter : MLAPI.NetworkBehaviour
    {
        /// <summary>
        /// The Visualization GameObject isn't in the same transform hierarchy as the object, but it registers itself here
        /// so that the visual GameObject can be found from a NetworkObjectId.
        /// </summary>
        public Visual.ClientCharacterVisualization ChildVizObject { get; private set; }

        [Tooltip("PCs instantiate their graphics representation GameObject from this component.")]
        [SerializeField]
        CharacterContainer m_CharacterContainer;

        public event Action CharacterGraphicsSpawned;

        public override void NetworkStart()
        {
            if (!IsClient)
            {
                enabled = false;
            }

            // spawn graphics GameObject for PCs
            if (m_CharacterContainer && !m_CharacterContainer.CharacterClass.IsNpc)
            {
                var graphicsGameObject = Instantiate(m_CharacterContainer.CharacterGraphics, transform);
                graphicsGameObject.name = "CharacterGraphics" + OwnerClientId;

                ChildVizObject = graphicsGameObject.GetComponent<Visual.ClientCharacterVisualization>();

                CharacterGraphicsSpawned?.Invoke();
            }
        }
    }
}
