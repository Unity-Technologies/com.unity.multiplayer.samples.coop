using UnityEngine;

namespace Unity.BossRoom.VisualEffects
{
    public class ScrollingMaterialUVs : MonoBehaviour
    {
        public float ScrollX = .01f;
        public float ScrollY = .01f;

        [SerializeField]
        Material m_Material;

        float m_OffsetX;
        float m_OffsetY;

        void Update()
        {
            m_OffsetX = Time.time * ScrollX;
            m_OffsetY = Time.time * ScrollY;
            m_Material.mainTextureOffset = new Vector2(m_OffsetX, m_OffsetY);
        }

        void OnDestroy()
        {
            ResetMaterialOffset();
        }

        void OnApplicationQuit()
        {
            ResetMaterialOffset();
        }

        void ResetMaterialOffset()
        {
            // reset UVs to avoid modifying the material file; this will be refactored
            m_Material.mainTextureOffset = new Vector2(0f, 0f);
        }
    }
}
