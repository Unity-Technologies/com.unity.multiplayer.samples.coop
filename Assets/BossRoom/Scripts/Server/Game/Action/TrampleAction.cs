using MLAPI;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// This represents a "charge-across-the-screen" attack. The character deals damage to every enemy hit.
    /// </summary>
    /// <remarks>
    /// It's called "Trample" instead of "Charge" because we already use the word "charge"
    /// to describe "charging up" an attack.
    /// </remarks>
    public class TrampleAction : Action
    {
        /// <summary>
        /// This is an internal indicator of which stage of the Action we're in.
        /// </summary>
        private enum ActionStage
        {
            Windup,     // performing animations prior to actually moving
            Charging,   // running across the screen and hitting characters
            Complete,   // ending action
        }

        /// <summary>
        /// Our ActionStage, as of last Update
        /// </summary>
        private ActionStage m_PreviousStage;

        /// <summary>
        /// Cached reference to a component in Parent
        /// </summary>
        private ServerCharacterMovement m_Movement;

        /// <summary>
        /// Keeps track of which Colliders we've already hit, so that our attack doesn't hit the same character twice.
        /// </summary>
        private HashSet<Collider> m_CollidedAlready = new HashSet<Collider>();

        /// <summary>
        /// When we begin our charge-attack, anyone within this range is treated as having already been touching us.
        /// </summary>
        private const float k_PhysicalTouchDistance = 1;

        public TrampleAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data) { }

        public override bool Start()
        {
            m_PreviousStage = ActionStage.Windup;
            m_Movement = m_Parent.GetComponent<ServerCharacterMovement>();

            if (m_Data.TargetIds != null && m_Data.TargetIds.Length > 0)
            {
                NetworkedObject initialTarget = MLAPI.Spawning.SpawnManager.SpawnedObjects[m_Data.TargetIds[0]];
                if (initialTarget)
                {
                    // snap to face our target! This is the direction we'll attack in
                    m_Parent.transform.LookAt(initialTarget.transform.position);
                }
            }

            m_Parent.NetState.ServerBroadcastAction(ref Data);
            return true;
        }

        private ActionStage GetCurrentStage()
        {
            float timeSoFar = Time.time - TimeStarted;
            if (timeSoFar < Description.ExecTimeSeconds)
            {
                return ActionStage.Windup;
            }
            if (timeSoFar < Description.ExecTimeSeconds + Description.DurationSeconds)
            {
                return ActionStage.Charging;
            }
            return ActionStage.Complete;
        }

        public override bool Update()
        {
            ActionStage newState = GetCurrentStage();
            if (newState != m_PreviousStage && newState == ActionStage.Charging)
            {
                // we've just started to charge across the screen! Anyone currently touching us gets hit
                SimulateCollisionWithNearbyFoes();
                m_Movement.StartForwardCharge(Description.MoveSpeed, Description.DurationSeconds - Description.ExecTimeSeconds);
            }

            m_PreviousStage = newState;

            return newState != ActionStage.Complete;
        }

        private void CollideWithVictim(ServerCharacter victim)
        {
            // if we collide with allies, we don't want to hurt them (but we do knock them back, see below)
            if (m_Parent.IsNpc != victim.IsNpc) 
            {
                // We deal a certain amount of damage to our "initial" target and a different amount to all other victims.
                int damage;
                if (m_Data.TargetIds != null && m_Data.TargetIds.Length > 0 && m_Data.TargetIds[0] == victim.NetworkId)
                {
                    damage = Description.Amount;
                }
                else
                {
                    damage = Description.SplashDamage;
                }
                victim.NetState.ServerBroadcastHitReaction();
                victim.ReceiveHP(this.m_Parent, -damage);
            }

            var victimMovement = victim.GetComponent<ServerCharacterMovement>();
            victimMovement.StartKnockback(m_Parent.transform.position, Description.KnockbackSpeed, Description.KnockbackDuration);
        }

        public override void OnCollisionEnter(Collision collision)
        {
            if (GetCurrentStage() == ActionStage.Windup)
                return; // it's too early to deal damage to 'em, we'll detect them when we start charging

            if (m_CollidedAlready.Contains(collision.collider))
                return; // already hit them!

            m_CollidedAlready.Add(collision.collider);

            var victim = collision.collider.gameObject.GetComponent<ServerCharacter>();
            if (victim)
            {
                CollideWithVictim(victim);
            }
        }

        private void SimulateCollisionWithNearbyFoes()
        {
            // We don't get OnCollisionEnter() calls for things that are already collided with us!
            // So when we start charging across the screen, we check to see what's already touching us
            // (or close enough) and treat that like a collision.
            RaycastHit[] results;
            int numResults = ActionUtils.DetectNearbyEntities(true, true, m_Parent.GetComponent<Collider>(), k_PhysicalTouchDistance, out results);
            for (int i = 0; i < numResults; i++)
            {
                m_CollidedAlready.Add(results[i].collider);
                var serverChar = results[i].collider.GetComponent<ServerCharacter>();
                if (serverChar)
                {
                    CollideWithVictim(serverChar);
                }
            }
        }

    }
}
