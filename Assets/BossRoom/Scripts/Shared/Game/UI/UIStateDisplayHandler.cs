using System.Collections;
using MLAPI;
using UnityEngine;
using UnityEngine.Assertions;

namespace BossRoom
{
    /// <summary>
    /// Class designed to only run on a client. Add this to a prefab with a NetworkHealthState attached and it will
    /// create, & position a UIHealth prefab instance in UI-space.
    /// </summary>
    [RequireComponent(typeof(NetworkHealthState))]
    public class UIStateDisplayHandler : MonoBehaviour
    {
        [SerializeField]
        UIHealth m_UIHealthPrefab;

        // spawned in world (only one instance of this)
        UIHealth m_UIHealth;

        RectTransform m_UIHealthRectTransform;

        bool m_UIHealthActive;

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

            Assert.IsTrue(m_Camera != null && m_CanvasTransform != null);

            if (m_BaseHP != null)
            {
                AddUIHealth();
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
                        AddUIHealth();
                    }
                }
            }

            m_NetworkHealthState.HitPointsReplenished += AddUIHealth;
            m_NetworkHealthState.HitPointsDepleted += RemoveUIHealth;
        }

        void AddUIHealth()
        {
            if (m_UIHealth != null)
            {
                return;
            }

            m_UIHealth = Instantiate(m_UIHealthPrefab, m_CanvasTransform);
            m_UIHealthRectTransform = m_UIHealth.GetComponent<RectTransform>();
            m_UIHealth.Initialize(m_NetworkHealthState.HitPoints, m_BaseHP.Value);
            m_UIHealthActive = true;
        }

        void RemoveUIHealth()
        {
            StartCoroutine(WaitToHideHealthBar());
        }

        IEnumerator WaitToHideHealthBar()
        {
            yield return new WaitForSeconds(k_DurationSeconds);

            Destroy(m_UIHealth.gameObject);
            m_UIHealthActive = false;
        }

        void LateUpdate()
        {
            if (m_UIHealthActive)
            {
                m_UIHealthRectTransform.position = m_Camera.WorldToScreenPoint(m_TransformToTrack.position);
            }
        }
    }
}
