using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Unity.BossRoom.Gameplay.Configuration
{
    /// <summary>
    /// Data storage of all the valid strings used to create a player's name.
    /// Currently names are a two word combination in Adjective-Noun Combo (e.g. Happy Apple)
    /// </summary>
    [CreateAssetMenu(menuName = "GameData/NameGeneration", order = 2)]
    public class NameGenerationData : ScriptableObject
    {
        [Tooltip("The list of all possible strings the game can use as the first word of a player name")]
        public string[] FirstWordList;

        [Tooltip("The list of all possible strings the game can use as the second word in a player name")]
        public string[] SecondWordList;

        public string GenerateName()
        {
            var firstWord = FirstWordList[Random.Range(0, FirstWordList.Length - 1)];
            var secondWord = SecondWordList[Random.Range(0, SecondWordList.Length - 1)];

            return firstWord + " " + secondWord;
        }
    }
}
