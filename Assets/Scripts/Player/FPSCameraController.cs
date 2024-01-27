using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.PanicBuying.Character
{
    public class FPSCameraController : MonoBehaviour
    {
        public float sensX;
        public float sensY;

        public Transform orientation;
        public Transform body;

        float xRotation;
        float yRotation;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * sensX;
            float mouseY = Input.GetAxisRaw("Mouse Y") * sensY;

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0);
            body.localRotation = Quaternion.Euler(0, yRotation, 0);

            transform.position = orientation.position;
        }
    }
}
