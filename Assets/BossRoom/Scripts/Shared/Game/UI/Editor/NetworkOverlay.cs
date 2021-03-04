#if DEBUG

using System;
using UnityEngine;
using UnityEngine.UI;

namespace BossRoom.Scripts.Editor
{
    public class NetworkOverlay : MonoBehaviour
    {
        public static NetworkOverlay Instance { get; private set; }

        [SerializeField]
        GameObject m_DebugCanvasPrefab;

        Transform m_VerticalLayoutTransform;

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }

        void Start()
        {
            var canvas = Instantiate(m_DebugCanvasPrefab, transform);
            m_VerticalLayoutTransform = canvas.GetComponentInChildren<VerticalLayoutGroup>().transform;
        }

        public void AddToUI(RectTransform displayTransform)
        {
            displayTransform.sizeDelta = new Vector2(100f, 24f);
            displayTransform.SetParent(m_VerticalLayoutTransform);
            displayTransform.SetAsFirstSibling();
        }
    }
}
#endif
