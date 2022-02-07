using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    /// <summary>
    /// Client specialization of the Character Select game state. Mainly controls the UI during character-select.
    /// </summary>
    [RequireComponent(typeof(CharSelectData))]
    public class ClientCharSelectState : GameStateBehaviour
    {
        /// <summary>
        /// Reference to the scene's state object so that UI can access state
        /// </summary>
        public static ClientCharSelectState Instance { get; private set; }

        public override GameState ActiveState { get { return GameState.CharSelect; } }
        public CharSelectData CharSelectData { get; private set; }

        [SerializeField]
        [Tooltip("This is triggered when the player chooses a character")]
        private string m_AnimationTriggerOnCharSelect = "BeginRevive";

        [SerializeField]
        [Tooltip("This is triggered when the player presses the \"Ready\" button")]
        private string m_AnimationTriggerOnCharChosen = "BeginRevive";

        [Header("Lobby Seats")]
        [SerializeField]
        [Tooltip("Collection of 8 portrait-boxes, one for each potential lobby member")]
        private List<UICharSelectPlayerSeat> m_PlayerSeats;

        [System.Serializable]
        public class ColorAndIndicator
        {
            public Sprite Indicator;
            public Color Color;
        }
        [Tooltip("Representational information for each player")]
        public ColorAndIndicator[] m_IdentifiersForEachPlayerNumber;

        [SerializeField]
        [Tooltip("Text element containing player count which updates as players connect")]
        private TextMeshProUGUI m_NumPlayersText;

        [SerializeField]
        [Tooltip("Text element for the Ready button")]
        private TextMeshProUGUI m_ReadyButtonText;

        [Header("UI Elements for different lobby modes")]
        [SerializeField]
        [Tooltip("UI elements to turn on when the player hasn't chosen their seat yet. Turned off otherwise!")]
        private List<GameObject> m_UIElementsForNoSeatChosen;

        [SerializeField]
        [Tooltip("UI elements to turn on when the player has locked in their seat choice (and is now waiting for other players to do the same). Turned off otherwise!")]
        private List<GameObject> m_UIElementsForSeatChosen;

        [SerializeField]
        [Tooltip("UI elements to turn on when the lobby is closed (and game is about to start). Turned off otherwise!")]
        private List<GameObject> m_UIElementsForLobbyEnding;

        [SerializeField]
        [Tooltip("UI elements to turn on when there's been a fatal error (and the client cannot proceed). Turned off otherwise!")]
        private List<GameObject> m_UIElementsForFatalError;

        [Header("Misc")]
        [SerializeField]
        [Tooltip("The controller for the class-info box")]
        private UICharSelectClassInfoBox m_ClassInfoBox;

        [SerializeField]
        Transform m_CharacterGraphicsParent;

        private int m_LastSeatSelected = -1;
        private bool m_HasLocalPlayerLockedIn = false;

        GameObject m_CurrentCharacterGraphics;

        Animator m_CurrentCharacterGraphicsAnimator;

        Dictionary<Guid, GameObject> m_SpawnedCharacterGraphics = new Dictionary<Guid, GameObject>();

        /// <summary>
        /// Conceptual modes or stages that the lobby can be in. We don't actually
        /// bother to keep track of what LobbyMode we're in at any given time; it's just
        /// an abstraction that makes it easier to configure which UI elements should
        /// be enabled/disabled in each stage of the lobby.
        /// </summary>
        private enum LobbyMode
        {
            ChooseSeat, // "Choose your seat!" stage
            SeatChosen, // "Waiting for other players!" stage
            LobbyEnding, // "Get ready! Game is starting!" stage
            FatalError, // "Fatal Error" stage
        }

        private Dictionary<LobbyMode, List<GameObject>> m_LobbyUIElementsByMode;

        private void Awake()
        {
            Instance = this;
            CharSelectData = GetComponent<CharSelectData>();
            m_LobbyUIElementsByMode = new Dictionary<LobbyMode, List<GameObject>>()
            {
                { LobbyMode.ChooseSeat, m_UIElementsForNoSeatChosen },
                { LobbyMode.SeatChosen, m_UIElementsForSeatChosen },
                { LobbyMode.LobbyEnding, m_UIElementsForLobbyEnding },
                { LobbyMode.FatalError, m_UIElementsForFatalError },
            };
        }

        protected override void Start()
        {
            base.Start();
            for (int i = 0; i < m_PlayerSeats.Count; ++i)
            {
                m_PlayerSeats[i].Initialize(i);
            }

            ConfigureUIForLobbyMode(LobbyMode.ChooseSeat);
            UpdateCharacterSelection(CharSelectData.SeatState.Inactive);
        }

        public override void OnNetworkDespawn()
        {
            if (CharSelectData)
            {
                CharSelectData.IsLobbyClosed.OnValueChanged -= OnLobbyClosedChanged;
                CharSelectData.LobbyPlayers.OnListChanged -= OnLobbyPlayerStateChanged;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (!IsClient)
            {
                enabled = false;
            }
            else
            {
                CharSelectData.IsLobbyClosed.OnValueChanged += OnLobbyClosedChanged;
                CharSelectData.LobbyPlayers.OnListChanged += OnLobbyPlayerStateChanged;
            }
        }

        /// <summary>
        /// Called when our PlayerNumber (e.g. P1, P2, etc.) has been assigned by the server
        /// </summary>
        /// <param name="playerNum"></param>
        private void OnAssignedPlayerNumber(int playerNum)
        {
            m_ClassInfoBox.OnSetPlayerNumber(playerNum);
        }

        private void UpdatePlayerCount()
        {
            int count = CharSelectData.LobbyPlayers.Count;
            var pstr = (count > 1) ? "players" : "player";
            m_NumPlayersText.text = "<b>" + count + "</b> " + pstr + " connected";
        }

        /// <summary>
        /// Called by the server when any of the seats in the lobby have changed. (Including ours!)
        /// </summary>
        private void OnLobbyPlayerStateChanged(NetworkListEvent<CharSelectData.LobbyPlayerState> changeEvent)
        {
            UpdateSeats();
            UpdatePlayerCount();

            // now let's find our local player in the list and update the character/info box appropriately
            int localPlayerIdx = -1;
            for (int i = 0; i < CharSelectData.LobbyPlayers.Count; ++i)
            {
                if (CharSelectData.LobbyPlayers[i].ClientId == NetworkManager.Singleton.LocalClientId)
                {
                    localPlayerIdx = i;
                    break;
                }
            }

            if (localPlayerIdx == -1)
            {
                // we aren't currently participating in the lobby!
                // this can happen for various reasons, such as the lobby being full and us not getting a seat.
                UpdateCharacterSelection(CharSelectData.SeatState.Inactive);
            }
            else if (CharSelectData.LobbyPlayers[localPlayerIdx].SeatState == CharSelectData.SeatState.Inactive)
            {
                // we haven't chosen a seat yet (or were kicked out of our seat by someone else)
                UpdateCharacterSelection(CharSelectData.SeatState.Inactive);
                // make sure our player num is properly set in Lobby UI
                OnAssignedPlayerNumber(CharSelectData.LobbyPlayers[localPlayerIdx].PlayerNum);
            }
            else
            {
                // we have a seat! Note that if our seat is LockedIn, this function will also switch the lobby mode
                UpdateCharacterSelection(CharSelectData.LobbyPlayers[localPlayerIdx].SeatState, CharSelectData.LobbyPlayers[localPlayerIdx].SeatIdx);
            }
        }

        /// <summary>
        /// Internal utility that sets the character-graphics and class-info box based on
        /// our chosen seat. It also triggers a LobbyMode change when it notices that our seat-state
        /// is LockedIn.
        /// </summary>
        /// <param name="state">Our current seat state</param>
        /// <param name="seatIdx">Which seat we're sitting in, or -1 if SeatState is Inactive</param>
        private void UpdateCharacterSelection(CharSelectData.SeatState state, int seatIdx = -1)
        {
            bool isNewSeat = m_LastSeatSelected != seatIdx;

            m_LastSeatSelected = seatIdx;
            if (state == CharSelectData.SeatState.Inactive)
            {
                if (m_CurrentCharacterGraphics)
                {
                    m_CurrentCharacterGraphics.SetActive(false);
                }

                m_ClassInfoBox.ConfigureForNoSelection();
            }
            else
            {
                if (seatIdx != -1)
                {
                    // change character preview when selecting a new seat
                    if (isNewSeat)
                    {
                        var selectedCharacterGraphics = GetCharacterGraphics(CharSelectData.AvatarConfiguration[seatIdx]);

                        if (m_CurrentCharacterGraphics)
                        {
                            m_CurrentCharacterGraphics.SetActive(false);
                        }

                        selectedCharacterGraphics.SetActive(true);
                        m_CurrentCharacterGraphics = selectedCharacterGraphics;
                        m_CurrentCharacterGraphicsAnimator = m_CurrentCharacterGraphics.GetComponent<Animator>();

                        m_ClassInfoBox.ConfigureForClass(CharSelectData.AvatarConfiguration[seatIdx].CharacterClass);
                    }
                }
                if (state == CharSelectData.SeatState.LockedIn && !m_HasLocalPlayerLockedIn)
                {
                    // the local player has locked in their seat choice! Rearrange the UI appropriately
                    // the character should act excited
                    m_CurrentCharacterGraphicsAnimator.SetTrigger(m_AnimationTriggerOnCharChosen);
                    ConfigureUIForLobbyMode(CharSelectData.IsLobbyClosed.Value ? LobbyMode.LobbyEnding : LobbyMode.SeatChosen);
                    m_HasLocalPlayerLockedIn = true;
                }
                else if (m_HasLocalPlayerLockedIn && state == CharSelectData.SeatState.Active)
                {
                    // reset character seats if locked in choice was unselected
                    if (m_HasLocalPlayerLockedIn)
                    {
                        ConfigureUIForLobbyMode(LobbyMode.ChooseSeat);
                        m_ClassInfoBox.SetLockedIn(false);
                        m_HasLocalPlayerLockedIn = false;
                    }
                }
                else if (state == CharSelectData.SeatState.Active && isNewSeat)
                {
                    m_CurrentCharacterGraphicsAnimator.SetTrigger(m_AnimationTriggerOnCharSelect);
                }
            }
        }

        /// <summary>
        /// Internal utility that sets the graphics for the eight lobby-seats (based on their current networked state)
        /// </summary>
        private void UpdateSeats()
        {
            // Players can hop between seats -- and can even SHARE seats -- while they're choosing a class.
            // Once they have chosen their class (by "locking in" their seat), other players in that seat are kicked out.
            // But until a seat is locked in, we need to display each seat as being used by the latest player to choose it.
            // So we go through all players and figure out who should visually be shown as sitting in that seat.
            CharSelectData.LobbyPlayerState[] curSeats = new CharSelectData.LobbyPlayerState[m_PlayerSeats.Count];
            foreach (CharSelectData.LobbyPlayerState playerState in CharSelectData.LobbyPlayers)
            {
                if (playerState.SeatIdx == -1 || playerState.SeatState == CharSelectData.SeatState.Inactive)
                    continue; // this player isn't seated at all!
                if (curSeats[playerState.SeatIdx].SeatState == CharSelectData.SeatState.Inactive
                    || (curSeats[playerState.SeatIdx].SeatState == CharSelectData.SeatState.Active && curSeats[playerState.SeatIdx].LastChangeTime < playerState.LastChangeTime))
                {
                    // this is the best candidate to be displayed in this seat (so far)
                    curSeats[playerState.SeatIdx] = playerState;
                }
            }

            // now actually update the seats in the UI
            for (int i = 0; i < m_PlayerSeats.Count; ++i)
            {
                m_PlayerSeats[i].SetState(curSeats[i].SeatState, curSeats[i].PlayerNum, curSeats[i].PlayerName);
            }
        }

        /// <summary>
        /// Called by the server when the lobby closes (because all players are seated and locked in)
        /// </summary>
        private void OnLobbyClosedChanged(bool wasLobbyClosed, bool isLobbyClosed)
        {
            if (isLobbyClosed)
            {
                ConfigureUIForLobbyMode(LobbyMode.LobbyEnding);
            }
        }

        /// <summary>
        /// Turns on the UI elements for a specified "lobby mode", and turns off UI elements for all other modes.
        /// It can also disable/enable the lobby seats and the "Ready" button if they are inappropriate for the
        /// given mode.
        /// </summary>
        private void ConfigureUIForLobbyMode(LobbyMode mode)
        {
            // first the easy bit: turn off all the inappropriate ui elements, and turn the appropriate ones on!
            foreach (var list in m_LobbyUIElementsByMode.Values)
            {
                foreach (var uiElement in list)
                {
                    uiElement.SetActive(false);
                }
            }

            foreach (var uiElement in m_LobbyUIElementsByMode[mode])
            {
                uiElement.SetActive(true);
            }

            // that finishes the easy bit. Next, each lobby mode might also need to configure the lobby seats and class-info box.
            bool isSeatsDisabledInThisMode = false;
            switch (mode)
            {
                case LobbyMode.ChooseSeat:
                    if (m_LastSeatSelected == -1)
                    {
                        if (m_CurrentCharacterGraphics)
                        {
                            m_CurrentCharacterGraphics.gameObject.SetActive(false);
                        }
                        m_ClassInfoBox.ConfigureForNoSelection();
                    }
                    m_ReadyButtonText.text = "READY!";
                    break;
                case LobbyMode.SeatChosen:
                    isSeatsDisabledInThisMode = true;
                    m_ClassInfoBox.SetLockedIn(true);
                    m_ReadyButtonText.text = "UNREADY";
                    break;
                case LobbyMode.FatalError:
                    isSeatsDisabledInThisMode = true;
                    m_ClassInfoBox.ConfigureForNoSelection();
                    break;
                case LobbyMode.LobbyEnding:
                    isSeatsDisabledInThisMode = true;
                    m_ClassInfoBox.ConfigureForNoSelection();
                    break;
            }

            // go through all our seats and enable or disable buttons
            foreach (var seat in m_PlayerSeats)
            {
                // disable interaction if seat is already locked or all seats disabled
                seat.SetDisableInteraction(seat.IsLocked() || isSeatsDisabledInThisMode);
            }

        }

        /// <summary>
        /// Called directly by UI elements!
        /// </summary>
        /// <param name="seatIdx"></param>
        public void OnPlayerClickedSeat(int seatIdx)
        {
            CharSelectData.ChangeSeatServerRpc(NetworkManager.Singleton.LocalClientId, seatIdx, false);
        }

        /// <summary>
        /// Called directly by UI elements!
        /// </summary>
        public void OnPlayerClickedReady()
        {
            // request to lock in or unlock if already locked in
            CharSelectData.ChangeSeatServerRpc(NetworkManager.Singleton.LocalClientId, m_LastSeatSelected, !m_HasLocalPlayerLockedIn);
        }

        GameObject GetCharacterGraphics(Avatar avatar)
        {
            if (!m_SpawnedCharacterGraphics.TryGetValue(avatar.Guid, out GameObject characterGraphics))
            {
                characterGraphics = Instantiate(avatar.GraphicsCharacterSelect, m_CharacterGraphicsParent);
                m_SpawnedCharacterGraphics.Add(avatar.Guid, characterGraphics);
            }

            return characterGraphics;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (gameObject.scene.rootCount > 1) // Hacky way for checking if this is a scene object or a prefab instance and not a prefab definition.
            {
                while (m_PlayerSeats.Count < CharSelectData.k_MaxLobbyPlayers)
                {
                    m_PlayerSeats.Add(null);
                }
            }
        }
#endif

    }
}
