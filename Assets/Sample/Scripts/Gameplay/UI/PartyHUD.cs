using System;
using System.Collections.Generic;
using Unity.BossRoom.Gameplay.UserInput;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using TMPro;
using Unity.BossRoom.Utils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// Provides logic for the Party HUD with information on the player and allies
    /// Party HUD shows hero portrait and class info for all ally characters
    /// Party HUD also shows healthbars for each player allows clicks to select an ally
    /// </summary>
    public class PartyHUD : MonoBehaviour
    {
        [SerializeField]
        ClientPlayerAvatarRuntimeCollection m_PlayerAvatars;

        [SerializeField]
        private Image m_HeroPortrait;

        [SerializeField]
        private GameObject[] m_AllyPanel;

        [SerializeField]
        private TextMeshProUGUI[] m_PartyNames;

        [SerializeField]
        private Image[] m_PartyClassSymbols;

        [SerializeField]
        private Slider[] m_PartyHealthSliders;

        [SerializeField]
        private Image[] m_PartyHealthGodModeImages;

        // track a list of hero (slot 0) + allies
        private ulong[] m_PartyIds;

        // track Hero's target to show when it is the Hero or an ally
        private ulong m_CurrentTarget;

        ServerCharacter m_OwnedServerCharacter;

        ClientPlayerAvatar m_OwnedPlayerAvatar;

        private Dictionary<ulong, ServerCharacter> m_TrackedAllies = new Dictionary<ulong, ServerCharacter>();

        private ClientInputSender m_ClientSender;

        void Awake()
        {
            // Make sure arrays are initialized
            InitPartyArrays();

            m_PlayerAvatars.ItemAdded += PlayerAvatarAdded;
            m_PlayerAvatars.ItemRemoved += PlayerAvatarRemoved;
        }

        void PlayerAvatarAdded(ClientPlayerAvatar clientPlayerAvatar)
        {
            if (clientPlayerAvatar.IsOwner)
            {
                SetHeroData(clientPlayerAvatar);
            }
            else
            {
                SetAllyData(clientPlayerAvatar);
            }
        }

        void PlayerAvatarRemoved(ClientPlayerAvatar clientPlayerAvatar)
        {
            if (m_OwnedPlayerAvatar == clientPlayerAvatar)
            {
                RemoveHero();
            }
            else if (m_TrackedAllies.ContainsKey(clientPlayerAvatar.NetworkObjectId))
            {
                RemoveAlly(clientPlayerAvatar.NetworkObjectId);
                m_TrackedAllies.Remove(clientPlayerAvatar.NetworkObjectId);
            }
        }

        void SetHeroData(ClientPlayerAvatar clientPlayerAvatar)
        {
            m_OwnedServerCharacter = clientPlayerAvatar.GetComponent<ServerCharacter>();

            Assert.IsTrue(m_OwnedServerCharacter, "ServerCharacter component not found on ClientPlayerAvatar");

            m_OwnedPlayerAvatar = clientPlayerAvatar;

            // Hero is always our slot 0
            m_PartyIds[0] = m_OwnedServerCharacter.NetworkObject.NetworkObjectId;

            // set hero portrait
            if (m_OwnedServerCharacter.TryGetComponent(out NetworkAvatarGuidState avatarGuidState))
            {
                m_HeroPortrait.sprite = avatarGuidState.RegisteredAvatar.Portrait;
            }

            SetUIFromSlotData(0, m_OwnedServerCharacter);

            m_OwnedServerCharacter.NetHealthState.HitPoints.OnValueChanged += SetHeroHealth;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            m_OwnedServerCharacter.NetLifeState.IsGodMode.OnValueChanged += SetHeroGodModeStatus;
#endif

            // plus we track their target
            m_OwnedServerCharacter.TargetId.OnValueChanged += OnHeroSelectionChanged;

            m_ClientSender = m_OwnedServerCharacter.GetComponent<ClientInputSender>();
        }

        void SetHeroHealth(int previousValue, int newValue)
        {
            m_PartyHealthSliders[0].value = newValue;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void SetHeroGodModeStatus(bool previousValue, bool newValue)
        {
            m_PartyHealthGodModeImages[0].gameObject.SetActive(newValue);
        }
#endif

        /// <summary>
        /// Gets Player Name from the NetworkObjectId of his controlled Character.
        /// </summary>
        string GetPlayerName(Component component)
        {
            var networkName = component.GetComponent<NetworkNameState>();
            return networkName.Name.Value;
        }

        // set the class type for an ally - allies are tracked  by appearance so you must also provide appearance id
        void SetAllyData(ClientPlayerAvatar clientPlayerAvatar)
        {
            var networkCharacterStateExists =
                clientPlayerAvatar.TryGetComponent(out ServerCharacter serverCharacter);

            Assert.IsTrue(networkCharacterStateExists,
                "NetworkCharacterState component not found on ClientPlayerAvatar");

            ulong id = serverCharacter.NetworkObjectId;
            int slot = FindOrAddAlly(id);
            // do nothing if not in a slot
            if (slot == -1)
            {
                return;
            }

            SetUIFromSlotData(slot, serverCharacter);

            serverCharacter.NetHealthState.HitPoints.OnValueChanged += (int previousValue, int newValue) =>
            {
                SetAllyHealth(id, newValue);
            };

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            serverCharacter.NetLifeState.IsGodMode.OnValueChanged += (value, newValue) =>
            {
                SetAllyGodModeStatus(id, newValue);
            };
#endif

            m_TrackedAllies.Add(serverCharacter.NetworkObjectId, serverCharacter);
        }

        void SetUIFromSlotData(int slot, ServerCharacter serverCharacter)
        {
            m_PartyHealthSliders[slot].maxValue = serverCharacter.CharacterClass.BaseHP.Value;
            m_PartyHealthSliders[slot].value = serverCharacter.HitPoints;
            m_PartyNames[slot].text = GetPlayerName(serverCharacter);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            m_PartyHealthGodModeImages[slot].gameObject.SetActive(serverCharacter.NetLifeState.IsGodMode.Value);
#endif

            m_PartyClassSymbols[slot].sprite = serverCharacter.CharacterClass.ClassBannerLit;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void SetAllyGodModeStatus(ulong id, bool newValue)
        {
            int slot = FindOrAddAlly(id);
            // do nothing if not in a slot
            if (slot == -1)
            {
                return;
            }
            m_PartyHealthGodModeImages[slot].gameObject.SetActive(newValue);
        }
#endif

        void SetAllyHealth(ulong id, int hp)
        {
            int slot = FindOrAddAlly(id);
            // do nothing if not in a slot
            if (slot == -1)
            {
                return;
            }

            m_PartyHealthSliders[slot].value = hp;
        }

        private void OnHeroSelectionChanged(ulong prevTarget, ulong newTarget)
        {
            SetHeroSelectFX(m_CurrentTarget, false);
            SetHeroSelectFX(newTarget, true);
        }

        // Helper to change name appearance for selected or unselected party members
        // also updates m_CurrentTarget
        private void SetHeroSelectFX(ulong target, bool selected)
        {
            // check id against all party slots
            int slot = FindOrAddAlly(target, true);
            if (slot >= 0)
            {
                m_PartyNames[slot].color = selected ? Color.green : Color.white;
                if (selected)
                {
                    m_CurrentTarget = target;
                }
                else
                {
                    m_CurrentTarget = 0;
                }
            }
        }

        public void SelectPartyMember(int slot)
        {
            m_ClientSender.RequestAction(GameDataSource.Instance.GeneralTargetActionPrototype.ActionID,
                ClientInputSender.SkillTriggerStyle.UI, m_PartyIds[slot]);
        }

        // helper to initialize the Allies array - safe to call multiple times
        private void InitPartyArrays()
        {
            if (m_PartyIds == null)
            {
                // clear party ID array
                m_PartyIds = new ulong[m_PartyHealthSliders.Length];

                for (int i = 0; i < m_PartyHealthSliders.Length; i++)
                {
                    // initialize all IDs positions to 0 and HP to 1000 on sliders
                    m_PartyIds[i] = 0;
                    m_PartyHealthSliders[i].maxValue = 1000;
                }
            }
        }

        // Helper to find ally slots, returns -1 if no slot is found for the id
        // If a slot is available one will be added for this id unless dontAdd=true
        private int FindOrAddAlly(ulong id, bool dontAdd = false)
        {
            // make sure allies array is ready
            InitPartyArrays();

            int openslot = -1;
            for (int i = 0; i < m_PartyIds.Length; i++)
            {
                // if this ID is in the list, return the slot index
                if (m_PartyIds[i] == id) { return i; }
                // otherwise, record the first open slot (not slot 0 thats for the Hero)
                if (openslot == -1 && i > 0 && m_PartyIds[i] == 0)
                {
                    openslot = i;
                }
            }

            // if we don't add, we are done nw and didnt fint the ID
            if (dontAdd) { return -1; }

            // Party slot was not found for this ID - add one in the open slot
            if (openslot > 0)
            {
                // activeate the correct ally panel
                m_AllyPanel[openslot - 1].SetActive(true);
                // and save ally ID to party array
                m_PartyIds[openslot] = id;
                return openslot;
            }

            // this should not happen unless there are too many players - we didn't find the ally or a slot
            return -1;
        }

        void RemoveHero()
        {
            if (m_OwnedServerCharacter && m_OwnedServerCharacter.NetHealthState)
            {
                m_OwnedServerCharacter.NetHealthState.HitPoints.OnValueChanged -= SetHeroHealth;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                m_OwnedServerCharacter.NetLifeState.IsGodMode.OnValueChanged -= SetHeroGodModeStatus;
#endif
            }

            m_OwnedServerCharacter = null;
        }

        /// <summary>
        /// Remove an ally from the PartyHUD UI.
        /// </summary>
        /// <param name="id"> NetworkObjectID of the ally. </param>
        void RemoveAlly(ulong id)
        {
            for (int i = 0; i < m_PartyIds.Length; i++)
            {
                // if this ID is in the list, return the slot index
                if (m_PartyIds[i] == id)
                {
                    m_AllyPanel[i - 1].SetActive(false);
                    // and save ally ID to party array
                    m_PartyIds[i] = 0;
                    return;
                }
            }

            if (m_TrackedAllies.TryGetValue(id, out ServerCharacter serverCharacter))
            {
                serverCharacter.NetHealthState.HitPoints.OnValueChanged -= (int previousValue, int newValue) =>
                {
                    SetAllyHealth(id, newValue);
                };
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                serverCharacter.NetLifeState.IsGodMode.OnValueChanged -= (value, newValue) =>
                {
                    SetAllyGodModeStatus(id, value);
                };
#endif
            }
        }

        void OnDestroy()
        {
            m_PlayerAvatars.ItemAdded -= PlayerAvatarAdded;
            m_PlayerAvatars.ItemRemoved -= PlayerAvatarRemoved;

            RemoveHero();
            foreach (var kvp in m_TrackedAllies)
            {
                RemoveAlly(kvp.Key);
            }
        }
    }
}
