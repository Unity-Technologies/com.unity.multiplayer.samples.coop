using System.Collections;
using System.Collections.Generic;
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
        private Sprite[] m_PortraitAppearances;

        // set sprites for classes - order must match class enum
        [SerializeField]
        private Sprite[] m_ClassSymbols;

        public void setHeroAppearance(int appearance)
        {
            if( appearance > m_PortraitAppearances.Length )
            {
                return;
            }
            m_HeroPortrait.sprite = m_PortraitAppearances[appearance];
        }

        public void setHeroType(CharacterTypeEnum characterType)
        {
            // treat character type as an index into our symbol array
            int symbol = (int)characterType;
            if (symbol  > m_ClassSymbols.Length)
            {
                return;
            }

            m_HeroClassSymbol.sprite = m_ClassSymbols[symbol];
        }

        public void setHeroHealth(int hp)
        {
            // TO DO - get real max hp
            m_HeroHealthbar.localScale = new Vector3(((float)hp) / 800.0f, 1.0f, 1.0f);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
