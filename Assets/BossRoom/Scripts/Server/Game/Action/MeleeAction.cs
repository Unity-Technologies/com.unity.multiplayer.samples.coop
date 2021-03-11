using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// Action that represents a swing of a melee weapon. It is not explicitly targeted, but rather detects the foe that was hit with a physics check.
    /// </summary>
    /// <remarks>
    /// Q: Why do we DetectFoe twice, once in Start, once when we actually connect?
    /// A: The weapon swing doesn't happen instantaneously. We want to broadcast the action to other clients as fast as possible to minimize latency,
    ///    but this poses a conundrum. At the moment the swing starts, you don't know for sure if you've hit anybody yet. There are a few possible resolutions to this:
    ///      1. Do the DetectFoe operation once--in Start.
    ///         Pros: Simple! Only one physics cast per swing--saves on perf.
    ///         Cons: Is unfair. You can step out of the swing of an attack, but no matter how far you go, you'll still be hit. The reverse is also true--you can
    ///               "step into an attack", and it won't affect you. This will feel terrible to the attacker.
    ///      2. Do the DetectFoe operation once--in Update. Send a separate RPC to the targeted entity telling it to play its hit react.
    ///         Pros: Always shows the correct behavior. The entity that gets hit plays its hit react (if any).
    ///         Cons: You need another RPC. Adds code complexity and bandwidth. You also don't have enough information when you start visualizing the swing on
    ///               the client to do any intelligent animation handshaking. If your server->client latency is even a little uneven, your "attack" animation
    ///               won't line up correctly with the hit react, making combat look floaty and disjointed.
    ///      3. Do the DetectFoe operation twice, once in Start and once in Update.
    ///         Pros: Is fair--you do the hit-detect at the moment of the swing striking home. And will generally play the hit react on the right target.
    ///         Cons: Requires more complicated visualization logic. The initial broadcast foe can only ever be treated as a "hint". The graphics logic
    ///               needs to do its own range checking to pick the best candidate to play the hit react on.
    ///
    /// As so often happens in networked games (and games in general), there's no perfect solution--just sets of tradeoffs. For our example, we're showing option "3".
    /// </remarks>
    public class MeleeAction : Action
    {
        private bool m_ExecutionFired;
        private ulong m_ProvisionalTarget;

        //cache Physics Cast hits, to minimize allocs.
        public MeleeAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
        {
        }

        public override bool Start()
        {
            ulong target = (Data.TargetIds != null && Data.TargetIds.Length > 0) ? Data.TargetIds[0] : m_Parent.NetState.TargetId.Value;
            IDamageable foe = DetectFoe(target);
            if (foe != null)
            {
                m_ProvisionalTarget = foe.NetworkObjectId;
                Data.TargetIds = new ulong[] { foe.NetworkObjectId };
            }

            m_Parent.NetState.RecvDoActionClientRPC(Data);
            return true;
        }

        public override bool Update()
        {
            if (!m_ExecutionFired && (Time.time - TimeStarted) >= Description.ExecTimeSeconds)
            {
                m_ExecutionFired = true;
                var foe = DetectFoe(m_ProvisionalTarget);
                if (foe != null)
                {
                    foe.ReceiveHP(this.m_Parent, -Description.Amount);
                }
            }

            return true;
        }


        /// <summary>
        /// Returns the ServerCharacter of the foe we hit, or null if none found.
        /// </summary>
        /// <returns></returns>
        private IDamageable DetectFoe(ulong foeHint = 0)
        {
            //this simple detect just does a boxcast out from our position in the direction we're facing, out to the range of the attack.

            RaycastHit[] results;
            int numResults = ActionUtils.DetectMeleeFoe(Description.IsFriendly ^ m_Parent.IsNpc, m_Parent.GetComponent<Collider>(), Description, out results);

            if (numResults == 0) { return null; }

            //everything that passes the mask should have a ServerCharacter component.
            IDamageable foundFoe = results[0].collider.GetComponent<IDamageable>();

            //we always prefer the hinted foe. If he's still in range, he should take the damage, because he's who the client visualization
            //system will play the hit-react on (in case there's any ambiguity).
            for (int i = 0; i < numResults; i++)
            {
                var serverChar = results[i].collider.GetComponent<IDamageable>();
                if (serverChar!=null && serverChar.NetworkObjectId == foeHint)
                {
                    foundFoe = serverChar;
                    break;
                }
            }

            return foundFoe;
        }
    }
}
