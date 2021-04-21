using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Client
{
    /// <summary>
    /// enum that records additional context for why a user was disconnected. The primary use case for this
    /// is to allow the MainMenu to display an appropriate message after a disconnect event.
    /// </summary>
    public enum DisconnectReasonType
    {
        Undefined,      //no reason has been set. 
        UserRequested,  //user explicitly requested a disconnect. 
        Disconnect,     //client unexpectedly lost connection with host. 
    }

    /// <summary>
    /// This class provides some additional context for the connection managed by the ClientGameNetPortal. If a disconnect occurrs, or is expected to occur, client
    /// code can set the reason why here. Then subsequent code can interrogate this class to get the disconnect reason, and display appropriate information to
    /// the user, even after a scene transition has occurred. The state is set back to Undefined if a new connection is begun.
    /// </summary>
    public class DisconnectReason
    {
        /// <summary>
        /// When a disconnect is detected (or expected), set this to provide some context for why it occurred.
        /// </summary>
        public void SetDisconnectReason( DisconnectReasonType reason)
        {
            //using an explicit setter here rather than the auto-property, to make the code locations where disconnect information is set more obvious.
            Reason = reason;
        }

        /// <summary>
        /// The reason why a disconnect occurred, or Undefined if not set.
        /// </summary>
        public DisconnectReasonType Reason { get; private set; } = DisconnectReasonType.Undefined;

        /// <summary>
        /// Clear the DisconnectReason, returning it to Undefined.
        /// </summary>
        public void Clear()
        {
            Reason = DisconnectReasonType.Undefined;
        }

        /// <summary>
        /// Has a TransitionReason already be set? (The TransitionReason provides context for why someone transition back to the MainMenu, and is a one-use item
        /// that is unset as soon as it is read).
        /// </summary>
        public bool HasTransitionReason => Reason != DisconnectReasonType.Undefined;
    }
}
