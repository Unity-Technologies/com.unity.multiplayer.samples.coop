using System.Collections;
using NUnit.Framework;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Multiplayer.Samples.BossRoom.Tests.Runtime
{
    public class ConnectionManagementTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        DIScope[] m_ClientScopes;
        DIScope m_ServerScope;

        protected override void OnServerAndClientsCreated()
        {
            m_ClientScopes = new DIScope[NumberOfClients];
            for (int i = 0; i < NumberOfClients; i++)
            {
                m_ClientScopes[i] = new DIScope();
                m_ClientScopes[i].BindInstanceAsSingle(m_ClientNetworkManagers[i]);
                m_ClientScopes[i].FinalizeScopeConstruction();
            }

            m_ServerScope = new DIScope();
            m_ServerScope.BindInstanceAsSingle(m_ServerNetworkManager);
            m_ServerScope.FinalizeScopeConstruction();

            base.OnServerAndClientsCreated();
        }

        protected override IEnumerator OnTearDown()
        {
            for (int i = 0; i < NumberOfClients; i++)
            {
                m_ClientScopes[i].Dispose();
            }
            m_ServerScope.Dispose();
            return base.OnTearDown();
        }

    }
}
