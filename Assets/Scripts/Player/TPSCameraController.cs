using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.PanicBuying.Character
{
    public class TPSCameraController : MonoBehaviour
    {
        public Transform orientation;
        public Transform player;
        public Transform playerObj;

        public float rotationSpeed;
        public float moveSpeed;

        void Update()
        {
            Vector3 viewDir = player.position - new Vector3(transform.position.x, player.position.y, transform.position.z);
            orientation.forward = viewDir.normalized;

            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            Vector3 inputDir = orientation.forward * verticalInput + orientation.right * horizontalInput;

            if (inputDir != Vector3.zero)
            {
                playerObj.forward = Vector3.Slerp(playerObj.forward, inputDir.normalized, Time.deltaTime * rotationSpeed);
            }
        }
    }
}
