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

        float xRotation;
        float yRotation;

        private void Start()
        {
            
        }

        private void Update()
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
            float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
            orientation.rotation = Quaternion.Euler(0, yRotation, 0);

            transform.position = orientation.position;
        }
    }
}
