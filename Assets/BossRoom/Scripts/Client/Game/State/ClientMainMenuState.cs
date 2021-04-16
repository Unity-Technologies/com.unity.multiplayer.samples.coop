using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BossRoom;

namespace BossRoom.Client
{
    /// <summary>
    /// Game Logic that runs when sitting at the MainMenu. This is likely to be "nothing", as no game has been started. But it is
    /// nonetheless important to have a game state, as the GameStateBehaviour system requires that all scenes have states. 
    /// </summary>
    public class ClientMainMenuState : GameStateBehaviour
    {
        /// <summary>
        /// Used in concert with SetTransitionReason to let code that triggers transition to the MainMenu scene set context
        /// for why the transition occurred. This is then used to display an appropriate message to the user in the UI.
        /// </summary>
        public enum TransitionReason
        {
            Undefined,      //no reason has been set. 
            UserRequested,  //user explicitly requested a disconnect. 
            Disconnect,     //client unexpectedly lost connection with host. 
        }

        private static TransitionReason s_TransitionReason;

        /// <summary>
        /// Set this to have the MainMenu display a MessageBox to the user on re-entering the MainMenu. Useful for displaying error states. 
        /// </summary>
        public static void SetTransitionReason( TransitionReason reason )
        {
            s_TransitionReason = reason;
        }

        /// <summary>
        /// Has a TransitionReason already be set? (The TransitionReason provides context for why someone transition back to the MainMenu, and is a one-use item
        /// that is unset as soon as it is read).
        /// </summary>
        public static bool HasTransitionReason => s_TransitionReason != TransitionReason.Undefined;

        /// <summary>
        /// MainMenuUI should invoke this on start, and display a message to the user if not Undefined
        /// </summary>
        /// <returns>TransitionReason set by whoever triggered the scene transition to main menu, or undefined if none. </returns>
        public TransitionReason ReadAndUnsetTransitionReason()
        {
            var reason = s_TransitionReason;
            s_TransitionReason = TransitionReason.Undefined;
            return reason;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            s_TransitionReason = TransitionReason.Undefined;
        }

        public override GameState ActiveState { get { return GameState.MainMenu;  } }

        public override void NetworkStart()
        {
            //note: this code won't ever run, because there is no network connection at the main menu screen.
            //fortunately we know you are a client, because all players are clients when sitting at the main menu screen. 
        }
    }

}
