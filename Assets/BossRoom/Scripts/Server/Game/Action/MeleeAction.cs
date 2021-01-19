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
        private bool m_ExecFired;

        //cache Physics Cast hits, to minimize allocs. 
        private static RaycastHit[] s_Hits;
        private const int k_MaxDetects = 4;
        private ulong m_ProvisionalTarget;

        public MeleeAction(ServerCharacter parent, ref ActionRequestData data, int level) : base(parent, ref data, level)
        {
            if (s_Hits == null)
            {
                s_Hits = new RaycastHit[k_MaxDetects];
            }
        }

        public override bool Start()
        {
            ServerCharacter foe = DetectFoe();
            if (foe != null)
            {
                m_ProvisionalTarget = foe.NetworkId;
                Data.TargetIds = new ulong[] { foe.NetworkId };
            }

            m_Parent.NetState.ServerBroadcastAction(ref Data);
            return true;
        }

        public override bool Update()
        {
            if (!m_ExecFired && (Time.time - TimeStarted) >= Description.ExecTime_s)
            {
                m_ExecFired = true;
                var foe = DetectFoe(m_ProvisionalTarget);
                if (foe != null)
                {
                    foe.RecieveHP(this.m_Parent, -Description.Amount);
                }
            }

            return true;
        }



        /// <summary>
        /// Returns the ServerCharacter of the foe we hit, or null if none found. 
        /// </summary>
        /// <returns></returns>
        private ServerCharacter DetectFoe(ulong foeHint = 0)
        {
            //this simple detect just does a boxcast out from our position in the direction we're facing, out to the range of the attack. 

            var myBounds = this.m_Parent.GetComponent<Collider>().bounds;

            //NPCs (monsters) can hit PCs, and vice versa. No friendly fire allowed on either side. 
            int mask = LayerMask.GetMask(m_Parent.IsNPC ? "PCs" : "NPCs");

            int numResults = Physics.BoxCastNonAlloc(m_Parent.transform.position, myBounds.extents,
                m_Parent.transform.forward, s_Hits, Quaternion.identity, Description.Range, mask);

            if (numResults == 0) { return null; }

            //everything that passes the mask should have a ServerCharacter component. 
            ServerCharacter foundFoe = s_Hits[0].collider.GetComponent<ServerCharacter>();

            //we always prefer the hinted foe. If he's still in range, he should take the damage, because he's who the client visualization
            //system will play the hit-react on (in case there's any ambiguity). 
            for (int i = 0; i < numResults; i++)
            {
                var serverChar = s_Hits[i].collider.GetComponent<ServerCharacter>();
                if (serverChar.NetworkId == foeHint)
                {
                    foundFoe = serverChar;
                    break;
                }
            }

            return foundFoe;
        }
    }
}
