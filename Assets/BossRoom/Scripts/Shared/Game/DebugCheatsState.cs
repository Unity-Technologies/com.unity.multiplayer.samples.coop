using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Unity.Multiplayer.Samples.BossRoom
{
    public class DebugCheatsState : MonoBehaviour
    {

        public Action<ulong> SpawnEnemy;
        public Action<ulong> SpawnBoss;
        public Action<ulong> GoToPostGame;
        public Action<ulong> ToggleGodMode;

        Dictionary<ulong, bool> m_GodModeState;
    }
}
#endif
