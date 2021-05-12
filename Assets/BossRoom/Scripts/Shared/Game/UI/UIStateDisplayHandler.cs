using System;
using System.Collections;
using MLAPI;
using UnityEngine;
using UnityEngine.Assertions;

namespace BossRoom
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
        BossRoomPlayerCharacter m_BossRoomPlayerCharacter;

        NetworkNameState m_NetworkNameState;

        [SerializeField]
        NetworkHealthState m_NetworkHealthState;

        NetworkCharacterTypeState m_NetworkCharacterTypeState;

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

        // used to compute corld pos based on target and offsets
        Vector3 m_WorldPos;

        /// <remarks>
        /// One needs to wait until NetworkStart to access properties like OwnerId from another NetworkBehaviour or else
        /// they will contain default values (0).
        /// </remarks>
        public override void NetworkStart()
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

            if (m_DisplayName)
            {
                if (m_BossRoomPlayerCharacter)
                {
                    if (m_BossRoomPlayerCharacter.Data)
                    {
                        NetworkInitializeName();
                    }
                    else
                    {
                        m_BossRoomPlayerCharacter.DataSet += NetworkInitializeName;
                        enabled = false;
                    }
                }
            }

            if (!m_DisplayHealth)
            {
                return;
            }

            if (m_BaseHP != null)
            {
                DisplayUIHealth();
            }
            else
            {
                // the lines below are added in case a player wanted to display a health bar, since their max HP is
                // dependent on their respective character class
                if (m_BossRoomPlayerCharacter)
                {
                    if (m_BossRoomPlayerCharacter.Data)
                    {
                        NetworkInitializeHealth();
                    }
                    else
                    {
                        m_BossRoomPlayerCharacter.DataSet += NetworkInitializeHealth;
                        enabled = false;
                    }
                }
                else
                {
                    DisplayUIHealth();
                }

                if (m_NetworkHealthState != null)
                {
                    m_NetworkHealthState.HitPointsReplenished += DisplayUIHealth;
                    m_NetworkHealthState.HitPointsDepleted += RemoveUIHealth;
                }
            }
        }

        void NetworkInitializeName()
        {
            if (m_BossRoomPlayerCharacter.Data.TryGetNetworkBehaviour(out m_NetworkNameState))
            {
                m_NetworkNameState.AddListener(CharacterNameChanged);
            }

            Assert.IsNotNull(m_NetworkNameState, "A NetworkNameState component has not been set");

            DisplayUIName();

            enabled = true;
        }

        void NetworkInitializeHealth()
        {
            if (m_BossRoomPlayerCharacter.Data.TryGetNetworkBehaviour(out m_NetworkCharacterTypeState))
            {
                m_NetworkCharacterTypeState.AddListener(CharacterTypeChanged);

                // we initialize the health bar with our current character type as well
                CharacterTypeChanged(m_NetworkCharacterTypeState.NetworkCharacterType,
                    m_NetworkCharacterTypeState.NetworkCharacterType);
            }

            Assert.IsNotNull(m_NetworkCharacterTypeState, "A NetworkCharacterTypeState component has not been set!");

            DisplayUIName();

            enabled = true;
        }

        void OnDisable()
        {
            if (!m_DisplayHealth)
            {
                return;
            }

            if (m_NetworkNameState != null)
            {
                m_BossRoomPlayerCharacter.DataSet -= NetworkInitializeName;
                m_NetworkNameState.RemoveListener(CharacterNameChanged);
            }

            if (m_NetworkCharacterTypeState != null)
            {
                m_BossRoomPlayerCharacter.DataSet -= NetworkInitializeHealth;
                m_NetworkCharacterTypeState.RemoveListener(CharacterTypeChanged);
            }

            if (m_NetworkHealthState != null)
            {
                m_NetworkHealthState.HitPointsReplenished -= DisplayUIHealth;
                m_NetworkHealthState.HitPointsDepleted -= RemoveUIHealth;
            }
        }

        void CharacterTypeChanged(CharacterTypeEnum previousValue, CharacterTypeEnum newValue)
        {
            var characterClass = GameDataSource.Instance.CharacterDataByType[newValue];
            if (characterClass)
            {
                m_BaseHP = characterClass.BaseHP;

                if (m_NetworkHealthState != null && m_NetworkHealthState.NetworkHealth > 0)
                {
                    DisplayUIHealth();
                }
            }
        }

        void CharacterNameChanged(string previousValue, string newValue)
        {
            DisplayUIName();
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

            m_UIState.DisplayName(m_NetworkNameState);
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

            m_UIState.DisplayHealth(m_NetworkHealthState, m_BaseHP.Value);
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

        void LateUpdate()
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

        void OnDestroy()
        {
            if (m_BossRoomPlayerCharacter)
            {
                m_BossRoomPlayerCharacter.DataSet -= NetworkInitializeName;
                m_BossRoomPlayerCharacter.DataSet -= NetworkInitializeHealth;
            }

            if (m_UIState != null)
            {
                Destroy(m_UIState.gameObject);
            }
        }
    }
}
