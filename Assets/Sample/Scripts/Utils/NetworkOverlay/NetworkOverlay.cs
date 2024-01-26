using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.BossRoom.Utils.Editor
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

        public void AddTextToUI(string gameObjectName, string defaultText, out TextMeshProUGUI textComponent)
        {
            var rootGO = new GameObject(gameObjectName);
            textComponent = rootGO.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = 28;
            textComponent.text = defaultText;
            textComponent.horizontalAlignment = HorizontalAlignmentOptions.Left;
            textComponent.verticalAlignment = VerticalAlignmentOptions.Middle;
            textComponent.raycastTarget = false;
            textComponent.autoSizeTextContainer = true;

            var rectTransform = rootGO.GetComponent<RectTransform>();
            AddToUI(rectTransform);
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
            displayTransform.localScale = Vector3.one;
        }

        void CreateDebugCanvas()
        {
            var canvas = Instantiate(m_DebugCanvasPrefab, transform);
            m_VerticalLayoutTransform = canvas.GetComponentInChildren<VerticalLayoutGroup>().transform;
        }
    }
}
