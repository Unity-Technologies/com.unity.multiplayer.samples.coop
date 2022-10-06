using System;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.Configuration
{
    /// <summary>
    /// Describes how a specific character visualization should be animated.
    /// </summary>
    [CreateAssetMenu]
    public class VisualizationConfiguration : ScriptableObject
    {
        [Header("Animation Triggers")]
        [Tooltip("Trigger for when a player character is resurrected")]
        [SerializeField] string m_AliveStateTrigger = "StandUp";
        [Tooltip("Trigger for when a player-character using this visualization becomes incapacitated")]
        [SerializeField] string m_FaintedStateTrigger = "FallDown";
        [Tooltip("Trigger for when a monster using this visualization becomes dead")]
        [SerializeField] string m_DeadStateTrigger = "Dead";
        [Tooltip("Trigger for when we expect to start moving very soon (to play a short animation in anticipation of moving soon)")]
        [SerializeField] string m_AnticipateMoveTrigger = "AnticipateMove";
        [Tooltip("Trigger for when a new character joins the game and we are already a dead monster")]
        [SerializeField] string m_EntryDeathTrigger = "EntryDeath";
        [Tooltip("Trigger for when a new character joins the game and we are already an incapacitated player")]
        [SerializeField] string m_EntryFaintedTrigger = "EntryFainted";

        [Header("Other Animation Variables")]
        [Tooltip("Variable that drives the character's movement animations")]
        [SerializeField] string m_SpeedVariable = "Speed";
        [Tooltip("Tag that should be on the \"do nothing\" default nodes of each animator layer")]
        [SerializeField] string m_BaseNodeTag = "BaseNode";

        [Header("Animation Speeds")]
        [Tooltip("The animator Speed value when character is dead")]
        public float SpeedDead = 0;
        [Tooltip("The animator Speed value when character is standing idle")]
        public float SpeedIdle = 0;
        [Tooltip("The animator Speed value when character is moving normally")]
        public float SpeedNormal = 1;
        [Tooltip("The animator Speed value when character is being pushed or knocked back")]
        public float SpeedUncontrolled = 0; // no leg movement; character appears to be sliding helplessly
        [Tooltip("The animator Speed value when character is magically slowed")]
        public float SpeedSlowed = 2; // hyper leg movement (character appears to be working very hard to move very little)
        [Tooltip("The animator Speed value when character is magically hasted")]
        public float SpeedHasted = 1.5f;
        [Tooltip("The animator Speed value when character is moving at a slower walking pace")]
        public float SpeedWalking = 0.5f;

        [Header("Associated Resources")]
        [Tooltip("Prefab for the Target Reticule used by this Character")]
        public GameObject TargetReticule;

        [Tooltip("Material to use when displaying a friendly target reticule (e.g. green color)")]
        public Material ReticuleFriendlyMat;

        [Tooltip("Material to use when displaying a hostile target reticule (e.g. red color)")]
        public Material ReticuleHostileMat;


        // These are maintained by our OnValidate(). Code refers to these hashed values, not the string versions!
        [SerializeField] [HideInInspector] public int AliveStateTriggerID;
        [SerializeField] [HideInInspector] public int FaintedStateTriggerID;
        [SerializeField] [HideInInspector] public int DeadStateTriggerID;
        [SerializeField] [HideInInspector] public int AnticipateMoveTriggerID;
        [SerializeField] [HideInInspector] public int EntryDeathTriggerID;
        [SerializeField] [HideInInspector] public int EntryFaintedTriggerID;
        [SerializeField] [HideInInspector] public int SpeedVariableID;
        [SerializeField] [HideInInspector] public int BaseNodeTagID;

        void OnValidate()
        {
            AliveStateTriggerID = Animator.StringToHash(m_AliveStateTrigger);
            FaintedStateTriggerID = Animator.StringToHash(m_FaintedStateTrigger);
            DeadStateTriggerID = Animator.StringToHash(m_DeadStateTrigger);
            AnticipateMoveTriggerID = Animator.StringToHash(m_AnticipateMoveTrigger);
            EntryDeathTriggerID = Animator.StringToHash(m_EntryDeathTrigger);
            EntryFaintedTriggerID = Animator.StringToHash(m_EntryFaintedTrigger);

            SpeedVariableID = Animator.StringToHash(m_SpeedVariable);
            BaseNodeTagID = Animator.StringToHash(m_BaseNodeTag);
        }
    }
}
