using System;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{
    public abstract class BaseActionInput : MonoBehaviour
    {
        protected NetworkCharacterState m_PlayerOwner;
        protected Vector3 m_Origin;
        protected ActionID m_ActionPrototypeID;
        protected Action<ActionRequestData> m_SendInput;
        System.Action m_OnFinished;

        public void Initiate(NetworkCharacterState playerOwner, Vector3 origin, ActionID actionPrototypeID, Action<ActionRequestData> onSendInput, System.Action onFinished)
        {
            m_PlayerOwner = playerOwner;
            m_Origin = origin;
            m_ActionPrototypeID = actionPrototypeID;
            m_SendInput = onSendInput;
            m_OnFinished = onFinished;
        }

        public void OnDestroy()
        {
            m_OnFinished();
        }

        public virtual void OnReleaseKey() { }
    }
}
