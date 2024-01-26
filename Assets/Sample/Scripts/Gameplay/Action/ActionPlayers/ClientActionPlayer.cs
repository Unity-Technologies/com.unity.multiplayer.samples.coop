using System.Collections.Generic;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;

namespace Unity.BossRoom.Gameplay.Actions
{
    /// <summary>
    /// This is a companion class to ClientCharacter that is specifically responsible for visualizing Actions. Action visualizations have lifetimes
    /// and ongoing state, making this class closely analogous in spirit to the Unity.Multiplayer.Samples.BossRoom.Actions.ServerActionPlayer class.
    /// </summary>
    public sealed class ClientActionPlayer
    {
        private List<Action> m_PlayingActions = new List<Action>();

        /// <summary>
        /// Don't let anticipated actionFXs persist longer than this. This is a safeguard against scenarios
        /// where we never get a confirmed action for an action we anticipated.
        /// </summary>
        private const float k_AnticipationTimeoutSeconds = 1;

        public ClientCharacter ClientCharacter { get; private set; }

        public ClientActionPlayer(ClientCharacter clientCharacter)
        {
            ClientCharacter = clientCharacter;
        }

        public void OnUpdate()
        {
            //do a reverse-walk so we can safely remove inside the loop.
            for (int i = m_PlayingActions.Count - 1; i >= 0; --i)
            {
                var action = m_PlayingActions[i];
                bool keepGoing = action.AnticipatedClient || action.OnUpdateClient(ClientCharacter); // only call OnUpdate() on actions that are past anticipation
                bool expirable = action.Config.DurationSeconds > 0f; //non-positive value is a sentinel indicating the duration is indefinite.
                bool timeExpired = expirable && action.TimeRunning >= action.Config.DurationSeconds;
                bool timedOut = action.AnticipatedClient && action.TimeRunning >= k_AnticipationTimeoutSeconds;
                if (!keepGoing || timeExpired || timedOut)
                {
                    if (timedOut) { action.CancelClient(ClientCharacter); } //an anticipated action that timed out shouldn't get its End called. It is canceled instead.
                    else { action.EndClient(ClientCharacter); }

                    m_PlayingActions.RemoveAt(i);
                    ActionFactory.ReturnAction(action);
                }
            }
        }

        //helper wrapper for a FindIndex call on m_PlayingActions.
        private int FindAction(ActionID actionID, bool anticipatedOnly)
        {
            return m_PlayingActions.FindIndex(a => a.ActionID == actionID && (!anticipatedOnly || a.AnticipatedClient));
        }

        public void OnAnimEvent(string id)
        {
            foreach (var actionFX in m_PlayingActions)
            {
                actionFX.OnAnimEventClient(ClientCharacter, id);
            }
        }

        public void OnStoppedChargingUp(float finalChargeUpPercentage)
        {
            foreach (var actionFX in m_PlayingActions)
            {
                actionFX.OnStoppedChargingUpClient(ClientCharacter, finalChargeUpPercentage);
            }
        }

        /// <summary>
        /// Called on the client that owns the Character when the player triggers an action. This allows actions to immediately start playing feedback.
        /// </summary>
        /// <remarks>
        ///
        /// What is Action Anticipation and what problem does it solve? In short, it lets Actions run logic the moment the input event that triggers them
        /// is detected on the local client. The purpose of this is to help mask latency. Because this demo is server authoritative, the default behavior is
        /// to only see feedback for your input after a server-client roundtrip. Somewhere over 200ms of round-trip latency, this starts to feel oppressively sluggish.
        /// To combat this, you can play visual effects immediately. For example, MeleeActionFX plays both its weapon swing and applies a hit react to the target,
        /// without waiting to hear from the server. This can lead to discrepancies when the server doesn't think the target was hit, but on the net, will feel
        /// more responsive.
        ///
        /// An important concept of Action Anticipation is that it is opportunistic--it doesn't make any strong guarantees. You don't get an anticipated
        /// action animation if you are already animating in some way, as one example. Another complexity is that you don't know if the server will actually
        /// let you play all the actions that you've requested--some may get thrown away, e.g. because you have too many actions in your queue. What this means
        /// is that Anticipated Actions (actions that have been constructed but not started) won't match up perfectly with actual approved delivered actions from
        /// the server. For that reason, it must always be fine to receive PlayAction and not have an anticipated action already started (this is true for playback
        /// Characters belonging to the server and other characters anyway). It also means we need to handle the case where we created an Anticipated Action, but
        /// never got a confirmation--actions like that need to eventually get discarded.
        ///
        /// Another important aspect of Anticipated Actions is that they are an "opt-in" system. You must call base.Start in your Start implementation, but other than
        /// that, if you don't have a good way to implement an Anticipation for your action, you don't have to do anything. In this case, that action will play
        /// "normally" (with visual feedback starting when the server's action broadcast reaches the client). Every action type will have its own particular set of
        /// problems to solve to sell the anticipation effect. For example, in this demo, the mage base attack (FXProjectileTargetedActionFX) just plays the attack animation
        /// anticipatively, but it could be revised to create and drive the mage bolt effect as well--leaving only damage to arrive in true server time.
        ///
        /// How to implement your own Anticipation logic:
        ///   1. Isolate the visual feedback you want play anticipatively in a private helper method on your ActionFX, like "PlayAttackAnim".
        ///   2. Override ActionFX.AnticipateAction. Be sure to call base.AnticipateAction, as well as play your visual logic (like PlayAttackAnim).
        ///   3. In your Start method, be sure to call base.Start (note that this will reset the "Anticipated" field to false).
        ///   4. In Start, check if the action was Anticipated. If NOT, then play call your PlayAttackAnim method.
        ///
        /// </remarks>
        /// <param name="data">The Action that is being requested.</param>
        public void AnticipateAction(ref ActionRequestData data)
        {
            if (!ClientCharacter.IsAnimating() && Action.ShouldClientAnticipate(ClientCharacter, ref data))
            {
                var actionFX = ActionFactory.CreateActionFromData(ref data);
                actionFX.AnticipateActionClient(ClientCharacter);
                m_PlayingActions.Add(actionFX);
            }
        }

        public void PlayAction(ref ActionRequestData data)
        {
            var anticipatedActionIndex = FindAction(data.ActionID, true);

            var actionFX = anticipatedActionIndex >= 0 ? m_PlayingActions[anticipatedActionIndex] : ActionFactory.CreateActionFromData(ref data);
            if (actionFX.OnStartClient(ClientCharacter))
            {
                if (anticipatedActionIndex < 0)
                {
                    m_PlayingActions.Add(actionFX);
                }
                //otherwise just let the action sit in it's existing slot
            }
            else if (anticipatedActionIndex >= 0)
            {
                var removedAction = m_PlayingActions[anticipatedActionIndex];
                m_PlayingActions.RemoveAt(anticipatedActionIndex);
                ActionFactory.ReturnAction(removedAction);
            }
        }

        /// <summary>
        /// Cancels all playing ActionFX.
        /// </summary>
        public void CancelAllActions()
        {
            foreach (var action in m_PlayingActions)
            {
                action.CancelClient(ClientCharacter);
                ActionFactory.ReturnAction(action);
            }
            m_PlayingActions.Clear();
        }

        public void CancelAllActionsWithSamePrototypeID(ActionID actionID)
        {
            for (int i = m_PlayingActions.Count - 1; i >= 0; --i)
            {
                if (m_PlayingActions[i].ActionID == actionID)
                {
                    var action = m_PlayingActions[i];
                    action.CancelClient(ClientCharacter);
                    m_PlayingActions.RemoveAt(i);
                    ActionFactory.ReturnAction(action);
                }
            }
        }
    }
}


