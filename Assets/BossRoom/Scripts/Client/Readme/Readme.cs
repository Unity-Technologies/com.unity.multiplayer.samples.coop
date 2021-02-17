using System;
using UnityEngine;

namespace BossRoom
{
    public class Readme : ScriptableObject
    {
        public Texture2D icon;
        public string title;
        public Section[] sections;
        public bool loadedLayout;

        [Serializable]
        public class Section
        {
            public string heading;
            public string text;
            public string linkText;
            public string url;
        }
    }
}
