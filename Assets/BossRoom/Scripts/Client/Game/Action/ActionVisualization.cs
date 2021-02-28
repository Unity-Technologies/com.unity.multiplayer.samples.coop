using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// This is a companion class to ClientCharacterVisualization that is specifically responsible for visualizing Actions. Action visualizations have lifetimes
    /// and ongoing state, making this class closely analogous in spirit to the BossRoom.Server.ActionPlayer class.
    /// </summary>
    public class ActionVisualization
    {
        private List<ActionFX> m_PlayingActions;

        public ClientCharacterVisualization Parent { get; private set; }

        public ActionVisualization(ClientCharacterVisualization parent)
        {
            Parent = parent;
            m_PlayingActions = new List<ActionFX>();
        }

        public void Update()
        {
            //do a reverse-walk so we can safely remove inside the loop.
            for (int i = m_PlayingActions.Count - 1; i >= 0; --i)
            {
                var action = m_PlayingActions[i];
                bool keepGoing = action.Update();
                bool expirable = action.Description.DurationSeconds > 0f; //non-positive value is a sentinel indicating the duration is indefinite.
                bool timeExpired = expirable && (Time.time - action.TimeStarted) >= action.Description.DurationSeconds;
                if (!keepGoing || timeExpired)
                {
                    action.End();
                    m_PlayingActions.RemoveAt(i);
                }
            }
        }

        public void OnAnimEvent(string id)
        {
            foreach (var actionFX in m_PlayingActions)
            {
                actionFX.OnAnimEvent(id);
            }
        }

        public void OnStoppedChargingUp()
        {
            foreach (var actionFX in m_PlayingActions)
            {
                actionFX.OnStoppedChargingUp();
            }
        }

        public void PlayAction(ref ActionRequestData data)
        {
            ActionDescription actionDesc = GameDataSource.Instance.ActionDataByType[data.ActionTypeEnum];

            //Do Trivial Actions (actions that just require playing a single animation, and don't require any state tracking).
            switch (actionDesc.Logic)
            {
                case ActionLogic.LaunchProjectile:
                case ActionLogic.Revive:
                case ActionLogic.Emote:
                    Parent.OurAnimator.SetTrigger(actionDesc.Anim);
                    return;
            }

            var actionFX = ActionFX.MakeActionFX(ref data, Parent);
            actionFX.TimeStarted = Time.time;
            if (actionFX.Start())
            {
                m_PlayingActions.Add(actionFX);
            }
        }

        public void CancelAllActions()
        {
            foreach (var actionFx in m_PlayingActions)
            {
                actionFx.Cancel();
            }
            m_PlayingActions.Clear();
        }

        public void CancelAllActionsOfType(ActionType actionType)
        {
            for (int i = m_PlayingActions.Count-1; i >=0; --i)
            {
                if (m_PlayingActions[i].Description.ActionTypeEnum == actionType)
                {
                    m_PlayingActions[i].Cancel();
                    m_PlayingActions.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Cancels all playing ActionFX.
        /// </summary>
        public void CancelAll()
        {
            foreach( var action in m_PlayingActions )
            {
                action.Cancel();
            }
            m_PlayingActions.Clear();
        }
    }
}


