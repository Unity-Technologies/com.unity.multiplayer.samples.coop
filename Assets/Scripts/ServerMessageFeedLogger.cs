using System;
using System.Collections;
using Unity.Multiplayer.Samples;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;
using UnityEngine;
// TODO lots of code duplication with UIMessageFeed?
public class ServerMessageFeedLogger : MonoBehaviour
{
    DisposableGroup m_Subscriptions;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    ISubscriber<CheatUsedMessage> m_cheatUsedMessageSubscriber;
#endif

    ISubscriber<DoorStateChangedEventMessage> m_doorStateChangedSubscriber;
    ISubscriber<ConnectionEventMessage> m_connectionEventSubscriber;
    ISubscriber<LifeStateChangedEventMessage> m_lifeStateChangedEventSubscriber;

    // TODO fix this
    [Inject]
    void InjectDependencies(
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ISubscriber<CheatUsedMessage> cheatUsedMessageSubscriber,
#endif
        ISubscriber<DoorStateChangedEventMessage> doorStateChangedSubscriber,
        ISubscriber<ConnectionEventMessage> connectionEventSubscriber,
        ISubscriber<LifeStateChangedEventMessage> lifeStateChangedEventSubscriber
    )
    {
        m_Subscriptions = new DisposableGroup();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        m_cheatUsedMessageSubscriber = cheatUsedMessageSubscriber;
#endif
        m_doorStateChangedSubscriber = doorStateChangedSubscriber;
        m_connectionEventSubscriber = connectionEventSubscriber;
        m_lifeStateChangedEventSubscriber = lifeStateChangedEventSubscriber;

        void Subscribe()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            m_Subscriptions.Add(cheatUsedMessageSubscriber.Subscribe(OnCheatUsedEvent));
#endif
            m_Subscriptions.Add(doorStateChangedSubscriber.Subscribe(OnDoorStateChangedEvent));
            m_Subscriptions.Add(connectionEventSubscriber.Subscribe(OnConnectionEvent));
            m_Subscriptions.Add(lifeStateChangedEventSubscriber.Subscribe(OnLifeStateChangedEvent));
        }

        // TODO fix me
        IEnumerator NetworkManagerExists()
        {
            while (NetworkManager.Singleton == null)
            {
                yield return null;
            }
            NetworkManager.Singleton.OnServerStarted += Subscribe; // no need to unsubscribe, this should be done every server start
        }
        StartCoroutine(NetworkManagerExists());
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void OnCheatUsedEvent(CheatUsedMessage eventMessage)
    {
        DedicatedServerUtilities.Log($"Cheat {eventMessage.CheatUsed} used by {eventMessage.CheaterName}");
    }
#endif

    void OnDoorStateChangedEvent(DoorStateChangedEventMessage eventMessage)
    {
        DedicatedServerUtilities.Log(eventMessage.IsDoorOpen ? "The Door has been opened!" : "The Door is closing.");
    }

    void OnConnectionEvent(ConnectionEventMessage eventMessage)
    {
        switch (eventMessage.ConnectStatus)
        {
            case ConnectStatus.Success:
                DedicatedServerUtilities.Log($"{eventMessage.PlayerName} has joined the game!");
                break;
            case ConnectStatus.ServerFull:
            case ConnectStatus.LoggedInAgain:
            case ConnectStatus.UserRequestedDisconnect:
            case ConnectStatus.GenericDisconnect:
            case ConnectStatus.IncompatibleBuildType:
            case ConnectStatus.HostEndedSession:
                DedicatedServerUtilities.Log($"{eventMessage.PlayerName} has left the game!");
                break;
        }
    }

    void OnLifeStateChangedEvent(LifeStateChangedEventMessage eventMessage)
    {
        switch (eventMessage.CharacterType)
        {
            case CharacterTypeEnum.Tank:
            case CharacterTypeEnum.Archer:
            case CharacterTypeEnum.Mage:
            case CharacterTypeEnum.Rogue:
            case CharacterTypeEnum.ImpBoss:
                switch (eventMessage.NewLifeState)
                {
                    case LifeState.Alive:
                        DedicatedServerUtilities.Log($"{eventMessage.CharacterName} has been reanimated!");
                        break;
                    case LifeState.Fainted:
                    case LifeState.Dead:
                        DedicatedServerUtilities.Log($"{eventMessage.CharacterName} has been defeated!");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                break;
        }
    }

    void OnDestroy()
    {
        m_Subscriptions?.Dispose();
    }
}
