using System;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Server;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{
    /// <summary>
    /// This represents a "charge-across-the-screen" attack. The character deals damage to every enemy hit.
    /// </summary>
    /// <remarks>
    /// It's called "Trample" instead of "Charge" because we already use the word "charge"
    /// to describe "charging up" an attack.
    /// </remarks>
    [CreateAssetMenu()]
    public class TrampleAction : Action
    {

        public StunnedAction StunnedActionPrototype;

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
        /// Keeps track of which Colliders we've already hit, so that our attack doesn't hit the same character twice.
        /// </summary>
        private HashSet<Collider> m_CollidedAlready = new HashSet<Collider>();

        /// <summary>
        /// When we begin our charge-attack, anyone within this range is treated as having already been touching us.
        /// </summary>
        private const float k_PhysicalTouchDistance = 1;

        /// <summary>
        /// We spawn the "visual cue" graphics a moment after we begin our action.
        /// (A little extra delay helps ensure we have the correct orientation for the
        /// character, so the graphics are oriented in the right direction!)
        /// </summary>
        private const float k_GraphicsSpawnDelay = 0.3f;

        /// <summary>
        /// Prior to spawning graphics, this is null. Once we spawn the graphics, this is a list of everything we spawned.
        /// </summary>
        /// <remarks>
        /// Mobile performance note: constantly creating new GameObjects like this has bad performance on mobile and should
        /// be replaced with object-pooling (i.e. reusing the same art GameObjects repeatedly). But that's outside the scope of this demo.
        /// </remarks>
        private List<SpecialFXGraphic> m_SpawnedGraphics = null;

        /// <summary>
        /// Set to true in the special-case scenario where we are stunned by one of the characters we tried to trample
        /// </summary>
        private bool m_WasStunned;

        public override bool OnStart(ServerCharacter parent)
        {
            m_PreviousStage = ActionStage.Windup;

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
                    parent.physicsWrapper.Transform.LookAt(lookAtPosition);
                }
            }

            // reset our "stop" trigger (in case the previous run of the trample action was aborted due to e.g. being stunned)
            if (!string.IsNullOrEmpty(Config.Anim2))
            {
                parent.serverAnimationHandler.NetworkAnimator.ResetTrigger(Config.Anim2);
            }
            // start the animation sequence!
            if (!string.IsNullOrEmpty(Config.Anim))
            {
                parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim);
            }

            parent.NetState.RecvDoActionClientRPC(Data);
            return true;
        }

        private ActionStage GetCurrentStage()
        {
            float timeSoFar = Time.time - TimeStarted;
            if (timeSoFar < Config.ExecTimeSeconds)
            {
                return ActionStage.Windup;
            }
            if (timeSoFar < Config.DurationSeconds)
            {
                return ActionStage.Charging;
            }
            return ActionStage.Complete;
        }

        public override bool OnUpdate(ServerCharacter parent)
        {
            ActionStage newState = GetCurrentStage();
            if (newState != m_PreviousStage && newState == ActionStage.Charging)
            {
                // we've just started to charge across the screen! Anyone currently touching us gets hit
                SimulateCollisionWithNearbyFoes(parent);
                parent.Movement.StartForwardCharge(Config.MoveSpeed, Config.DurationSeconds - Config.ExecTimeSeconds);
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
        private void CollideWithVictim(ServerCharacter parent, ServerCharacter victim)
        {
            if (victim == parent)
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
            if (parent.IsNpc != victim.IsNpc)
            {
                // first see if this victim has the special ability to stun us!
                float chanceToStun = victim.GetBuffedValue(BuffableValue.ChanceToStunTramplers);
                if (chanceToStun > 0 && Random.Range(0, 1) < chanceToStun)
                {
                    // we're stunned! No collision behavior for the victim. Stun ourselves and abort.
                    StunSelf(parent);
                    return;
                }

                // We deal a certain amount of damage to our "initial" target and a different amount to all other victims.
                int damage;
                if (m_Data.TargetIds != null && m_Data.TargetIds.Length > 0 && m_Data.TargetIds[0] == victim.NetworkObjectId)
                {
                    damage = Config.Amount;
                }
                else
                {
                    damage = Config.SplashDamage;
                }

                if (victim.gameObject.TryGetComponent(out IDamageable damageable))
                {
                    damageable.ReceiveHP(parent, -damage);
                }
            }

            var victimMovement = victim.Movement;
            victimMovement.StartKnockback(parent.physicsWrapper.Transform.position, Config.KnockbackSpeed, Config.KnockbackDuration);
        }

        // called by owning class when parent's Collider collides with stuff
        public override void CollisionEntered(ServerCharacter parent, Collision collision)
        {
            // we only detect other possible victims when we start charging
            if (GetCurrentStage() != ActionStage.Charging)
                return;

            Collide(parent, collision.collider);
        }

        // here we handle colliding with anything (whether a victim or not)
        private void Collide(ServerCharacter parent, Collider collider)
        {
            if (m_CollidedAlready.Contains(collider))
                return; // already hit them!

            m_CollidedAlready.Add(collider);

            var victim = collider.gameObject.GetComponentInParent<ServerCharacter>();
            if (victim)
            {
                CollideWithVictim(parent, victim);
            }
            else if (!m_WasStunned)
            {
                // they aren't a living, breathing victim, but they might still be destructible...
                var damageable = collider.gameObject.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.ReceiveHP(parent, -Config.SplashDamage);

                    // lastly, a special case: if the trampler runs into certain breakables, they are stunned!
                    if ((damageable.GetSpecialDamageFlags() & IDamageable.SpecialDamageFlags.StunOnTrample) == IDamageable.SpecialDamageFlags.StunOnTrample)
                    {
                        StunSelf(parent);
                    }
                }
            }
        }

        private void SimulateCollisionWithNearbyFoes(ServerCharacter parent)
        {
            // We don't get OnCollisionEnter() calls for things that are already collided with us!
            // So when we start charging across the screen, we check to see what's already touching us
            // (or close enough) and treat that like a collision.
            RaycastHit[] results;
            int numResults = ActionUtils.DetectNearbyEntities(true, true, parent.physicsWrapper.DamageCollider, k_PhysicalTouchDistance, out results);
            for (int i = 0; i < numResults; i++)
            {
                Collide(parent, results[i].collider);
            }
        }

        private void StunSelf(ServerCharacter parent)
        {
            if (!m_WasStunned)
            {
                parent.Movement.CancelMove();
                parent.NetState.RecvCancelAllActionsClientRpc();
            }
            m_WasStunned = true;
        }

        public override bool ChainIntoNewAction(ref ActionRequestData newAction)
        {
            if (m_WasStunned)
            {
                newAction = ActionRequestData.Create(StunnedActionPrototype);
                newAction.ShouldQueue = false;
                return true;
            }
            return false;
        }

        public override void Cancel(ServerCharacter parent)
        {
            if (!string.IsNullOrEmpty(Config.Anim2))
            {
                parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim2);
            }
        }

        public override bool OnUpdateClient(ClientCharacterVisualization parent)
        {
            float age = Time.time - TimeStarted;
            if (age > k_GraphicsSpawnDelay && m_SpawnedGraphics == null)
            {
                m_SpawnedGraphics = InstantiateSpecialFXGraphics(parent.transform, false);
            }
            return true;
        }

        public override void CancelClient(ClientCharacterVisualization parent)
        {
            // we've been aborted -- destroy the "cue graphics"
            if (m_SpawnedGraphics != null)
            {
                foreach (var fx in m_SpawnedGraphics)
                {
                    if (fx)
                    {
                        fx.Shutdown();
                    }
                }
            }
            m_SpawnedGraphics = null;
        }
    }
}
