using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A Temporary animation script that rotates the image on the game
/// </summary>

namespace BossRoom.Visual
{
    [RequireComponent(typeof(Image))]
    public class ConnectionAnimation : MonoBehaviour
    {
        [SerializeField]
        private float m_RotationSpeed;

        public void Update()
        {
            gameObject.transform.Rotate(new Vector3(0, 0, m_RotationSpeed * Mathf.PI * Time.deltaTime));
        }
    }
}

