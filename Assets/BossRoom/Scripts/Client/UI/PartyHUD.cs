using MLAPI.Spawning;
using UnityEngine;
using UnityEngine.UI;

namespace BossRoom.Visual
{
    /// <summary>
    /// Provides logic for the Party HUD with information on the player and allies
    /// Party HUD shows hero portrait and class info for all ally characters
    /// Party HUD also shows healthbars for each player allows clicks to select an ally
    /// </summary>
    public class PartyHUD : MonoBehaviour
    {
        [SerializeField]
        Image m_HeroPortrait;

        [SerializeField]
        GameObject[] m_AllyPanel;

        [SerializeField]
        Text[] m_PartyNames;

        [SerializeField]
        Image[] m_PartyClassSymbols;

        [SerializeField]
        Slider[] m_PartyHealthSliders;

        [SerializeField]
        Sprite[] m_PortraitAppearances;

        // set sprites for classes - order must match class enum
        [SerializeField]
        Sprite[] m_ClassSymbols;

        // track a list of hero (slot 0) + allies
        ulong[] m_PartyIds;

        // track Hero's target to show when it is the Hero or an ally
        ulong m_CurrentTarget;

        Client.ClientInputSender m_ClientSender;

        public void SetHeroData(NetworkCharacterState netState)
        {
            // Make sure arrays are initialized
            InitPartyArrays();
            // Hero is always our slot 0
            m_PartyIds[0] = netState.NetworkObject.NetworkObjectId;
            SetUIFromSlotData(0, netState);
            // Hero also gets a portrait
            int appearance = netState.CharacterAppearance.Value;
            if (appearance < m_PortraitAppearances.Length)
            {
                m_HeroPortrait.sprite = m_PortraitAppearances[appearance];
            }
            // plus we track their target
            netState.TargetId.OnValueChanged += OnHeroSelectionChanged;

            m_ClientSender = netState.GetComponent<Client.ClientInputSender>();
        }

        public void SetHeroHealth(int hp)
        {
            m_PartyHealthSliders[0].value = hp;
        }

        int GetMaxHPForClass(CharacterTypeEnum characterType)
        {
            return GameDataSource.Instance.CharacterDataByType[characterType].BaseHP.Value;
        }

        /// <summary>
        /// Gets Player Name from the NetworkObjectId of his controlled Character.
        /// </summary>
        string GetPlayerName(ulong netId)
        {
            var netState = NetworkSpawnManager.SpawnedObjects[netId].GetComponent<NetworkCharacterState>();
            return netState.Name;
        }

        // set the class type for an ally - allies are tracked  by appearance so you must also provide appearance id
        public void SetAllyData(NetworkCharacterState netState)
        {
            ulong id = netState.NetworkObjectId;
            int slot = FindOrAddAlly(id);
            // do nothing if not in a slot
            if (slot == -1) { return; }

            SetUIFromSlotData(slot, netState);
        }

        void SetUIFromSlotData(int slot, NetworkCharacterState netState)
        {
            m_PartyHealthSliders[slot].maxValue = GetMaxHPForClass(netState.CharacterType);
            m_PartyHealthSliders[slot].value = netState.HitPoints;
            m_PartyNames[slot].text = GetPlayerName(m_PartyIds[slot]);

            int symbol = (int)netState.CharacterType;
            if (symbol > m_ClassSymbols.Length)
            {
                return;
            }
            m_PartyClassSymbols[slot].sprite = m_ClassSymbols[symbol];
        }

        public void SetAllyHealth(ulong id, int hp)
        {
            int slot = FindOrAddAlly(id);
            // do nothing if not in a slot
            if (slot == -1) { return; }

            m_PartyHealthSliders[slot].value = hp;
        }

        public void SetAllyName(ulong id, string name)
        {
            int slot = FindOrAddAlly(id);
            // do nothing if not in a slot
            if (slot == -1) { return; }

            m_PartyNames[slot].text = name;
        }

        void OnHeroSelectionChanged(ulong prevTarget, ulong newTarget)
        {
            SetHeroSelectFX(m_CurrentTarget, false);
            SetHeroSelectFX(newTarget, true);
        }

        // Helper to change name appearance for selected or unselected party members
        // also updates m_CurrentTarget
        void SetHeroSelectFX(ulong target, bool selected)
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
            m_ClientSender.RequestAction(ActionType.GeneralTarget, Client.ClientInputSender.SkillTriggerStyle.UI, m_PartyIds[slot]);
        }

        // helper to initialize the Allies array - safe to call multiple times
        void InitPartyArrays()
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
        int FindOrAddAlly(ulong id, bool dontAdd = false)
        {
            // make sure allies array is ready
            InitPartyArrays();

            int openSlot = -1;
            for (int i = 0; i < m_PartyIds.Length; i++)
            {
                // if this ID is in the list, return the slot index
                if (m_PartyIds[i] == id) { return i; }
                // otherwise, record the first open slot (not slot 0, that is for the Hero)
                if (openSlot == -1 && i > 0 && m_PartyIds[i] == 0)
                {
                    openSlot = i;
                }
            }

            // if we don't add, we are done nw and didnt find the ID
            if (dontAdd) { return -1; }

            // Party slot was not found for this ID - add one in the open slot
            if (openSlot > 0)
            {
                // activate the correct ally panel
                m_AllyPanel[openSlot - 1].SetActive(true);
                // and save ally ID to party array
                m_PartyIds[openSlot] = id;
                return openSlot;
            }

            // this should not happen unless there are too many players - we didn't find the ally or a slot
            return -1;
        }
    }
}
