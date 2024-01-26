using System;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// A Temporary animation script that rotates the image on the game
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ConnectionAnimation : MonoBehaviour
    {
        [SerializeField]
        private float m_RotationSpeed;

        void Update()
        {
            transform.Rotate(new Vector3(0, 0, m_RotationSpeed * Mathf.PI * Time.deltaTime));
        }
    }
}

