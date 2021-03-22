using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace BossRoom.Visual
{
    [RequireComponent(typeof(Renderer))]
    public class ScrollingUVs : MonoBehaviour
    {
        [FormerlySerializedAs("ScrollX")]
        [SerializeField]
        float m_ScrollX = .01f;

        [FormerlySerializedAs("ScrollY")]
        [SerializeField]
        float m_ScrollY = .01f;

        float m_OffsetX;

        float m_OffsetY;

        Material m_Material;

        void Awake()
        {
            m_Material = GetComponent<Renderer>().material;
            Assert.IsNotNull(m_Material, "No Material found!");
        }

        void Update()
        {
            m_OffsetX = Time.time * m_ScrollX;
            m_OffsetY = Time.time * m_ScrollY;
            m_Material.mainTextureOffset = new Vector2(m_OffsetX, m_OffsetY);
        }
    }
}
