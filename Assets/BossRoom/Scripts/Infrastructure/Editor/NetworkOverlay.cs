using UnityEngine;
using UnityEngine.UI;

namespace Unity.Multiplayer.Samples.BossRoom.Editor
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

        public void AddToUI(RectTransform displayTransform)
        {
            if (m_VerticalLayoutTransform == null)
            {
                CreateDebugCanvas();
            }

            displayTransform.sizeDelta = new Vector2(100f, 24f);
            displayTransform.SetParent(m_VerticalLayoutTransform);
            displayTransform.SetAsFirstSibling();
        }

        void CreateDebugCanvas()
        {
            var canvas = Instantiate(m_DebugCanvasPrefab, transform);
            m_VerticalLayoutTransform = canvas.GetComponentInChildren<VerticalLayoutGroup>().transform;
        }
    }
}
