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
            foreach (var action in m_PlayingActions)
            {
                action.OnAnimEvent(id);
            }
        }

        public void PlayAction(ref ActionRequestData data)
        {
            ActionDescription actionDesc;
            //Do Trivial Actions (actions that just require playing a single animation, and don't require any state tracking).
            switch (data.ActionTypeEnum)
            {
                case ActionType.GeneralRevive:
                    actionDesc = GameDataSource.Instance.ActionDataByType[data.ActionTypeEnum];
                    Parent.OurAnimator.SetTrigger(actionDesc.Anim);
                    return;
                case ActionType.Emote1:
                case ActionType.Emote2:
                case ActionType.Emote3:
                case ActionType.Emote4:
                    actionDesc = GameDataSource.Instance.ActionDataByType[data.ActionTypeEnum];
                    Parent.OurAnimator.SetTrigger(actionDesc.Anim);
                    return;
            }

            ActionFX action = ActionFX.MakeActionFX(ref data, Parent);
            action.TimeStarted = Time.time;
            if (action.Start())
            {
                m_PlayingActions.Add(action);
            }
        }
    }
}


