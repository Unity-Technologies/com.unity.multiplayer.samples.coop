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
        private Image m_HeroPortrait;

        [SerializeField]
        private Image m_HeroClassSymbol;

        [SerializeField]
        private RectTransform m_HeroHealthbar;

        [SerializeField]
        Text m_HeroName;

        private int m_HeroMaxHealth;

        [SerializeField]
        private GameObject[] m_AllyPanel;

        [SerializeField]
        private Image[] m_AllyClassSymbol;

        [SerializeField]
        private RectTransform[] m_AllyHealthbar;

        [SerializeField]
        Text[] m_AllyNames;

        [SerializeField]
        private Sprite[] m_PortraitAppearances;

        // set sprites for classes - order must match class enum
        [SerializeField]
        private Sprite[] m_ClassSymbols;

        // track a list of allies
        private ulong[] m_Allies;
        // and their max HP
        private int[] m_AllyMaxHP;

        public void SetHeroAppearance(int appearance)
        {
            if (appearance > m_PortraitAppearances.Length)
            {
                return;
            }
            m_HeroPortrait.sprite = m_PortraitAppearances[appearance];
        }

        public void SetHeroType(CharacterTypeEnum characterType)
        {
            // treat character type as an index into our symbol array
            int symbol = (int)characterType;
            if (symbol > m_ClassSymbols.Length)
            {
                return;
            }

            m_HeroClassSymbol.sprite = m_ClassSymbols[symbol];
            m_HeroMaxHealth = GetMaxHPForClass(characterType);
        }

        public void SetHeroName(string name)
        {
            m_HeroName.text = name;
        }

        public void SetHeroHealth(int hp)
        {
            // TO DO - get real max hp
            m_HeroHealthbar.localScale = new Vector3(((float)hp) / (float)m_HeroMaxHealth, 1.0f, 1.0f);
        }

        private int GetMaxHPForClass(CharacterTypeEnum characterType)
        {
            // TO DO - make this come from character data
            if (characterType == CharacterTypeEnum.Archer || characterType == CharacterTypeEnum.Mage)
            {
                return 800;
            }
            else if (characterType == CharacterTypeEnum.Rogue)
            {
                return 900;
            }
            // otherwise including for (characterType == CharacterTypeEnum.Tank)
            // default to 1000
            return 1000;
        }

        // set the class type for an ally - allies are tracked  by appearance so you must also provide appearance id
        public void SetAllyType(ulong id, CharacterTypeEnum characterType)
        {
            int symbol = (int)characterType;
            if (symbol > m_ClassSymbols.Length)
            {
                return;
            }

            int slot = FindOrAddAlly(id);
            // do nothing if not in a slot
            if ( slot == -1 ) { return; }

            m_AllyClassSymbol[slot].sprite = m_ClassSymbols[symbol];
            m_AllyMaxHP[slot] = GetMaxHPForClass(characterType);
        }

        public void SetAllyHealth(ulong id, int hp)
        {
            int slot = FindOrAddAlly(id);
            // do nothing if not in a slot
            if (slot == -1) { return; }

            m_AllyHealthbar[slot].localScale = new Vector3(((float)hp) / (float)m_AllyMaxHP[slot], 1.0f, 1.0f);
        }

        public void SetAllyName(ulong id, string name)
        {
            int slot = FindOrAddAlly(id);
            // do nothing if not in a slot
            if (slot == -1) { return; }

            m_AllyNames[slot].text = name;
        }

        // helper to initialize the Allies array - safe to call multiple times
        private void InitAllies()
        {
            if (m_Allies == null)
            {
                // clear clicked state
                m_Allies = new ulong[m_AllyHealthbar.Length];

                // also setup the max HP array
                m_AllyMaxHP = new int[m_AllyHealthbar.Length];
                for (int i = 0; i < m_AllyHealthbar.Length; i++)
                {
                    // initialize all ally positions to 0 and HP to 1000
                    m_Allies[i] = 0;
                    m_AllyMaxHP[i] = 1000;
                }
            }
        }

        private int FindOrAddAlly(ulong id)
        {
            // make sure allies array is ready
            InitAllies();

            int openslot = -1;
            for (int i = 0; i < m_Allies.Length; i++)
            {
                // if this Ally is in the list, return the slot index
                if (m_Allies[i] == id) { return i; }
                // otherwise, record the first open slot
                if (openslot == -1 && m_Allies[i] == 0)
                {
                    openslot = i;
                }
            }

            // ally slot was not found - add one in an open slot
            if (openslot >= 0)
            {
                m_AllyPanel[openslot].SetActive(true);
                m_Allies[openslot] = id;
                return openslot;
            }

            // this should not happen unless wthere are too many players- we didnt find the ally or a slot
            return -1;
        }
    }
}
