using System;
using MLAPI;
using UnityEngine;

namespace BossRoom
{
    public class ClientSpawnerVisualization : NetworkedBehaviour
    {
        [SerializeField]
        NetworkSpawnerState m_NetworkSpawnerState;
        
        // TODO: Integrate visuals (GOMPS-123)
        [SerializeField]
        Animator m_Animator;
        
        public override void NetworkStart()
        {
            if (!IsClient)
            {
                enabled = false;
                return;
            }
            
            m_NetworkSpawnerState.Broken.OnValueChanged += BrokenStateChanged;
            m_NetworkSpawnerState.HitPoints.OnValueChanged += HitPointsChanged;
        }
        
        void BrokenStateChanged(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                if (previousValue == false)
                {
                    // spawner is newly broken
                }
            }
            else
            {
                if (previousValue)
                {
                    // spawner is newly revived
                }
            }
        }

        void HitPointsChanged(int previousValue, int newValue)
        {
            if (previousValue > newValue && newValue > 0)
            {
                // received a hit
            }
        }
    }
}