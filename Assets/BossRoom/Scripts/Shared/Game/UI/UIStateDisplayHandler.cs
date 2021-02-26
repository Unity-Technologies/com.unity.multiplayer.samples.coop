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
    public class UIStateDisplayHandler : MonoBehaviour
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
        IntVariable m_BaseHP;

        [Tooltip("UI object(s) will appear positioned at this transforms position.")]
        [SerializeField]
        Transform m_TransformToTrack;

        Camera m_Camera;

        Transform m_CanvasTransform;

        // as soon as any HP goes to 0, we wait this long before removing health bar UI object
        const float k_DurationSeconds = 2f;

        [Tooltip("Screen space vertical offset for positioning.")]
        [SerializeField]
        float m_VerticalScreenOffset;

        Vector3 m_VerticalOffset;

        void OnEnable()
        {
            if (!NetworkingManager.Singleton.IsClient)
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
            Assert.IsTrue(m_Camera != null && m_CanvasTransform != null);

            m_VerticalOffset = new Vector3(0f, m_VerticalScreenOffset, 0f);

            if (m_DisplayName)
            {
                DisplayUIName();
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
                var networkCharacterTypeState = GetComponent<NetworkCharacterTypeState>();
                if (networkCharacterTypeState)
                {
                    var characterType = networkCharacterTypeState.CharacterType;
                    var characterClass = GameDataSource.Instance.CharacterDataByType[characterType.Value];
                    if (characterClass)
                    {
                        m_BaseHP = characterClass.BaseHP;
                        DisplayUIHealth();
                    }
                }
            }

            m_NetworkHealthState.HitPointsReplenished += DisplayUIHealth;
            m_NetworkHealthState.HitPointsDepleted += RemoveUIHealth;
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
            m_UIStateActive = false;
        }

        void LateUpdate()
        {
            if (m_UIStateActive)
            {
                m_UIStateRectTransform.position = m_Camera.WorldToScreenPoint(m_TransformToTrack.position) +
                    m_VerticalOffset;
            }
        }

        void OnDestroy()
        {
            if (m_UIState)
            {
                Destroy(m_UIState.gameObject);
            }
        }
    }
}
