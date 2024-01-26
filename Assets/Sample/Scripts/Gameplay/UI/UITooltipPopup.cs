using System;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// This controls the tooltip popup -- the little text blurb that appears when you hover your mouse
    /// over an ability icon.
    /// </summary>
    public class UITooltipPopup : MonoBehaviour
    {
        [SerializeField]
        private Canvas m_Canvas;
        [SerializeField]
        [Tooltip("This transform is shown/hidden to show/hide the popup box")]
        private GameObject m_WindowRoot;
        [SerializeField]
        private TextMeshProUGUI m_TextField;
        [SerializeField]
        private Vector3 m_CursorOffset;

        private void Awake()
        {
            Assert.IsNotNull(m_Canvas);
        }

        /// <summary>
        /// Shows a tooltip at the given mouse coordinates.
        /// </summary>
        public void ShowTooltip(string text, Vector3 screenXy)
        {
            screenXy += m_CursorOffset;
            m_WindowRoot.transform.position = GetCanvasCoords(screenXy);
            m_TextField.text = text;
            m_WindowRoot.SetActive(true);
        }

        /// <summary>
        /// Hides the current tooltip.
        /// </summary>
        public void HideTooltip()
        {
            m_WindowRoot.SetActive(false);
        }

        /// <summary>
        /// Maps screen coordinates (e.g. Input.mousePosition) to coordinates on our Canvas.
        /// </summary>
        private Vector3 GetCanvasCoords(Vector3 screenCoords)
        {
            Vector2 canvasCoords;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_Canvas.transform as RectTransform,
                screenCoords,
                m_Canvas.worldCamera,
                out canvasCoords);
            return m_Canvas.transform.TransformPoint(canvasCoords);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (gameObject.scene.rootCount > 1) // Hacky way for checking if this is a scene object or a prefab instance and not a prefab definition.
            {
                if (!m_Canvas)
                {
                    // typically there's only one canvas in the scene, so pick that
                    m_Canvas = FindObjectOfType<Canvas>();
                }
            }
        }
#endif

    }
}
