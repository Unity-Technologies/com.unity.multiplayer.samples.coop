using System;
using System.Collections;
using MLAPI;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// Add this component to a GameObject to visually represent it's NetworkHealthState on UI.
    /// </summary>
    [RequireComponent(typeof(NetworkHealthState))]
    public class AddHealthToUI : NetworkedBehaviour
    {
        [SerializeField]
        NetworkHealthState m_NetworkHealthState;

        [SerializeField]
        IntVariable m_BaseHP;

        [Tooltip("Health bar will appear where this transform is. If null this transform is used.")]
        [SerializeField]
        Transform m_TransformToTrack;

        // as soon as any HP goes to 0, we wait this long before removing health bar UI object
        const float k_DurationSeconds = 2f;

        public override void NetworkStart()
        {
            if (!IsClient)
            {
                enabled = false;
                return;
            }

            if (m_BaseHP != null)
            {
                AddHealthBar();
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
                        AddHealthBar();
                    }
                }
            }

            m_NetworkHealthState.HitPointsReplenished += AddHealthBar;
            m_NetworkHealthState.HitPointsDepleted += RemoveHealthBar;
        }

        void AddHealthBar()
        {
            HealthBarManager.Instance.AddHealthBar(NetworkedObject.NetworkId, m_NetworkHealthState.HitPoints,
                m_BaseHP.Value, m_TransformToTrack == null ? transform : m_TransformToTrack);
        }

        void RemoveHealthBar()
        {
            StartCoroutine(WaitToHideHealthBar());
        }

        IEnumerator WaitToHideHealthBar()
        {
            yield return new WaitForSeconds(k_DurationSeconds);

            HealthBarManager.Instance.RemoveHealthBar(NetworkedObject.NetworkId);
        }
    }
}
