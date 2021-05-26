using UnityEngine;

namespace BossRoom.Visual
{
    [RequireComponent(typeof(Renderer))]
    public class ScrollingUVs : MonoBehaviour
    {
        public float ScrollX = .01f;
        public float ScrollY = .01f;

        [SerializeField]
        Renderer m_Renderer;

        float m_OffsetX;
        float m_OffsetY;

        void Update()
        {
            m_OffsetX = Time.time * ScrollX;
            m_OffsetY = Time.time * ScrollY;
            m_Renderer.sharedMaterial.mainTextureOffset = new Vector2(m_OffsetX, m_OffsetY);
        }
    }
}
