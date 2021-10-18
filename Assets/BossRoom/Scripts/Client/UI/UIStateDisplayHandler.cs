using System.Collections;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    /// <summary>
    /// Class designed to only run on a client. Add this to a world-space prefab to display health or name on UI.
    /// </summary>
    public class UIStateDisplayHandler : NetworkBehaviour
    {
        [SerializeField]
        bool m_DisplayHealth;

        [SerializeField]
        bool m_DisplayName;

        [SerializeField]
        UIStateDisplay m_UIStatePrefab;

        // spawned in world (only one instance of this)
        UIStateDisplay m_UIState;

        RectTransform m_UIStateRectTransform;

        bool m_UIStateActive;

        [SerializeField]
        NetworkNameState m_NetworkNameState;

        [SerializeField]
        NetworkHealthState m_NetworkHealthState;

        [SerializeField]
        ClientCharacter m_ClientCharacter;

        ClientAvatarGuidHandler m_ClientAvatarGuidHandler;
        private NetworkAvatarGuidState m_NetworkAvatarGuidState;

        [SerializeField]
        IntVariable m_BaseHP;

        [Tooltip("UI object(s) will appear positioned at this transforms position.")]
        [SerializeField]
        Transform m_TransformToTrack;

        Camera m_Camera;

        Transform m_CanvasTransform;

        // as soon as any HP goes to 0, we wait this long before removing health bar UI object
        const float k_DurationSeconds = 2f;

        [Tooltip("World space vertical offset for positioning.")]
        [SerializeField]
        float m_VerticalWorldOffset;

        [Tooltip("Screen space vertical offset for positioning.")]
        [SerializeField]
        float m_VerticalScreenOffset;

        Vector3 m_VerticalOffset;

        // used to compute world position based on target and offsets
        private Vector3 m_WorldPos;

        public override void OnNetworkSpawn()
        {
            if (!NetworkManager.Singleton.IsClient)
            {
                enabled = false;
                return;
            }

            m_Camera = Camera.main;
            var canvasGameObject = GameObject.FindWithTag("GameCanvas");
            if (canvasGameObject)
            {
                m_CanvasTransform = canvasGameObject.transform;
            }

            Assert.IsTrue(m_DisplayHealth || m_DisplayName, "Neither display fields are toggled on!");
            if (m_DisplayHealth)
            {
                Assert.IsNotNull(m_NetworkHealthState, "A NetworkHealthState component needs to be attached!");
            }
            Assert.IsTrue(m_Camera != null && m_CanvasTransform != null);

            m_VerticalOffset = new Vector3(0f, m_VerticalScreenOffset, 0f);

            // if PC, find our graphics transform and update health through callbacks, if displayed
            if (TryGetComponent(out m_ClientAvatarGuidHandler) && TryGetComponent(out m_NetworkAvatarGuidState))
            {
                m_BaseHP = m_NetworkAvatarGuidState.RegisteredAvatar.CharacterClass.BaseHP;

                if (m_ClientCharacter.ChildVizObject)
                {
                    TrackGraphicsTransform(m_ClientCharacter.ChildVizObject.gameObject);
                }
                else
                {
                    m_ClientAvatarGuidHandler.AvatarGraphicsSpawned += TrackGraphicsTransform;
                }

                if (m_DisplayHealth)
                {
                    m_NetworkHealthState.hitPointsReplenished += DisplayUIHealth;
                    m_NetworkHealthState.hitPointsDepleted += RemoveUIHealth;
                }
            }

            if (m_DisplayName)
            {
                DisplayUIName();
            }

            if (m_DisplayHealth)
            {
                DisplayUIHealth();
            }
        }

        void OnDisable()
        {
            if (!m_DisplayHealth)
            {
                return;
            }

            if (m_NetworkHealthState != null)
            {
                m_NetworkHealthState.hitPointsReplenished -= DisplayUIHealth;
                m_NetworkHealthState.hitPointsDepleted -= RemoveUIHealth;
            }

            if (m_ClientAvatarGuidHandler)
            {
                m_ClientAvatarGuidHandler.AvatarGraphicsSpawned -= TrackGraphicsTransform;
            }
        }

        void DisplayUIName()
        {
            if (m_NetworkNameState == null)
            {
                return;
            }

            if (m_UIState == null)
            {
                SpawnUIState();
            }

            m_UIState.DisplayName(m_NetworkNameState.Name);
            m_UIStateActive = true;
        }

        void DisplayUIHealth()
        {
            if (m_NetworkHealthState == null)
            {
                return;
            }

            if (m_UIState == null)
            {
                SpawnUIState();
            }

            m_UIState.DisplayHealth(m_NetworkHealthState.HitPoints, m_BaseHP.Value);
            m_UIStateActive = true;
        }

        void SpawnUIState()
        {
            m_UIState = Instantiate(m_UIStatePrefab, m_CanvasTransform);
            // make in world UI state draw under other UI elements
            m_UIState.transform.SetAsFirstSibling();
            m_UIStateRectTransform = m_UIState.GetComponent<RectTransform>();
        }

        void RemoveUIHealth()
        {
            StartCoroutine(WaitToHideHealthBar());
        }

        IEnumerator WaitToHideHealthBar()
        {
            yield return new WaitForSeconds(k_DurationSeconds);

            m_UIState.HideHealth();
        }

        void TrackGraphicsTransform(GameObject graphicsGameObject)
        {
            m_TransformToTrack = graphicsGameObject.transform;
        }

        void Update()
        {
            if (m_UIStateActive && m_TransformToTrack)
            {
                // set world position with world offset added
                m_WorldPos.Set(m_TransformToTrack.position.x,
                    m_TransformToTrack.position.y + m_VerticalWorldOffset, m_TransformToTrack.position.z );

                m_UIStateRectTransform.position = m_Camera.WorldToScreenPoint(m_WorldPos) +
                    m_VerticalOffset;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (m_UIState != null)
            {
                Destroy(m_UIState.gameObject);
            }
        }
    }
}
