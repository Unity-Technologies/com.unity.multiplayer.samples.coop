using System;
using BossRoom;
using UnityEngine;
using UnityEngine.Events;

public abstract class BaseActionInput : MonoBehaviour
{
    protected NetworkCharacterState m_PlayerOwner;
    protected ActionType m_ActionType;
    UnityAction m_OnFinished;

    public void Initiate(NetworkCharacterState playerOwner, ActionType actionType, UnityAction onFinished)
    {
        m_PlayerOwner = playerOwner;
        m_ActionType = actionType;
        m_OnFinished = onFinished;
    }

    public void OnDestroy()
    {
        m_OnFinished();
    }
}
