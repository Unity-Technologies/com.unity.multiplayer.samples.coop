using System;
using UnityEngine;

namespace BossRoom
{
    public abstract class BaseActionInput : MonoBehaviour
    {
        protected NetworkCharacterState m_PlayerOwner;
        protected ActionType m_ActionType;
        protected Action<ActionRequestData> m_SendInput;
        Action m_OnFinished;

        public void Initiate(NetworkCharacterState playerOwner, ActionType actionType, Action<ActionRequestData> onSendInput, Action onFinished)
        {
            m_PlayerOwner = playerOwner;
            m_ActionType = actionType;
            m_SendInput = onSendInput;
            m_OnFinished = onFinished;
        }

        public void OnDestroy()
        {
            m_OnFinished();
        }

        public virtual void OnReleaseKey() {}
    }
}
