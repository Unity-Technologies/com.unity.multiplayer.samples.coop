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
        private GameObject[] m_AllyPanel;

        [SerializeField]
        private Image[] m_AllyClassSymbol;

        [SerializeField]
        private RectTransform[] m_AllyHealthbar;

        [SerializeField]
        private Sprite[] m_PortraitAppearances;

        // set sprites for classes - order must match class enum
        [SerializeField]
        private Sprite[] m_ClassSymbols;

        // track a list of allies
        private int[] m_Allies;

        void Start()
        {
            // clear clicked state
            m_Allies = new int[m_AllyHealthbar.Length];
            for (int i = 0; i < m_AllyHealthbar.Length; i++)
            {
                // initialize all ally positions to -1
                m_Allies[i] = -1;
            }
        }

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
        }

        public void SetHeroHealth(int hp)
        {
            // TO DO - get real max hp
            m_HeroHealthbar.localScale = new Vector3(((float)hp) / 800.0f, 1.0f, 1.0f);
        }

        // set the class type for an ally - allies are tracked  by appearance so you must also provide appearance id 
        public void SetAllyType(int appearance, CharacterTypeEnum characterType)
        {
            int symbol = (int)characterType;
            if (symbol > m_ClassSymbols.Length)
            {
                return;
            }

            int slot = FindOrAddAlly(appearance);
            m_AllyClassSymbol[slot].sprite = m_ClassSymbols[symbol];
        }

        public void SetAllyHealth(int appearance, int hp)
        {
            int slot = FindOrAddAlly(appearance);
            m_AllyHealthbar[slot].localScale = new Vector3(((float)hp) / 800.0f, 1.0f, 1.0f);
        }

        private int FindOrAddAlly(int appearance)
        {
            int slot = -1;
            for(int i =0; i < m_Allies.Length; i++)
            {
                if ( m_Allies[i] == appearance ) { return i; }
                if ( slot == -1 && m_Allies[i] == -1)
                {
                    slot = i;
                }
            }

            // ally slot was not found - add one in an open slot 
            if ( slot >= 0 )
            {
                m_AllyPanel[slot].SetActive(true);
                return slot;
            }

            // this should not happen - we didnt find the ally or a slot
            return -1;
        }
    }
}
