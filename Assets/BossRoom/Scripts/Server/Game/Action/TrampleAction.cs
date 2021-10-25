using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
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

        /// <summary>
        /// Set to true in the special-case scenario where we are stunned by one of the characters we tried to trample
        /// </summary>
        private bool m_WasStunned;

        public TrampleAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data) { }

        public override bool Start()
        {
            m_PreviousStage = ActionStage.Windup;
            m_Movement = m_Parent.Movement;

            if (m_Data.TargetIds != null && m_Data.TargetIds.Length > 0)
            {
                NetworkObject initialTarget = NetworkManager.Singleton.SpawnManager.SpawnedObjects[m_Data.TargetIds[0]];
                if (initialTarget)
                {
                    Vector3 lookAtPosition;
                    if (PhysicsWrapper.TryGetPhysicsWrapper(initialTarget.NetworkObjectId, out var physicsWrapper))
                    {
                        lookAtPosition = physicsWrapper.Transform.position;
                    }
                    else
                    {
                        lookAtPosition = initialTarget.transform.position;
                    }

                    // snap to face our target! This is the direction we'll attack in
                    m_Parent.physicsWrapper.Transform.LookAt(lookAtPosition);
                }
            }

            // reset our "stop" trigger (in case the previous run of the trample action was aborted due to e.g. being stunned)
            if (!string.IsNullOrEmpty(Description.Anim2))
            { 
                m_Parent.serverAnimationHandler.NetworkAnimator.ResetTrigger(Description.Anim2);
            }
            // start the animation sequence!
            if (!string.IsNullOrEmpty(Description.Anim))
            {
                m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim);
            }

            m_Parent.NetState.RecvDoActionClientRPC(Data);
            return true;
        }

        private ActionStage GetCurrentStage()
        {
            float timeSoFar = Time.time - TimeStarted;
            if (timeSoFar < Description.ExecTimeSeconds)
            {
                return ActionStage.Windup;
            }
            if (timeSoFar < Description.DurationSeconds)
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
            return newState != ActionStage.Complete && !m_WasStunned;
        }

        /// <summary>
        /// We've crashed into a victim! This function determines what happens to them... and to us!
        /// It's possible for us to be stunned by our victim if they have a special power that allows that.
        /// This function checks for that special case; if we become stunned, the victim is entirely unharmed,
        /// and further collisions with other victims will also have no effect.
        /// </summary>
        /// <param name="victim">The character we've collided with</param>
        private void CollideWithVictim(ServerCharacter victim)
        {
            if (victim == m_Parent)
            {
                // can't collide with ourselves!
                return;
            }

            if (m_WasStunned)
            {
                // someone already stunned us, so no further damage can happen
                return;
            }

            // if we collide with allies, we don't want to hurt them (but we do knock them back, see below)
            if (m_Parent.IsNpc != victim.IsNpc)
            {
                // first see if this victim has the special ability to stun us!
                float chanceToStun = victim.GetBuffedValue(BuffableValue.ChanceToStunTramplers);
                if (chanceToStun > 0 && Random.Range(0,1) < chanceToStun)
                {
                    // we're stunned! No collision behavior for the victim. Stun ourselves and abort.
                    StunSelf();
                    return;
                }

                // We deal a certain amount of damage to our "initial" target and a different amount to all other victims.
                int damage;
                if (m_Data.TargetIds != null && m_Data.TargetIds.Length > 0 && m_Data.TargetIds[0] == victim.NetworkObjectId)
                {
                    damage = Description.Amount;
                }
                else
                {
                    damage = Description.SplashDamage;
                }
                victim.ReceiveHP(m_Parent, -damage);
            }

            var victimMovement = victim.Movement;
            victimMovement.StartKnockback(m_Parent.physicsWrapper.Transform.position, Description.KnockbackSpeed, Description.KnockbackDuration);
        }

        // called by owning class when parent's Collider collides with stuff
        public override void OnCollisionEnter(Collision collision)
        {
            // we only detect other possible victims when we start charging
            if (GetCurrentStage() != ActionStage.Charging)
                return;

            Collide(collision.collider);
        }

        // here we handle colliding with anything (whether a victim or not)
        private void Collide(Collider collider)
        {
            if (m_CollidedAlready.Contains(collider))
                return; // already hit them!

            m_CollidedAlready.Add(collider);

            var victim = collider.gameObject.GetComponentInParent<ServerCharacter>();
            if (victim)
            {
                CollideWithVictim(victim);
            }
            else if (!m_WasStunned)
            {
                // they aren't a living, breathing victim, but they might still be destructible...
                var damageable = collider.gameObject.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.ReceiveHP(this.m_Parent, -Description.SplashDamage);

                    // lastly, a special case: if the trampler runs into certain breakables, they are stunned!
                    if ((damageable.GetSpecialDamageFlags() & IDamageable.SpecialDamageFlags.StunOnTrample) == IDamageable.SpecialDamageFlags.StunOnTrample)
                    {
                        StunSelf();
                    }
                }
            }
        }

        private void SimulateCollisionWithNearbyFoes()
        {
            // We don't get OnCollisionEnter() calls for things that are already collided with us!
            // So when we start charging across the screen, we check to see what's already touching us
            // (or close enough) and treat that like a collision.
            RaycastHit[] results;
            int numResults = ActionUtils.DetectNearbyEntities(true, true, m_Parent.physicsWrapper.DamageCollider, k_PhysicalTouchDistance, out results);
            for (int i = 0; i < numResults; i++)
            {
                Collide(results[i].collider);
            }
        }

        private void StunSelf()
        {
            if (!m_WasStunned)
            {
                m_Movement.CancelMove();
                m_Parent.NetState.RecvCancelAllActionsClientRpc();
            }
            m_WasStunned = true;
        }

        public override bool ChainIntoNewAction(ref ActionRequestData newAction)
        {
            if (m_WasStunned)
            {
                newAction = new ActionRequestData()
                {
                    ActionTypeEnum = ActionType.Stun,
                    ShouldQueue = false,
                };
                return true;
            }
            return false;
        }

        public override void Cancel()
        {
            if (!string.IsNullOrEmpty(Description.Anim2))
            {
                m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim2);
            }
        }
    }
}
