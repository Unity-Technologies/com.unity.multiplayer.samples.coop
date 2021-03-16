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
        private List<ActionFX> m_PlayingActions = new List<ActionFX>();

        /// <summary>
        /// Don't let anticipated actionFXs persist longer than this. This is a safeguard against scenarios
        /// where we never get a confirmed action for an action we anticipated. 
        /// </summary>
        private const float k_AnticipationTimeoutSeconds = 1;

        public ClientCharacterVisualization Parent { get; private set; }

        public ActionVisualization(ClientCharacterVisualization parent)
        {
            Parent = parent;
        }

        public void Update()
        {
            //do a reverse-walk so we can safely remove inside the loop.
            for (int i = m_PlayingActions.Count - 1; i >= 0; --i)
            {
                var action = m_PlayingActions[i];
                bool keepGoing = action.Update();
                bool expirable = action.Description.DurationSeconds > 0f; //non-positive value is a sentinel indicating the duration is indefinite.
                bool timeExpired = expirable && action.TimeRunning >= action.Description.DurationSeconds;
                bool timedOut = action.Anticipated && action.TimeRunning >= k_AnticipationTimeoutSeconds;
                if (!keepGoing || timeExpired || timedOut)
                {
                    if (timedOut) { action.Cancel(); } //an anticipated action that timed out shouldn't get its End called. It is canceled instead. 
                    else { action.End(); }

                    m_PlayingActions.RemoveAt(i);
                }
            }
        }

        //helper wrapper for a FindIndex call on m_PlayingActions. 
        private int FindAction(ActionType action, bool anticipatedOnly )
        {
            return m_PlayingActions.FindIndex(a => a.Description.ActionTypeEnum == action && (!anticipatedOnly || a.Anticipated));
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

        /// <summary>
        /// Called on the client that owns the Character when the player triggers an action. This allows actions to immediately start playing feedback. 
        /// </summary>
        /// <remarks>
        /// An important concept of Action Anticipation is that it is opportunistic--it doesn't make any strong guarantees. You don't get an anticipated
        /// action animation if you are already animating in some way, as one example. Another complexity is that you don't know if the server will actually
        /// let you play all the actions that you've requested--some may get thrown away, e.g. because you have too many actions in your queue. What this means
        /// is that Anticipated Actions (actions that have been constructed but not started) won't match up perfectly with actual approved delivered actions from
        /// the server. For that reason, it must always be fine to receive PlayAction and not have an anticipated action already started (this is true for playback
        /// Characters belonging to the server and other characters anyway). It also means we need to handle the case where we created an Anticipated Action, but
        /// never got a confirmation--actions like that need to eventually get discarded.
        ///
        /// Current limitations:
        ///   * the only anticipation performed is to play an animation.
        ///   * Individual Actions might need to implement their own sophisticated Anticipation logic. For example, LaunchProjectile could actually create the projectile
        ///     and start it moving along its expected path, and then somehow link up its purely visual projectile with the real networked projectile that the server
        ///     eventually creates. 
        /// </remarks>
        /// <param name="data">The Action that is being requested.</param>

        public void AnticipateAction(ref ActionRequestData data)
        {
            if (!Parent.IsAnimating && ActionFX.ShouldAnticipate(this, ref data))
            {
                var actionFX = ActionFX.MakeActionFX(ref data, Parent);
                actionFX.TimeStarted = Time.time;
                actionFX.AnticipateAction();
                m_PlayingActions.Add(actionFX);
            }
        }

        public void PlayAction(ref ActionRequestData data)
        {
            var anticipatedActionIndex = FindAction(data.ActionTypeEnum, true);

            var actionFX = anticipatedActionIndex>=0 ? m_PlayingActions[anticipatedActionIndex] : ActionFX.MakeActionFX(ref data, Parent);
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
            for (int i = m_PlayingActions.Count - 1; i >= 0; --i)
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
            foreach (var action in m_PlayingActions)
            {
                action.Cancel();
            }
            m_PlayingActions.Clear();
        }
    }
}


